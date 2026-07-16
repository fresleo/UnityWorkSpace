Shader "XKnight/Glass/ScreenGlass"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A("基础控制", Float) = 1
        [Sub(A)][HDR]_GlassTint("基础染色颜色", Color) = (0.6, 0.7, 1.0, 1)
        [Sub(A)]_GlassTintStrength("颜色强度", Range(0, 1)) = 0
        [Sub(A)][HDR]_InnerGlowColor("叠加颜色", Color) = (0.15, 0.2, 0.6, 1)
        [Sub(A)]_InnerGlowIntensity("叠加颜色强度", Range(0, 5)) = 0
        [Sub(A)]_Chromatic("色散", Range(0, 2.0)) = 0
        [Sub(A)]_UVOffset("UV偏移", Vector) = (0, 0, 0, 0)
        [Sub(A)][Normal][NoScaleOffset]_NormalMap("法线贴图", 2D) = "bump" {}
        [Sub(A)]_NormalStrenght("法线强度", Range(0, 3)) = 0

        [Main(B, __, on, off)]
        _B("折射反射", Float) = 1
        [Sub(B)]_IOR("折射率", Range(1, 5)) = 1.52
        [SubToggle(B, _PHYSICAL_REFRACTION)] _EnablePhysicalRefraction("物理折射(强IOR)", Float) = 0
        [Sub(B)]_ReflectionIntensity("反射强度", Range(0, 5)) = 0.5
        [Sub(B)]_FresnelPower("菲涅尔强度", Range(0, 10)) = 5.0
        [Sub(B)]_CaptureMipmapLevel("屏幕截图MipMapLevel", Range(0, 8)) = 0
        [Sub(B)]_ReflectionRoughness("反射模糊", Range(0, 1)) = 0
        [Sub(B)]_ReflectionCubemap("反射 Cubemap", Cube) = "" {}

        [Main(C, __, on, off)]
        _C("边缘光", Float) = 1
        [Sub(C)][HDR]_RimColor("边缘光", Color) = (0.5, 0.7, 1.0, 1)
        [Sub(C)]_RimIntensity("边缘强度", Range(0, 5)) = 1.0
        [Sub(C)]_RimAngleMin("最小角度", Range(0, 90)) = 65
        [Sub(C)]_RimAngleMax("最大角度", Range(0, 90)) = 90
        
        [Main(D, _FACET_SPECULAR, off, on)]
        _EnableFacetSpecular("反射高光", Float) = 0
        [Sub(D)] _SpecularIntensity("高光强度", Range(0, 10)) = 2.0
        [Sub(D)] _SpecularPower("高光锐度", Range(1, 512)) = 64
        [Sub(D)][HDR] _SpecularColor("高光颜色", Color) = (1, 1, 1, 1)

        [Main(E, _PARALLAX_OCCLUSION_MAPPING, off, on)]
        _EnableParallax("焦散效果", Float) = 0
        [Sub(E)]_CausticTexture("焦散纹理", 2D) = "white" {}
        [Sub(E)]_CausticTiling("Caustic Tiling", Vector) = (1, 1, 0, 0)
        [Sub(E)]_ParallaxAmplitude("焦散幅度", Range(0, 100)) = 30
        [Space(10)]
        [Sub(E)]_CausticLayer2Strength("第二层焦散的强度", Range(0, 2)) = 0.8
        [Sub(E)]_CausticLayer2Scale("第二层 UV 缩放", Range(0.1, 5)) = 1.5
        [Sub(E)][HDR]_CausticEmissionColor("焦散发光颜色", Color) = (0.3, 0.5, 2.5, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "UniversalMaterialType" = "Lit"
            "Queue"="Transparent"
        }
        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            Blend Off
            ZTest LEqual
            ZWrite On
        
            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _FACET_SPECULAR
            #pragma shader_feature_local _PARALLAX_OCCLUSION_MAPPING
            #pragma shader_feature_local _PHYSICAL_REFRACTION
            
            
            #define _FOG_FRAGMENT 1
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv0 : TEXCOORD0;
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float4 texCoord0 : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float4 fogFactorAndVertexLight : TEXCOORD5;
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _GlassTint;
                float _GlassTintStrength;
                float4 _InnerGlowColor;
                float _InnerGlowIntensity;
                float _IOR;
                float _Chromatic;
                float _NormalStrenght;
                float _CaptureMipmapLevel;
                float _ReflectionRoughness;
                float _ReflectionIntensity;
                float _FresnelPower;
                float4 _RimColor;
                float _RimIntensity;
                float _RimAngleMin;
                float _RimAngleMax;
                float2 _CausticTiling;
                float _ParallaxAmplitude;
                float _CausticLayer2Strength;
                float _CausticLayer2Scale;
                float4 _CausticEmissionColor;
                float _SpecularIntensity;
                float _SpecularPower;
                float4 _SpecularColor;
                float4 _UVOffset;
            CBUFFER_END
            
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_CausticTexture);
            SAMPLER(sampler_CausticTexture);

            // 屏幕截图纹理
            TEXTURE2D(_ScreenCaptureTexture);
            SAMPLER(sampler_ScreenCaptureTexture);
            TEXTURECUBE(_ReflectionCubemap);
            SAMPLER(sampler_ReflectionCubemap);

            Varyings vert(Attributes input)
            {
                VertexPositionInputs vertexPos = GetVertexPositionInputs(input.positionOS);
                VertexNormalInputs vertexNormal = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                Varyings output;
                ZERO_INITIALIZE(Varyings, output);
                output.positionCS = vertexPos.positionCS;
                output.positionWS = vertexPos.positionWS;
                output.normalWS = vertexNormal.normalWS;
                output.tangentWS = float4(vertexNormal.tangentWS.xyz, input.tangentOS.w * GetOddNegativeScale());
                output.texCoord0 = input.uv0;
                output.screenPos = ComputeScreenPos(vertexPos.positionCS);
                
                half fogFactor = ComputeFogFactor(vertexPos.positionCS.z);
                output.fogFactorAndVertexLight = half4(fogFactor, 0, 0, 0);
                                
                #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                #endif
                
                return output;
            }
            
            // 简化折射 UV 计算：基于法线在屏幕空间的投影偏移（默认模式）
            // IOR 通过 (_IOR - 1.0) * 0.5 映射，比原来 (1-1/IOR)² 范围更大
            float2 GetRefractionUV(float3 N, float2 baseScreenUV, float ior) 
            { 
                float2 normalScreenOffset = TransformWorldToViewDir(N, true).xy;
                float refractionStrength = (ior - 1.0) * 0.5;
                return baseScreenUV + normalScreenOffset * refractionStrength;
            }
            
            // 物理折射 UV 计算：refract() + 方向投影差（不依赖世界坐标，适合近平面 mesh）
            float2 GetPhysicalRefractionUV(float3 viewDirWS, float3 worldNormal, float2 baseScreenUV, float eta)
            {
                // 入射方向（从相机到表面）
                float3 incidentDir = -viewDirWS;
                float3 N = normalize(worldNormal);
                
                // Snell 定律计算折射方向
                float3 refractedDir = refract(incidentDir, N, eta);
                
                // 全反射时 refract() 返回零向量，回退到无偏移
                if (dot(refractedDir, refractedDir) < 0.001)
                    return baseScreenUV;
                
                refractedDir = normalize(refractedDir);
                
                // 将入射和折射方向变换到视图空间
                float3 viewI = mul((float3x3)UNITY_MATRIX_V, incidentDir);
                float3 viewR = mul((float3x3)UNITY_MATRIX_V, refractedDir);
                
                // 除以 -z 投影到同一深度平面（视图空间 z 朝屏幕内为负）
                float2 projI = viewI.xy / max(0.01, -viewI.z);
                float2 projR = viewR.xy / max(0.01, -viewR.z);
                
                // 乘以投影矩阵的焦距因子（考虑 FOV 和宽高比），转换到 UV 空间
                float2 projScale = float2(UNITY_MATRIX_P[0][0], UNITY_MATRIX_P[1][1]) * 0.5;
                float2 uvOffset = (projR - projI) * projScale;
                
                return clamp(baseScreenUV + uvOffset, 0.001, 0.999);
            }
            
            
            float4 frag(Varyings input, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float2 uv = input.texCoord0.xy + _UVOffset;
                
                float3 normalValue = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
                float3 finalNormal = float3(normalValue.rg * _NormalStrenght, lerp(1, normalValue.b, saturate(_NormalStrenght)));
                
                float2 baseScreenUV = input.screenPos.xy / input.screenPos.w + _UVOffset;
                
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * (input.tangentWS.w * GetOddNegativeScale());
                float3x3 tangentToWorld = float3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                float3 finalNormalWS = NormalizeNormalPerPixel(TransformTangentToWorld(finalNormal, tangentToWorld));
                
                // 双面渲染：背面翻转法线
                float facingSign = IS_FRONT_VFACE(cullFace, 1.0, -1.0);
                finalNormalWS *= facingSign;
                
                float rawNdotV = dot(finalNormalWS, viewDirWS);
                float normalFixThreshold = 0.3;
                float fixBlend = saturate(rawNdotV / normalFixThreshold);
                finalNormalWS = normalize(lerp(viewDirWS, finalNormalWS, fixBlend));
                
                float NdotV = max(0.001, dot(finalNormalWS, viewDirWS));
                
                // --- 碎屏折射色散计算 ---
                float3 refractionColor = float3(0, 0, 0);
                #ifdef _PHYSICAL_REFRACTION
                    // 物理折射：refract() 方向投影差，IOR 影响更强
                    float etaG = 1.0 / _IOR;
                    float2 uvG = GetPhysicalRefractionUV(viewDirWS, finalNormalWS, baseScreenUV, etaG);
                        
                    // 色散：用不同 IOR 计算 R/B 通道
                    float etaR = 1.0 / max(0.01, _IOR - _Chromatic * 0.1);
                    float etaB = 1.0 / (_IOR + _Chromatic * 0.1);
                    float2 uvR = GetPhysicalRefractionUV(viewDirWS, finalNormalWS, baseScreenUV, etaR);
                    float2 uvB = GetPhysicalRefractionUV(viewDirWS, finalNormalWS, baseScreenUV, etaB);
                #else
                    // 简化折射：法线屏幕空间投影偏移
                    float2 uvG = GetRefractionUV(finalNormalWS, baseScreenUV, _IOR);
                        
                    float2 refrDir = uvG - baseScreenUV;
                    float2 uvR = uvG - refrDir * _Chromatic;  
                    float2 uvB = uvG + refrDir * _Chromatic; 
                #endif
                refractionColor.r = SAMPLE_TEXTURE2D_LOD(_ScreenCaptureTexture, sampler_ScreenCaptureTexture, uvR, _CaptureMipmapLevel).r;
                refractionColor.g = SAMPLE_TEXTURE2D_LOD(_ScreenCaptureTexture, sampler_ScreenCaptureTexture, uvG, _CaptureMipmapLevel).g;
                refractionColor.b = SAMPLE_TEXTURE2D_LOD(_ScreenCaptureTexture, sampler_ScreenCaptureTexture, uvB, _CaptureMipmapLevel).b;
                
                float3 tintedRefraction = refractionColor * lerp(float3(1,1,1), _GlassTint.rgb, _GlassTintStrength);
                
                float F0 = pow((1.0 - _IOR) / (1.0 + _IOR), 2);
                float F = lerp(F0, 1.0, pow(saturate(1.0 - NdotV), _FresnelPower));
                
                // 反射：采样指定的 Cubemap
                float3 reflectionDir = reflect(-viewDirWS, finalNormalWS);
                float mipLevel = _ReflectionRoughness * 6.0;
                float3 envRefl = SAMPLE_TEXTURECUBE_LOD(_ReflectionCubemap, sampler_ReflectionCubemap, reflectionDir, mipLevel).rgb * _ReflectionIntensity;
                
                float3 finalColor = tintedRefraction * (1.0 - F) + envRefl * F;
                
                // 叠加颜色
                finalColor += _InnerGlowColor.rgb * _InnerGlowIntensity * NdotV;
                
                float rimNdotV = max(0.1, rawNdotV); 
                float viewAngleDeg = degrees(acos(saturate(rimNdotV)));
                float rimFactor = smoothstep(_RimAngleMin, _RimAngleMax, viewAngleDeg);
                finalColor += rimFactor * _RimColor.rgb * _RimIntensity;
                
                #ifdef _FACET_SPECULAR
                    Light mainLight = GetMainLight();
                    float3 halfDir = normalize(mainLight.direction + viewDirWS);
                    float NdotH = max(0.0, dot(finalNormalWS, halfDir));
                    float NdotL = max(0.0, dot(finalNormalWS, mainLight.direction));
                    float specHighlight = pow(NdotH, _SpecularPower);
                    finalColor += specHighlight * NdotL * mainLight.color 
                                  * _SpecularColor.rgb * _SpecularIntensity;
                #endif
                
                #ifdef _PARALLAX_OCCLUSION_MAPPING
                    float causticAmplitude = _ParallaxAmplitude * 0.05;
                    
                    float2 causticUV1 = uv * _CausticTiling + normalValue.rg * causticAmplitude;
                    float caustic1 = SAMPLE_TEXTURE2D(_CausticTexture, sampler_CausticTexture, causticUV1).r;
                    
                    float2 causticUV2 = uv * _CausticTiling * _CausticLayer2Scale - normalValue.rg * causticAmplitude * 0.6;
                    float caustic2 = SAMPLE_TEXTURE2D(_CausticTexture, sampler_CausticTexture, causticUV2).r;
                    
                    float causticIntensity = max(caustic1, caustic2 * _CausticLayer2Strength);
                                        
                    finalColor += causticIntensity * _CausticEmissionColor.rgb;
                #endif
                
                return float4(finalColor, 1.0f);
            }
            
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Off
            ZTest LEqual
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct AttributesDepth
            {
                float3 positionOS : POSITION;
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };
            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                #if UNITY_ANY_INSTANCING_ENABLED
                    uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
            };
            VaryingsDepth vertDepth(AttributesDepth input)
            {
                VaryingsDepth output;
                VertexPositionInputs vertexPos = GetVertexPositionInputs(input.positionOS);
                output.positionCS = vertexPos.positionCS;
                #if UNITY_ANY_INSTANCING_ENABLED
                    output.instanceID = input.instanceID;
                #endif
                return output;
            }
            float4 fragDepth(VaryingsDepth input) : SV_Target0
            {
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "LWGUI.LWGUI"
}
