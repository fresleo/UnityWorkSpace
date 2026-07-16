//这个 shader 是为了解决 "XKnight/Scene/TerrainPBR" 在 terrain 组件上 bake 不出 gi 的问题
Shader "XKnight/Scene/TerrainPbrBake"
{
    Properties
    {
        [HideInInspector] [ToggleUI] _EnableHeightBlend ("EnableHeightBlend", Float) = 0.0
        _HeightTransition ("Height Transition", Range(0, 1.0)) = 0.0
        // Layer count is passed down to guide height-blend enable/disable, due
        // to the fact that heigh-based blend will be broken with multipass.
        [HideInInspector] [PerRendererData] _NumLayersCount ("Total Layer Count", Float) = 1.0

        [HideInInspector] _Control ("控制混合纹理", 2D) = "red" {}

        [HideInInspector] _Splat0 ("笔刷 0", 2D) = "grey" {}
        [HideInInspector] _Splat1 ("笔刷 1", 2D) = "grey" {}
        [HideInInspector] _Splat2 ("笔刷 2", 2D) = "grey" {}
        [HideInInspector] _Splat3 ("笔刷 3", 2D) = "grey" {}

        [HideInInspector] _Normal0 ("法线 0", 2D) = "black" {}
        [HideInInspector] _Normal1 ("法线 1", 2D) = "black" {}
        [HideInInspector] _Normal2 ("法线 2", 2D) = "black" {}
        [HideInInspector] _Normal3 ("法线 3", 2D) = "black" {}
        
        [HideInInspector] _Mask0 ("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Mask1 ("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask2 ("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask3 ("Mask 3 (A)", 2D) = "grey" {}

        [HideInInspector][Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0

        [HideInInspector] _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 0.5

        // used in fallback on old cards & base map
        [HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "grey" {}
        [HideInInspector] _BaseColor ("Main Color", Color) = (1, 1, 1, 1)

        [ToggleUI] _EnableInstancedPerPixelNormal ("启用实例化的每像素法线", Float) = 1.0
        
        [HideInInspector] _TerrainHolesTexture ("洞贴图 (RGB)", 2D) = "white" {}

        
        [HideInInspector] _SpecularScaleBRDF0 ("镜面高光比例（BRDF） 0", Range(0, 1)) = 1
        [HideInInspector] _SpecularScaleBRDF1 ("镜面高光比例（BRDF） 1", Range(0, 1)) = 1
        [HideInInspector] _SpecularScaleBRDF2 ("镜面高光比例（BRDF） 2", Range(0, 1)) = 1
        [HideInInspector] _SpecularScaleBRDF3 ("镜面高光比例（BRDF） 3", Range(0, 1)) = 1

        [HideInInspector] _GIIndirectDiffuseBoost0 ("GI 间接光比例（漫反射） 0", Range(-3, 3)) = 1
        [HideInInspector] _GIIndirectDiffuseBoost1 ("GI 间接光比例（漫反射） 1", Range(-3, 3)) = 1
        [HideInInspector] _GIIndirectDiffuseBoost2 ("GI 间接光比例（漫反射） 2", Range(-3, 3)) = 1
        [HideInInspector] _GIIndirectDiffuseBoost3 ("GI 间接光比例（漫反射） 3", Range(-3, 3)) = 1

        [HideInInspector] _BakedGITintIntensity0 ("TOD GI 调色强度 0", Range(0, 1)) = 1
        [HideInInspector] _BakedGITintIntensity1 ("TOD GI 调色强度 1", Range(0, 1)) = 1
        [HideInInspector] _BakedGITintIntensity2 ("TOD GI 调色强度 2", Range(0, 1)) = 1
        [HideInInspector] _BakedGITintIntensity3 ("TOD GI 调色强度 3", Range(0, 1)) = 1
        
        [Toggle(_USE_PACKED_TEXTURE_MDOE)]
        _TempPackTextureMode ("为了兼容 Lit 的合并贴图模式 - 如果变黑的话，先尝试切换1下这里", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-1500"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "False"
            "TerrainCompatible" = "True"
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
            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
            
            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL // 执行实例化时在像素着色器中采样法线

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _MASKMAP
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

            // bake shader 不是运行时用的，所以 C_BUFFER 不一致也没关系，因为我们不指望用它去进行 SRP 合批
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

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
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
            Tags
            {
                "LightMode" = "DepthNormals"
            }
            
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalOnlyVertex
            #pragma fragment DepthNormalOnlyFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma shader_feature_local _NORMALMAP

            #include "./TerrainPbrBakeInput.hlsl"
            #include "./TerrainPbrDepthNormal.hlsl"
            ENDHLSL
        }

        // DepthMask
        Pass
        {
            Name "DepthMask"
            Tags
            {
                "LightMode" = "DepthMask"
            }

            ZWrite On
            
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthMaskVertex
            #pragma fragment DepthMaskFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "./TerrainPbrBakeInput.hlsl"
            #include "./TerrainPbrDepthMask.hlsl"
            ENDHLSL
        }

        // ViewSpaceNormals
        Pass
        {
            Name "ViewSpaceNormals"
            Tags
            {
                "LightMode" = "ViewSpaceNormals"
            }
            
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ViewSpaceNormalsVertex
            #pragma fragment ViewSpaceNormalsFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma shader_feature_local _NORMALMAP

            #include "./TerrainPbrBakeInput.hlsl"
            #include "./TerrainPbrViewSpaceNormals.hlsl"
            ENDHLSL
        }
        
        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
    }

    Dependency "AddPassShader" = "Hidden/XKnight/Scene/TerrainPbrBake (Add Pass)"
    Dependency "BaseMapShader" = "Hidden/XKnight/Scene/TerrainPbrBake (Base Pass)"
    Dependency "BaseMapGenShader" = "Hidden/XKnight/Scene/TerrainPbrBake (Basemap Gen)"

    CustomEditor "XKnight.ShaderGUI.TerrainPbrBakeShaderGUI"
}