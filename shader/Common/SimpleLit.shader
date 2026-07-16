Shader "XKnight/SimpleLit"
{
    Properties
    {
        [Main(Base, __, on, off)]
        _Base ("基础设置", Float) = 1
        [Sub(Base)] [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        [Sub(Base)] [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        
        [Main(Dither, __, on)]
        _Dither ("抖动透明", Float) = 0
        [Sub(Dither)] _DitherIntensity ("抖动强度", Range(0, 1)) = 0
        [Sub(Dither)] _DitherSize ("抖动尺寸", Float) = 1
        [Sub(Dither)] _DitherAlpha ("抖动 Alpha", Range(0, 1)) = 1
        
        [Sub(Dither)] [DitherMatrixSelector] _DitherWithMatrix ("抖动矩阵", Int) = 0
        [Sub(Dither)] [DitherTextureReadOnly] _DitherTexture ("抖动图", 2D) = "black" {}
        
        [Main(Advanced, __, off, off)]
        _Advanced ("遮罩设置", Float) = 1
        [Sub(Advanced)] _BloomFactor ("Bloom系数", Range(0, 1)) = 0.0
        [Sub(Advanced)] _WaterColorOn ("水彩开关", Range(0, 1)) = 0.0
        [Sub(Advanced)] _SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1.0
        
        [HideInInspector] _Cull ("__cull", Float) = 2.0
        
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 0
    }
    
    // LOD 500 - 支持 MRT
    /*
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"
        }
        LOD 500

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Cull [_Cull]
            
            HLSLPROGRAM
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
            
            // #pragma multi_compile_fragment _ _LIGHT_COOKIES
            
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // #pragma multi_compile_fragment _ _MRT_BUFFER

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./SimpleLitInput.hlsl"
            #include "./SimpleLitForwardPass.hlsl"
            ENDHLSL
        }

        UsePass "XKnight/SimpleLit/SHADOWCASTER"
        UsePass "XKnight/SimpleLit/META"
        UsePass "XKnight/SimpleLit/DEPTHONLY"
    }
    */
    
    // LOD 400~300 - 不支持 MRT
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"
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
            
            Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]

            HLSLPROGRAM
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
            
            // #pragma multi_compile_fragment _ _LIGHT_COOKIES
            
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _DITHER_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./SimpleLitInput.hlsl"
            #include "./SimpleLitForwardPass.hlsl"
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

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
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
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./SimpleLitShadowCasterPass.hlsl"
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
            #pragma vertex UniversalVertexMeta
            #pragma fragment FragmentMeta

            #include "./SimpleLitInput.hlsl"
            #include "./SimpleLitMeta.hlsl"
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

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./SimpleLitInput.hlsl"
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
            
            ZWrite On

            HLSLPROGRAM
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
            
            #include "./SimpleLitInput.hlsl"
            #include "./SimpleLitDepthNormalsPass.hlsl"
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

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // #pragma multi_compile_fragment _BLOOMFACTORMASK _WATERCOLORMASK _SCENESPACEOUTLINEMASK

            // -------------------------------------
            // Material Keywords

            // -------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./SimpleLitInput.hlsl"
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

            ZWrite On
            
            HLSLPROGRAM
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

            #include "./SimpleLitInput.hlsl"
            #include "./LitViewSpaceNormals.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LWGUI.LWGUI"
}