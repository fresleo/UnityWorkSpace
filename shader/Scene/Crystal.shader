Shader "XKnight/Scene/Crystal"
{
	Properties
	{
		[Main(Main, _, off, off)]
		_Main("Main", Float) = 1
		[SubToggle(Main, __)] _Receive_DirectLight("Direct Light", Float) = 0
		[Sub(Main)] _BaseColor("Color", Color) = (1,1,1,1)
		[Tex(Main)] [NoScaleOffset] [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		[Tex(Main)] _MetallicGlossMap("Rough(R), AO(G) Metallic(B) Emission(A)", 2D) = "white" {}
		[Tex(Main, _BumpScale)] _BumpMap("Normal Map", 2D) = "bump" {}
		[HideInInspector] _BumpScale("Scale", Range(0, 1)) = 1.0
		[Sub(Main)] _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
		[Sub(Main)] [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
		[Tex(Main)] [NoScaleOffset] _ReflectionCube("Reflection Cube", Cube) = "gray" {}
		
		[Main(Volume, _, off, off)]
		_Volume("Volume", Float) = 1
		[Tex(Volume, _DistortIntensity)] _DistortTex("Distort Texture", 2D) = "black" {}
		[Sub(Volume)] _DistortTex_ScaleOffset("Distort Scale Offset", Vector) = (1,1,0,0)
		[HideInInspector] _DistortIntensity("Distort Intensity", Range(0.0, 1.0)) = 0.1
		
		[Tex(Volume, _NoiseColor)] _NoiseTex("Noise Texture", 2D) = "black" {}
		[Sub(Volume)] _NoiseTex_ScaleOffset("Noise Scale Offset", Vector) = (1,1,0,0)
		[HideInInspector] _NoiseColor("Noise Color" , Color) = (1,1,1,1)
		[Sub(Volume)]_NoiseColorPower("Noise Color Power", Range(0.001, 10)) = 1
		[Sub(Volume)] _NoiseColorMultiply("Noise Color Multiply", Range(0.1, 10)) = 1
		[Sub(Volume)] _NoiseRangePower("Noise Range Power", Range(0.001, 1)) = 1
		[Sub(Volume)] _NoiseRangeMultiply("Noise Range Multiply", Range(0.001, 1)) = 1
		
		//IceDepth
		[Main(IceDepth, _, off, off)]
		_IceDepth("IceDepth", Float) = 1
		[SubToggle(IceDepth, __)] _Ice_Depth("Ice Depth(冰面深度开关)", Float) = 0
		[Sub(IceDepth)] _IceColor("Ice Color(冰面颜色)", Color) = (0.65,0.65,0.65,1)
		[Sub(IceDepth)] _IceSaturation("Ice Saturation (冰面饱和度)", Range(1, 4)) = 1
		[Sub(IceDepth)] _IceLayer("Ice Layer(冰层数(层越多,消耗越大))", Range(1,8)) = 6
		[Sub(IceDepth)] _IceOffset("Ice Offset(冰层偏移数)", Range(-0.02, 0)) = -0.002
		[Sub(IceDepth)] _IceBlur("Ice Blur(冰层模糊度)", Range(0, 1)) = 0.5
		[Sub(IceDepth)] _IceLOD("Ice Lod(冰层LOD)", Range(0, 5)) = 2
		
		// 旋涡
		[Main(SpiralFlow, _, off, off)]
		_SpiralFlow("旋涡", Float) = 1
		[SubToggle(SpiralFlow, __)] _Spiral_Flow("旋涡开关", Float) = 0
		[Sub(SpiralFlow)] _SpiralFlowTex("旋涡图", 2D) = "black" {}
		[Sub(SpiralFlow)] _SpiralFlowMaskTex("旋涡Mask图", 2D) = "white" {}
		[Sub(SpiralFlow)] [HDR] _SpiralFlowColor("旋涡颜色", Color) = (1,1,1,1)
		[Sub(SpiralFlow)] _RadialStrength("旋涡强度", Range(0, 3)) = 1
		[Sub(SpiralFlow)] _TilingAndSpeed("Tiling和流动速度", Vector) = (1,1,1,1)
		
    	// Dissolve
		[Main(Dissolve, _, off, off)]
		_Dissolve("Dissolve", Float) = 1
		[SubToggle(Dissolve, __)] _Random_Dissolve("开启随机溶解", Int) = 0
        [Tex(Dissolve)] _DissolveTex("溶解纹理", 2D) = "white" {}
        [Sub(Dissolve)] _DissolveTexChannel("溶解纹理通道", Vector) = (1,0,0,0)
		[Sub(Dissolve)] _DissolveFadingMin("溶解过渡最小值Min", Range(0, 1.0)) = 0
		[Sub(Dissolve)] _DissolveFadingMax("溶解过渡最大值Max", Range(0, 1.0)) = 0.2
		[Sub(Dissolve)] _EdgeWidth("边缘宽度", Range(0,0.3)) = 0.1
		[Sub(Dissolve)] [HDR] _EdgeColor1("边缘颜色1", Color) = (1,0,0,1)
		[Sub(Dissolve)] [HDR] _EdgeColor2("边缘颜色2", Color) = (0,1,0,1)
        [Sub(Dissolve)] _DissolveCutoff("溶解裁剪强度", Range(-1, 2)) = 0.5
        [HideInInspector] _DissolveDir("方向溶解", Vector) = (0, 1, 0)

		[HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "Queue" = "Geometry+15" }
		
		// ForwardLit
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
            // Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			
			#pragma multi_compile _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY

			// -------------------------------------
            // Material Keywords
			#pragma shader_feature_local_fragment _ _RECEIVE_DIRECTLIGHT_ON
			#pragma shader_feature_local_fragment _ _ICE_DEPTH_ON
			#pragma shader_feature_local_fragment _ _SPIRAL_FLOW_ON

			#pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON // 消融

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			
			#include "CrystalCommon.hlsl"
			ENDHLSL
		}

		// ShadowCaster
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ColorMask 0

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

			// -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "CrystalInput.hlsl"
			#include "CrystalShadowCasterPass.hlsl"
			ENDHLSL
		}
		
		// Meta
		Pass
		{
			Name "Meta"
			Tags { "LightMode" = "Meta" }

			Cull Off

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex UniversalVertexMeta
			#pragma fragment FragmentMeta

			// -------------------------------------
            // Pipeline keywords
			#pragma shader_feature EDITOR_VISUALIZATION

			// -------------------------------------
            // Material Keywords
			#pragma shader_feature_local_fragment _ _ALPHATEST_ON

			#include "CrystalInput.hlsl"
			#include "CrystalMetaPass.hlsl"
			ENDHLSL
		}
	}

	CustomEditor "LWGUI.LWGUI"
}
