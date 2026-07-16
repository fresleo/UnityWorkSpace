Shader "XKnight/Scene/Grasslands/Vegetation"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A ("主映射", Float) = 1
        
        [Sub(A)] _Tiling ("Tiling", Float) = 1
        [Sub(A)] [NoScaleOffset] _Albedo ("反照率", 2D) = "white" {}
        [Sub(A)] [NoScaleOffset][Normal] _Normal ("法线", 2D) = "bump" {}
        [Sub(A)] [NoScaleOffset] _SmoothnessTexture ("平滑度", 2D) = "white" {}
        [Sub(A)] _AlphaCutoff ("不透明裁切", Range(0, 1)) = 0.35

        [Space(10)]
        [Main(B, __, on, off)]
        _B ("设置", Float) = 1
        
        [Sub(B)] _MainColor ("主调色", Color) = (1,1,1,0)
        [Sub(B)] _NormalScale ("法线强度", Float) = 1
        [Sub(B)] _Smoothness ("平滑度", Range(0, 1)) = 0

        [Space(10)]
        [Main(C, __, on, off)]
        _C ("2级颜色", Float) = 1
        
        [SubToggle(C, _COLOR2ENABLE_ON)] _Color2Enable ("启用 2级颜色", Float) = 0
        [Sub(C)] _SecondColor ("2级颜色", Color) = (0,0,0,0)
        [Sub(C)] [KeywordEnum(World_Noise_2D, World_Noise_3D, Vertex_Gradient, UV_Gradient)] _SecondColorOverlayType ("叠加方法", Float) = 0
        [Sub(C)] _SecondColorOffset ("2级偏移", Float) = 1
        [Sub(C)] _SecondColorFade ("2级平衡", Float) = 1
        [Sub(C)] _WorldNoiseScale ("世界噪声缩放", Float) = 1

        [Space(10)]
        [Main(D, __, on, off)]
        _D ("风", Float) = 1
        
        [SubToggle(D, _ENABLEWIND_ON)] _EnableWind ("启用 风", Float) = 1
        [Sub(D)] _WindForce ("力", Range(0, 1)) = 0.6696684
        [Sub(D)] _WindWavesScale ("波浪缩放", Range(0, 1)) = 0.25
        [Sub(D)] _WindFlowDensity ("流量密度", Range(0.5, 5)) = 1
        [Sub(D)] [Toggle] _UVBaseLock ("UV 基本锁", Float) = 0
        

        [Space(10)]
        [Main(E, __, on, off)]
        _E ("草按距离淡入淡出", Float) = 1
        
        [Sub(E)] [Toggle] _GrassDistanceFadeEnable ("启用", Float) = 0
        [Sub(E)] _GrassFadeDistance ("距离", Float) = 30
        [Sub(E)] _GrassFalloff ("衰减", Range(0, 1)) = 0.7

        [Space(10)]
        [Main(F, __, on, off)]
        _F ("照明", Float) = 1
        
        [Sub(F)] _LightingFlatness ("照明平整度", Range(0, 1)) = 0
        [Sub(F)] _TranslucencyInt ("半透明", Range(0, 10)) = 0
        [Sub(F)] _TranslucencyColor ("半透明颜色", Color) = (1,1,1,0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"
        }

        Cull Off
        ZWrite On
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

            Blend One Zero, One Zero
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
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
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
            #pragma shader_feature_local _ENABLEWIND_ON
            #pragma shader_feature_local _COLOR2ENABLE_ON
            #pragma shader_feature_local _SECONDCOLOROVERLAYTYPE_WORLD_NOISE_2D _SECONDCOLOROVERLAYTYPE_WORLD_NOISE_3D _SECONDCOLOROVERLAYTYPE_VERTEX_GRADIENT _SECONDCOLOROVERLAYTYPE_UV_GRADIENT

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_VERT_NORMAL
            #define ASE_NEEDS_FRAG_WORLD_POSITION
            #define ASE_NEEDS_FRAG_WORLD_VIEW_DIR
            #define ASE_NEEDS_FRAG_SHADOWCOORDS
            
            #include "./Vegetation_Input.hlsl"
            #include "./Vegetation_Forward.hlsl"
            ENDHLSL
        }

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
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Pipeline keywords

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ENABLEWIND_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_VERT_NORMAL
            
            #include "./Vegetation_Input.hlsl"
            #include "./Vegetation_ShadowCaster.hlsl"
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
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ENABLEWIND_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_VERT_NORMAL
            
            #include "./Vegetation_Input.hlsl"
            #include "./Vegetation_DepthOnly.hlsl"
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
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ENABLEWIND_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_VERT_NORMAL
            
            #include "./Vegetation_Input.hlsl"
            #include "./Vegetation_DepthNormals.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "LWGUI.LWGUI"
}