// 图集模式
Shader "XKnight/Decal/Atlas"
{
    Properties
    {
        [Main(Base, __, on, off)]
        _MainBase ("基础", Float) = 1
        
        [Sub(Base)] _AlphaClip ("透明裁切值", Range(0, 1))= 0.01
        
        [Sub(Base)] _BaseMap ("反照率 (RGB) 透明度 (A)", 2D) = "white" {}
        [Sub(Base)] _BaseColor ("主颜色 (RGB) 透明度 (A)", Color) = (1, 1, 1, 1)
        
        [Sub(Base)] _BumpMap ("法线", 2D) = "bump" {}
        [Sub(Base)] _BumpScale ("法线比例", Float) = 1.0
        
        [Main(Specular, __, on, off)]
        _MainSpecular ("高光", Float) = 1
        
        [Sub(Specular)] _SpecularMap ("镜面反射贴图", 2D) = "white" {}
        [Sub(Specular)] _SpecularColor ("镜面反射颜色", Color) = (1, 1, 1, 1)
        [Sub(Specular)] _Shininess ("光泽度", Range(0, 1)) = 0.5

        [Main(Atlas, __, on, off)]
        _MainAtlas ("图集", Float) = 1
        
        [Sub(Atlas)] _AtlasWidth ("图集宽度", Float) = 4
        [Sub(Atlas)] _AtlasHeight ("图集高度", Float) = 4
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "RenderType"="AlphaTest" "Queue"="AlphaTest"
        }
        LOD 300
        
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Fog { Mode Off }
            Offset -1, -1
            
            ZWrite Off
            Blend One OneMinusSrcAlpha
            
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
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            //#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords

            #define _ALPHATEST_ON 1 // 默认打开透明剪裁
            
            #include "./AtlasInput.hlsl"
            #include "./AtlasForwardPass.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "LWGUI.LWGUI"
}