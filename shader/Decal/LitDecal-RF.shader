Shader "XKnight/Decal/Lit Decal (RendererFeature)"
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

        [Sub(Main)] _BaseMap ("基础纹理", 2D) = "white" {}
        [Sub(Main)] [HDR] _BaseColor ("颜色", color) = (1,1,1,1)

        [Sub(Main)] _NormalMap ("法线", 2D) = "white" {}
        // 未来如果有需要，可以考虑用 A 来放自发光贴图
        [Sub(Main)] _MetallicGlossMap ("金属光泽图：粗糙度(R)，AO(G)，金属性(B)", 2D) = "white" {}

        [Sub(Main)] _AlphaRemap ("透明度重映射 - 透明度将先 *x，然后 +y，zw 没用", vector) = (1,0,0,0)
        [Sub(Main)] _MulAlphaToRGB ("0 = 透明度=1，1 = 最终混合的透明度", float) = 0

        // 不常修改的附加设置 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(Additive, __, off, off)]
        _Additive ("附加设置", float) = 1

        [Sub(Additive)] _StencilRef ("模版引用值", Float) = 0
        [SubEnum(Additive, UnityEngine.Rendering.CompareFunction)] _StencilComp ("模版比较方式 - 如果要按特定值屏蔽，请设置为 NotEqual，否则设置为 Disable", float) = 0
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
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../ShaderLibrary/Lighting.hlsl"

            // Decal 要用到的数据结构
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;

                float3 normalWS : TEXCOORD0;
                float3 viewDirectionWS : TEXCOORD1;

                float3 positionWS: TEXCOORD2;
                UBPA_FOG_COORDS(3)

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _NormalMap_ST;
                half4 _BaseColor;
                half4 _AlphaRemap;
                half _MulAlphaToRGB;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_MetallicGlossMap);

            void GetSurfaceData(SurfaceDescription surfaceDescription, out DecalSurfaceData surfaceData)
            {
                half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);

                surfaceData = (DecalSurfaceData)0;
                surfaceData.baseColor.xyz = surfaceDescription.BaseColor;
                surfaceData.baseColor.w = surfaceDescription.Alpha;
                surfaceData.normalWS.xyz = mul((half3x3)normalToWorld, surfaceDescription.NormalTS.xyz);
                surfaceData.normalWS.w = surfaceDescription.NormalAlpha;
                surfaceData.metallic = surfaceDescription.Metallic;
                surfaceData.smoothness = surfaceDescription.Smoothness;
                surfaceData.occlusion = surfaceDescription.Occlusion;
            }

            void InitializeInputData(float3 positionWS, half3 normalWS, half3 viewDirectionWS, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirectionWS;
                inputData.shadowCoord = float4(0, 0, 0, 0);
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
                surfaceData.clearCoatSmoothness = 0;
            }

            PackedVaryings vert(Attributes inputMesh)
            {
                PackedVaryings packedOutput = (PackedVaryings)0;

                UNITY_SETUP_INSTANCE_ID(inputMesh);
                UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);

                float3 normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
                packedOutput.normalWS.xyz = normalWS;
                packedOutput.viewDirectionWS.xyz = GetWorldSpaceViewDir(vertexInput.positionWS);

                packedOutput.positionCS = vertexInput.positionCS;
                packedOutput.positionWS = vertexInput.positionWS;
                UBPA_TRANSFER_FOG(packedOutput, vertexInput.positionWS);

                return packedOutput;
            }

            void frag(PackedVaryings packedInput, out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(packedInput);

                #if UNITY_REVERSED_Z
                float depth = LoadSceneDepth(packedInput.positionCS.xy);
                #else
                float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(packedInput.positionCS.xy));
                #endif

                float2 positionSS = packedInput.positionCS.xy * _ScreenSize.zw;
                float3 positionWS = ComputeWorldSpacePosition(positionSS, depth, UNITY_MATRIX_I_VP);
                float3 positionDS = TransformWorldToObject(positionWS);
                positionDS = positionDS * float3(1.0, -1.0, 1.0);

                float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
                clip(clipValue);

                // uv
                float2 texCoord = positionDS.xz + float2(0.5, 0.5);

                float2 uv_BaseMap = texCoord * _BaseMap_ST.xy + _BaseMap_ST.zw;
                float4 baseMapT2d = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv_BaseMap);

                float2 uv_NormalMap = texCoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
                float3 normalMapT2d = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv_NormalMap);

                float4 metallicGlossMapT2d = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, texCoord);

                SurfaceDescription surfaceDescription = (SurfaceDescription)0;
                surfaceDescription.BaseColor = baseMapT2d.rgb;
                surfaceDescription.Alpha = baseMapT2d.a;
                surfaceDescription.NormalTS = normalMapT2d.rgb;
                surfaceDescription.NormalAlpha = 1;
                surfaceDescription.Smoothness = 1.0f - metallicGlossMapT2d.r;
                surfaceDescription.Occlusion = metallicGlossMapT2d.g;
                surfaceDescription.Metallic = metallicGlossMapT2d.b;

                DecalSurfaceData surfaceData;
                GetSurfaceData(surfaceDescription, surfaceData);

                half3 viewDirectionWS = half3(packedInput.viewDirectionWS);

                InputData inputData;
                InitializeInputData(positionWS, surfaceData.normalWS.xyz, viewDirectionWS, inputData);

                SurfaceData surface = (SurfaceData)0;
                GetSurface(surfaceData, surface);

                ExtendData extendData = (ExtendData)0;
                extendData.specularScaleBRDF = 1;

                //half4 col = UniversalFragmentPBR(inputData, surface);
                half4 col = FragmentPBR(inputData, surface, extendData);

                col *= _BaseColor;
                col.a = saturate(col.a * _AlphaRemap.x + _AlphaRemap.y); // 透明通道重新映射
                col.rgb *= lerp(1, col.a, _MulAlphaToRGB); // 插值

                UBPA_APPLY_FOG(packedInput, col);

                outColor = col;
            }
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}