Shader "XKnight/Scene/Grasslands/Mountains"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A ("主映射", Float) = 1
        
        [Sub(A)] [NoScaleOffset] _Albedo ("反照率", 2D) = "white" {}
        [Sub(A)] [NoScaleOffset][Normal] _Normal ("法线", 2D) = "bump" {}

        [Sub(A)] _Color ("调色", Color) = (0.8,0.8,0.8,0)
        [Sub(A)] _NormalScale ("法线强度", Float) = 1
        [Sub(A)] _Smoothness ("平滑度", Range( 0 , 1)) = 0
        
        [Space(10)]
        [Main(B, __, on, off)]
        _B ("雾", Float) = 1
        
        [SubToggle(B, _ENABLEFOG_ON)] _EnableFog ("启用 雾", Float) = 1
        [Sub(B)] _FogColor ("雾的调色", Color) = (1,1,1,0)
        [Sub(B)] _Height ("高度", Float) = 1
        [Sub(B)] _Density ("密度", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"
        }

        Cull Back
        ZWrite On
        ZTest LEqual
        AlphaToMask Off

        // Forward
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode"="UniversalForwardOnly"
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
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            //#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            // 高度指数雾
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ENABLEFOG_ON
            
            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #include "./Mountains_Input.hlsl"
            #include "./Mountains_Forward.hlsl"
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

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ENABLEFOG_ON
            
            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #include "./Mountains_Input.hlsl"
            #include "./Mountains_Meta.hlsl"
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
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1

            #include "./Mountains_Input.hlsl"
            #include "./Mountains_ShadowCaster.hlsl"
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

            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1

            #include "./Mountains_Input.hlsl"
            #include "./Mountains_DepthOnly.hlsl"
            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode"="DepthNormalsOnly"
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

            #define _EMISSION
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1

            #include "./Mountains_Input.hlsl"
            #include "./Mountains_DepthNormals.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}