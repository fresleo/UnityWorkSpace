Shader "XKnight/Scene/Grass"
{
    Properties
    {
        [Main(Base, __, on, off)]
        _Base ("基础设置", Float) = 1

        [Sub(Base)] _MainTex ("MainTex", 2D) = "white" {}
        [Sub(Base)] _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        [Sub(Base)] _SpecularColor2("Specular2 Color", Color) = (1, 1, 1, 1)
        [Sub(Base)] _AOStrength("AO Strength", Range(0, 1)) = 0
        [Sub(Base)] _PersectiveCorrection("Persective Correction Strength", Range(0, 1)) = 0
        
        [SubToggle(Base, __)] _Intersection("Intersection", Float) = 1
        [Sub(Base)] _GIIntensity("GI Intensity", Range(1, 3)) = 1

        [Main(VariationColor, _, off, off)]
        _VariationColor("杂色", Float) = 0
        [Sub(VariationColor)] _VariationMask("Variation Mask", 2D) = "white" {}
        [Sub(VariationColor)] _VariationMaskScale("Variation Scale", Float) = 50
        [Sub(VariationColor)] _VariationColorA("Variation Color A", Color) = (1,1,1,1)
        [Sub(VariationColor)] _VariationColorB("Variation Color B", Color) = (1,1,1,1)

        [Main(WindLine, __, on)]
        _WindLine("风线", Float) = 0
        [Sub(WindLine)] _WindLineTexture("Wind Line Texture", 2D) = "white" {}
        [Sub(WindLine)] _WindLineColor("WindLine Color", Color) = (1,1,1,1)
        [Sub(WindLine)] _WindLineScale("Wind Line Scale", Float) = 50
        [Sub(WindLine)] _WindLineLocalStrength("Wind Line Local Strength", Float) = 1

        [Main(Wind, __, on)]
        _Wind ("风场", Float) = 1
        [Sub(Wind)] _WindVariation ("Wind Variation", Range(0, 1)) = 0.3
        [Sub(Wind)] _WindStrength ("Wind Strength", Range(0, 2)) = 1
        [Sub(Wind)] _TurbulenceStrength ("Turbulence Strength", Range(0, 2)) = 1
        
        [Main(BlendWithTerrain, __, off)]
        _BlendWithTerrain("地形混合", Float) = 1
        [Sub(BlendWithTerrain)] _BlendWithTerrainStrength("Blend Strength", Range(0.0, 1.0)) = 0.0
        [Sub(BlendWithTerrain)] _BlendWithTerrainHeight("Blend Height", Range(0.0, 1.0)) = 0.0
        [Sub(BlendWithTerrain)] _BBlendWithTerrainStrength("B Blend Strength", Range(0.0, 1.0)) = 1.0
        [Sub(BlendWithTerrain)] _BBlendWithTerrainHeight("B Blend Height", Range(0.0, 1.0)) = 1.0

        [Main(Mask, __, off, off)]
        _Mask("遮罩设置", Float) = 1
        
        [Sub(Mask)] _BloomFactor ("Bloom系数", Range(0, 1)) = 0.0
        [Sub(Mask)] _WaterColorOn ("水彩开关", Range(0, 1)) = 0.0
        [Sub(Mask)] _SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1.0
        
        [Main(Dither, __, on)]
        _Dither ("抖动透明", Float) = 0
        [Sub(Dither)] _DitherIntensity ("抖动强度", Range(0, 1)) = 0
        [Sub(Dither)] _DitherSize ("抖动尺寸", Float) = 1
        [Sub(Dither)] _DitherAlpha ("抖动 Alpha", Range(0, 1)) = 1
        
        [Sub(Dither)] [DitherMatrixSelector] _DitherWithMatrix ("抖动矩阵", Int) = 0
        [Sub(Dither)] [DitherTextureReadOnly] _DitherTexture ("抖动图", 2D) = "black" {}
        
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 0
    }

    // LOD 500
    /*
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+20"
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

            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // #pragma multi_compile _ _GLOBAL_RAIN_ON
            
            // #pragma multi_compile_fragment _ _MRT_BUFFER

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _ _WIND_ON
            #pragma shader_feature_local_vertex _ _INTERSECTION_ON
            #pragma shader_feature_local_fragment _ _BLENDWITHTERRAIN_ON

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./GrassInput.hlsl"
            #include "./GrassForwardPass.hlsl"
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

            Cull Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./GrassInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthOnlyPass_Simple.hlsl"
            ENDHLSL
        }
    }
    */
    
    // LOD 400
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+20"
        }
        LOD 400

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Off
            
            Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // #pragma multi_compile _ _GLOBAL_RAIN_ON
            
            #pragma multi_compile _ _EXCLUDE_CHARACTER_ON

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _ _WIND_ON
            #pragma shader_feature_local_vertex _ _INTERSECTION_ON
            #pragma shader_feature_local_fragment _ _BLENDWITHTERRAIN_ON
            #pragma shader_feature_local_fragment _ _DITHER_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup assumeuniformscaling
            #pragma multi_compile _ _MESH_INSTANCE_CULL_ON
            #pragma multi_compile _ _MESH_INSTANCE_TEX_FETCH_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./GrassInput.hlsl"
            #include "./GrassForwardPass.hlsl"
            ENDHLSL
        }
        
        UsePass "XKnight/Scene/Grass/DEPTHONLY"
        UsePass "XKnight/Scene/Grass/DEPTHNORMALS"
        UsePass "XKnight/Scene/Grass/DEPTHMASK"
        UsePass "XKnight/Scene/Grass/VIEWSPACENORMALS"
    }

    // LOD 300
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+20"
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

            Cull Off
            
            Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY

            // #pragma multi_compile _ _GLOBAL_RAIN_ON
            
            #pragma multi_compile _ _EXCLUDE_CHARACTER_ON

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _ _WIND_ON
            #pragma shader_feature_local_vertex _ _INTERSECTION_ON
            #pragma shader_feature_local_fragment _ _BLENDWITHTERRAIN_ON
            #pragma shader_feature_local_fragment _ _DITHER_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup assumeuniformscaling
            #pragma multi_compile _ _MESH_INSTANCE_CULL_ON
            #pragma multi_compile _ _MESH_INSTANCE_TEX_FETCH_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./GrassInput.hlsl"
            #include "./GrassForwardPassLod1.hlsl"
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
            
            Cull Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            
            // -------------------------------------
            // Pipeline keywords
            #pragma multi_compile _ _DISABLE_DEPTHONLY

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _ _WIND_ON
            #pragma shader_feature_local_vertex _ _INTERSECTION_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup assumeuniformscaling
            #pragma multi_compile _ _MESH_INSTANCE_CULL_ON
            #pragma multi_compile _ _MESH_INSTANCE_TEX_FETCH_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./GrassInput.hlsl"
            #include "./GrassDepthOnlyPass.hlsl"
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

            Cull Off

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
            #pragma instancing_options procedural:setup assumeuniformscaling
            #pragma multi_compile _ _MESH_INSTANCE_CULL_ON
            #pragma multi_compile _ _MESH_INSTANCE_TEX_FETCH_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./GrassInput.hlsl"
            #include "./GrassDepthNormalsPass.hlsl"
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

            Cull Off

            HLSLPROGRAM
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup assumeuniformscaling
            #pragma multi_compile _ _MESH_INSTANCE_CULL_ON
            #pragma multi_compile _ _MESH_INSTANCE_TEX_FETCH_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./GrassInput.hlsl"
            #include "./GrassDepthMaskPass.hlsl"
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

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ViewSpaceNormalsVertex
            #pragma fragment ViewSpaceNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Material Keywords

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup assumeuniformscaling
            #pragma multi_compile _ _MESH_INSTANCE_CULL_ON
            #pragma multi_compile _ _MESH_INSTANCE_TEX_FETCH_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "./GrassInput.hlsl"
            #include "./GrassViewSpaceNormalsPass.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "LWGUI.LWGUI"
}
