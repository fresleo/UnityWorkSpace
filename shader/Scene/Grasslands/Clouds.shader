Shader "XKnight/Scene/Grasslands/Clouds"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A ("主映射", Float) = 1
        
        [Sub(A)] [HDR] _Color ("调色", Color) = (1,1,1,0)
        [Sub(A)] [NoScaleOffset] _MainTex ("主纹理", 2D) = "white" {}
        [Sub(A)] _ParticleSoftness ("颗粒柔软度", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"
        }

        Cull Off
        AlphaToMask Off

        // Forward
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode"="UniversalForwardOnly"
            }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
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
            #pragma multi_compile _ LIGHTMAP_ON

            // -------------------------------------
            // Pipeline keywords
            // 高度指数雾
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define REQUIRE_DEPTH_TEXTURE 1
            
            #define ASE_NEEDS_FRAG_COLOR
            #define ASE_NEEDS_FRAG_SCREEN_POSITION
            
            #include "./Clouds_Input.hlsl"
            #include "./Clouds_Forward.hlsl"
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
            
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define REQUIRE_DEPTH_TEXTURE 1

            #define ASE_NEEDS_FRAG_SCREEN_POSITION

            #include "./Clouds_Input.hlsl"
            #include "./Clouds_DepthOnly.hlsl"
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
            
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define REQUIRE_DEPTH_TEXTURE 1

            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_NORMAL_WS

            #define ASE_NEEDS_FRAG_SCREEN_POSITION

            #include "./Clouds_Input.hlsl"
            #include "./Clouds_DepthNormals.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}