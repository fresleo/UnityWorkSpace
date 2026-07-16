Shader "XKnight/Scene/TerrainPBR"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A ("遮罩控制图 (RGBA)", Float) = 1
        
        [Sub(A)] _Control ("遮罩控制图 (RGBA)", 2D) = "red" {}
        
        [Space(10)]
        [Main(B, __, on, off)]
        _B ("层1", Float) = 1
        
        [Sub(B)] _Splat0 ("笔刷 - rgb:颜色, a:光滑度", 2D) = "grey" {}
        [Sub(B)] _Normal0 ("法线+遮罩 - rg:法线, b:AO, a:金属性", 2D) = "black" {}
        [Sub(B)] _NormalScale0 ("法线比例", Range(0, 10)) = 1
        [Sub(B)] _SpecularScaleBRDF0 ("镜面高光比例（BRDF）", Range(0, 1)) = 1
        [Sub(B)] _GIIndirectDiffuseBoost0 ("GI 间接光比例（漫反射）", Range(-3, 3)) = 1
        [Sub(B)] _BakedGITintIntensity0 ("TOD GI 调色强度", Range(0, 1)) = 1
        
        [Space(10)]
        [Main(C, __, on, off)]
        _C ("层2", Float) = 1
        
        [Sub(C)] _Splat1 ("笔刷 - rgb:颜色, a:光滑度", 2D) = "grey" {}
        [Sub(C)] _Normal1 ("法线+遮罩 - rg:法线, b:AO, a:金属性", 2D) = "black" {}
        [Sub(C)] _NormalScale1 ("法线比例", Range(0, 10)) = 1
        [Sub(C)] _SpecularScaleBRDF1 ("镜面高光比例（BRDF）", Range(0, 1)) = 1
        [Sub(C)] _GIIndirectDiffuseBoost1 ("GI 间接光比例（漫反射）", Range(-3, 3)) = 1
        [Sub(C)] _BakedGITintIntensity1 ("TOD GI 调色强度", Range(0, 1)) = 1
        
        [Space(10)]
        [Main(D, __, on, off)]
        _D ("层3", Float) = 1
        
        [Sub(D)] _Splat2 ("笔刷 - rgb:颜色, a:光滑度", 2D) = "grey" {}
        [Sub(D)] _Normal2 ("法线+遮罩 - rg:法线, b:AO, a:金属性", 2D) = "black" {}
        [Sub(D)] _NormalScale2 ("法线比例", Range(0, 10)) = 1
        [Sub(D)] _SpecularScaleBRDF2 ("镜面高光比例（BRDF）", Range(0, 1)) = 1
        [Sub(D)] _GIIndirectDiffuseBoost2 ("GI 间接光比例（漫反射）", Range(-3, 3)) = 1
        [Sub(D)] _BakedGITintIntensity2 ("TOD GI 调色强度", Range(0, 1)) = 1
        
        [Space(10)]
        [Main(E, __, on, off)]
        _E ("层4", Float) = 1
        
        [Sub(E)] _Splat3 ("笔刷 - rgb:颜色, a:光滑度", 2D) = "grey" {}
        [Sub(E)] _Normal3 ("法线+遮罩 - rg:法线, b:AO, a:金属性", 2D) = "black" {}
        [Sub(E)] _NormalScale3 ("法线比例", Range(0, 10)) = 1
        [Sub(E)] _SpecularScaleBRDF3 ("镜面高光比例（BRDF）", Range(0, 1)) = 1
        [Sub(E)] _GIIndirectDiffuseBoost3 ("GI 间接光比例（漫反射）", Range(-3, 3)) = 1
        [Sub(E)] _BakedGITintIntensity3 ("TOD GI 调色强度", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry-1500"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            // float4 _Control_ST, _Control_TexelSize;
            float4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
            
            half _NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3;
            half _SpecularScaleBRDF0, _SpecularScaleBRDF1, _SpecularScaleBRDF2, _SpecularScaleBRDF3;
            half _GIIndirectDiffuseBoost0, _GIIndirectDiffuseBoost1, _GIIndirectDiffuseBoost2, _GIIndirectDiffuseBoost3;
            half _BakedGITintIntensity0, _BakedGITintIntensity1, _BakedGITintIntensity2, _BakedGITintIntensity3;
        CBUFFER_END

        half4 _BakedGITint; // TOD GI 调色

        TEXTURE2D(_Control);	SAMPLER(sampler_Control);

        TEXTURE2D(_Splat0);     SAMPLER(sampler_Splat0);
        TEXTURE2D(_Splat1);
        TEXTURE2D(_Splat2);
        TEXTURE2D(_Splat3);

        TEXTURE2D(_Normal0);    SAMPLER(sampler_Normal0);
        TEXTURE2D(_Normal1);
        TEXTURE2D(_Normal2);
        TEXTURE2D(_Normal3);

        half3 SplatmapMix(half4 splatControl, half4 uvPack0, half4 uvPack1, half2 uvPack2
            , out half smoothness)
        {
            half4 lay0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvPack0.zw);
            half4 lay1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvPack1.xy);
            half4 lay2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvPack1.zw);;
            half4 lay3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvPack2.xy);
            
            half4 result = lay0 * splatControl.r + lay1 * splatControl.g + lay2 * splatControl.b + lay3 * splatControl.a;
            smoothness = 1.0 - result.a;
            
            return result.rgb;
        }

        half3 UnpackNormalRG(half2 packNormal, half scale = 1.0)
        {
            half3 normal;
            normal.xy = packNormal * 2.0 - 1.0;
            normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));

            normal.xy *= scale;
            return normalize(normal);
        }

        void SampleNormals(half4 splatControl, half4 uvPack0, half4 uvPack1, half2 uvPack2
            , inout half3 mixedNormal, out half ao, out half metallic)
        {
            half4 normal0 = SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uvPack0.zw);
            half4 normal1 = SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, uvPack1.xy);
            half4 normal2 = SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, uvPack1.zw);
            half4 normal3 = SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, uvPack2.xy);

            half3 realNormal0 = UnpackNormalRG(normal0.rg);
            half3 realNormal1 = UnpackNormalRG(normal1.rg);
            half3 realNormal2 = UnpackNormalRG(normal2.rg);
            half3 realNormal3 = UnpackNormalRG(normal3.rg);

            mixedNormal = SafeNormalize(realNormal0 * splatControl.r + realNormal1 * splatControl.g + realNormal2 * splatControl.b + realNormal3 * splatControl.a);
            ao = normal0.b * splatControl.r + normal1.b * splatControl.g + normal2.b * splatControl.b + normal3.b * splatControl.a;
            metallic = normal0.a * splatControl.r + normal1.a * splatControl.g + normal2.a * splatControl.b + normal3.a * splatControl.a;
        }

        half ControlMixValue(half4 splatControl, half val0, half val1, half val2, half val3)
        {
            half result = splatControl.r * val0 + splatControl.g * val1 + splatControl.b * val2 + splatControl.a * val3;
            return result;
        }
        ENDHLSL
        
        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SplatmapVert
            #pragma fragment SplatmapFragment
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // #pragma multi_compile _ _GLOBAL_RAIN_ON

            // -------------------------------------
            // Material Keywords

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "../ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

            struct Attributes
            {
                float4 positionOS           : POSITION;
                float3 normalOS             : NORMAL;
                float4 tangentOS            : TANGENT;
                
                float2 texcoord             : TEXCOORD0;
                float2 staticLightmapUV     : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pack0        : TEXCOORD0; // xy: _Control uv  zw: _Splat0 uv
                float4 pack1        : TEXCOORD1; // xy: _Splat1 uv   zw: _Splat2 uv
                float2 pack2        : TEXCOORD2; // xy: _Splat3 uv
                
                float3 positionWS   : TEXCOORD3;
                float3 normalWS     : TEXCOORD4;
                float4 tangentWS    : TEXCOORD5;
                float3 viewDirWS    : TEXCOORD6;
                
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord  : TEXCOORD7;
                #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);

                UBPA_FOG_COORDS(9)

                float4 positionCS   : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings SplatmapVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
                
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
                #endif
                
                output.pack0.xy = input.texcoord;
                output.pack0.zw = TRANSFORM_TEX(input.texcoord, _Splat0);
                output.pack1.xy = TRANSFORM_TEX(input.texcoord, _Splat1);
                output.pack1.zw = TRANSFORM_TEX(input.texcoord, _Splat2);
                output.pack2.xy = TRANSFORM_TEX(input.texcoord, _Splat3);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
                
                UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

                return output;
            }
            
            void InitializeInputData(Varyings input, half3 normalTS, half GIIndirectDiffuseBoost, half BakedGITintIntensity
                , out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = input.positionWS;

                float sgn = input.tangentWS.w; // should be either +1 or -1
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

                inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

                inputData.viewDirectionWS = SafeNormalize(input.viewDirWS);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                half3 bakedGITint = lerp(half3(1, 1, 1), _BakedGITint.rgb, BakedGITintIntensity);
                // GI = lightmap * GI贡献比例 * 全局调色参数
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS) * GIIndirectDiffuseBoost * bakedGITint;
                
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            }

            void InitializeSurfaceData(half3 albedo, half metallic, half smoothness, half occlusion
                , out SurfaceData surfaceData)
            {
                surfaceData = (SurfaceData)0;

                surfaceData.albedo = albedo;
                surfaceData.metallic = metallic;
                surfaceData.occlusion = occlusion;
                surfaceData.smoothness = smoothness;
            }

            half4 SplatmapFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.pack0.xy);
                half smoothness;
                half3 albedo = SplatmapMix(splatControl, input.pack0, input.pack1, input.pack2, smoothness);

                half3 normalTS = half3(0.0h, 0.0h, 1.0h);
                half ao, metallic;
                SampleNormals(splatControl, input.pack0, input.pack1, input.pack2, normalTS, ao, metallic);

                half mixSpecularScaleBRDF = ControlMixValue(splatControl, _SpecularScaleBRDF0, _SpecularScaleBRDF1, _SpecularScaleBRDF2, _SpecularScaleBRDF3);
                half mixGIIndirectDiffuseBoost = ControlMixValue(splatControl, _GIIndirectDiffuseBoost0, _GIIndirectDiffuseBoost1, _GIIndirectDiffuseBoost2, _GIIndirectDiffuseBoost3);
                half mixBakedGITintIntensity = ControlMixValue(splatControl, _BakedGITintIntensity0, _BakedGITintIntensity1, _BakedGITintIntensity2, _BakedGITintIntensity3);
                
                InputData inputData;
                InitializeInputData(input, normalTS, mixGIIndirectDiffuseBoost, mixBakedGITintIntensity
                    , inputData);

                SurfaceData surfaceData;
                InitializeSurfaceData(albedo, metallic, smoothness, ao
                    , surfaceData);

                ExtendData extendData = (ExtendData)0;
                extendData.specularScaleBRDF = mixSpecularScaleBRDF;
                
                half3 color = FragmentPBR(inputData, surfaceData, extendData).rgb;

                UBPA_APPLY_FOG(input, color);

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma vertex VertexMeta
            #pragma fragment FragmentMeta

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 pack0 : TEXCOORD2; // xy: _Control uv  zw: _Splat0 uv
                float4 pack1 : TEXCOORD3; // xy: _Splat1 uv   zw: _Splat2 uv
                float2 pack2 : TEXCOORD4; // xy: _Splat3 uv
                float4 positionCS : SV_POSITION;
            };

            Varyings VertexMeta(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = MetaVertexPosition(
                    input.positionOS, input.uv1, input.uv2,
                    unity_LightmapST, unity_DynamicLightmapST);

                output.pack0.xy = input.uv0;
                output.pack0.zw = TRANSFORM_TEX(input.uv0, _Splat0);
                output.pack1.xy = TRANSFORM_TEX(input.uv0, _Splat1);
                output.pack1.zw = TRANSFORM_TEX(input.uv0, _Splat2);
                output.pack2.xy = TRANSFORM_TEX(input.uv0, _Splat3);

                return output;
            }

            half4 FragmentMeta(Varyings input) : SV_Target
            {
                half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.pack0.xy);
                half smoothness;
                half3 albedo = SplatmapMix(splatControl, input.pack0, input.pack1, input.pack2, smoothness);

                half3 normalTS = half3(0.0h, 0.0h, 1.0h);
                half ao, metallic;
                SampleNormals(splatControl, input.pack0, input.pack1, input.pack2, normalTS, ao, metallic);

                half alpha = 1.0;
                BRDFData brdfData;
                InitializeBRDFData(albedo, metallic, half3(0.0, 0.0, 0.0), smoothness, alpha, brdfData);

                MetaInput metaInput;
                metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
                metaInput.Emission = half3(0.0, 0.0, 0.0);
                return UnityMetaFragment(metaInput);
            }
            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #define NOT_REQUIRES_TERRAIN_INSTANCING

            #include "./TerrainPbrShadowCaster.hlsl"
            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define NOT_REQUIRES_TERRAIN_INSTANCING
            
            #include "./TerrainPbrDepthOnly.hlsl"
            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalOnlyVertex
            #pragma fragment DepthNormalOnlyFragment
            
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            struct AttributesDepthNormal
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                
                float2 texcoord     : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDepthNormal
            {
                float4 positionCS       : SV_POSITION;
                
                float4 uvMainAndLM      : TEXCOORD0; // xy: control, zw: lightmap
                float4 uvSplat01        : TEXCOORD1; // xy: splat0, zw: splat1
                float4 uvSplat23        : TEXCOORD2; // xy: splat2, zw: splat3

                float3 positionWS       : TEXCOORD3;
                float3 normalWS         : TEXCOORD4;
                float4 tangentWS        : TEXCOORD5;
                float3 viewDirWS        : TEXCOORD6;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            VaryingsDepthNormal DepthNormalOnlyVertex(AttributesDepthNormal input)
            {
                VaryingsDepthNormal output = (VaryingsDepthNormal)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uvMainAndLM.xy = input.texcoord;
                output.uvMainAndLM.zw = input.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;
                
                output.uvSplat01.xy = TRANSFORM_TEX(input.texcoord, _Splat0);
                output.uvSplat01.zw = TRANSFORM_TEX(input.texcoord, _Splat1);
                output.uvSplat23.xy = TRANSFORM_TEX(input.texcoord, _Splat2);
                output.uvSplat23.zw = TRANSFORM_TEX(input.texcoord, _Splat3);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

                return output;
            }

            void SampleNormals(half4 splatControl, half4 uvSplat01, half4 uvSplat23
                , inout half3 mixedNormal, out half ao, out half metallic)
            {
                half4 normal0 = SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uvSplat01.xy);
                half4 normal1 = SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, uvSplat01.zw);
                half4 normal2 = SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, uvSplat23.xy);
                half4 normal3 = SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, uvSplat23.zw);

                float3 realNormal0 = UnpackNormalRG(normal0.rg);
                float3 realNormal1 = UnpackNormalRG(normal1.rg);
                float3 realNormal2 = UnpackNormalRG(normal2.rg);
                float3 realNormal3 = UnpackNormalRG(normal3.rg);

                mixedNormal = SafeNormalize(realNormal0 * splatControl.r + realNormal1 * splatControl.g + realNormal2 * splatControl.b + realNormal3 * splatControl.a);
                ao = normal0.b * splatControl.r + normal1.b * splatControl.g + normal2.b * splatControl.b + normal3.b * splatControl.a;
                metallic = normal0.a * splatControl.r + normal1.a * splatControl.g + normal2.a * splatControl.b + normal3.a * splatControl.a;
            }

            void DepthNormalOnlyFragment(
                VaryingsDepthNormal input
                , out half4 outNormalWS : SV_Target0
                )
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.uvMainAndLM.xy);

                half3 normalTS = half3(0.0h, 0.0h, 1.0h);
                half ao, metallic;
                SampleNormals(splatControl, input.uvSplat01, input.uvSplat23, normalTS, ao, metallic);

                float sgn = input.tangentWS.w; // should be either +1 or -1
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                
                half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
                normalWS = NormalizeNormalPerPixel(normalWS);
                
                outNormalWS = half4(normalWS, 0.0);
            }
            ENDHLSL
        }

        // DepthMask
        Pass
        {
            Name "DepthMask"
            Tags
            {
                "LightMode" = "DepthMask"
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthMaskVertex
            #pragma fragment DepthMaskFragment
            
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define NOT_REQUIRES_TERRAIN_INSTANCING
            
            #include "./TerrainPbrDepthMask.hlsl"
            ENDHLSL
        }

        // ViewSpaceNormals
        Pass
        {
            Name "ViewSpaceNormals"
            Tags
            {
                "LightMode" = "ViewSpaceNormals"
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ViewSpaceNormalsVertex
            #pragma fragment ViewSpaceNormalsFragment
            
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #define NOT_REQUIRES_TERRAIN_INSTANCING
            
            #include "./TerrainPbrViewSpaceNormals.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}