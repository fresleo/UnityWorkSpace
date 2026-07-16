Shader "XKnight/ShadowOnly"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Unity defined keywords
            // 这在阴影贴图生成期间用于区分定向和精准的灯光阴影，因为它们使用不同的公式来应用 Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./SimpleLitShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}