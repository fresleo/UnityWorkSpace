Shader "XKnight/Scene/InteractiveSnow"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}

        _MetallicGlossMap ("Rough(R), AO(G) Metallic(B) Emission(A)", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _AdditionalSpecular("Additional Specular", Float) = 0.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpMixMap("BumpMix Map", 2D) = "white" {}

        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0

        _EmissionColorMap("Emission Color Map", 2D) = "White" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionColorScale("Emission Color Scale", Float) = 1.0

        _DetailMap("Detail Map", 2D) = "bump" {}

        [Toggle(_OPAQUE_BLEND)] _EarlyBlendToggle("Terrain Blend",Float) = 0.0
        _TerrainBlendFactor("Terrain Blend Factor", Range(2,50)) = 5.0

        _GIIndirectDiffuseBoost ("GI 间接光比例（漫反射）", Range(-3, 5)) = 1
        
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        
        [ToggleUI] _AlphaClip ("__clip", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        _TempPackTextureMode("3张图转2张图模式", Float) = 0.0
        
        [HideInInspector] _QueueOffset("渲染队列的偏移量", Float) = 0.0

        _BloomFactor ("Bloom系数", Range(0, 1)) = 0.0
        _WaterColorOn ("水彩开关", Range(0, 1)) = 0.0
        _SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1.0
        
        // 交互式顶点变形
        [Toggle(_IVD_ON)] _IVD_On ("开关", Float) = 0
        _IVD_Mask ("遮罩", 2D) = "white" {}
        _IVD_Noise ("噪声", 2D) = "white" {}
        _IVD_MaskIntensity ("遮罩强度", Float) = 0
        _IVD_VertexNoiseIntensity ("顶点噪声强度", Float) = 0
        _IVD_SeamTintColor ("接缝色调", Color) = (1,1,1,1)
    }

    // todo 后面看情况支持曲面细分功能
    SubShader
    {
        Tags
        {
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
            
            ZWrite [_ZWrite]
            
            Blend [_SrcBlend] [_DstBlend]
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            
            // #pragma multi_compile_fragment _ _LIGHT_COOKIES
            
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            //#pragma shader_feature_local_fragment _ _SPECULARHIGHLIGHTS_OFF
            //#pragma shader_feature_local_fragment _ _ENVIRONMENTREFLECTIONS_OFF
            
            #pragma shader_feature_local_fragment _ _EMISSION
            
            // #pragma shader_feature_local_fragment _ _DETAIL
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON

            // #pragma shader_feature_local _ _USE_PACKED_TEXTURE_MDOE
            #define _USE_PACKED_TEXTURE_MDOE
            
            #pragma shader_feature_local _ _OPAQUE_BLEND
            #pragma shader_feature_local _ _IVD_ON
            
            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./InteractiveSnowInput.hlsl"
            #include "./InteractiveSnowPass.hlsl"
            ENDHLSL
        }

        // Meta
        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
            
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment FragMeta

            // -------------------------------------
            // Pipeline keywords
            // #pragma multi_compile EDITOR_VISUALIZATION
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            
            #include "./InteractiveSnowInput.hlsl"
            #include "./InteractiveSnowMetaPass.hlsl"
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
            
            ZWrite [_ZWrite]
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./InteractiveSnowInput.hlsl"
            #include "../Common/LitDepthOnlyPass.hlsl"
            ENDHLSL
        }

        // DepthNormals
        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
            
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./InteractiveSnowInput.hlsl"
            #include "../Common/LitDepthNormalsPass.hlsl"
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

            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthMaskVertex
            #pragma fragment DepthMaskFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            
            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./InteractiveSnowInput.hlsl"
            #include "../Common/LitDepthMask.hlsl"
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

            ZWrite [_ZWrite]
            
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ViewSpaceNormalsVertex
            #pragma fragment ViewSpaceNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            
            // -------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./InteractiveSnowInput.hlsl"
            #include "../Common/LitViewSpaceNormals.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "XKnight.ShaderGUI.InteractiveSnowShaderGUI"
}