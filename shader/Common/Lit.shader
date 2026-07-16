Shader "XKnight/Lit"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}

        //_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _MetallicGlossMap ("Rough(R), AO(G) Metallic(B) Emission(A)", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights ("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections ("Environment Reflections", Float) = 1.0
        [ToggleOff] _AdditionalSpecular ("Additional Specular", Float) = 0.0

        _BumpScale ("Scale", Float) = 1.0
        _BumpMap ("Normal Map", 2D) = "bump" {}

        _BumpMixMap ("BumpMix Map", 2D) = "white" {}

        _OcclusionStrength ("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _RoughnessStrength ("临时的粗糙度强度调整", Range(0.0, 1.0)) = 1.0
        
        _BakerAOMap ("Baker AO Map", 2D) = "white"{}
        _BakerAOMapScale ("Baker AO Map 缩放", Range(0, 1)) = 1
        
        // 自发光
        _EmissionColorMap ("自发光颜色贴图", 2D) = "White" {}
        [HDR] _EmissionColor ("自发光颜色", Color) = (0, 0, 0)
        _EmissionColorScale ("自发光颜色系数", Float) = 1.0
        
        // 不同时态的自发光
        _EmissionTOD ("通过 TOD 来控制的自发光", Float) = 0
        
        [HDR] _EmissionColor_1 ("自发光颜色 - 清晨", Color) = (0, 0, 0)
        _EmissionColorScale_1 ("自发光颜色系数 - 清晨", Float) = 1
        [HDR] _EmissionColor_2 ("自发光颜色 - 白天", Color) = (0, 0, 0)
        _EmissionColorScale_2 ("自发光颜色系数 - 白天", Float) = 1
        [HDR] _EmissionColor_3 ("自发光颜色 - 黄昏", Color) = (0, 0, 0)
        _EmissionColorScale_3 ("自发光颜色系数 - 黄昏", Float) = 1
        [HDR] _EmissionColor_4 ("自发光颜色 - 夜晚", Color) = (0, 0, 0)
        _EmissionColorScale_4 ("自发光颜色系数 - 夜晚", Float) = 1

        // _DetailMap ("细节纹理", 2D) = "bump" {}

        [Toggle(_OPAQUE_BLEND)] _EarlyBlendToggle ("Terrain Blend", Float) = 0.0
        _TerrainBlendFactor ("Terrain Blend Factor", Range(2, 50)) = 5.0

        [Toggle] _LocalRainToggle ("Local Rain", Float) = 0.0

        // Dissolve
        _DissolveType ("Dissolve Type", Float) = 0
        _Dissolve ("Dissolve", Float) = 0
        _Random_Dissolve ("Random Dissolve", Int) = 0
        _DissolveTex ("Dissolve Texture", 2D) = "white" {}
        _DissolveTexChannel ("Dissolve Texture Channel", Vector) = (1, 0, 0, 0)
        _DissolveFadingMin ("Dissolve Fading Min", Range(0, 1.0)) = 0
        _DissolveFadingMax ("Dissolve Fading Max", Range(0, 1.0)) = 0.2
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.1
        [HDR] _EdgeColor1 ("Edge Color1", Color) = (1, 0, 0, 1)
        [HDR] _EdgeColor2 ("Edge Color2", Color) = (0, 1, 0, 1)
        _DissolveCutoff ("Dissolve Cutoff", Range(-1, 2)) = 0.5
        _DissolveDir ("DirectionDir", Vector) = (0, 1, 0)
        
        // 抖动
        _DitherIntensity ("抖动强度", Range(0, 1)) = 0
        _DitherSize ("抖动尺寸", Float) = 1
        _DitherAlpha ("抖动 Alpha", Range(0, 1)) = 1
        
        _DitherWithMatrix ("抖动矩阵", Int) = 0
        _DitherTexture ("抖动图", 2D) = "black" {}
        
        // 雨表面 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        [Toggle] _NewPuddles ("开启新水坑", Float) = 0.0
        
        // 范围遮罩
        _RainMask ("坑的范围遮罩", 2D) = "black" {}
        [Toggle] _RainMaskInvert ("反转遮罩", Float) = 0
        [Toggle] _RainExcludeBaseMapAlpha ("排除 BaseMap 的 Alpha", Float) = 0
        _RainMaskIntensity ("遮罩强度", Range(0, 1)) = 1
        _RainMaskContrast ("遮罩对比度", Float) = 0
        _RainMaskSpread ("遮罩扩散", Range(0, 1)) = 0.5
        
        // 反射
        [HDR] _RainCubemap ("雨的反射 Cubemap", CUBE) = "black" {}
        _RainCubemapColor ("雨的反射调色", Color) = (1, 1, 1, 1)
        _RainReflectionIntensity ("反射强度", Range(0, 10)) = 0.2
        _RainBlurReflectionFactor ("模糊反射采样", Range(0, 7)) = 0.5
        [Toggle] _UseMainNormalToRainNormal ("用主法线作为雨的法线", Float) = 1
        
        //SSPR
        [Toggle(_PlANARREFLECTION)]_Enable_PlanarReflection("采样平面反射贴图",Float)=0
     
        
        // 水坑的属性
        _WaveNormalMap ("波浪的法线", 2D) = "bump" {}
        [Toggle] _PuddleBlendMainNormal ("混合主法线", Float) = 0
        
        _PuddleColor ("水坑调色", Color) = (0.5019608, 0.5019608, 0.5019608, 0)
        _PuddleMetallic ("水坑金属性", Range( 0 , 2)) = 0
        _PuddleGlossiness ("水坑光泽度", Range( 0 , 2)) = 0.95
        
        [Toggle] _MainWave ("主波浪", Float) = 1
        _NormalWaveIntensity1 ("主波浪 - 强度", Float) = 0.5
        _TranslationSpeed1 ("主波浪 - 平移速度", Float) = 1
        _RotationAngle1 ("主波浪 - 旋转", Float) = 0
        _TilingWave1 ("主波浪 - Tiling", Float) = 1
        
        [Toggle] _DetailWave ("副波浪", Float) = 1
        _NormalWaveIntensity2 ("副波浪 - 强度", Float) = 0.3
        _TranslationSpeed2 ("副波浪 - 平移速度", Float) = 1
        _RotationAngle2 ("副波浪 - 旋转", Float) = 90
        _TilingWave2 ("副波浪 - Tiling", Float) = 1
        
        // 雨点属性
        _RainDotsGradientTex ("雨点渐变纹理", 2D) = "white" {}
        _RainDotsIntensity ("雨点强度", Range(0, 1)) = 0
        _RainDotsTiling ("Tiling", Float) = 100
        _RainDotsSplashSpeed ("雨点飞溅速度", Range(0, 1)) = 0.1
        _RainDotsSize ("雨点尺寸", Range(0, 1)) = 0.5
        
        // 涟漪属性
        _XColumnsYRowsZSpeedWStrartFrameNormal ("图集翻页: X(列) - Y(行) - Z(速度) - W(开始帧) 法线", Vector) = (8, 8, 0.25, 0)
        _RainRipplesAtlasNormal ("雨滴的涟漪法线图集", 2D) = "bump" {}
        _FlipBTilingNormal ("翻页的 Tiling", Float) = 1
        _IntensityScaleNormal1 ("强度", Range(0 , 1)) = 0.4
        [Toggle] _DuplicateRainDotsNormalAtlas ("重复采样雨滴法线图集", Float) = 1
        _IntensityScaleNormal2 ("强度", Range(0 , 1)) = 0.3
        
        _ScaleFBDetailsNormal ("比例", Float) = 1
        _FBDetailsNormal ("旋转细节", Float) = 45
        _OffsetFBDetailsNormal ("偏移 (XY)", Vector) = (1.5, 1.5, 0, 0)
        _Distortion ("畸变", Range(0, 1)) = 0
        [Toggle] _UseAoFromMainProperties ("使用主属性中的 AO", Float) = 1
        [Toggle] _UseEmissionFromMainProperties ("使用主属性中的自发光", Float) = 1
        // 雨表面 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        
        _SpecularScaleBRDF ("镜面高光比例（BRDF）", Range(0, 1)) = 1
        _GIIndirectDiffuseBoost ("GI 间接光比例（漫反射）", Range(-3, 3)) = 1
        
        [HDR] _GIBakingAlbedoColor ("烘培时附加的Albedo颜色, a用来和原始效果Lerp", Color) = (1, 1, 1, 1)
        _GIBakingSpecularScale ("烘培时Albedo的添加的高光比例", Range(0, 1)) = 0.5
        
        _BakedGITintIntensity ("TOD GI 调色强度", Range(0, 1)) = 1
        [Toggle(_RECEIVE_SHADOWS_OFF)] _ReceiveShadowsOff ("不接收实时阴影", Range(0, 1)) = 0
        
        [Toggle(_BAKED_SPECULARHIGHLIGHTS)] _AHDBakedSpecularHighlights ("启用 AHD 烘焙高光", Float) = 0
        _AHDBakedSpecularScale ("强度比例", Range(-20, 20)) = 0
        _AHDBakedSpecularDirectionBlur ("运行时采样模糊半径", Range(0, 4)) = 0
        _AHDBakedSpecularRougheningMaxAmount ("低置信度最大粗糙化", Range(-1, 1)) = 0
        _AHDBakedSpecularStrengthGateMin ("方向强度门限下限", Range(-0.3, 0.3)) = 0
        _AHDBakedSpecularStrengthGateMax ("方向强度门限上限", Range(-0.5, 0.5)) = 0
        _AHDBakedSpecularRougheningConfidenceMin ("粗糙化置信度下限", Range(-0.6, 0.6)) = 0
        _AHDBakedSpecularRougheningConfidenceMax ("粗糙化置信度上限", Range(-1, 1)) = 0
        
        [MaterialToggle] _MainLightSpecularSoftClamp ("主灯高光软钳制", Float) = 0
        _MainLightMinPerceptualRoughness ("主灯最小感知粗糙度", Range(0, 1)) = 0.18
        [MaterialToggle] _MainLightClampPreserveBaseMapAlpha ("用 BaseMap.A 当保留高光的遮罩", Float) = 0
        [MaterialToggle] _MainLightClampPreserveBaseMapAlphaInvert ("反转遮罩", Float) = 0
        _MainLightClampPreserveBaseMapAlphaThreshold ("保留阈值", Range(0, 1)) = 0.5

        // 用于做半透
        [HideInInspector] _Surface ("__surface", Float) = 0.0
        
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 0
        
        [HideInInspector] _Cull ("__cull", Float) = 2.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        
        [HideInInspector] _OffsetFactor ("Offset 系数", Range(-1, 1)) = 0
        [HideInInspector] _OffsetUnits ("Offset 单位", Range(-1, 1)) = 0

        _TempPackTextureMode ("合并纹理模式", Float) = 0.0

        [ToggleUI] _ReceiveShadows ("是否接收阴影", Float) = 1.0
        
        [HideInInspector] _QueueOffset ("渲染队列偏移", Int) = 0

        _BloomFactor ("Bloom系数", Range(0, 1)) = 0.0
        _WaterColorOn ("水彩开关", Range(0, 1)) = 0.0
        _SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1.0
    }
    
    // LOD 500 - 支持 MRT
    /*
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            
            "RenderType" = "Opaque"
        }
        LOD 500
        
        Offset [_OffsetFactor], [_OffsetUnits]
        
        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Cull [_Cull]
            ZWrite [_ZWrite]
            
            ColorMask RGBA
            //ColorMask RGBA 0
            //ColorMask RGBA 1
            Blend 0 [_SrcBlend] [_DstBlend]
            Blend 1 One Zero
            Blend 2 One Zero
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma shader_feature_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            //#pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Pipeline keywords
            #pragma shader_feature _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature _ _ADDITIONAL_LIGHTS
            
            #pragma shader_feature_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma shader_feature_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #pragma shader_feature_fragment _ _LIGHT_COOKIES

            // MRT
            #pragma multi_compile_fragment _ _MRT_BUFFER

            // 高度指数雾
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // 随机消融
            #pragma shader_feature _ _RANDOM_DISSOLVE_ON
            // 雨的全局开关
            #pragma shader_feature _ _GLOBAL_RAIN_ON

            // 灯光分层
            // #pragma multi_compile _ _LIGHT_LAYERS
            // forward+
            // #pragma multi_compile _ _FORWARD_PLUS
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            
            // -------------------------------------
            // Material Keywords
            //#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            
            #pragma shader_feature_local_fragment _ _EMISSION
            
            // #pragma shader_feature_local_fragment _ _DETAIL
            
            #pragma shader_feature_local_fragment _ _LOCAL_RAIN_ON
            #pragma shader_feature_local _ _OPAQUE_BLEND
            
            // #pragma shader_feature_local _ _USE_PACKED_TEXTURE_MDOE
            #define _USE_PACKED_TEXTURE_MDOE
            
            //--------------------------------------
            #pragma multi_compile_instancing
            // #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitInput.hlsl"
            #include "./LitForwardPass.hlsl"
            ENDHLSL
        }

        UsePass "XKnight/Lit/SHADOWCASTER"
        UsePass "XKnight/Lit/META"
        UsePass "XKnight/Lit/DEPTHONLY"
    }
    */
    
    // LOD 300~400 - 不支持 MRT
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            
            "RenderType" = "Opaque"
        }
        LOD 300
        
        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Offset [_OffsetFactor], [_OffsetUnits]
            
            Cull [_Cull]
            ZWrite [_ZWrite]
            
            Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_MEDIUM

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            //#pragma multi_compile_fragment _ _LIGHT_COOKIES

            // 高度指数雾
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // #pragma multi_compile _ _GLOBAL_RAIN_ON // 雨的全局开关

            // dx11, vulkan 才能享受到的高级功能
            // #pragma multi_compile _ _LIGHT_LAYERS
            // #pragma multi_compile _ _FORWARD_PLUS
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            
            #pragma multi_compile _ _BLEND_VOLUME_COLOR

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            
            #pragma shader_feature_local_fragment _ _EMISSION
            
            #pragma shader_feature_local_fragment _ _BAKED_SPECULARHIGHLIGHTS
            
            // #pragma shader_feature_local_fragment _ _LOCAL_RAIN_ON // 雨的本地开关
            // #pragma shader_feature_local _ _OPAQUE_BLEND // 地形混合

            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON // 消融

            // #pragma shader_feature_local_fragment _ _DETAIL
            
            // #pragma shader_feature_local _ _USE_PACKED_TEXTURE_MDOE
            #define _USE_PACKED_TEXTURE_MDOE

            #pragma shader_feature_local_fragment _ _DITHER_ON
            
            //Refelction
            #pragma shader_feature_local_fragment _ _PlANARREFLECTION
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            // #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitInput.hlsl"
            #include "./LitForwardPass.hlsl"
            ENDHLSL
        }

        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // 这在阴影贴图生成期间用于区分定向和精准的灯光阴影，因为它们使用不同的公式来应用 Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON // 消融

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitInput.hlsl"
            #include "./LitShadowCasterPass.hlsl"
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
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex UniversalVertexMeta
            #pragma fragment FragmentMeta

            // -------------------------------------
            // Pipeline keywords
            // #pragma shader_feature EDITOR_VISUALIZATION
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _EMISSION
            
            // #pragma shader_feature_local _DETAIL
            
            #include "./LitInput.hlsl"
            #include "./LitMetaPass.hlsl"
            ENDHLSL
        }
        
        // MotionVectors
        /*
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma vertex MotionVectorsVertex
            #pragma fragment MotionVectorsFragment

            // -------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include_with_pragmas "./LitMotionVectors.hlsl"
            ENDHLSL
        }
        */

        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            
            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Material Keywords
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitInput.hlsl"
            #include "./LitDepthOnlyPass.hlsl"
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

            Cull Back
            ZWrite On
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Material Keywords

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitInput.hlsl"
            #include "./LitDepthNormalsPass.hlsl"
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

            Cull Back
            ZWrite On
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex DepthMaskVertex
            #pragma fragment DepthMaskFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Pipeline keywords
            // 通道控制
            // #pragma multi_compile_fragment _BLOOMFACTORMASK _WATERCOLORMASK _SCENESPACEOUTLINEMASK
            
            // -------------------------------------
            // Material Keywords
            
            // -------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitInput.hlsl"
            #include "./LitDepthMask.hlsl"
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

            Cull Back
            ZWrite On
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex ViewSpaceNormalsVertex
            #pragma fragment ViewSpaceNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Material Keywords
            
            // -------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitInput.hlsl"
            #include "./LitViewSpaceNormals.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "XKnight.ShaderGUI.LitShaderNew"
}
