Shader "XKnight/Sky/CloudParticle" 
{
	Properties
	{
		_CloudCurlTex("Cloud Curl Tex", 2D) = "white" {}
        _CloudParticleAtlas("Cloud Particle Atlas", 2D) = "white" {}

        _AtlasTiles("Atlas Tiles", Vector) = (2, 4, 0, 0)

        _CloudCurlSpeed("Cloud Curl Speed", Float) = 1
        _CloudCurlTiling("Cloud Curl Tiling", Float) = 3
		_CloudCurlAmplitude("Cloud Curl Amplitude", Float) = 0.02
		//_CloudSunBrightenIntensity("Cloud Sun Brighten Intensity", Float) = 0.8299
		_CloudTransparency("Cloud Transparency", Float) = 1
		_CloudCoverage("Cloud Coverage", Float) = 0.11
		_CloudVolumeChangeSpeed("Cloud Volume Change Speed", Float) = 1

		//_CloudFrontAndBackBlendFactor("Cloud Front And Back Blend Factor", Float) = 0.0881
		//[HDR]_CloudDarkBackColor("Cloud Dark Back Color", Color) = (0.02257, 0.23783, 0.45227, 1)
		//[HDR]_CloudDarkFrontColor("Cloud Dark Front Color", Color) = (0.08773, 0.35994, 0.58044, 1)
		//[HDR]_CloudLightBackColor("Cloud Light Back Color", Color) = (0.57591, 0.79012, 0.94779, 1)
		//[HDR]_CloudLightFrontColor("Cloud Light Front Color", Color) = (0.57203, 0.70038, 0.76956, 1)
	}

	SubShader
	{
		Tags { "Queue" = "Geometry+501" "RenderType" = "Background" }

		Blend  SrcAlpha OneMinusSrcAlpha
		Cull Off
		Zwrite Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float3 _SunDirection;

			float2 _AtlasTiles;

			float _CloudCurlSpeed;
			float _CloudCurlTiling;
			float _CloudCurlAmplitude;
			float _CloudSunBrightenIntensity;
			float _CloudTransparency;
			float _CloudCoverage;
			float _CloudVolumeChangeSpeed;

			float _CloudFrontAndBackBlendFactor;
			float3 _CloudDarkBackColor;
			float3 _CloudDarkFrontColor;
			float3 _CloudLightBackColor;
			float3 _CloudLightFrontColor;

			TEXTURE2D(_CloudCurlTex);               SAMPLER(sampler_CloudCurlTex);
			TEXTURE2D(_CloudParticleAtlas);         SAMPLER(sampler_CloudParticleAtlas);

			struct Attributes
			{
				float4 positionOS			: POSITION;
				float4 color				: COLOR;
                float2 texcoord				: TEXCOORD0;
			};

			struct Varyings
			{
				float4 uv					: TEXCOORD0;
				float3 sunAndVolume			: TEXCOORD1;
				float3 lightColor		    : TEXCOORD2;
				float3 darkColor		    : TEXCOORD3;

				float4 positionCS			: SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;

				// UV
                float tileCount = _AtlasTiles.x * _AtlasTiles.y - 1.0;
                float tileIndex = floor(input.color.y * tileCount + 0.5);
                float tileRow = tileIndex / _AtlasTiles.x;
                float2 tileUV = float2(frac(tileRow) * _AtlasTiles.x, floor(tileRow));
                output.uv.xy = (tileUV + input.texcoord) / _AtlasTiles.xy;
                output.uv.zw = input.texcoord * _CloudCurlTiling + _Time.x * _CloudCurlSpeed * float2(1.2, 0.8);

				// Volume
				float volumeFullPercentStart = 0.4;
				float volumeFullPercentEnd = 0.6;
				float volumePercent = sin(input.color.y * 2.0 * PI + _Time.x * _CloudVolumeChangeSpeed) * 0.4 + 0.5;				
				float volumeVal0 = saturate((volumePercent - volumeFullPercentEnd) / (1.0 - volumeFullPercentEnd));
				volumeVal0 = -(volumeVal0 * -2.0 + 3.0) * volumeVal0 * volumeVal0 + 1.0;
				float volumeVal1 = saturate(volumePercent / volumeFullPercentStart);
                volumeVal1 = (volumeVal1 * -2.0 + 3.0) * volumeVal1 * volumeVal1;
				output.sunAndVolume.y = input.color.z;
                output.sunAndVolume.z = -volumeVal1 * volumeVal0 + 1.0;
				
				// Sun
				float3 positionWS = normalize(vertexInput.positionWS);
				float dotSun = dot(positionWS, _SunDirection);
				output.sunAndVolume.x = (dotSun * 0.5 + 0.5) * _CloudSunBrightenIntensity;
				
				// Blend Color
                float blendVal = 1.0 + (dotSun - 1.0) * _CloudFrontAndBackBlendFactor;
				blendVal = max(blendVal, 0.0);
				blendVal = blendVal * blendVal * blendVal;
				output.lightColor = blendVal * (_CloudLightFrontColor.xyz - _CloudLightBackColor.xyz) + _CloudLightBackColor.xyz;
				output.darkColor = blendVal * (_CloudDarkFrontColor.xyz - _CloudDarkBackColor.xyz) + _CloudDarkBackColor.xyz;

				return output;
			}

            float4 frag(Varyings input) : SV_Target
            {
				// Sample
				float3 noiseVal = SAMPLE_TEXTURE2D(_CloudCurlTex, sampler_CloudCurlTex, input.uv.zw).xyz;
				float2 cloudUV = (noiseVal.xy - 0.5) * noiseVal.z * _CloudCurlAmplitude + input.uv.xy;
				float4 cloudVal = SAMPLE_TEXTURE2D(_CloudParticleAtlas, sampler_CloudParticleAtlas, cloudUV);

				// Volume
                float volumeMax = min(input.sunAndVolume.z + input.sunAndVolume.y, 1.0);
                float volumeMin = max(input.sunAndVolume.z - input.sunAndVolume.y, 0.0);
				float volumeVal = saturate((cloudVal.z - volumeMin) / (volumeMax - volumeMin));
				volumeVal = (volumeVal * -2.0 + 3.0) * volumeVal * volumeVal;
				volumeVal *= cloudVal.a;
				clip(volumeVal * _CloudTransparency - 0.01);

				// Color
				float alpha = volumeVal * _CloudTransparency;
				float3 col = cloudVal.x * (input.lightColor - input.darkColor) + input.darkColor;
				col += input.lightColor * _CloudCoverage * 0.4;
				col *= input.sunAndVolume.x + 1.0;

                return float4(col, alpha);
			}

			ENDHLSL
		}
	}
} 