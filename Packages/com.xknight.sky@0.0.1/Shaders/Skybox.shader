Shader "XKnight/Sky/Skybox"
{
	Properties
	{
		[NoScaleOffset] _Moon("Moon", 2D) = "black" {}
		[NoScaleOffset] _MilkyWay("Milky Way", Cube) = "black" {}

		_MoonDirection("MoonDirection", Vector) = (0.3004115, -0.3052047, -0.9036609, 0)

		//_Exposure("Exposure", Float) = 1.3
		_HorizonOffset("Horizon Offset", Float) = 0.01

		_SunBetaMiePhase("Sun Beta Mie Phase", Vector) = (0.1019284, 1.7225, 1.7, 0)
		_SunMieScattering("Sun Mie Scattering", Float) = 1
		[HDR]_SunMieColor("Sun Mie Color", Color) = (1, 0.8, 0.5, 1)

		_MoonBetaMiePhase("Moon Beta Mie Phase", Vector) = (0.2220759, 1.777924, 1.764, 0)
		_MoonMieScattering("Moon Mie Scattering", Float) = 1.15
		[HDR]_MoonMieColor("Moon Mie Color", Color) = (1, 0.95, 0.83, 1)

		_SunE("Sun E", Float) = 90
		[HDR]_SunAtmosphereTint("Sun Atmosphere Tint", Color) = (1, 1, 1, 1)
		[HDR]_MoonAtmosphereTint("Moon Atmosphere Tint", Color) = (0.1, 0.1, 0.1, 0.1)
		_FadeParams("Fade Params", Vector) = (0.7444934, 0, 0.5055066, 0)
		_AtmosphereExponent("Atmosphere Exponent", Float) = 1.5

		_BetaRay("Beta Ray", Vector) = (0.000002903518, 0.000006784368, 0.0000165634, 0)
		_BetaMie("Beta Mie", Vector) = (0.000001594785, 0.000002416427, 0.000003725622, 0)
		_RayleighZenithLength("Rayleigh Zenith Length", Float) = 8400
		_MieZenithLength("Mie Zenith Length", Float) = 1250
	}

	SubShader
	{
		Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox"}
		Cull Off
		ZWrite Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ _NIGHT
			#pragma multi_compile _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/PreethamAtmosphere.hlsl"

			float3 _SunDirection;
			float3 _MoonDirection;

			//float _Exposure;
			float _HorizonOffset;

			TEXTURE2D(_Moon);				SAMPLER(sampler_Moon);
			TEXTURECUBE(_MilkyWay);         SAMPLER(sampler_MilkyWay);

			struct Attributes
			{
				float4 positionOS			: POSITION;
				float2 texcoord				: TEXCOORD0;
			};

			struct Varyings
			{
				float3 positionWS			: TEXCOORD0;
				UBPA_FOG_COORDS(1)
#if _NIGHT				
				float2 uvMoon				: TEXCOORD2;
#endif

				float4 positionCS			: SV_POSITION;
			};

			// LDR Mode
			//#define FAST_TONEMAPING(color) 1.0 - exp(_Exposure * -color)
			//inline void ColorCorrection(inout float3 color)
			//{
			//	color *= _Exposure;
			//	color = FAST_TONEMAPING(color);
			//}

			float3 Calculate(float3 positionWS)
			{
				float3 ray = normalize(positionWS);

				float3 sR; float3 sM;
				OpticalDepth(abs(ray.y + _HorizonOffset), sR, sM);

				float2 cosTheta = float2(dot(ray, _SunDirection.xyz), dot(ray, _MoonDirection.xyz));
				float3 color = AtmosphericScattering(sR, sM, cosTheta);
				color = ATMOSPHERE_EXPONENT(color);

				// LDR Mode
				//ColorCorrection(color);

				return color;
			}

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

				output.positionWS = vertexInput.positionWS;
				output.positionCS = vertexInput.positionCS;

#if _NIGHT				
				float3 right = normalize(cross(_MoonDirection, float3(0, 0, 1)));
				float3 up = cross(_MoonDirection, right);
				output.uvMoon = float2(dot(right, input.positionOS.xyz), dot(up, input.positionOS.xyz)) * 3 + 0.5;
#endif

				UBPA_TRANSFER_FOG(output, vertexInput.positionWS)

				return output;
			}

			float4 frag(Varyings input) : SV_Target
			{
				float3 col = Calculate(input.positionWS);

#if _NIGHT
				float4 moonAlbedo = SAMPLE_TEXTURE2D(_Moon, sampler_Moon, input.uvMoon);
				float moonMask = moonAlbedo.a;
				float4 spaceAlbedo = SAMPLE_TEXTURECUBE(_MilkyWay, sampler_MilkyWay, input.positionWS);
				col += (moonAlbedo.rgb + spaceAlbedo.rgb * (1 - moonMask)) * _FadeParams.y;
#endif

				UBPA_APPLY_FOG(input, col)

				return float4(col, 1.0);
			}
			ENDHLSL
		}
	}
}