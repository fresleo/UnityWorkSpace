Shader "XKnight/Scene/Tree" 
{
	Properties
	{
        [Main(Base, __, on, off)]
        _Base ("基础设置", Float) = 1
		
		[Sub(Base)] [MainColor] _BaseColor ("主颜色", Color) = (1, 1, 1, 1)
		[Sub(Base)] [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
		
		[SubToggle(Base, __)] _VertexColor ("开启顶点色", float) = 0
		[Sub(Base)] _AlbedoExposure ("AlbedoExposure", Range(0.25, 2)) = 1
		
		[Sub(Base)] _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

		[Sub(Base)] _AOStrength ("AO强度", Range(0, 1)) = 1
		[Sub(Base)] _AOWitoutSSSStrength ("没透射时AO强度", Range(0, 1)) = 1
		
        [Main(Wind, __, on)]
        _Wind ("风场", Float) = 1
        [Sub(Wind)] _WindVariation ("风的变化", Range(0, 1)) = 0.3
        [Sub(Wind)] _WindStrength ("风的强度", Range(0, 2)) = 1
        [Sub(Wind)] _TurbulenceStrength ("湍流强度", Range(0, 2)) = 1

        [Main(Translucency, __, on)]
        _Translucency ("透射设置", Float) = 1
        [Sub(Translucency)] _TranslucencyStrength ("Translucency Strength", Range(0, 2)) = 1
        [Sub(Translucency)] _TranslucencyDistortion ("Translucency Distortion", Range(0, 1)) = 0.5
        [Sub(Translucency)] _TranslucencyScattering ("Translucency Scattering", Range(0.01, 3)) = 2
        [Sub(Translucency)] _TranslucencyAmbient ("Translucency Ambient", Range(0, 1)) = 0.5
        [Sub(Translucency)] _TranslucencyShadow ("Translucency Shadow", Range(0, 1)) = 0.8
		
		[Sub(Translucency)] _TranslucencyFakeColor ("Translucency Fake Color", Color) = (1, 1, 1, 1)
		[Sub(Translucency)] _TranslucencyFakeLightIntensity ("Translucency Fake Light Intensity", Range(0.01, 3)) = 1
		[Sub(Translucency)] _TranslucencyFakeLightFalloff ("Translucency Fake Light Falloff", Range(0.1, 10)) = 4
		
		[Main(Mask, __, off, off)]
        _Mask ("遮罩设置", Float) = 1
		
		[Sub(Mask)] _BloomFactor ("Bloom系数", Range(0, 1)) = 0
		[Sub(Mask)] _WaterColorOn ("水彩开关", Range(0, 1)) = 0
		[Sub(Mask)] _SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1
		
		[Main(Colorful, __, on)]
		_Colorful ("颜色化", Float) = 0
		[Sub(Colorful)] _TopColor ("顶部颜色", Color) = (0.7137, 1, 0.776, 1)
		[Sub(Colorful)] _BottomColor ("底部颜色", Color) = (0.568, 0.996, 0.9764, 1)
		[Sub(Colorful)] _ShadowColor ("阴影颜色", Color) = (0.890, 0.827, 0.980, 1)
        [Sub(Colorful)] _ShadowRange ("阴影阈值", Range(0, 1)) = 0.454
        [Sub(Colorful)] _ShadowSmooth ("阴影平滑", Range(0, 0.1)) = 0.0252
		
        [Main(Rim, __, on)]
        _Rim ("边缘光", Float) = 0
        [Sub(Rim)] _RimThreshold ("边缘光阈值", float) = 2.19
        [Sub(Rim)] _RimOffset ("边缘光偏移", float) = 5.29
//      _RimWidth ("边缘光宽度", Range(0, 1.2)) = 0.012
		
		[Sub(Rim)] [RimNonTODParameter] _RimColor ("边缘光颜色", Color) = (1, 1, 1, 1)
        [Sub(Rim)] [RimNonTODParameter] _RimIntensity ("边缘光强度", Range(0, 10)) = 2.29
		
		[Sub(Rim)] [RimTOD] _RimTOD ("通过 TOD 来控制边缘光", Float) = 0
		
		[Sub(Rim)] [RimTODParameter(morning)] _RimColor_1 ("边缘光颜色", Color) = (0, 0, 0, 1)
		[Sub(Rim)] [RimTODParameter] _RimIntensity_1 ("边缘光强度", Range(0, 10)) = 2.29
		
		[Sub(Rim)] [RimTODParameter(daytime)] _RimColor_2 ("边缘光颜色", Color) = (0, 0, 0, 1)
		[Sub(Rim)] [RimTODParameter] _RimIntensity_2 ("边缘光强度", Range(0, 10)) = 2.29
		
		[Sub(Rim)] [RimTODParameter(nightfall)] _RimColor_3 ("边缘光颜色", Color) = (0, 0, 0, 1)
		[Sub(Rim)] [RimTODParameter] _RimIntensity_3 ("边缘光强度", Range(0, 10)) = 2.29
		
		[Sub(Rim)] [RimTODParameter(night)] _RimColor_4 ("边缘光颜色", Color) = (0, 0, 0, 1)
		[Sub(Rim)] [RimTODParameter] _RimIntensity_4 ("边缘光强度", Range(0, 10)) = 2.29
		
		[Main(Thecut, __, on)]
        _Thecut ("插片感削弱", Float) = 0
		[Sub(Thecut)] _ThecutAngle ("插片虚化裁剪角度", Range(0, 90)) = 0
        [Sub(Thecut)] _ThecutSmoothness ("插片虚化裁剪平滑", Range(0.01, 1)) = 1
        [Sub(Thecut)] _ThecutStrength ("插片虚化裁剪强度", Range(0, 1)) = 1
//		[SubToggle(TThecut,_TOGGLE_KEYWORD)] _IfDistance ("是否开启距离裁剪", float) = 0
//      [Sub(TThecut)] _thecutDistnce ("裁剪相机距离", float) = 30
		
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
			#pragma target 4.5
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

			#include "./Tree0Input.hlsl"
			#include "./Tree0ForwardPass.hlsl"
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
			#pragma target 4.5
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

			#include "./Tree0Input.hlsl"
			#include "./Tree0ShadowCasterPass.hlsl"
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
            
            #include "./Tree0Input.hlsl"
            #include "./Tree0DepthOnlyPass.hlsl"
            ENDHLSL
        }
	}
	*/
	
	// LOD 400
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Opaque" "Queue" = "AlphaTest+3" }
		LOD 400
		
		// ForwardLit
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Cull Off
			//ZWrite Off
			
			Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]

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
			#pragma shader_feature_local_fragment _ _RIM_ON
			#pragma shader_feature_local_fragment _ _COLORFUL_ON
			#pragma shader_feature_local_vertex _ _THECUT_ON
			#pragma shader_feature_local_fragment _ _DITHER_ON
			
			//--------------------------------------
			// GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "./Tree0Input.hlsl"
			#include "./Tree0ForwardPass.hlsl"
			ENDHLSL
		}

		UsePass "XKnight/Scene/Tree/SHADOWCASTER"
		UsePass "XKnight/Scene/Tree/DEPTHONLY"
		UsePass "XKnight/Scene/Tree/DEPTHNORMALS"
		UsePass "XKnight/Scene/Tree/DEPTHMASK"
		UsePass "XKnight/Scene/Tree/VIEWSPACENORMALS"
	}

	// LOD 300
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
			
			Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]

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
            #pragma shader_feature_local_fragment _ _DITHER_ON
			
			//--------------------------------------
			// GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "./Tree0Input.hlsl"
			#include "./Tree0ForwardPassLod1.hlsl"
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
			#pragma shader_feature_local_vertex _ _WIND_ON

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "./Tree0Input.hlsl"
			#include "./Tree0ShadowCasterPass.hlsl"
			ENDHLSL
		}

		// PreAlphaTest
		/*
		Pass
		{
			Name "PreAlphaTest"
			Tags{ "LightMode" = "PreAlphaTest" }

			Cull Off
			ColorMask 0

			HLSLPROGRAM

			#define _TYPE_PLANT_ON

			#pragma multi_compile_local_vertex _ _WIND_ON

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Wind.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4	_BaseMap_ST;
			half4	_BaseColor;
			half	_Cutoff;
			half    _Alpha;
			half	_AlbedoExposure;
			half    _AOStrength;
			half	_AOWitoutSSSStrength;

			float _TranslucencyStrength;
			float _TranslucencyDistortion;
			float _TranslucencyScattering;
			float _TranslucencyAmbient;
			float _TranslucencyShadow;

			half4 _TranslucencyFakeColor;
			half _TranslucencyFakeLightIntensity;
			half _TranslucencyFakeLightFalloff;

			float _WindVariation;
			float _WindStrength;
			float _TurbulenceStrength;
		CBUFFER_END

			TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

			SurfaceInput vert(VertexAttributes input)
			{
				SurfaceInput output = (SurfaceInput)0;

				output.positionWS = TransformObjectToWorld(input.positionOS);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);

				output.uv0.xy = TRANSFORM_TEX(input.uv0, _BaseMap);

#ifdef _WIND_ON
				Wind(input, output, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
#endif

				output.positionCS = TransformWorldToHClip(output.positionWS);

				return output;
			}

			half4 frag(SurfaceInput input) : SV_TARGET
			{
				half baseAlpha = _BaseColor.a;
				half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0);
				clip(albedoAlpha.a * baseAlpha - _Cutoff);

				return 0;
			}

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
			// Material Keywords
            #pragma shader_feature_local_vertex _ _WIND_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./Tree0Input.hlsl"
            #include "./Tree0DepthOnlyPass.hlsl"
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

            // -------------------------------------
			// Material Keywords

            //--------------------------------------
            // GPU Instancing
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

            // -------------------------------------
			// Material Keywords

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "./Tree0Input.hlsl"
            #include "./Tree0ViewSpaceNormals.hlsl"
            ENDHLSL
		}
	}

	CustomEditor "LWGUI.LWGUI"
}
