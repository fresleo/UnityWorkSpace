Shader "XKnight/LitAlphaTest"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Albedo", 2D) = "white" {}

        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [ToggleOff] _SpecularHighlights ("Specular Highlights", Float) = 1
        [ToggleOff] _EnvironmentReflections ("Environment Reflections", Float) = 1
        
        _BumpScale ("Scale", Range(0, 3)) = 1
        _BumpMixMap ("Normal Map", 2D) = "white" {}
        

        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1
        _RoughnessStrength ("临时的粗糙度强度调整", Range(0, 1)) = 1
        
        // 注意： meta pass 自己的 clip 没有用，主要是靠 [MainTexture] 纹理的 A 通道来工作的，所以这个标记就不能给 _BaseMap 了
        [MainTexture] _AlphaTestMap ("Alpha 剔除遮罩", 2D) = "white" {}
        _BakerAOMap ("烘焙 AO", 2D) = "white"{}
        
        _EmissionColorMap ("Emission Color Map", 2D) = "White" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0)
        _EmissionColorScale ("Emission Color Scale", Float) = 1

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

        [Toggle(_PlANARREFLECTION)]_Enable_PlanarReflection("采样平面反射贴图",Float)=0
        [Toggle(_PlANARREFLECTION_FLIPX)]_Enable_PlanarReflection_FlipX("采样翻转后平面反射贴图",Float)=0
      
      
        //_DetailMap ("细节纹理", 2D) = "black" {}
          //Noise
        _WindowDetailMap("窗户细节纹理",2D)= "black" {}
        _NoiseUVTiling("噪声UV",Vector)=(1,1,0,0)
        _DetailFresnelRange("细节图FresnelRange",Vector)=(-1,3,0,0)
        
        // 抖动
        _DitherIntensity ("抖动强度", Range(0, 1)) = 0
        _DitherSize ("抖动尺寸", Float) = 1
        _DitherAlpha ("抖动 Alpha", Range(0, 1)) = 1
        
        _DitherWithMatrix ("抖动矩阵", Int) = 0
        _DitherTexture ("抖动图", 2D) = "black" {}
        
        // 风场
        [Toggle(_WIND_ON)] _WindOn ("Wind On", Float) = 0
        _WindVariation ("Wind Variation", Range(0, 1)) = 0.3
        _WindStrength ("Wind Strength", Range(0, 2)) = 1
        _TurbulenceStrength ("Turbulence Strength", Range(0, 2)) = 1
        
        // 玻璃
        [Toggle(_FROSTED_GLASS_ON)] _FrostedGlassOn ("FrostedGlassOn", Float) = 0
        _FrostTexture ("FrostTexture", 2D) = "white" {}
        _FrostIntensity ("FrostIntensity", Range(0, 1)) = 1
        
        [Toggle(_REFRACTION_ON)] _RefractionOn ("RefractionOn", Float) = 0
        _IOR ("Index Of Refraction", Range(1.0, 2.0)) = 1.5
        _RefractStrengthPX ("Refract Strength", Range(0.0, 100.0)) = 10.0
        
        _SpecularScaleBRDF ("镜面高光比例（BRDF）", Range(0, 1)) = 1
        _GIIndirectDiffuseBoost ("GI 间接光比例（漫反射）", Range(-3, 3)) = 1
        _BakedGITintIntensity ("TOD GI 调色强度", Range(0, 1)) = 1
        
        // MRT 遮罩
        _BloomFactor ("Bloom系数", Range(0, 1)) = 0
        _WaterColorOn ("水彩开关", Range(0, 1)) = 0
        _SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1
        
        // 表面控制
        _Surface ("__surface", Float) = 1
        _Blend ("__blend", Float) = 0
        
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 0
        [HideInInspector] _Cull ("__cull", Float) = 0.0
        
        [HideInInspector] [MaterialToggle] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _AlphaToMask ("__alphaToMask", Float) = 0
        
        [HideInInspector] [MaterialToggle] _ByTAA ("参与 TAA", Float) = 0
        [HideInInspector] _QueueOffset ("渲染队列偏移", Float) = 0
        
        [HideInInspector] _OffsetFactor ("Offset 系数", Range(-1, 1)) = 0
        [HideInInspector] _OffsetUnits ("Offset 单位", Range(-1, 1)) = 0
    }
    
    /*
    // LOD 500 - 支持 MRT
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True" 
            
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
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
            //ZWrite Off
            
            ColorMask RGBA
            Blend 0 [_SrcBlend] [_DstBlend]
            Blend 1 One Zero
            Blend 2 One Zero
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Pipeline keywords
            #pragma shader_feature _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature _ _ADDITIONAL_LIGHTS
            
            #pragma shader_feature_fragment _ _LIGHT_COOKIES

            #pragma shader_feature _ LIGHTMAP_ON
            // #pragma shader_feature _ DIRLIGHTMAP_COMBINED
            #pragma shader_feature_fragment _ SHADOWS_SHADOWMASK

            // #pragma shader_feature_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma shader_feature_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            #pragma multi_compile_fragment _ _MRT_BUFFER
            
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _DETAIL
            #pragma shader_feature_local_vertex _ _WIND_ON

            // #define _SPECULARHIGHLIGHTS_OFF
            // #define _ENVIRONMENTREFLECTIONS_OFF

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./LitAlphaTestInput.hlsl"
            #include "./LitAlphaTestForwardPass.hlsl"
            ENDHLSL
        }

        UsePass "XKnight/LitAlphaTest/SHADOWCASTER"
        UsePass "XKnight/LitAlphaTest/META"
        UsePass "XKnight/LitAlphaTest/DEPTHONLY"
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
            "Queue" = "AlphaTest"
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
            
            Cull [_Cull]
            ZWrite [_ZWrite]
            
            Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            AlphaToMask [_AlphaToMask] // 提升透明物体在 MSAA 下的抗锯齿效果
            
            Offset [_OffsetFactor], [_OffsetUnits]
            
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

            #pragma shader_feature_fragment _ _REFLECTION_PROBE_BLENDING
             #pragma shader_feature_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            // #pragma multi_compile_fragment _ _LIGHT_COOKIES
            
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
             //Refelction
            #pragma shader_feature_local_fragment _ _PlANARREFLECTION 
              #pragma shader_feature_local_fragment _ _PlANARREFLECTION_FLIPX
            // -------------------------------------
            // Material Keywords
            // #define _SPECULARHIGHLIGHTS_OFF
            // #define _ENVIRONMENTREFLECTIONS_OFF
            
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            
            #pragma shader_feature_local_fragment _ _EMISSION
            
            #pragma shader_feature_local_fragment _ _WINDOW_DETAIL
            
            #pragma shader_feature_local_vertex _ _WIND_ON

            #pragma shader_feature_local_fragment _ _DITHER_ON

            #pragma shader_feature_local_fragment _ _FROSTED_GLASS_ON
            #pragma shader_feature_local_fragment _ _REFRACTION_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./LitAlphaTestInput.hlsl"
            #include "./LitAlphaTestForwardPass.hlsl"
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

            Cull [_Cull]
            ZWrite On
            
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
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _ _WIND_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitAlphaTestInput.hlsl"
            #include "./LitAlphaTestShadowCasterPass.hlsl"
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

            Cull [_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex UniversalVertexMeta
            #pragma fragment FragmentMeta

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _EMISSION

            #include "./LitAlphaTestInput.hlsl"
            #include "./LitAlphaTestMetaPass.hlsl"
            ENDHLSL
        }

        // 溶解暂未支持，目前没需求
        /*
        Pass
        {
            Name "PreAlphaTest"
            Tags
            {
                "LightMode" = "PreAlphaTest"
            }
            
            Cull [_Cull]
            ColorMask 0
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex PreAlphaTestPassVertex
            #pragma fragment PreAlphaTestPassFragment

            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            #include "./LitAlphaTestInput.hlsl"
            #include "./LitPreAlphaTestPass.hlsl"
            ENDHLSL
        }
        */

        // MotionVectors
        /*
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectorsTransparent"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma vertex MotionVectorsVertex
            #pragma fragment MotionVectorsFragment
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #include "./LitMotionVectors.hlsl"
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
            
            Cull [_Cull]
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
            
            #include "./LitAlphaTestInput.hlsl"
            #include "./LitAlphaTestDepthOnlyPass.hlsl"
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

            Cull [_Cull]
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
            
            #include "./LitAlphaTestInput.hlsl"
            #include "./LitAlphaTestDepthNormalsPass.hlsl"
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

            Cull [_Cull]
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
            // #pragma multi_compile_fragment _BLOOMFACTORMASK _WATERCOLORMASK _SCENESPACEOUTLINEMASK

            // -------------------------------------
            // Material Keywords

            // -------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./LitAlphaTestInput.hlsl"
            #include "./LitAlphaTestDepthMaskPass.hlsl"
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

            Cull [_Cull]
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
            
            #include "./LitAlphaTestInput.hlsl"
            #include "./LitAlphaTestViewSpaceNormals.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "XKnight.ShaderGUI.LitAlphaTestShaderGUI"
}
