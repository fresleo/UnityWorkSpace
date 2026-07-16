Shader "XKnight/Scene/Grasslands/Surface"
{
    Properties
    {
        [Main(A, __, on, off)]
        _A ("主映射", Float) = 1
        
        [Sub(A)] _Tiling ("Tiling", Float) = 1
        [Sub(A)] [NoScaleOffset] _MainTex ("反照率", 2D) = "white" {}
        [Sub(A)] [NoScaleOffset][Normal] _BumpMap ("法线", 2D) = "bump" {}
        [Sub(A)] [NoScaleOffset] _MetallicGlossMap ("金属性/平滑度", 2D) = "white" {}
        [Sub(A)] [NoScaleOffset] _OcclusionMap ("AO", 2D) = "white" {}
        
        [Space(10)]
        [Main(B, __, on, off)]
        _B ("主映射的设置", Float) = 1
        
        [Sub(B)] _Color ("调色", Color) = (1,1,1,0)
        [Sub(B)] _BumpScale ("法线强度", Float) = 1
        [Sub(B)] _Metallic ("金属性", Range(0, 1)) = 0
        [Sub(B)] _Glossiness ("平滑度", Range(0, 1)) = 0
        [Sub(B)] [Enum(Metallic Alpha,0, Albedo Alpha,1)] _SmoothnessTextureChannel ("平滑度的源通道", Float) = 0
        [Sub(B)] _OcclusionStrength ("AO", Range(0, 1)) = 1
        
        [Space(10)]
        [Main(C, __, on, off)]
        _C ("覆盖映射", Float) = 1
        
        [Sub(C)] _CovTiling ("Tiling", Float) = 1
        [Sub(C)] [NoScaleOffset] _CovMainTex ("反照率", 2D) = "white" {}
        [Sub(C)] [NoScaleOffset][Normal] _CovBumpMap ("法线", 2D) = "bump" {}
        [Sub(C)] [NoScaleOffset] _CoverageMetallicSmoothness ("金属性/平滑度", 2D) = "white" {}
        [Sub(C)] [NoScaleOffset] _CovMask ("覆盖的遮罩", 2D) = "white" {}
        
        [Space(10)]
        [Main(D, __, on, off)]
        _D ("覆盖映射的设置", Float) = 1
        
        [SubToggle(D, _COVERAGEON_ON)] _Coverageon ("启用 覆盖", Float) = 0
        [Sub(D)] _CovColor ("调色", Color) = (1,1,1,0)
        [Sub(D)] _CovBumpScale ("法线强度", Float) = 1
        [Sub(D)] _CovMetallic ("金属性", Range(0, 1)) = 0
        [Sub(D)] _CovGlossiness ("平滑度", Range(0, 1)) = 0
        
        [Sub(D)] [Enum(Metallic Alpha,0, Albedo Alpha,1)] _CovSmoothnessTextureChannel ("平滑度的源通道", Float) = 0
        [Sub(D)] [Enum(World Normal,0, Vertex Position,1)] _CovOverlayMethod ("叠加方法", Float) = 0
        [Sub(D)] _CovOffset ("偏移", Float) = 1
        [Sub(D)] _CovBalance ("平衡", Float) = -1
        [Sub(D)] _MaskContrast ("遮罩的对比度", Float) = 1
        [Sub(D)] _NormalBlending ("法线的混合度", Range(0, 1)) = 1
        [Sub(D)] _MaskTilingX ("遮罩的 Tiling X", Float) = 1
        [Sub(D)] _MaskTilingY ("遮罩的 Tiling Y", Float) = 1
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
            #pragma shader_feature_local _COVERAGEON_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_FRAG_WORLD_POSITION
            #define ASE_NEEDS_FRAG_WORLD_NORMAL
            #define ASE_NEEDS_FRAG_WORLD_TANGENT
            #define ASE_NEEDS_FRAG_WORLD_BITANGENT
            
            #include "./Surface_Input.hlsl"
            #include "./Surface_Forward.hlsl"
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
            #pragma shader_feature_local _COVERAGEON_ON

            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1

            #define ASE_NEEDS_FRAG_WORLD_POSITION
            #define ASE_NEEDS_VERT_NORMAL
            
            #include "./Surface_Input.hlsl"
            #include "./Surface_Meta.hlsl"
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #include "./Surface_Input.hlsl"
            #include "./Surface_ShadowCaster.hlsl"
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #include "./Surface_Input.hlsl"
            #include "./Surface_DepthOnly.hlsl"
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
            #pragma shader_feature_local _COVERAGEON_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            
            #define ASE_NEEDS_FRAG_WORLD_POSITION
            #define ASE_NEEDS_FRAG_WORLD_NORMAL
            #define ASE_NEEDS_FRAG_WORLD_TANGENT
            #define ASE_NEEDS_VERT_NORMAL
            #define ASE_NEEDS_VERT_TANGENT
            
            #include "./Surface_Input.hlsl"
            #include "./Surface_DepthNormals.hlsl"
            ENDHLSL
        }
    }

    //CustomEditor "XKnight.ShaderGUI.GrasslandsShaderGUI"
    CustomEditor "LWGUI.LWGUI"
}