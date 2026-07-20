Shader "GarenaTA/GPerObjectShadow/Apply"
{
    Properties
    {
        // _Alpha("Alpha", Range(0.0, 1.0)) = 1
        _Color("Color", Color) = (0,0,0,1)

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Blend[_SrcBlend][_DstBlend]
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            // Core.hlsl for XR dependencies
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "./GPerObjectShadow.hlsl"

            // SAMPLER(sampler_BlitTexture);
            // float _Alpha;
            float4 _Color;

            TEXTURE2D(_GPerObjectScreenSpaceShadowMap); 
            SAMPLER(sampler_GPerObjectScreenSpaceShadowMap);

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half a = SAMPLE_TEXTURE2D(_GPerObjectScreenSpaceShadowMap, sampler_GPerObjectScreenSpaceShadowMap, uv).r;

                return half4(_Color.rgb, (1 - a) * _Color.a);
            }
            ENDHLSL
        }
    }
}
