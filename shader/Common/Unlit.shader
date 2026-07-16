Shader "XKnight/Unlit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        
        _Surface ("__surface", Float) = 0.0
        
        _SrcBlend ("__src", Float) = 1.0
        _DstBlend ("__dst", Float) = 0.0
        _SrcBlendAlpha ("__srcA", Float) = 1.0
        _DstBlendAlpha ("__dstA", Float) = 0.0
        
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        _Cutoff ("AlphaCutout", Range(0.0, 1.0)) = 0.5
        
        _Cull ("__cull", Float) = 2.0
        _ZWrite ("__zw", Float) = 1.0
        
        _QueueOffset ("Queue offset", Float) = 0.0
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull [_Cull]
        ZWrite [_ZWrite]
        
        Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
        
        Pass
        {
            Name "Unlit"
//            Tags
//            {
//                "LightMode" = "UniversalForward"
//            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #ifdef LOD_FADE_CROSSFADE
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif
            
            CBUFFER_START(UnityPerMaterial)
                float4  _BaseMap_ST;
                half4   _BaseColor;
                half    _Cutoff;
                half    _Surface;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }

            void InitializeInputData(Varyings input, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = float3(0, 0, 0);
                inputData.normalWS = half3(0, 0, 1);
                inputData.viewDirectionWS = half3(0, 0, 1);
                
                inputData.shadowCoord = 0;
                inputData.fogCoord = 0;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = half3(0, 0, 0);
                inputData.normalizedScreenSpaceUV = 0;
                inputData.shadowMask = half4(1, 1, 1, 1);
            }

            void UnlitPassFragment(Varyings input
                , out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined( LOD_FADE_CROSSFADE )
                LODFadeCrossFade(input.positionCS);
                #endif

                half2 uv = input.uv;
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half3 color = texColor.rgb * _BaseColor.rgb;
                half alpha = texColor.a * _BaseColor.a;

                alpha = AlphaDiscard(alpha, _Cutoff);

                InputData inputData;
                InitializeInputData(input, inputData);

                half4 finalColor = UniversalFragmentUnlit(inputData, color, alpha);

                finalColor.a = OutputAlpha(finalColor.a, IsSurfaceTypeTransparent(_Surface));
                
                outColor = finalColor;
            }
            ENDHLSL
        }
    }

    CustomEditor "XKnight.ShaderGUI.UnlitShaderGUI"
}