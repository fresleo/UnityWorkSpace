Shader "XKnight/Decal/Unlit Decal (RendererFeature)"
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
        [Sub(Main)] [HDR] _BaseColor ("颜色", Color) = (1,1,1,1)
        
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
                "DisableBatching"="LODFading"
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

            #include "../ShaderLibrary/Lighting.hlsl"
            
            // Decal 要用到的数据结构
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderVariablesDecal.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;

                float3 positionWS: TEXCOORD0;
                UBPA_FOG_COORDS(1)

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _AlphaRemap;
                half _MulAlphaToRGB;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            PackedVaryings vert(Attributes inputMesh)
            {
                PackedVaryings packedOutput = (PackedVaryings)0;

                UNITY_SETUP_INSTANCE_ID(inputMesh);
                UNITY_TRANSFER_INSTANCE_ID(inputMesh, packedOutput);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(inputMesh.positionOS.xyz);
                
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
                
                half4 col = baseMapT2d;
                
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