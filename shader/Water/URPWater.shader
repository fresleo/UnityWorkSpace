Shader "XKnight/Water"
{
	Properties
	{
		[Main(Main, _, on, off)]
		_Main("Base Settings", Float) = 1
		[Sub(Main)] _Color("Shallow Color", color) = (0.6,1,0.8,1)
		[Sub(Main)] _DepthColor("Depth Color", color) = (0,0.26,0.4,1)
		[MinMaxSlider(Main, _DepthStart, _DepthEnd)] _DepthColorRange("Depth Range", Range(0.0, 10.0)) = 1.0
		[HideInInspector] _DepthStart("Depth Start", Range(0.0, 4.0)) = 0
		[HideInInspector] _DepthEnd("Depth End", Range(4.0, 10.0)) = 1
		[Sub(Main)] _EdgeSize("EdgeFade", Range(0, 5.0)) = 1
		[Sub(Main)] _Distortion("Refraction Distortion", Range(0,128)) = 32
		[SubToggle(Main, __)] _WorldUV("World UV(Used for seamless connection of multiple water.)", Float) = 0
		
		[Main(Specular, __, on)]
		_Specular("Specular On", Float) = 1
		[Sub(Specular)] _Smoothness("Smoothness", Range(0,1)) = 0.5
		[Sub(Specular)] _SpecColor("SpecularColor", Color) = (1,1,1,1) 
		
		[Main(NormalMaps, _, on, off)]
		_NormalMaps("Normal Maps", Float) = 1
		[Tex(NormalMaps, _NormalMapAIntensity)] _NormalMapA("Normal Map A", 2D) = "bump" {}
		[HideInInspector] _NormalMapAIntensity("Normal Map A: Intensity", Range(0,1)) = 1
		[Sub(NormalMaps)] _NormalMapATilings("Normal Map A: Tilings", Vector) = (1,1,1,1)
		[Sub(NormalMaps)] _NormalMapASpeeds("Normal Map A: Speeds", Vector) = (1,1,0.5,0.5)
		
		[Tex(NormalMaps, _NormalMapBIntensity)] _NormalMapB("Normal Map B", 2D) = "bump" {}
		[HideInInspector] _NormalMapBIntensity("Normal Map B: Intensity", Range(0,1)) = 1
		[Sub(NormalMaps)] _NormalMapBTilings("Normal Map B: Tilings", Vector) = (1,1,1,1)
		[Sub(NormalMaps)] _NormalMapBSpeeds("Normal Map B: Speeds", Vector) = (1,1,1,1)
		
		[SubToggle(NormalMaps, __)] _Flowmap("Flowmap Mode(Only sample Normal A.)", Float) = 0
		[Tex(NormalMaps, _FlowIntensity)] _FlowMap("Flow Map ", 2D) = "black" {}
		[HideInInspector] _FlowIntensity("Flow Intensity", Range(0.1, 0.5)) = 0.1
		// TODO flow map理论上没必要tiling，与美术确认后删掉
		[Sub(NormalMaps)] _FlowTiling("Flow Tiling(TODO)", Vector) = (1,1,0,0)
		[Sub(NormalMaps)] _FlowSpeed("Flow Speed", Range(0.0, 10.0)) = 5

		[Main(Caustics, __, on)]
		_Caustics("Caustics On", Float) = 1
		[Tex(Caustics, _CausticsIntensity)] _CausticsTex("Caustics Texture", 2D) = "black" {}
		[HideInInspector] _CausticsIntensity("Intensity", Range(0.0, 100.0)) = 4
		[Sub(Caustics)] _CausticsTiling("Caustics Tiling", Vector) = (1,1,1,1)
		[Sub(Caustics)] _CausticsSpeed("Speed", vector) = (1,1,-1,-1)
		[MinMaxSlider(Caustics, _CausticsStart, _CausticsEnd)] _CausticsRange("Caustics Range", Range(0.0, 10.0)) = 1.0
		[HideInInspector] _CausticsStart("Start", Range(0.0, 2.0)) = 0
		[HideInInspector] _CausticsEnd("End", Range(2.0, 10.0)) = 1
		
		[Main(Reflection, _, off, off)]
		_Reflection("Reflection On", Float) = 1
		[Sub(Reflection)] _CubemapTexture("Reflection Cubemap", Cube) = "" {}
		[Sub(Reflection)] _ReflectionIntensity("Reflection Intensity", Range(0,1)) = 1
		[Sub(Reflection)] _ReflectionDistortion("Reflection Distortion", Range(0,0.5)) = 0.1
		[Sub(Reflection)] _ReflectionFarDistortion("Reflection Far Distortion", Range(0, 0.2)) = 0
		[Sub(Reflection)] _ReflectionFresnel("Reflection Fresnel", Range(0, 10)) = 1
		[SubToggle(Reflection, __)] _PlanarReflection("开启平面反射,默认为SSR", Float) = 0
		[Sub(Reflection)] _PlanarReflectionDistortionIntensity("平面反射扭曲强度", Range(0, 3)) = 1
		[Sub(Reflection)] _PlanarReflectionLightDirection("平面反射主光方向", Vector) = (0,0,-1,0)
	}

	// LOD 400
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent-402" "RenderPipeline" = "UniversalPipeline" }
		LOD 400
		
		Pass
		{
			Name "Front"
			Tags { "LightMode" = "UniversalForward" }
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			
			#pragma shader_feature_local _ _WORLDUV_ON
			#pragma shader_feature_local_fragment _ _CAUSTICS_ON
			#pragma shader_feature_local_fragment _ _SPECULAR_ON
			#pragma shader_feature_local _ _FLOWMAP_ON
			
			#pragma shader_feature _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY
			
			#pragma shader_feature _ _GLOBAL_RAIN_ON
			#pragma shader_feature_local_fragment _ _PLANARREFLECTION_ON

			#pragma shader_feature _ _FLOWMAP_VISUALIZATION
			
			#pragma target 3.0

			#include "../ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			
			#include "Includes/Water_Variables.hlsl"  
			#include "Includes/Water_Helpers.hlsl"  
			#include "Includes/Water_Lighting.hlsl"
			#include "Includes/Water_Refraction.hlsl"
			#include "Includes/Water_Reflection.hlsl" 
			#include "Includes/Water_Normals.hlsl"
			#include "Includes/Water_Alpha.hlsl"
			#include "Includes/Water_Reflection.hlsl"
			#include "Includes/Water_Common.hlsl"
		
			ENDHLSL
		}
	}
	
	// LOD 300
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent-402" "RenderPipeline" = "UniversalPipeline" }
		LOD 300
		
		Pass
		{
			Name "Universal Forward"
			Tags { "LightMode" = "UniversalForward" }
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag_LOD1

			#pragma shader_feature _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			
			#pragma shader_feature_local _ _WORLDUV_ON
			#pragma shader_feature_local_fragment _ _CAUSTICS_ON
			#pragma shader_feature_local_fragment _ _SPECULAR_ON
			#pragma shader_feature_local _ _FLOWMAP_ON
			
			#pragma shader_feature _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY
			
			#pragma shader_feature _ _GLOBAL_RAIN_ON

			#pragma shader_feature _ _FLOWMAP_VISUALIZATION
			
			#pragma target 3.0

			#include "../ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			
			#include "Includes/Water_Variables.hlsl"  
			#include "Includes/Water_Helpers.hlsl"  
			#include "Includes/Water_Lighting.hlsl"
			#include "Includes/Water_Refraction.hlsl"
			#include "Includes/Water_Reflection.hlsl" 
			#include "Includes/Water_Normals.hlsl"
			#include "Includes/Water_Alpha.hlsl"
			#include "Includes/Water_Reflection.hlsl"
			#include "Includes/Water_Common.hlsl"
		
			ENDHLSL
		}
	}
	
	CustomEditor "LWGUI.LWGUI"
}
