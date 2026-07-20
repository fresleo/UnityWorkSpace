Shader "Hidden/ToonPostProcessing/PreObjectIdOutline"
{
    Properties
    {
        _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineData ("描边的外部参数", Vector) = (0, 0, 0, 0)

        _BlurScale ("模糊比例", float) = 1

        _OutlineTexture ("与屏幕合成时，要用到的描边纹理", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"
        }

        Cull Off ZWrite Off ZTest Always

        // 0
        Pass
        {
            Name "Outline Detection Pass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragOutline

            #pragma multi_compile_fragment _ _MRT_BUFFER
            #pragma multi_compile_local_fragment _ OUTLINE_MIN_SEPARATION_ON

            #include "./PreObjectIdOutlineInput.hlsl"
            #include "./PreObjectIdOutlinePass.hlsl"
            ENDHLSL
        }

        // 1
        Pass
        {
            Name "Outline Blur Horizontally (depth aware)"

            HLSLPROGRAM
            #pragma vertex VertBlur
            #pragma fragment FragBlur
            
            #define OUTLINE_BLUR_HORIZ

            #include "./PreObjectIdOutlineInput.hlsl"
            #include "./PreObjectIdOutlinePass.hlsl"
            ENDHLSL
        }

        // 2
        Pass
        {
            Name "Outline Blur Vertically (depth aware)"

            HLSLPROGRAM
            #pragma vertex VertBlur
            #pragma fragment FragBlur

            #include "./PreObjectIdOutlineInput.hlsl"
            #include "./PreObjectIdOutlinePass.hlsl"
            ENDHLSL
        }

        // 3
        Pass
        {
            Name "Outline Blend Pass"

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCopy

            #include "./PreObjectIdOutlineInput.hlsl"
            #include "./PreObjectIdOutlinePass.hlsl"
            ENDHLSL
        }

        // 4
        Pass
        {
            Name "Outline Combine"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCombine

            #include "./PreObjectIdOutlineInput.hlsl"
            #include "./PreObjectIdOutlinePass.hlsl"
            ENDHLSL
        }

        // 5
        Pass
        {
            Name "Outline Diffusion (Anti Aliasing)"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDiffusion

            #include "./PreObjectIdOutlineInput.hlsl"
            #include "./PreObjectIdOutlinePass.hlsl"
            ENDHLSL
        }

    }
}