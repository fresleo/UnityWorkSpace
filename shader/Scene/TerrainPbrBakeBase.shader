Shader "Hidden/XKnight/Scene/TerrainPbrBake (Base Pass)"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo(RGB), Smoothness(A)", 2D) = "white" {}
        _MetallicTex ("Metallic (R)", 2D) = "black" {}

        [HideInInspector] _TerrainHolesTexture ("洞贴图 (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-1500"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma target 2.0
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
            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
            
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
            #define TERRAIN_SPLAT_BASEPASS 1
            
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _USE_PACKED_TEXTURE_MDOE
            
            #include "./TerrainPbrBakeInput.hlsl"
            #include "./TerrainPbrBakePasses.hlsl"
            ENDHLSL
        }
        
        // Meta
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }

            Cull Off

            HLSLPROGRAM
            #pragma vertex TerrainVertexMeta
            #pragma fragment TerrainFragmentMeta

            #pragma shader_feature EDITOR_VISUALIZATION

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            
            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitMetaPass.hlsl"
            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            
            #include "./TerrainPbrBakeInput.hlsl"
            #include "./TerrainPbrShadowCaster.hlsl"
            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "./TerrainPbrBakeInput.hlsl"
            #include "./TerrainPbrDepthOnly.hlsl"
            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}
            
            ZWrite On
            
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalOnlyVertex
            #pragma fragment DepthNormalOnlyFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _USE_PACKED_TEXTURE_MDOE

            #include "./TerrainPbrBakeInput.hlsl"
            #include "./TerrainPbrDepthNormal.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}