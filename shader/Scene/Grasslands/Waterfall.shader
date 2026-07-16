Shader "XKnight/Scene/Grasslands/Waterfall"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A ("贴图", Float) = 1
        
        [Sub(A)] [NoScaleOffset][Normal] _NormalMap ("法线", 2D) = "bump" {}
        [Sub(A)] _NormalTilingX ("法线 Tiling X", Float) = 1
        [Sub(A)] _NormalTilingY ("法线 Tiling Y", Float) = 1

        [Space(10)]
        [Main(B, __, on, off)]
        _B ("设置", Float) = 1
        
        [Sub(B)] _MainColor ("主调色", Color) = (1, 1, 1, 0)
        [Sub(B)] _FlowSpeed ("流速", Float) = 1
        [Sub(B)] _NormalScale ("法线强度", Float) = 1
        [Sub(B)] _Smoothness ("平滑度", Range(0, 1)) = 0
        [Sub(B)] _RefractionFactor ("折射系数", Range(0, 1)) = 0.5

        [Space(10)]
        [Main(C, __, on, off)]
        _C ("泡沫", Float) = 1
        
        [Sub(C)] _FoamColor ("泡沫颜色", Color) = (1, 1, 1, 0)
        [Sub(C)] _FoamTilingX ("Tiling X", Float) = 1
        [Sub(C)] _FoamTilingY ("Tiling Y", Float) = 1
        [Sub(C)] _FoamVoronoiSpeed ("Voronoi Speed", Float) = 1
        [Sub(C)] _FoamLevel ("Level", Float) = 0
        [Sub(C)] _FoamFade ("Fade", Float) = 1
        [Sub(C)] _FoamScale ("Scale", Float) = 1
        [Sub(C)] _FoamOffset ("Offset", Float) = 1

        [Space(10)]
        [Main(D, __, on, off)]
        _D ("梯度", Float) = 1
        
        [Sub(D)] _GradientColor ("梯度颜色", Color) = (1,1,1,0)
        [Sub(D)] _GradientLevel ("Level", Float) = 0
        [Sub(D)] _GradientFade ("Fade", Float) = 1

        [Space(10)]
        [Main(E, __, on, off)]
        _E ("顶点偏移", Float) = 1
        
        [Sub(E)] _VOIntensity ("强度", Float) = 1
        [Sub(E)] _VOScale ("缩放", Float) = 5

        [Space(10)]
        [Main(F, __, on, off)]
        _F ("不透明度", Float) = 1
        
        [SubToggle(F, _OPACITYENABLE_ON)] _OpacityEnable ("启用 不透明度", Float) = 0
        [Sub(F)] _OpacityLevel ("Level", Float) = 0
        [Sub(F)] _OpacityFade ("Fade", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"
        }

        Cull Back
        ZWrite Off
        ZTest LEqual
        AlphaToMask Off

        // Forward
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            ColorMask RGBA

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON

            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            //#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            //#pragma multi_compile_fragment _ _LIGHT_COOKIES

            // 高度指数雾
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _OPACITYENABLE_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define REQUIRE_OPAQUE_TEXTURE 1
            #define REQUIRE_DEPTH_TEXTURE 1
            
            #define ASE_NEEDS_VERT_NORMAL
            #define ASE_NEEDS_FRAG_SCREEN_POSITION
            #define ASE_NEEDS_VERT_POSITION
            
            #include "./Waterfall_Input.hlsl"
            #include "./Waterfall_Forward.hlsl"
            ENDHLSL
        }

        // Meta

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            AlphaToMask Off
            ColorMask 0

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Pipeline keywords

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _OPACITYENABLE_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_NORMAL
            
            #include "./Waterfall_Input.hlsl"
            #include "./Waterfall_ShadowCaster.hlsl"
            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode"="DepthOnly"
            }

            ZWrite On
            ColorMask 0
            AlphaToMask Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _OPACITYENABLE_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1

            #define ASE_NEEDS_VERT_NORMAL
            
            #include "./Waterfall_Input.hlsl"
            #include "./Waterfall_DepthOnly.hlsl"
            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode"="DepthNormals"
            }

            Blend One Zero
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _OPACITYENABLE_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_NORMAL
            
            #include "./Waterfall_Input.hlsl"
            #include "./Waterfall_DepthNormals.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}