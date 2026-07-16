Shader "XKnight/UI/UI_GlichEffect"
{
	// 色散效果采样三次，描边效果采样四次
	Properties
	{
		// PerRendererData 表明纹理由每个渲染器提供，似乎是起某种优化作用。
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		_Color ("主要颜色", Color) = (1,1,1,1)
		[Main(ColorLine, _, on)]
		_ColorLine("颜色线条故障效果", Float) = 0
		[Tex(ColorLine)]_NoiseTex2 ("噪波贴图", 2D) = "white" {}  //[Texture]
		[Sub(ColorLine)]_ColorLineSpeed("颜色线条故障速度", Range(0.01,0.05)) = 0.01
		[Sub(ColorLine)]_ColorLineScale("颜色线密度", Range(0.5,3)) = 1.5
		[Sub(ColorLine)]_ColorLineIntensity("颜色深浅", Range(0,1)) = 0.8
		[Main(Dispersion, __, on)]
		_Dispersion ("色散效果", Float) = 0
		[SubToggle(Dispersion,_)]_HorizonTalToggle("横向色散",int) = 0
		[SubToggle(Dispersion,_)]_VerticalToggle("竖向色散",int) = 0
		[Sub(Dispersion)]_DSSpeed("色散速度", Range(0,10)) = 1.5
		[Sub(Dispersion)]_DSAmplitude("色散幅度", Range(0,2)) = 0.34
		[Sub(Dispersion)]_DSIndensity("色散强度", float) = 0.5
		//[Main(Dispersion, __, on)]
		[Main(LineGlitc, __, on)]
		_LineGlitc ("错位线条故障效果", Float) = 0
		[SubToggle(LineGlitc,_)]_HorizonTalToggle2("横向故障",int) = 0
		[SubToggle(LineGlitc,_)]_VerticalToggle2("竖向故障",int) = 0
		[Sub(LineGlitc)]_LineAmount("线条故障数量", float) = 3
		[Sub(LineGlitc)]_LineSpeed("线条故障速度", float) = 1
		[Sub(LineGlitc)]_LineOffset("线条故障偏移幅度", Range(0,0.05)) = 0.02
		
		[Main(Disturb, __, on)]
		_Disturb ("扰动效果", Float) = 0
		[Tex(Disturb)]_NoiseTex ("噪波贴图", 2D) = "white" {}  //[Texture]
		[Sub(Disturb)]_MaskCenter("遮罩中心", Vector) = (0.5,0.5,0,0)
		[Sub(Disturb)]_MaskRadius("遮罩半径", Range(0,1)) = 0.1
		[Sub(Disturb)]_NoiseScale("噪波缩放", float) = 1
		[Sub(Disturb)]_NoiseSpeed("噪波速度", float) = 1
		[Sub(Disturb)]_DisturbInstensity("扰动强度", float) = 0.1
		
		[Main(Flash, __, on)]
		_Flash ("闪烁效果", Float) = 0
		[Tex(Flash)]_FlashMaskTex ("闪烁遮罩贴图", 2D) = "white" {}
		[Sub(Flash)]_FlashSpeed("闪烁速度", float) = 0.01
		
		[Main(Pixel,_, on)]
		_Pixel ("像素效果", Float) = 0
		[Sub(Pixel)]_PixelScale("像素数量", float) = 100
		
		[Main(Polkadot, __, on)]
		_Polkadot ("波点效果", Float) = 0
		[SubToggle(Polkadot,_)]_UseMask("是否使用遮罩图",int) = 0
		[Tex(Polkadot)]_PolkadotMaskTex ("波点遮罩贴图", 2D) = "white" {}
		[Tex(Polkadot)]_PolkadotTex("波点贴图", 2D) = "white" {}
		[Sub(Polkadot)]_PolkaDotDensity("波点密度", float) = 10
//		[SubToggle(Polkadot,_)]_UseScreenSpace("是否是屏幕空间",int) = 0
		//[Sub(Polkadot)]_PolkaDotRotation("波点旋转", float) = 0
		
		
		
		[Main(Outline, __, on)]
		_Outline ("描边效果（较消耗）", Float) = 0
		[Sub(Outline)][HDR]_OutlineColor ("描边颜色", Color) = (0,0,0,1) 
		[Sub(Outline)]_OutlineWidth ("描边宽度（像素单位）", Range(0, 3)) = 3 
		[Sub(Outline)]_AlphaThreshold("透明剔除阈值（像素单位）", Range(0.01,0.99)) = 0.01


		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		// 启用Clip功能，Clip掉的像素将不会参与模板测试等。
		//[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}
	SubShader
	{
		LOD 100 //内置无光照系列Shader的LOD一般为100
		Tags
		{
			"Queue"="Transparent" 
			"PreviewType"="Plane" 
			"RenderType"="Transparent" 
			"CanUseSpriteAtlas"="True" 
			"IgnoreProjector"="True" 
			"RenderPipeline"="UniversalPipeline"
		}

		//UI利用模板功能实现遮罩效果
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		//渲染器将自动排序，再加上UI都是半透明物体，所以无需深度功能
		ZWrite Off
		ZTest [unity_GUIZTestMode] // unity_GUIZTestMode 根据当前画布的渲染模式自动设置

		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertexPass
			#pragma fragment PixelPass
			
			//#pragma shader_feature _ UNITY_UI_ALPHACLIP
			#pragma multi_compile_local_fragment _ _DISPERSION_ON
			#pragma multi_compile_local_fragment _ _LINEGLITC_ON
			#pragma multi_compile_local_fragment _ _DISTURB_ON
			#pragma multi_compile_local_fragment _ _FLASH_ON 
			#pragma multi_compile_local_fragment _ _OUTLINE_ON
			//#pragma shader_feature_local_fragment _ _PIXEL_ON
			#pragma multi_compile_local_fragment _ _POLKADOT_ON
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "UI_GlichFunction.hlsl"
			
			v2f VertexPass(const a2f input)
			{
				v2f pixel;
				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
				
				half4 vertexColor = input.color;
				if (_UIVertexColorAlwaysGammaSpace)
				{
					#ifndef UNITY_COLORSPACE_GAMMA
					vertexColor.rgb = SRGBToLinear(vertexColor.rgb);
					#endif
				}

				pixel.positionCS = TransformObjectToHClip(input.positionOS);
				pixel.color = vertexColor * _Color;
				pixel.uv = TRANSFORM_TEX(input.uv, _MainTex);
				//pixel.positionNDC = positionInputs.positionNDC;
				return pixel;
			}

			float4 PixelPass(const v2f input):SV_TARGET
			{
				
				half2 theuv = input.uv;
				
				#ifdef _LINEGLITC_ON
				BlockLineGlitchEffect(theuv);
				#endif
				#ifdef _DISTURB_ON
				DisturbEffect(theuv);
				#endif
				//#ifdef _PIXEL_ON
				PixelEffect(theuv);
				//#endif
				float4 color = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,theuv) * input.color;//tex2D(_MainTex, pixel.uv)
				ColorLineEffect(color,theuv);
				#ifdef  _DISPERSION_ON
				DispersionEffect(color,input.color,theuv);
				#endif
				#ifdef _POLKADOT_ON
				PolkaDotEffect(color,input.uv);
				#endif
				#ifdef _FLASH_ON
				FlashEffect(color,input.uv);
				#endif
				#ifdef _OUTLINE_ON
				color = OutlineEffect(color,theuv);
				
				#endif
				
				if (_IsGammaUI)
				{
					color.rgb = LinearToSRGB(color.rgb);
				}
				return color;
			}
			ENDHLSL
		}
	}
	CustomEditor "LWGUI.LWGUI"
}
