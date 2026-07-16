Shader "XKnight/VAT/Blood"
{
    Properties
    {
        // 主要设置 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(Main, __, on, off)]
        _Main ("主要设置", float) = 1
        
        [Sub(Main)] _Color ("主颜色", Color) = (1,1,1,1)
        [Sub(Main)] _Metallic ("金属性", Range(0, 1)) = 0
        [Sub(Main)] _Smoothness ("光滑度", Range(0, 1)) = 0.6
        [Sub(Main)] _Occlusion ("AO", Range(0, 1)) = 1
        
        // VAT动画 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Main(VAT, __, on, off)]
        _VAT ("VAT动画", float) = 1
        
        [Sub(VAT)] _BoundingMax ("Bounding Max", Float) = 1.0
        [Sub(VAT)] _BoundingMin ("Bounding Min", Float) = 1.0
        [Sub(VAT)] _HeightOffset ("Height Offset", Vector) = (0, 0, 0)
        
        [Sub(VAT)] _PosMap ("顶点坐标图", 2D) = "white" {}
        [Sub(VAT)] _BumpMap ("法线图", 2D) = "grey" {}
        
        [Sub(VAT)] _TimeInFrames ("帧时间", float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            
            "RenderType" = "AlphaTest"
            "Queue" = "AlphaTest+1"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite On
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ForwardPassVertex
            #pragma fragment ForwardPassFragment

            // -------------------------------------
            // Pipeline keywords
            #pragma shader_feature _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature _ _ADDITIONAL_LIGHTS
            
            #pragma shader_feature _ LIGHTMAP_ON
            #pragma shader_feature_fragment _ SHADOWS_SHADOWMASK

            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            
            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./BFX_VATBlood_Input.hlsl"
            #include "./BFX_VATBlood_ForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            // -------------------------------------
            // Pipeline keywords

            // -------------------------------------
            // Material Keywords

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./BFX_VATBlood_Input.hlsl"
            #include "./BFX_VATBlood_ShadowPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    
    CustomEditor "LWGUI.LWGUI"
}