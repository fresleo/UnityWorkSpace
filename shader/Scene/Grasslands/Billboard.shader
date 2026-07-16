Shader "XKnight/Scene/Grasslands/Billboard"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A ("主映射", Float) = 1
        
        [Sub(A)] _OpacityCutoff ("不透明度裁切", Range(0 , 1)) = 0.35

        [Sub(A)] [NoScaleOffset] _MainTexture ("主纹理", 2D) = "white" {}
        [Sub(A)] [NoScaleOffset] _NormalTexture ("法线纹理", 2D) = "bump" {}

        [Sub(A)] _Color ("调色", Color) = (1, 1, 1, 0)
        [Sub(A)] _Normal ("法线强度", Float) = 1
        [Sub(A)] _Smoothness ("平滑度", Range(0, 1)) = 0
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_VERT_NORMAL
            #define ASE_NEEDS_VERT_TANGENT
            
            #include "./Billboard_Input.hlsl"
            #include "./Billboard_Forward.hlsl"
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

            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1

            #include "./Billboard_Input.hlsl"
            #include "./Billboard_Meta.hlsl"
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_VERT_NORMAL

            #include "./Billboard_Input.hlsl"
            #include "./Billboard_ShadowCaster.hlsl"
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_VERT_NORMAL

            #include "./Billboard_Input.hlsl"
            #include "./Billboard_DepthOnly.hlsl"
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _ALPHATEST_ON 1
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1

            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_VERT_NORMAL
            #define ASE_NEEDS_VERT_TANGENT

            #include "./Billboard_Input.hlsl"
            #include "./Billboard_DepthNormals.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "LWGUI.LWGUI"
}