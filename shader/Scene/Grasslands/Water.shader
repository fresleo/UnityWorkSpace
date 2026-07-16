Shader "XKnight/Scene/Grasslands/Water"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A ("贴图", Float) = 1
        
        [Sub(A)] [NoScaleOffset][Normal] _WavesNormal ("波浪法线", 2D) = "bump" {}
        
        [Space(10)]
        [Main(B, __, on, off)]
        _B ("水", Float) = 1
        
        [Sub(B)] _WaterColor ("水颜色", Color) = (0.2705882, 0.4823529, 0.5372549, 0)
        [Sub(B)] _Smoothness ("平滑度", Range(0, 1)) = 0.96
        [Sub(B)] _Tiling ("Tiling", Float) = 0.15
        [Sub(B)] _WavesSpeed ("波浪速度", Range(0.1, 1)) = 0.3
        [Sub(B)] _NormalIntensity ("法线强度", Range(0, 2)) = 1
        [Sub(B)] _Transparency ("透明度", Range(0, 10)) = 0
        [Sub(B)] _TransparencyFade ("透明度淡入淡出", Range(0, 2)) = 0
        [Sub(B)] _CoastalBlending ("海岸混合度", Range(0, 1)) = 1
        [Sub(B)] _RefractionFactor ("折射系数", Range(0, 1)) = 0.5

        [Space(10)]
        [Main(C, __, on, off)]
        _C ("泡沫", Float) = 1
        
        [SubToggle(C, _ENABLEFOAM_ON)] _EnableFoam ("启用 泡沫", Float) = 1
        [Sub(C)] _FoamTiling ("Tiling", Float) = 50
        [Sub(C)] _FoamOpacity ("不透明度", Range(0, 1)) = 0
        [Sub(C)] _FoamDistance ("距离", Range(0.01, 1)) = 0.07
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
            #pragma shader_feature_local _ENABLEFOAM_ON
            
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define REQUIRE_OPAQUE_TEXTURE 1
            #define REQUIRE_DEPTH_TEXTURE 1

            #define ASE_NEEDS_FRAG_SCREEN_POSITION
            #define ASE_NEEDS_FRAG_WORLD_POSITION
            #define ASE_NEEDS_VERT_POSITION
            
            #include "./Water_Input.hlsl"
            #include "./Water_Forward.hlsl"
            ENDHLSL
        }

        // Meta
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode"="Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define REQUIRE_OPAQUE_TEXTURE 1
            #define REQUIRE_DEPTH_TEXTURE 1

            #include "./Water_Input.hlsl"
            #include "./Water_Meta.hlsl"
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
            
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define REQUIRE_DEPTH_TEXTURE 1

            #include "./Water_Input.hlsl"
            #include "./Water_DepthOnly.hlsl"
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
            
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define REQUIRE_DEPTH_TEXTURE 1

            #define ASE_NEEDS_FRAG_WORLD_POSITION
            #define ASE_NEEDS_FRAG_SCREEN_POSITION

            #include "./Water_Input.hlsl"
            #include "./Water_DepthNormals.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}