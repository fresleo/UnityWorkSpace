// 镶嵌+视差模式，镶嵌暂时先不开
Shader "XKnight/Decal/TessellatedParallax"
{
    Properties
    {
        [Main(Base, __, on, off)]
        _MainBase ("基础", Float) = 1
        [Sub(Base)] _AlphaClip ("透明度裁切", Range(0, 1))= 0.01
        
        [Sub(Base)] _BaseMap ("反照率 (RGB) 透明度 (A)", 2D) = "white" {}
        [Sub(Base)] _BaseColor ("主颜色 (RGB) 闲置 (A)", Color) = (1, 1, 1, 1)
        
        [Sub(Base)] _BumpMap ("法线纹理", 2D) = "bump" {}
        [Sub(Base)] _BumpScale ("法线比例", Float) = 1.0
        
        [Main(Specular, __, on, off)]
        _MainSpecular ("高光", Float) = 1
        [Sub(Specular)] _SpecularMap ("镜面反射贴图", 2D) = "white" {}
        [Sub(Specular)] _SpecularColor ("镜面反射颜色", Color) = (1, 1, 1, 1)
        [Sub(Specular)] _Shininess ("光泽度", Range(0, 1)) = 0.5
        
        [Main(Parallax, __, on, off)]
        _MainParallax ("视差，顶点", Float) = 1
        [Sub(Parallax)] _Displacement ("顶点扩张位移", Range(0, 0.1)) = 0.01
        [Sub(Parallax)] _ParallaxMap ("高度图", 2D) = "gray" {}
        [Sub(Parallax)] _Parallax ("视差量", Range(0, 0.1)) = 0.02
        
        [Main(Emission, __, on, off)]
        _MainEmission ("自发光", Float) = 1
        [SubToggle(Emission, __)] _Emission ("启用自发光", Float) = 0
        [Sub(Emission)] [HDR] _EmissionColor ("自发光颜色", Color) = (0,0,0)
        [Sub(Emission)] _EmissionMap ("自发光纹理", 2D) = "white" {}
        
        [Main(Dissolve, __, on, off)]
        _MainDissolve ("消融", Float) = 1
        [Sub(Dissolve)] _DissolveCutoff ("消融裁切", Range(0, 1)) = 0
        [Sub(Dissolve)] _DissolveFadingMin ("消融渐隐的最小值", Range(0, 1)) = 0
        [Sub(Dissolve)] _DissolveFadingMax ("消融渐隐的最大值", Range(0, 1)) = 0.2
        
        [Main(Other, __, on, off)]
        _MainOther ("其它设置", Float) = 1
        [Sub(Other)] _ShowProgressY ("显示进度 - 沿uv y轴", Range(0, 1)) = 1.0
        [SubToggle(Other, __)] _ReverseProgress ("反向进度", Float) = 0
        
        /*
        _Tess ("镶嵌", Range(1, 32)) = 4
        _MinDistance ("最小距离", Range(0, 50))= 10
        _MaxDistance ("最大距离", Range(0, 50))= 25
        */
    }
    
    // 410 - 支持镶嵌
    /*
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "RenderType"="AlphaTest" "Queue"="AlphaTest"
        }
        LOD 410

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
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            // #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // -------------------------------------
            // Material Keywords
            
            #define _ALPHATEST_ON 1

            // 镶嵌
            #pragma require tessellation tessHW
            #pragma hull HullFunction
            #pragma domain DomainFunction
            // 距离镶嵌
            #define ASE_TESSELLATION 1
            #define ASE_DISTANCE_TESSELLATION
            
            #include "./TessellatedParallax_Input.hlsl"
            #include "./TessellatedParallax_ForwardPass.hlsl"
            ENDHLSL
        }
    }
    */
    
    // 400 - 不支持镶嵌
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "RenderType"="AlphaTest" "Queue"="AlphaTest"
        }
        LOD 400

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
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            // #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma shader_feature _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _REVERSEPROGRESS_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            
            #define _ALPHATEST_ON 1 // 默认打开透明剪裁
            
            #include "./TessellatedParallax_Input.hlsl"
            #include "./TessellatedParallax_ForwardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}