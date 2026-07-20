Shader "Hidden/ToonPostProcessing/ViewSpaceNormalsOutline"
{
    Properties
    {
        _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        
        _OutlineData_0 ("描边的参数_0", Vector) = (0, 0, 0, 0)
        _OutlineData_1 ("描边的参数_1", Vector) = (0, 0, 0, 0)
        _OutlineData_2 ("描边的参数_2", Vector) = (0, 0, 0, 0)
        
        _OutlineTexture ("与屏幕合成时，要用到的描边纹理", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"
        }
        
        Cull Off ZWrite Off ZTest Always
        
        HLSLINCLUDE
        #pragma target 3.0
        #pragma prefer_hlslcc gles
        ENDHLSL
        
        // 0
        Pass
        {
            Name "Outline Combine"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragCombine

            #include "./ViewSpaceNormalsOutlineInput.hlsl"
            #include "./ViewSpaceNormalsOutlinePass.hlsl"
            ENDHLSL
        }
        
        // 1
        Pass
        {
            Name "Outline Diffusion (Anti Aliasing)"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDiffusion

            #include "./ViewSpaceNormalsOutlineInput.hlsl"
            #include "./ViewSpaceNormalsOutlinePass.hlsl"
            ENDHLSL
        }

        // 2
        Pass
        {
            Name "Outline Detection"
            
            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment FragOutline

            #pragma multi_compile_fragment _ _MRT_BUFFER

            #include "./ViewSpaceNormalsOutlineInput.hlsl"
            #include "./ViewSpaceNormalsOutlinePass.hlsl"
            ENDHLSL
        }

        // 3
        Pass
        {
            Name "Outline Detection And Blend"
            
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment FragOutline

            #pragma multi_compile_fragment _ _MRT_BUFFER

            #pragma multi_compile_local_fragment _ _DIRECT_BLEND

            #include "./ViewSpaceNormalsOutlineInput.hlsl"
            #include "./ViewSpaceNormalsOutlinePass.hlsl"
            ENDHLSL
        }
    }
}