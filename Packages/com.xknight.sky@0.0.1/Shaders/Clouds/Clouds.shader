Shader "XKnight/Sky/Clouds" 
{
	Properties
	{
		_CloudTex("Cloud", 2D) = "white" {}
		_NoiseTex("Noise", 2D) = "white" {}

		_Tiling("Tiling", Vector) = (1.25, 1.25, 0.75, 0.75)
		_FadeStartY("Fade Start Y", Float) = 500
		_FadeEndY("Fade End Y", Float) = 0
		_CloudSpeed("Cloud Speed", Vector) = (0.0005, 0, 0, 0)
		_ShadowStrength("Shadow Strength", Range(0,25)) = 20

		_CloudCover("Cloud Cover", Range(0.5,1.2)) = 0.8
		_Attenuation("Attenuation", Range(0,1)) = 1.0
		_CloudSharpness("Cloud Sharpness", Range(0.2,0.99)) = 0.7
		_CloudDensity("Density", Range(0,1)) = 0.5

		_NoiseSeed("Noise Seed", float) = 10
		_NoiseScale("Noise Scale", Range(1, 10)) = 4.5

		_LightColor("Light Color", Color) = (1,1,1,1)
		_CloudColor("Cloud Color", Color) = (0.6745,0.6745,0.6745,1)
	}

	SubShader
	{
		Tags { "Queue" = "Geometry+501" "RenderType" = "Background" }

		Blend  SrcAlpha OneMinusSrcAlpha
		//Cull Front
		Zwrite Off

		Cull Off


		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/NoiseSimplex.hlsl"

			float4 _Tiling;
			float4 _LightColor;
			float4 _CloudColor;
			float _CloudCover;
			float _Attenuation;
			float _CloudSharpness;
			float _CloudDensity;
			float2 _CloudSpeed;
			int _ShadowStrength;
			float _FadeStartY;
			float _FadeEndY;			
			float _NoiseSeed;
			float _NoiseScale;

			TEXTURE2D(_CloudTex);            SAMPLER(sampler_CloudTex);
			TEXTURE2D(_NoiseTex);            SAMPLER(sampler_NoiseTex);

			struct Attributes
			{
				float4 positionOS			: POSITION;
				float3 normalOS				: NORMAL;
				float2 texcoord				: TEXCOORD0;
			};

			struct Varyings
			{
				float2 uv					: TEXCOORD0;
				float2 diffuseAndAlpha		: TEXCOORD1;

				float4 positionCS			: SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				// GetVertexNormalInputs中的SafeNormalize返回half导致精度丢失，这里不能使用
				//VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
				float3 normalWS = normalize(mul(input.normalOS, (float3x3)GetWorldToObjectMatrix()));

				float atten = clamp(_Attenuation, 0, 1) - (_CloudCover - 0.25);

				float3 lightDirection = normalize(_MainLightPosition.xyz);
				float diffuse = atten * max(0.0, dot(normalWS, lightDirection));
				float alpha = 1 - saturate((vertexInput.positionWS.y - _FadeStartY) / (_FadeEndY - _FadeStartY));
				output.diffuseAndAlpha = float2(diffuse, alpha);

				output.uv = input.texcoord;	
				
				output.positionCS = vertexInput.positionCS;

				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				float2 uv = input.uv;

				float2 offset = _Time.x * _CloudSpeed;
				float4 cloudCol = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv + offset * 50);
				float alpha = cloudCol.a;

				float ns = snoise(uv * _NoiseScale + offset * 210 + _NoiseSeed);
				float4 noiseCol = float4(ns, ns, ns, ns + _CloudCover);
				float ns2 = snoise(noiseCol * 0.001);
				float4 noiseCol2 = float4(ns, ns, ns, ns2 + _CloudCover);

				half4 col0 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * _Tiling.xy + offset * 150) * 3;
				half4 col1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * _Tiling.zw + offset * 15) * 0.5;

				float val = alpha * col0.a * noiseCol2.a * 1.25 * col1.a * noiseCol.a * noiseCol2.a;

				alpha = val * input.diffuseAndAlpha.y;
				alpha = max(alpha - (1 - _CloudCover), 0);
				alpha = 1.0 - pow(_CloudSharpness, alpha * 128);

				// Shadow
				float TraceDir = input.diffuseAndAlpha.x;
				float CurTracePos = TraceDir * 50;
				TraceDir *= 4.0;
				float Density = 0;
				val *= 256 * _CloudDensity;
				for (int i = 0; i < _ShadowStrength; i++)
				{
					CurTracePos += TraceDir;
					Density += 0.05 * step(CurTracePos, val);
				}
				float shadowAttenuation = 1 / exp(Density * 2);

				float4 FinalColor = float4 (_LightColor.rgb * shadowAttenuation, alpha);
				FinalColor.xyz += (_CloudColor.xyz * 1.25) * cloudCol.xyz;
				return FinalColor;
			}

			ENDHLSL
		}
	}
} 