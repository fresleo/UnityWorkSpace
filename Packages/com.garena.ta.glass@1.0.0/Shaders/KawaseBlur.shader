Shader "Custom/KawaseBlur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		
		[Header(Bilateral (Depth))]
		_SigmaSpatial ("Sigma Spatial", Range(0.01, 8)) = 1
		_SigmaDepth ("Sigma Depth", Range(0.0001, 1)) = 0.01
	}
	SubShader
	{
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
		ZTest Always
		ZWrite Off
		Cull Off
		Blend Off

		Pass
		{
			Name "KawaseBlur"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma shader_feature_local_fragment _ _BILATERAL_DEPTH_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

			float4 _BlurAmount;
			float _SigmaSpatial;
			float _SigmaDepth;

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings Vert(Attributes v)
			{
				Varyings o;
				VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
				o.positionCS = posInputs.positionCS;
				o.uv = v.uv;
				return o;
			}

			inline float4 SampleMain(float2 uv)
			{
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
			}

			inline void AccumulateBilateral(
				float2 baseUV,
				float2 offsetInStepUnits,
				float2 stepUV,
				float centerDepth,
				float spatialDen,
				float depthDen,
				inout float4 acc,
				inout float wsum)
			{
				float2 tapUV = baseUV + stepUV * offsetInStepUnits;
				float tapDepth = SampleSceneDepth(tapUV);
				float2 o = offsetInStepUnits;
				float spatial = exp(-dot(o, o) / spatialDen);
				float dz = tapDepth - centerDepth;
				float depthw = exp(-(dz * dz) / depthDen);
				float w = spatial * depthw;

				acc += SampleMain(tapUV) * w;
				wsum += w;
			}

			float4 Frag(Varyings i) : SV_Target
			{
				const float2 step = _BlurAmount.xy;
				const float2 uv = i.uv;

#if defined(_BILATERAL_DEPTH_ON)
				float centerDepth = SampleSceneDepth(uv);

				float spatialDen = max(1e-4, 2.0 * _SigmaSpatial * _SigmaSpatial);
				float depthDen = max(1e-4, 2.0 * _SigmaDepth * _SigmaDepth);

				float4 acc = 0.0;
				float wsum = 0.0;

				// Center + 8 taps (same pattern as non-bilateral Kawase)
				AccumulateBilateral(uv, float2(0, 0), step, centerDepth, spatialDen, depthDen, acc, wsum);

				// Diagonals
				AccumulateBilateral(uv, float2( 1,  1), step, centerDepth, spatialDen, depthDen, acc, wsum);
				AccumulateBilateral(uv, float2(-1,  1), step, centerDepth, spatialDen, depthDen, acc, wsum);
				AccumulateBilateral(uv, float2( 1, -1), step, centerDepth, spatialDen, depthDen, acc, wsum);
				AccumulateBilateral(uv, float2(-1, -1), step, centerDepth, spatialDen, depthDen, acc, wsum);

				// Axial (bigger stride)
				AccumulateBilateral(uv, float2( 2,  0), step, centerDepth, spatialDen, depthDen, acc, wsum);
				AccumulateBilateral(uv, float2(-2,  0), step, centerDepth, spatialDen, depthDen, acc, wsum);
				AccumulateBilateral(uv, float2( 0,  2), step, centerDepth, spatialDen, depthDen, acc, wsum);
				AccumulateBilateral(uv, float2( 0, -2), step, centerDepth, spatialDen, depthDen, acc, wsum);

				return acc / max(wsum, 1e-4);
#else
				float4 sum = 0.0;
				sum += SampleMain(uv);

				// Diagonals
				sum += SampleMain(uv + step * float2( 1,  1));
				sum += SampleMain(uv + step * float2(-1,  1));
				sum += SampleMain(uv + step * float2( 1, -1));
				sum += SampleMain(uv + step * float2(-1, -1));

				// Axial (bigger stride)
				sum += SampleMain(uv + step * float2( 2,  0));
				sum += SampleMain(uv + step * float2(-2,  0));
				sum += SampleMain(uv + step * float2( 0,  2));
				sum += SampleMain(uv + step * float2( 0, -2));

				return sum / 9.0;
#endif
			}
			ENDHLSL
		}
	}
}


