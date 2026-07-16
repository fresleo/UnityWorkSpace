Shader "Xknight/Scene/FakeGlass"
{
    Properties
    {
        [Enum(InteriorMapping,0,ParallaxMapping,1)] _MappingType ("Mapping Type", Float) = 0

        _BaseColor ("Base Color", Color) = (1,1,1,0.5)
        _BaseMap ("Base Map", 2D) = "white" {}

        _RoomTex ("Room Tex", 2D) = "white" {}
        _RoomTintColor ("Room Tint Color", Color) = (1,1,1,1)
        
        _Rooms ("Room Atlas Rows&Cols (XY)", Vector) = (1,1,0,0)
        _RoomDepth ("Room Depth", Range(0.001,0.999)) = 0.5
        
        _ParallaxDepth ("Parallax Depth", Range(0, 0.2)) = 0.05

        [Toggle(_FROST_BLEND_ON)] _FrostBlendOn ("Enable Frost Blend", Float) = 1
        [NoScaleOffset]_FrostedRoomTex ("Frosted Room Tex (Blurred)", 2D) = "white" {}
        _FrostMask ("Frost Mask", 2D) = "white" {}
        _FrostStrength ("Frost Strength", Range(0,2)) = 1

        [Toggle(_NORMAL_MAP_ON)] _NormalMapOn ("Enable Normal Map", Float) = 1
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0,2)) = 1
        
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionStrength ("Emission Strength", Range(0,10)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma multi_compile_fragment _ _RECEIVE_SHADOWS_OFF
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma shader_feature_local_fragment _ _FROST_BLEND_ON
            #pragma shader_feature_local_fragment _ _NORMAL_MAP_ON
            #pragma shader_feature_local_fragment _ _MAPPING_PARALLAX

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                float3 viewDirTS  : TEXCOORD4; 
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_RoomTex);
            SAMPLER(sampler_RoomTex);

            TEXTURE2D(_FrostedRoomTex);
            SAMPLER(sampler_FrostedRoomTex);

            TEXTURE2D(_FrostMask);
            SAMPLER(sampler_FrostMask);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _RoomTex_ST;
                float4 _NormalMap_ST;

                float2 _Rooms;
                float  _RoomDepth;
                float  _ParallaxDepth;

                float4 _BaseColor;
                float4 _RoomTintColor;
                float  _Metallic;
                float  _Smoothness;
                float  _NormalScale;

                float  _FrostStrength;
                float4 _EmissionColor;
                float  _EmissionStrength;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;

                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = posInputs.positionCS;
                o.uv = v.uv;

                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);

                float3 tWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                float tangentSign = v.tangentOS.w * unity_WorldTransformParams.w;
                float3 bWS = cross(o.normalWS, tWS) * tangentSign;
                o.tangentWS = float4(normalize(tWS), v.tangentOS.w);

                float3 viewDirWS = _WorldSpaceCameraPos.xyz - o.positionWS;
                o.viewDirTS = float3(
                    dot(viewDirWS, tWS),
                    dot(viewDirWS, bWS),
                    dot(viewDirWS, o.normalWS)
                );

                return o;
            }

            // float2 rand2(float co)
            // {
            //     return frac(sin(co * float2(12.9898,78.233)) * 43758.5453);
            // }

            float2 ParallaxMapping(float2 uv, float3 viewDirTS)
            {
                float z = max(abs(viewDirTS.z), 1e-4);
                float2 parallaxOffset = viewDirTS.xy * _ParallaxDepth / z;
                return uv - parallaxOffset;
            }

            float2 ComputeInteriorAtlasUV(float2 uvRoom, float3 viewDirTS)
            {
                float2 roomUV = frac(uvRoom);
                float2 roomIndexUV = floor(uvRoom);

                // Randomize room selection within atlas
                //float2 n = floor(rand2(roomIndexUV.x + roomIndexUV.y * (roomIndexUV.x + 1)) * _Rooms.xy);
                //roomIndexUV += n;

                float farFrac = _RoomDepth;
                float depthScale = 1.0 / (1.0 - farFrac) - 1.0;

                float3 pos = float3(roomUV * 2 - 1, -1);

                float3 rayDirTS = -viewDirTS;

                rayDirTS *= float3(_RoomTex_ST.x, _RoomTex_ST.y, _RoomTex_ST.x);
                rayDirTS.z *= -depthScale;

                float3 invDir = 1.0 / rayDirTS;
                float3 kRay = abs(invDir) - pos * invDir;
                float  kMin = min(min(kRay.x, kRay.y), kRay.z);
                pos += kMin * rayDirTS;

                float interp = pos.z * 0.5 + 0.5;
                float realZ = saturate(interp) / depthScale + 1;
                interp = 1.0 - (1.0 / realZ);
                interp *= depthScale + 1.0;

                float2 interiorUV = pos.xy * lerp(1.0, farFrac, interp);
                interiorUV = interiorUV * 0.5 + 0.5;

                return (roomIndexUV + interiorUV.xy) / _Rooms;
            }

            float3 ComputePBRLighting(float3 N, float3 V, float3 baseTint, float metallic, float smoothness, float3 ambient, Light mainLight, float3 emission)
            {
                const float kPi = 3.14159265359;

                metallic = saturate(metallic);
                smoothness = saturate(smoothness);
                
                float3 f0 = lerp(float3(0.04, 0.04, 0.04), baseTint, metallic);
                float3 diffuseColor = baseTint * (1.0 - metallic);
                
                float3 L = SafeNormalize(mainLight.direction);
                float3 H = SafeNormalize(V + L);
                float NoV = saturate(dot(N, V));
                float NoL = saturate(dot(N, L));
                float NoH = saturate(dot(N, H));
                float VoH = saturate(dot(V, H));

                float roughness = 1.0 - smoothness;
                float perceptualRoughness = roughness * roughness;
                float a2 = perceptualRoughness * perceptualRoughness;
                a2 = max(a2, 1e-8);

                // GGX Distribution (D)
                float d = NoH * NoH * (a2 - 1.0) + 1.0;
                float D = a2 / max(kPi * d * d, 1e-7);

                // Schlick-GGX Geometry (G)
                // Avoid 0/0 when perceptualRoughness==0 and NoV/NoL==0.
                float kGGX = max(perceptualRoughness / 2.0, 1e-4);
                float G_V = NoV / max(NoV * (1.0 - kGGX) + kGGX, 1e-4);
                float G_L = NoL / max(NoL * (1.0 - kGGX) + kGGX, 1e-4);
                float G = G_V * G_L;

                // Fresnel (F)
                float3 F = f0 + (1.0 - f0) * pow(1.0 - VoH, 5.0);

                // Cook-Torrance BRDF
                float3 specular = (D * G * F) / max(4.0 * NoV * NoL, 0.001);
                float3 kD = (1.0 - F) * (1.0 - metallic);

                // Direct lighting (with shadows)
                float3 directDiffuse = kD * diffuseColor / kPi;
                float3 directLight = (directDiffuse + specular) * mainLight.color * NoL * mainLight.shadowAttenuation;

                // Indirect lighting (ambient + IBL)
                float3 indirectDiffuse = ambient * diffuseColor;

                // IBL specular with roughness-based mip selection
                float mipLevel = saturate(perceptualRoughness) * 6.0;
                float3 R = reflect(-V, N);
                float4 encodedIBL = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, R, mipLevel);
                float3 iblSpecular = DecodeHDREnvironment(encodedIBL, unity_SpecCube0_HDR);

                // Environment BRDF approximation (Fresnel for IBL)
                float surfaceReduction = 1.0 / (roughness * roughness + 1.0);
                float grazingTerm = saturate(smoothness + metallic);
                float fresnelTerm = pow(1.0 - NoV, 5.0);
                float3 indirectSpecular = iblSpecular * (F * surfaceReduction + (grazingTerm - F * grazingTerm) * fresnelTerm);

                float3 lit = directLight + indirectDiffuse + indirectSpecular + emission;
                return lit;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uvBase = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                half4  baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvBase);
                half3  baseTint = _BaseColor.rgb * baseTex.rgb;
                half   alpha    = _BaseColor.a * baseTex.a;

                float3 N = normalize(i.normalWS);
                #if defined(_NORMAL_MAP_ON)
                    float2 uvN = i.uv * _NormalMap_ST.xy + _NormalMap_ST.zw;
                    float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvN), _NormalScale);
                    float3 tWS = normalize(i.tangentWS.xyz);
                    float3 bWS = normalize(cross(i.normalWS, tWS)) * i.tangentWS.w;
                    N = normalize(tWS * nTS.x + bWS * nTS.y + i.normalWS * nTS.z);
                #endif
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                float2 uvRoom = i.uv * _RoomTex_ST.xy + _RoomTex_ST.zw;

                #if defined(_MAPPING_PARALLAX)
                    float3 viewDirTS = normalize(i.viewDirTS);
                    float2 mapUV = ParallaxMapping(uvRoom, viewDirTS);

                    half3 backgroundRGB = SAMPLE_TEXTURE2D(_RoomTex, sampler_RoomTex, mapUV).rgb;
                #else
                    float2 mapUV = ComputeInteriorAtlasUV(uvRoom, i.viewDirTS);

                    half3 backgroundRGB = SAMPLE_TEXTURE2D(_RoomTex, sampler_RoomTex, mapUV).rgb;
                #endif

                half smoothness = _Smoothness;

                #if defined(_FROST_BLEND_ON)
                    half3 blurRGB = SAMPLE_TEXTURE2D(_FrostedRoomTex, sampler_FrostedRoomTex, mapUV).rgb;
                    half  mask = SAMPLE_TEXTURE2D(_FrostMask, sampler_FrostMask, uvBase).r;
                    half  frostK = saturate(mask * _FrostStrength);
                    backgroundRGB = lerp(backgroundRGB, blurRGB, frostK);
                    smoothness = min(smoothness, 1 - frostK);
                 #endif

                backgroundRGB *= _RoomTintColor.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half3 ambient = SampleSH(N);

                half3 emission = _EmissionColor.rgb * _EmissionStrength;

                half3 glassColor = ComputePBRLighting(N, V, baseTint, _Metallic, smoothness, ambient, mainLight, emission);

                half3 color = lerp(backgroundRGB, glassColor, alpha);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    CustomEditor "XKnight.Glass.Editor.FakeGlassShaderGUI"
}


