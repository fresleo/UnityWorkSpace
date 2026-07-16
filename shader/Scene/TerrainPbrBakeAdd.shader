Shader "Hidden/XKnight/Scene/TerrainPbrBake (Add Pass)"
{
    Properties
    {
        // Layer count is passed down to guide height-blend enable/disable, due
        // to the fact that heigh-based blend will be broken with multipass.
        [HideInInspector] [PerRendererData] _NumLayersCount ("Total Layer Count", Float) = 1.0

        [HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}

        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}

        [HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "black" {}
        [HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "black" {}
        [HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "black" {}
        [HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "black" {}

        [HideInInspector][Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0

        [HideInInspector] _Mask0 ("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Mask1 ("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask2 ("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask3 ("Mask 3 (A)", 2D) = "grey" {}

        [HideInInspector] _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 1.0

        // used in fallback on old cards & base map
        [HideInInspector] _BaseMap("BaseMap (RGB)", 2D) = "white" {}
        [HideInInspector] _BaseColor("Main Color", Color) = (1,1,1,1)
        
        [HideInInspector] _TerrainHolesTexture ("洞贴图 (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-1499"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        // TerrainAddLit
        Pass
        {
            Name "TerrainAddLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Blend One One
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SplatmapVert
            #pragma fragment SplatmapFragment

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // #pragma shader_feature_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma shader_feature_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // #pragma shader_feature _ _GLOBAL_RAIN_ON
            // #pragma shader_feature _ _GLOBAL_RAIN_AREA_VISUALIZATION_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL // 执行实例化时在像素着色器中采样法线
            #define TERRAIN_SPLAT_ADDPASS

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _MASKMAP
            #pragma shader_feature_local _USE_PACKED_TEXTURE_MDOE
            
            #include "./TerrainPbrBakeInput.hlsl"
            #include "./TerrainPbrBakePasses.hlsl"
            ENDHLSL
        }
    }
}