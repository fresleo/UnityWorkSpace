Shader "XKnight/Decal/Blood Decal (RendererFeature)"
{
    Properties
    {
        // 渲染模式 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(RenderMode, __, off, off)]
        _RenderMode ("渲染模式", float) = 1

        [SubEnum(RenderMode, UnityEngine.Rendering.BlendMode)] _SrcBlend ("混合模式 - Src", float) = 5 // 5 = SrcAlpha
        [SubEnum(RenderMode, UnityEngine.Rendering.BlendMode)] _DstBlend ("混合模式 - Dst", float) = 10 // 10 = OneMinusSrcAlpha

        // 主要设置 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(Main, __, on, off)]
        _Main ("主要设置", float) = 1

        [SubToggle(Main, _USE_PACKED_TEXTURE_MDOE)] _UsePackedTextureMode ("遮罩+法线 用1张图", float) = 0

        [Sub(Main)] _MainMaskMap ("主遮罩纹理 - r:遮罩，gb:1张图模式时是法线，a:Alpha", 2D) = "white" {}
        [Sub(Main)] _MainNormalMap ("主法线纹理", 2D) = "bump" {}

        [Sub(Main)] _OverallColorTint ("整体色调 - a没用", Color) = ( 1, 0, 0, 1 )
        [Sub(Main)] _OverallOpacity ("整体不透明度", Range(0, 1)) = 1
        [Sub(Main)] _OverallNormalIntensity ("整体法线强度", Range(0, 4)) = 1

        [Sub(Main)] _Gloss ("光泽度", Range(0, 1)) = 0.85
        [Sub(Main)] _Metallic ("金属性", Range(0, 1)) = 0.12

        // 干涸的血液 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(DriedBlood, _DRIEDBLOOD_ON, off, on)]
        _DriedBlood ("干涸血液 - 开关", float) = 0

        [Sub(DriedBlood)] _DriedBlood_EffectLevel ("干涸血液 - 效果级别", Range(0, 1)) = 0
        [Sub(DriedBlood)] _DriedBlood_Blend ("干涸血液 - 混合图", 2D) = "white" {}
        [Sub(DriedBlood)] _DriedBlood_Normal ("干涸血液 - 混合法线", 2D) = "bump" {}
        [Sub(DriedBlood)] _DriedBlood_ColorGloss ("干涸血液 - rgb=颜色，a=光泽度", Color) = ( 0.16, 0.08, 0.05, 0 )
        [Sub(DriedBlood)] _DriedBlood_EffectTiling ("干涸血液 - 混合图的 Tiling", Range(0, 1000)) = 430
        [Sub(DriedBlood)] _DriedBlood_Metallic ("干涸血液 - 金属性", Range(0, 1)) = 0.8

        [Sub(DriedBlood)] _NoiseMap ("噪声纹理", 2D) = "white" {}
        [Sub(DriedBlood)] _Noise1 ("噪声系数1", Range(0, 50)) = 25
        [Sub(DriedBlood)] _Noise2 ("噪声系数2", Range(0, 25)) = 5
        [Sub(DriedBlood)] _Bias ("避免彼得潘效应 - 偏差", Range(0, 1)) = 0.3
        [Sub(DriedBlood)] _Scale ("避免彼得潘效应 - 比例", Range(0, 1)) = 0.7

        // 不常修改的附加设置 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(Additive, __, off, off)]
        _Additive ("附加设置", float) = 1

        [Sub(Additive)] _StencilRef ("模版引用值", Float) = 0
        [SubEnum(Additive, UnityEngine.Rendering.CompareFunction)] _StencilComp ("模版比较方式 - 如果要按特定值屏蔽，请设置为 NotEqual，否则设置为 Disable", float) = 0

        // 一些配合 UnityEditor.Rendering.Universal.DecalShaderGraphGUI 工作的变量
        [HideInInspector] _DrawOrder ("绘制顺序", Range(-50, 50)) = 0
        [HideInInspector] _DecalAngleFadeSupported ("支持贴花角度淡入淡出", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane"
            "DisableBatching"="LODFading"
        }

        HLSLINCLUDE
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x

        #pragma target 3.0
        #pragma editor_sync_compilation

        // GPU Instancing
        #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        ENDHLSL

        Pass
        {
            Name "DecalScreenSpaceProjector"
            Tags
            {
                "LightMode"="DecalScreenSpaceProjector"
            }

            Blend [_SrcBlend] [_DstBlend]
            Cull Front
            ZTest Greater
            ZWrite Off

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // -------------------------------------
            // Pipeline keywords
            //#pragma shader_feature _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma shader_feature _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma shader_feature_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma shader_feature _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature _ _ADDITIONAL_LIGHTS

            //#pragma shader_feature _ _FORWARD_PLUS

            #pragma shader_feature _DECAL_NORMAL_BLEND_LOW _DECAL_NORMAL_BLEND_MEDIUM _DECAL_NORMAL_BLEND_HIGH
            //#pragma shader_feature _ _DECAL_LAYERS
            
            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _USE_PACKED_TEXTURE_MDOE
            #pragma shader_feature_local _ _DRIEDBLOOD_ON

            // 宏开关 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            #define _MATERIAL_AFFECTS_ALBEDO 1
            #define _MATERIAL_AFFECTS_NORMAL 1
            #define _MATERIAL_AFFECTS_NORMAL_BLEND 1
            #define _MATERIAL_AFFECTS_MAOS 1
            #define DECAL_ANGLE_FADE 1

            // 贴花投影模式宏
            #define DECAL_PROJECTOR
            #define DECAL_SCREEN_SPACE

            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../ShaderLibrary/Lighting.hlsl"

            // Decal 要用到的数据结构
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"

            #include "../ShaderLibrary/ASENoise.hlsl"

            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

            struct SurfaceDescription
            {
                float3 BaseColor;
                float Alpha;
                float3 NormalTS;
                float NormalAlpha;
                float Metallic;
                float Occlusion;
                float Smoothness;
                float MAOSAlpha;
                float3 Emission;
            };

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;

                float2 staticLightmapUV : TEXCOORD0;
                float2 dynamicLightmapUV : TEXCOORD1;
                float3 sh : TEXCOORD2;

                float3 normalWS : TEXCOORD3;
                float3 viewDirectionWS : TEXCOORD4;
                float4 fogFactorAndVertexLight : TEXCOORD5;

                float3 positionWS: TEXCOORD6;
                UBPA_FOG_COORDS(7)

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainMaskMap_ST, _MainNormalMap_ST, _NoiseMap_ST;

                float4 _OverallColorTint;
                float _OverallOpacity;
                float _OverallNormalIntensity;

                float _Gloss, _Metallic;
                float _Noise1, _Noise2, _Bias, _Scale;

                float4 _DriedBlood_ColorGloss;
                float _DriedBlood_Metallic;
                float _DriedBlood_EffectLevel;
                float _DriedBlood_EffectTiling;

                #if defined( DECAL_ANGLE_FADE )
                float _DecalAngleFadeSupported;
                #endif
            CBUFFER_END

            TEXTURE2D(_MainMaskMap);
            SAMPLER(sampler_MainMaskMap);
            TEXTURE2D(_MainNormalMap);
            SAMPLER(sampler_MainNormalMap);

            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            TEXTURE2D(_DriedBlood_Blend);
            SAMPLER(sampler_DriedBlood_Blend);
            TEXTURE2D(_DriedBlood_Normal);
            SAMPLER(sampler_DriedBlood_Normal);

            // 是重建法线，还是加载法线
            #if ((!defined( _MATERIAL_AFFECTS_NORMAL ) && defined( _MATERIAL_AFFECTS_ALBEDO )) || (defined( _MATERIAL_AFFECTS_NORMAL ) && defined( _MATERIAL_AFFECTS_NORMAL_BLEND ))) && (defined( DECAL_SCREEN_SPACE ))
            #define DECAL_RECONSTRUCT_NORMAL
            #elif defined( DECAL_ANGLE_FADE )
				#define DECAL_LOAD_NORMAL
            #endif

            #if defined( _DECAL_LAYERS )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
            #endif

            #if defined( DECAL_LOAD_NORMAL )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #endif

            #if defined( DECAL_PROJECTOR ) || defined( DECAL_RECONSTRUCT_NORMAL )
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            #ifdef DECAL_RECONSTRUCT_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
            #endif

            PackedVaryings Vert(Attributes inputMesh)
            {
                PackedVaryings packedOutput = (PackedVaryings)0;

                UNITY_SETUP_INSTANCE_ID(inputMesh);
                UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(packedOutput);

                inputMesh.tangentOS = float4(1, 0, 0, -1);
                inputMesh.normalOS = float3(0, 1, 0);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);

                packedOutput.positionCS = vertexInput.positionCS;

                float3 positionWS = vertexInput.positionWS;
                float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);

                packedOutput.normalWS.xyz = normalWS;
                packedOutput.viewDirectionWS.xyz = GetWorldSpaceViewDir(positionWS);

                // 用自己的雾，所以这里不用给值
                half fogFactor = 0;
                half3 vertexLight = VertexLighting(positionWS, normalWS);
                packedOutput.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

                #if defined( LIGHTMAP_ON )
					OUTPUT_LIGHTMAP_UV(inputMesh.uv1, unity_LightmapST, packedOutput.staticLightmapUV);
                #endif

                #if defined( DYNAMICLIGHTMAP_ON )
					packedOutput.dynamicLightmapUV.xy = inputMesh.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif

                #if !defined( LIGHTMAP_ON )
                packedOutput.sh.xyz = float3(SampleSHVertex(half3(normalWS)));
                #endif

                packedOutput.positionWS = vertexInput.positionWS;
                UBPA_TRANSFER_FOG(packedOutput, vertexInput.positionWS);

                return packedOutput;
            }

            float4 BlendOperations(float4 blendOpSrc, float4 blendOpDest)
            {
                float4 blendOp = saturate((blendOpDest > 0.5) ? (1.0 - 2.0 * (1.0 - blendOpDest) * (1.0 - blendOpSrc)) : (2.0 * blendOpDest * blendOpSrc));
                return blendOp;
            }

            float3 ReconstructedNormal(float2 normalXY)
            {
                float3 normal;
                // 将纹理值从 [0, 1] 映射到 [-1, 1]
                normal.xy = normalXY.xy * 2.0f - 1.0f;
                // 计算Z分量
                normal.z = max(1.0e-16, sqrt(1.0f - saturate(dot(normal.xy, normal.xy))));

                return normal;
            }

            void SurfaceDescriptionFunction(float2 texCoord0, inout SurfaceDescription surfaceDescription)
            {
                // 采样主遮罩纹理
                float2 uv_MainMaskMap = texCoord0 * _MainMaskMap_ST.xy + _MainMaskMap_ST.zw;
                float4 mainMaskMapT2d = SAMPLE_TEXTURE2D(_MainMaskMap, sampler_MainMaskMap, uv_MainMaskMap);
                float4 mainMaskCol = mainMaskMapT2d.r * float4(_OverallColorTint.rgb, _OverallOpacity);

                // 主法线
                #if defined( _USE_PACKED_TEXTURE_MDOE )
					float3 mainNormal = ReconstructedNormal(mainMaskMapT2d.gb);
                #else
                float2 uv_MainNormalMap = texCoord0 * _MainNormalMap_ST.xy + _MainNormalMap_ST.zw;
                float4 mainNormalMapT2d = SAMPLE_TEXTURE2D(_MainNormalMap, sampler_MainNormalMap, uv_MainNormalMap);
                float3 mainNormal = UnpackNormalScale(mainNormalMapT2d, 1.0f);
                #endif

                // 混合干涸的血液纹理
                #if defined( _DRIEDBLOOD_ON )
					// 噪声
					float2 temp_cast_0 = _Noise1.xx;
					float2 texCoord_n1 = texCoord0 * temp_cast_0;
					float simplePerlin_n1 = snoise( texCoord_n1 );
					
					float2 temp_cast_1 = ( _Noise1 * _Noise2 ).xx;
					float2 texCoord_n2 = texCoord0 * temp_cast_1;
					float simplePerlin_n2 = snoise( texCoord_n2 );

					float2 uv_NoiseMap = texCoord0 * _NoiseMap_ST.xy + _NoiseMap_ST.zw;
					float4 noiseMapT2d = SAMPLE_TEXTURE2D( _NoiseMap, sampler_NoiseMap, uv_NoiseMap );

					// 为了减少失真，避免彼得潘效应
					float4 constantBiasScale = abs( ( 1.0 - ( simplePerlin_n1 * simplePerlin_n2 * noiseMapT2d )  + _Bias ) * _Scale );
					float4 temp_power = ( 3.0 * _Bias ).xxxx;

					// 噪声强度
					float3 noiseIntensity = pow( constantBiasScale, temp_power ).rgb;

					// 反照率混合
					// blendOp left
					float temp_output_3_0_g2 = 1.0 - _DriedBlood_EffectLevel;
					float3 appendResult7_g2 = float3(temp_output_3_0_g2 , temp_output_3_0_g2 , temp_output_3_0_g2);
					float3 temp_output_67_0 = noiseIntensity * _DriedBlood_EffectLevel + appendResult7_g2;
					float3 clampResult59 = clamp( temp_output_67_0 , float3( 0,0,0 ) , float3( 1,0,0 ) );
					float4 blendOpSrc24 = float4( clampResult59 , 0.0 );
					float4 blendOpLerpLeft = BlendOperations(blendOpSrc24, mainMaskCol);

					// blendOp right
					float2 driedBloodUV = texCoord0 * _DriedBlood_EffectTiling.xx + float2( 0,0 );
					float4 driedBloodBlendT2d = SAMPLE_TEXTURE2D( _DriedBlood_Blend, sampler_DriedBlood_Blend, driedBloodUV );
					float4 blendOpLerpRight = BlendOperations(_DriedBlood_ColorGloss, driedBloodBlendT2d);

					// 反照率
					float3 temp_output_60_0 = clampResult59 * _DriedBlood_EffectLevel;
					float4 lerpResult34 = lerp( blendOpLerpLeft , blendOpLerpRight , float4( temp_output_60_0 , 0.0 ));
					float4 Out_Albedo = lerpResult34;

					// 不透明度
					float3 clampResult25 = clamp( temp_output_67_0 * abs( mainMaskMapT2d.a ) , float3( 0,0,0 ) , float3( 1,0,0 ) );
					float Out_Opacity = clampResult25.r * _OverallOpacity;

					// 干涸血液法线
					float3 driedBloodNormal = UnpackNormalScale( SAMPLE_TEXTURE2D( _DriedBlood_Normal, sampler_DriedBlood_Normal, driedBloodUV ), 1.0f );
					float3 lerpMainNormal = lerp( mainNormal,  driedBloodNormal, _DriedBlood_EffectLevel );

					float Out_Metalness = lerp( _Metallic, _DriedBlood_Metallic, temp_output_60_0.x );
					float Out_Smoothness = lerp( _Gloss, _DriedBlood_ColorGloss.a, temp_output_60_0.x );
                #else // _DRIEDBLOOD_ON
                // 反照率
                float4 Out_Albedo = mainMaskCol;

                // 不透明度
                float Out_Opacity = mainMaskMapT2d.a * _OverallOpacity;

                // 干涸血液法线
                float3 lerpMainNormal = mainNormal;

                float Out_Metalness = _Metallic;
                float Out_Smoothness = _Gloss;
                #endif // _DRIEDBLOOD_ON

                float3 Out_Normal = lerp(float3(0, 0, 1), lerpMainNormal, _OverallNormalIntensity);

                surfaceDescription.BaseColor = Out_Albedo.rgb;
                surfaceDescription.Alpha = Out_Opacity;
                surfaceDescription.NormalTS = Out_Normal;
                surfaceDescription.NormalAlpha = Out_Opacity;
                #if defined( _MATERIAL_AFFECTS_MAOS )
                surfaceDescription.Metallic = Out_Metalness;
                surfaceDescription.Occlusion = 1;
                surfaceDescription.Smoothness = Out_Smoothness;
                surfaceDescription.MAOSAlpha = 0.5;
                #endif
            }

            void GetSurfaceData(SurfaceDescription surfaceDescription, float angleFadeFactor, inout DecalSurfaceData surfaceData)
            {
                half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                half fadeFactor = clamp(normalToWorld[0][3], 0.0f, 1.0f) * angleFadeFactor;

                surfaceData.occlusion = 1.0;
                surfaceData.smoothness = 0;

                surfaceData.baseColor.xyz = half3(surfaceDescription.BaseColor);
                surfaceData.baseColor.w = half(surfaceDescription.Alpha * fadeFactor);

                // 会影响材质的 法线
                #if defined( _MATERIAL_AFFECTS_NORMAL )
                surfaceData.normalWS.w = half(1.0);
                surfaceData.normalWS.xyz = mul((half3x3)normalToWorld, surfaceDescription.NormalTS.xyz);
                #else
            		surfaceData.normalWS.w = half(0.0);
            		surfaceData.normalWS.xyz = normalToWorld[2].xyz;
                #endif

                surfaceData.normalWS.w = surfaceDescription.NormalAlpha * fadeFactor;

                // 会影响材质的 MAOS - 屏幕空间模式下没用
                #if defined( _MATERIAL_AFFECTS_MAOS )
                surfaceData.metallic = half(surfaceDescription.Metallic);
                surfaceData.occlusion = half(surfaceDescription.Occlusion);
                surfaceData.smoothness = half(surfaceDescription.Smoothness);
                surfaceData.MAOSAlpha = half(surfaceDescription.MAOSAlpha * fadeFactor);
                #endif
            }

            void InitializeInputData(PackedVaryings input, float3 positionWS, half3 normalWS, half3 viewDirectionWS, inout InputData inputData)
            {
                inputData.positionWS = positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirectionWS;

                inputData.shadowCoord = float4(0, 0, 0, 0);

                inputData.fogCoord = half(input.fogFactorAndVertexLight.x);
                inputData.vertexLighting = half3(input.fogFactorAndVertexLight.yzw);

                #if defined( DYNAMICLIGHTMAP_ON )
					inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, half3(input.sh), normalWS);
                #else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, half3(input.sh), normalWS);
                #endif

                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                #if defined( DEBUG_DISPLAY )
                #if defined( DYNAMICLIGHTMAP_ON )
						inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
                #endif

                #if defined( LIGHTMAP_ON )
						inputData.staticLightmapUV = input.staticLightmapUV;
                #else
						inputData.vertexSH = input.sh;
                #endif
                #endif

                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            }

            void GetSurface(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
            {
                surfaceData.albedo = decalSurfaceData.baseColor.rgb;
                surfaceData.metallic = saturate(decalSurfaceData.metallic);
                surfaceData.specular = 0;
                surfaceData.smoothness = saturate(decalSurfaceData.smoothness);
                surfaceData.occlusion = decalSurfaceData.occlusion;
                surfaceData.emission = decalSurfaceData.emissive;
                surfaceData.alpha = saturate(decalSurfaceData.baseColor.w);
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 1;
            }

            void Frag(PackedVaryings packedInput, out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
                UNITY_SETUP_INSTANCE_ID(packedInput);

                half angleFadeFactor = 1.0;

                // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
                #if defined( DECAL_SCREEN_SPACE )
                TransformScreenUV(packedInput.positionCS.xy, _ScreenSize.y);
                #endif

                #if defined( _DECAL_LAYERS )
					uint surfaceRenderingLayer = LoadSceneRenderingLayer(packedInput.positionCS.xy);
            		
            		uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
            		clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
                #endif

                #if UNITY_REVERSED_Z
                float depth = LoadSceneDepth(packedInput.positionCS.xy);
                #else
					float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
                #endif

                // 法线
                #if defined( DECAL_RECONSTRUCT_NORMAL )
                #if defined( _DECAL_NORMAL_BLEND_HIGH )
						half3 normalWS = half3(ReconstructNormalTap9(packedInput.positionCS.xy));
                #elif defined( _DECAL_NORMAL_BLEND_MEDIUM )
						half3 normalWS = half3(ReconstructNormalTap5(packedInput.positionCS.xy));
                #else
                half3 normalWS = half3(ReconstructNormalDerivative(packedInput.positionCS.xy));
                #endif
                #elif defined( DECAL_LOAD_NORMAL )
					half3 normalWS = half3(LoadSceneNormals(packedInput.positionCS.xy));
                #endif

                float2 positionSS = packedInput.positionCS.xy * _ScreenSize.zw;

                float3 positionWS = ComputeWorldSpacePosition(positionSS, depth, UNITY_MATRIX_I_VP);

                // 贴花空间坐标
                float3 positionDS = TransformWorldToObject(positionWS);
                positionDS = positionDS * float3(1.0, -1.0, 1.0);

                float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
                clip(clipValue);

                float2 texCoord = positionDS.xz + float2(0.5, 0.5);

                float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
                float2 scale = float2(normalToWorld[3][0], normalToWorld[3][1]);
                float2 offset = float2(normalToWorld[3][2], normalToWorld[3][3]);
                texCoord.xy = texCoord.xy * scale + offset;

                float2 texCoord0 = texCoord;

                #ifdef DECAL_ANGLE_FADE
                half2 angleFade = half2(normalToWorld[1][3], normalToWorld[2][3]);
                if (angleFade.y < 0.0f)
                {
                    half3 decalNormal = half3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
                    half dotAngle = dot(normalWS, decalNormal);
                    angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
                }
                #endif

                half3 viewDirectionWS = half3(packedInput.viewDirectionWS);

                SurfaceDescription surfaceDescription = (SurfaceDescription)0;
                SurfaceDescriptionFunction(texCoord0, surfaceDescription);

                DecalSurfaceData surfaceData = (DecalSurfaceData)0;
                GetSurfaceData(surfaceDescription, angleFadeFactor, surfaceData);

                #ifdef DECAL_RECONSTRUCT_NORMAL
                surfaceData.normalWS.xyz = normalize(lerp(normalWS.xyz, surfaceData.normalWS.xyz, surfaceData.normalWS.w));
                #endif

                InputData inputData = (InputData)0;
                InitializeInputData(packedInput, positionWS, surfaceData.normalWS.xyz, viewDirectionWS, inputData);

                SurfaceData surface = (SurfaceData)0;
                GetSurface(surfaceData, surface);

            	ExtendData extendData = (ExtendData)0;
            	extendData.specularScaleBRDF = 1;

                // PBR 光照
                //half4 color = UniversalFragmentPBR(inputData, surface);
                half4 color = FragmentPBR(inputData, surface, extendData);

                // 混合雾
                UBPA_APPLY_FOG(packedInput, color);

                outColor = color;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"

    //CustomEditor "UnityEditor.Rendering.Universal.DecalShaderGraphGUI"
    CustomEditor "LWGUI.LWGUI"
}