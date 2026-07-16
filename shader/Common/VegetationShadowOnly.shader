Shader "XKnight/VegetationShadowOnly"
{
    Properties
    {
        [Main(Base, __, on, off)]
        _Base ("基础设置", Float) = 1

        [Sub(Base)] [MainTexture] _BaseMap ("反照率纹理", 2D) = "white" {}
        [Sub(Base)] [MainColor] _BaseColor ("基本颜色", Color) = (1, 1, 1, 1)
        [Sub(Base)] _Cutoff ("透明裁切", Range(0, 1)) = 0.5
        
        [SubToggle(Base, __)] _Intersection ("启用顶点交互功能", Float) = 0
        [Sub(Base)] _IntersectionIntensity ("顶点交互强度", Range(0, 2)) = 1

        [Main(Wind, __, on)]
        _Wind ("风场", Float) = 1
        [Sub(Wind)] _WindVariation ("风的变化", Range(0, 1)) = 0.3
        [Sub(Wind)] _WindStrength ("风的强度", Range(0, 2)) = 1
        [Sub(Wind)] _TurbulenceStrength ("湍流强度", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest+3"
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material keywords
            #pragma shader_feature_local_vertex _ _WIND_ON
            #pragma shader_feature_local_vertex _ _INTERSECTION_ON

            // -------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./VegetationShadowOnlyPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}




