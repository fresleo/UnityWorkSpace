Shader "GarenaTA/GPerObjectShadow/ResolvePost"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 10 // ScrAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 5 // OneMinusSrcAlpha
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0

        // _CharacterMaskTexture ("_CharacterMaskTexture", 2D) = "black" { }
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Blit"
            Blend[_SrcBlend][_DstBlend]
            ZTest Always
            ZWrite Off
            Cull Off


            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "./GPerObjectShadow.hlsl"

            // SAMPLER(sampler_BlitTexture);
            float4 _Color;

            TEXTURE2D(_CharacterMaskTexture);
            SAMPLER(sampler_CharacterMaskTexture);

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif

                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                half perObjectShadow = 1 - PerObjectRealtimeShadow(worldPos);

                float mask = SAMPLE_TEXTURE2D(_CharacterMaskTexture, sampler_CharacterMaskTexture, uv);
                perObjectShadow *= 1 - mask;

                half4 col = half4(_Color.rgb, _Color.a * perObjectShadow);

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Blit"
            Blend[_SrcBlend][_DstBlend]
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "./GPerObjectShadow.hlsl"

            // SAMPLER(sampler_BlitTexture);
            float4 _Color;

            TEXTURE2D(_CharacterMaskTexture);
            SAMPLER(sampler_CharacterMaskTexture);

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif

                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                half perObjectShadow = 1 - PerObjectRealtimeShadow(worldPos);

                float4 coords = TransformWorldToShadowCoord(worldPos);

                half realtimeShadow = 1 - MainLightRealtimeShadow(coords);

                float mask = SAMPLE_TEXTURE2D(_CharacterMaskTexture, sampler_CharacterMaskTexture, uv);
                perObjectShadow *= 1 - mask;
                
                half4 col = half4(_Color.rgb, _Color.a * max(realtimeShadow, perObjectShadow));

                return col;
            }
            ENDHLSL
        }
    }
}
