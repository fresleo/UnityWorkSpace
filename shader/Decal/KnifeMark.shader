Shader "XKnight/Decal/KnifeMark"
{
    Properties
    {
        [Main(Base, __, on, off)] // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        _MainBase ("基本信息", Float) = 1
        
        [Sub(Base)] [NoScaleOffset] _BaseMap ("RGB(基本纹理) A(透明度)", 2D) = "white" {}
        [Sub(Base)] _BaseColor ("RGB(主颜色) A(闲置)", Color) = (1, 1, 1, 1)
        // [Sub(Base)] _Cutoff ("透明裁切", Range(0.0, 1.0)) = 0.5
        [Sub(Base)] _AlphaControl ("透明度控制", Range(0, 1)) = 1
        
        [Sub(Base)] _Metallic ("金属性", Range(0, 1)) = 0
        [Sub(Base)] _Smoothness ("平滑度", Range(0, 1)) = 0.5
        
        [Main(Normal, _NORMALMAP, on, on)] // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        _MainNormal ("法线", Float) = 1
        
        [Sub(Normal)] _BumpMap ("法线纹理", 2D) = "bump" {}
        [Sub(Normal)] _BumpScale ("法线比例", Range(0, 3)) = 1
        
        [Main(Parallax, _PARALLAXMAP, on, on)] // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        _MainParallax ("视差映射", Float) = 1
        
        [Sub(Parallax)] _ParallaxMap2D ("高度图", 2D) = "black" {}
        [Sub(Parallax)] _Parallax ("视差映射比例", Range(0.005, 0.08)) = 0.005
        
        [Main(Stretch, __, on, off)] // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        _MainStretch ("刀痕拉伸", Float) = 1
        
        [Sub(Stretch)] _UVTilingOffset ("Tiling (xy), Offset (zw)", Vector) = (1, 1, 0, 0)
        [SubToggle(Stretch, __)] _SingleImage ("限制为单个图像", Float) = 1
        
        [Main(LowTemp, __, on, off)] // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        _MainLowTemp ("刀痕低温区", Float) = 1
        
        [Sub(LowTemp)] [HDR] _HighTempColor_1 ("低温区颜色", Color) = (1, 1, 1, 1)
        [Sub(LowTemp)] _HighTempStrength_1 ("低温区强度", Range(0, 1)) = 1
        
        [Main(HighTemp, __, on, off)] // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        _MainHighTemp ("刀痕高温区", Float) = 1
        
        [Sub(HighTemp)] _HighTempMap ("高温区遮罩", 2D) = "black" {}
        [Sub(HighTemp)] [HDR] _HighTempColor_2 ("高温区颜色", Color) = (1, 1, 1, 1)
        [Sub(HighTemp)] _HighTempStrength_2 ("高温区强度", Range(0, 1)) = 1
        [Sub(HighTemp)] _HighTempSmoothingFactor ("高温区平滑系数", Float) = 1
        
        [Main(Projection, __, on, off)] // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        _MainProjection ("投影矫正", Float) = 1
        [Sub(Projection)] _CurvatureCorrection ("曲面修正强度", Range(0, 1)) = 0
        [Sub(Projection)] _UVCorrection ("UV 修正强度", Range(0, 1)) = 1
        
//        [Main(Advanced, __, on, off)] // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
//        _MainAdvanced ("高级设置", Float) = 1
//        _GIIndirectDiffuseBoost ("GI 间接光比例（漫反射）", Range(-3, 3)) = 1
//        _SpecularScaleBRDF ("镜面高光比例（BRDF）", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "RenderType"="Transparent" "Queue"="Transparent"
        }
        LOD 400

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            //Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            
            #pragma target 3.0
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE //_MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS //_ADDITIONAL_LIGHTS_VERTEX
            
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX

            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            
            #pragma shader_feature_local_fragment _SINGLEIMAGE_ON

            //#define _ALPHATEST_ON 1 // 默认打开透明剪裁

            #include "./KnifeMark_Input.hlsl"
            #include "./KnifeMark_ForwardPass.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "LWGUI.LWGUI"
}