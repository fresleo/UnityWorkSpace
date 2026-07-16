Shader "XKnight/Scene/Tree1" 
{
	Properties
	{
        [Main(Base, __, on, off)]
        _Main("基础设置", Float) = 1
		
		[MainTexture] _BaseMap("Main Tex", 2D) = "white" {}
		[Sub(Base)] [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
		[Sub(Base)] _SSSColor("SSS Color", Color) = (1,1,1,1)
		[Sub(Base)] _TopColor("Top Color", Color) = (1,1,1,1)

		[Sub(Base)] _GIExposure("GI Exposure", Range(0.1, 4.0)) = 1.0
		[Sub(Base)] _GIFalloff("GI Falloff", Range(0, 1)) = 1
		[Sub(Base)] _AOOffset("AO Offset", Range(-0.5, 1.0)) = 0.0
		
		[Sub(Base)] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Sub(Base)] _CutOffset("Cut Offset", Range(0.0, 10.0)) = 2.0
		[Sub(Base)] _ClipEnhanceDistance("Clip EnhanceDistance", Range(1.0, 200.0)) = 10.0
		[Sub(Base)] _ClipEnhance("Clip Enhance", Range(0.0, 1.0)) = 0.5

        [Main(Wind, __, on)]
        _Wind ("风场", Float) = 1
        [Sub(Wind)] _WindVariation ("Wind Variation", Range(0, 1)) = 0.3
        [Sub(Wind)] _WindStrength ("Wind Strength", Range(0, 2)) = 1
        [Sub(Wind)] _TurbulenceStrength ("Turbulence Strength", Range(0, 2)) = 1

        [Main(Translucency, __, on)]
        _Translucency("透射设置", Float) = 1
        [Sub(Translucency)] _TranslucencyStrength ("Translucency Strength", Range(0, 2)) = 1
        [Sub(Translucency)] _TranslucencyDistortion ("Translucency Distortion", Range(0, 1)) = 0.5
        [Sub(Translucency)] _TranslucencyScattering ("Translucency Scattering", Range(0.01, 3)) = 2
		
//		[Sub(Translucency)] _TranslucencyFakeColor ("Translucency Fake Color", Color) = (1, 1, 1, 1)
//		[Sub(Translucency)] _TranslucencyFakeLightIntensity ("Translucency Fake Light Intensity", Range(0.01, 3.0)) = 1.0
//		[Sub(Translucency)] _TranslucencyFakeLightFalloff ("Translucency Fake Light Falloff", Range(0.1, 10.0)) = 4.0
		
		// 用于做半透
		[HideInInspector] _Alpha("Alpha", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
		
		[Main(Mask, __, off, off)]
        _Mask("遮罩设置", Float) = 1
		
		[Sub(Mask)] _BloomFactor ("Bloom系数", Range(0, 1)) = 0.0
		[Sub(Mask)] _WaterColorOn ("水彩开关", Range(0, 1)) = 0.0
		[Sub(Mask)] _SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1.0
	}
	
	// LOD 500
	/*
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Opaque" "Queue" = "AlphaTest+3" }
		LOD 500
		
		// ForwardLit
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Cull Off
			//ZWrite Off
			
			Blend 0 [_SrcBlend] [_DstBlend]
			Blend 1 One Zero
			Blend 2 One Zero

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

			// -------------------------------------
			// Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			
			#pragma multi_compile _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY
			
			// #pragma multi_compile_fragment _ _MRT_BUFFER

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ _TRANSLUCENCY_ON
			#pragma shader_feature_local_vertex _ _WIND_ON
			
			//--------------------------------------
			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "./Tree1Input.hlsl"
			#include "./Tree1ForwardPass.hlsl"
			ENDHLSL
		}

		// ShadowCaster
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			Cull Off
			ColorMask 0

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
			// Material Keywords
			#pragma multi_compile_local_vertex _ _WIND_ON

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "./Tree1Input.hlsl"
			#include "./Tree0ShadowCasterPass.hlsl"
			ENDHLSL
		}

		// DepthOnly
		Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
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
            
            #include "./Tree0Input.hlsl"
            #include "./Tree0DepthOnlyPass.hlsl"
            ENDHLSL
        }
	}
	*/

	// LOD 400~300 - 不支持 MRT
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Opaque" "Queue" = "AlphaTest+3" }
		LOD 300
		
		// ForwardLit
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Cull Off
			//ZWrite Off
			
			Blend [_SrcBlend] [_DstBlend]

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

			// -------------------------------------
			// Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			
			#pragma multi_compile _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ _TRANSLUCENCY_ON
			#pragma shader_feature_local_vertex _ _WIND_ON
			
			//--------------------------------------
			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "./Tree1Input.hlsl"
			#include "./Tree1ForwardPass.hlsl"
			ENDHLSL
		}

		// ShadowCaster
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			Cull Off
			ColorMask 0

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
			// Material Keywords
			#pragma multi_compile_local_vertex _ _WIND_ON

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "./Tree1Input.hlsl"
			#include "./Tree0ShadowCasterPass.hlsl"
			ENDHLSL
		}
		
		// DepthOnly
		Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
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
            
            #include "./Tree0Input.hlsl"
            #include "./Tree0DepthOnlyPass.hlsl"
            ENDHLSL
        }
		
		// DepthNormals
		// 树叶子的 GI 效果很奇怪，所以先不让它参与场景法线
		// This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./Tree0Input.hlsl"
            #include "./Tree0DepthNormalsPass.hlsl"
            ENDHLSL
        }

		// DepthMask
		Pass
		{
			Name "DepthMask"
			Tags { "LightMode" = "DepthMask" }
			
			Cull Off
			
			HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthMaskVertex
            #pragma fragment DepthMaskFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // #pragma multi_compile_fragment _BLOOMFACTORMASK _WATERCOLORMASK _SCENESPACEOUTLINEMASK

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./Tree0Input.hlsl"
            #include "./Tree0DepthMask.hlsl"
            ENDHLSL
		}

		// ViewSpaceNormals
		Pass
		{
			Name "ViewSpaceNormals"
			Tags { "LightMode" = "ViewSpaceNormals" }
			
			Cull Off
			
			HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ViewSpaceNormalsVertex
            #pragma fragment ViewSpaceNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./Tree0Input.hlsl"
            #include "./Tree0ViewSpaceNormals.hlsl"
            ENDHLSL
		}
	}

	CustomEditor "LWGUI.LWGUI"
}
