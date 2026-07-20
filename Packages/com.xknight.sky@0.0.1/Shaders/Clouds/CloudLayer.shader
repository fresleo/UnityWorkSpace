Shader "XKnight/Sky/CloudLayer" 
{
	Properties
	{
		_CloudCurlTex("Cloud Curl Tex", 2D) = "white" {}
		_WeatherMap("Weather Map", 2D) = "white" {}
		_CloudNormalMap("Cloud Normal Map", 2D) = "bump" {}
		_CloudDensityMap("Cloud Density Map", 2D) = "white" {}
		_CloudWispsTex("Cloud Wisps Tex", 2D) = "white" {}

        _CloudCurlSpeed("Cloud Curl Speed", Float) = 1
        _CloudCurlTiling("Cloud Curl Tiling", Float) = 3
		_CloudCurlAmplitude("Cloud Curl Amplitude", Float) = 0.02
		//_CloudSunBrightenIntensity("Cloud Sun Brighten Intensity", Float) = 0.8299
		_CloudCoverage("Cloud Coverage", Float) = 0.11

		//_CloudFrontAndBackBlendFactor("Cloud Front And Back Blend Factor", Float) = 0.0881
		//[HDR]_CloudDarkBackColor("Cloud Dark Back Color", Color) = (0.02257, 0.23783, 0.45227, 1)
		//[HDR]_CloudDarkFrontColor("Cloud Dark Front Color", Color) = (0.08773, 0.35994, 0.58044, 1)
		//[HDR]_CloudLightBackColor("Cloud Light Back Color", Color) = (0.57591, 0.79012, 0.94779, 1)
		//[HDR]_CloudLightFrontColor("Cloud Light Front Color", Color) = (0.57203, 0.70038, 0.76956, 1)

		_LightDirection("Light Direction", Vector) = (0.29465, 0.78223, -0.54891, 0)
		_CloudChangeSpeed("Cloud Change Speed", Float) = 0.5
		_CloudDirection("Cloud Direction", Vector) = (-1, 0, 0, 0)
		_CloudHeight("Cloud Height", Float) = 0.087
		_CloudTiling("Cloud Tiling", Float) = 0.8
		_CloudWispsSpeed("Cloud Wisps Speed", Float) = 0.05
		_CloudWispsCoverage("Cloud Wisps Coverage", Float) = 1
		_CloudWispsOpacity("Cloud Wisps Opacity", Float) = 0.6
		_CloudOpacity("Cloud Opacity", Float) = 1
		_CloudSmoothness("Cloud Smoothness", Vector) = (0.05, 0.5, 0, 0)
	}

	SubShader
	{
		Tags { "Queue" = "Geometry+501" "RenderType" = "Background" }

		Blend One OneMinusSrcAlpha
		Zwrite Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float3 _SunDirection;

			float _CloudCurlSpeed;
			float _CloudCurlTiling;
			float _CloudCurlAmplitude;
			float _CloudSunBrightenIntensity;
			float _CloudCoverage;

			float _CloudFrontAndBackBlendFactor;
			float3 _CloudDarkBackColor;
			float3 _CloudDarkFrontColor;
			float3 _CloudLightBackColor;
			float3 _CloudLightFrontColor;

			float3 _LightDirection;
			float _CloudChangeSpeed;
			float2 _CloudDirection;
			float _CloudHeight;
			float _CloudTiling;
			float _CloudWispsSpeed;
			float _CloudWispsCoverage;
			float _CloudWispsOpacity;
			float _CloudOpacity;
			float2 _CloudSmoothness;

			TEXTURE2D(_CloudCurlTex);               SAMPLER(sampler_CloudCurlTex);
			TEXTURE2D(_WeatherMap);					SAMPLER(sampler_WeatherMap);
			TEXTURE2D(_CloudNormalMap);             SAMPLER(sampler_CloudNormalMap);
			TEXTURE2D(_CloudDensityMap);            SAMPLER(sampler_CloudDensityMap);
			TEXTURE2D(_CloudWispsTex);				SAMPLER(sampler_CloudWispsTex);

			struct Attributes
			{
				float4 positionOS				: POSITION;
                float3 normalOS					: NORMAL;
                float4 tangentOS				: TANGENT;
                float2 texcoord					: TEXCOORD0;
                float2 texcoord1				: TEXCOORD1;
                float2 texcoord2				: TEXCOORD2;
			};

			struct Varyings
			{
				float4 densityUV				: TEXCOORD0;
				float4 weatherUVAndDotVal		: TEXCOORD1;
				float4 normalVal				: TEXCOORD2;
				float4 curlAndWispsUV			: TEXCOORD3;
				float4 lightDirTSAndCoverage	: TEXCOORD4;				
				float3 lightColor				: TEXCOORD5;
                float3 darkColor				: TEXCOORD6;                

				float4 positionCS				: SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
                Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;

				//// UV
				//float2 weatherUV = _CloudHeight * (input.texcoord2 - input.texcoord1) + input.texcoord1;
                //output.weatherUVAndDotVal.xy = weatherUV;

				//float2 weatherDir = weatherUV - 0.5;
				//float2 dotWeather = float2(dot(float2(_CloudDirection.y, -_CloudDirection.x), weatherDir), dot(_CloudDirection, weatherDir));
				//float2 baseUV = dotWeather + 0.5;
				//float cloudChangeVal = _CloudChangeSpeed * _Time.x;
				//output.densityUV = baseUV.xyxy *_CloudTiling * float4(1.2, 1.2, 0.9, 0.9) + cloudChangeVal * float4(0.6, 0.0, 0.78, 0.0);
				//output.normalVal.xy = baseUV.xy *_CloudTiling * 0.7 + cloudChangeVal * float2(0.28, 0.0035);
 
				// Pos
				float3 positionWS = normalize(vertexInput.positionWS);
				//float cosPos = -positionWS.y;
				//float sinPos = sin(acos(cosPos));

				//// Normal
				//float3 normalWS = mul(input.normalOS, (float3x3)GetWorldToObjectMatrix());
				//normalWS = normalize(normalWS);
				//float posVal = float2(positionWS.z, -positionWS.x);
				//float dotN = dot(normalWS.xz, posVal);
				//float2 normalOffset = positionWS.xz * normalWS.y * sinPos;
				//normalOffset += cosPos * normalWS.xz + dotN * posVal * (1.0 - cosPos);
				//output.normalVal.zw = normalWS.xz + normalOffset;

                // Wisps
                //output.curlAndWispsUV.xy = baseUV * _CloudCurlTiling + _Time.x * _CloudCurlSpeed * float2(1.2, 0.8);
                output.curlAndWispsUV.z = input.texcoord.x + _CloudWispsSpeed * _Time.x;
                output.curlAndWispsUV.w = input.texcoord.y;

				//// Coverage
				//float coverageVal = 1.0 - _CloudCoverage * 0.7;
				//output.lightDirTSAndCoverage.w = coverageVal * coverageVal * coverageVal - 0.15;

				// Dot
				float dotSun = dot(positionWS, _SunDirection);
				output.weatherUVAndDotVal.z = dotSun * 0.5 + 0.5;
				//float dotUp = dot(float3(0, 1, 0), positionWS);
				//output.weatherUVAndDotVal.w = asin(dotUp) * 0.636619806;

				////  World To Tangent
				//float3 tangentWS = normalize(mul(input.tangentOS.xyz, (float3x3)GetWorldToObjectMatrix()));
				//float3 bitangentWS = cross(normalWS, tangentWS) * input.tangentOS.w;
				//float3 lightDirTS;
				//lightDirTS.x = dot(tangentWS, _LightDirection.xyz);
				//lightDirTS.y = dot(bitangentWS, _LightDirection.xyz);
				//lightDirTS.z = dot(normalWS, _LightDirection.xyz);
				//output.lightDirTSAndCoverage.xyz = normalize(lightDirTS);

				// Blend Color
				float blendVal = 1.0 + (dotSun - 1.0) * _CloudFrontAndBackBlendFactor;
				blendVal = max(blendVal, 0.0);
				blendVal = blendVal * blendVal * blendVal;
				output.lightColor = 1 * (_CloudLightFrontColor.xyz - _CloudLightBackColor.xyz) + _CloudLightBackColor.xyz;
				output.darkColor = blendVal * (_CloudDarkFrontColor.xyz - _CloudDarkBackColor.xyz) + _CloudDarkBackColor.xyz;

				return output;
			}

            float4 frag(Varyings input) : SV_Target
            {
				//// Curl
				//float2 curlVal = SAMPLE_TEXTURE2D(_CloudCurlTex, sampler_CloudCurlTex, input.curlAndWispsUV.xy).xy;
				//curlVal = curlVal - 0.5;
 
				//// Weather
				//float3 weatherVal = SAMPLE_TEXTURE2D(_WeatherMap, sampler_WeatherMap, input.weatherUVAndDotVal.xy).xyz;
				//float curlAmplitude = _CloudCurlAmplitude * 0.2 + weatherVal.y * _CloudCurlAmplitude * 0.8;
				//curlVal *= curlAmplitude;

				//// Normal
				//float2 normalUV = input.normalVal.xy + curlVal;
				//float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_CloudNormalMap, sampler_CloudNormalMap, normalUV));
				//normalTS = normalize(normalTS);
				//float NdotL = dot(normalTS, input.lightDirTSAndCoverage.xyz);
				//NdotL = saturate(NdotL * 0.5 + 0.5);
				//float diffuse = (2.0 * NdotL - 3.0) * NdotL * NdotL + 1.0;

				//// Density
				//float4 densityUV = input.densityUV + curlVal.xyxy;
				//float3 density0 = SAMPLE_TEXTURE2D(_CloudDensityMap, sampler_CloudDensityMap, densityUV.xy).xyz;
				//float3 density1 = SAMPLE_TEXTURE2D(_CloudDensityMap, sampler_CloudDensityMap, densityUV.zw).xyz;
			
				//float normalOffset = (-density0.y) * 0.01 + 0.03;
				//densityUV.xy = input.normalVal.xy + input.normalVal.zw * normalOffset + curlVal * 0.5;
				//float3 density2 = SAMPLE_TEXTURE2D(_CloudDensityMap, sampler_CloudDensityMap, densityUV.xy).xyz;

				//// Color
				//float blendVal = saturate(dot(density2.yy, density2.zz)) + density2.x - 0.5;
				//blendVal = saturate(blendVal);
				//blendVal = blendVal * blendVal;
				//blendVal *= diffuse;
				//float3 baseCol = blendVal * (input.lightColor - input.darkColor) + input.darkColor;
 
				//// Coverage
				//float densityVal = density1.z * density0.z + (density0.x - 0.5) * 0.15;
				//densityVal *= weatherVal.x; 
				//float dotUp = saturate(input.weatherUVAndDotVal.w * 10.0);
				//float specular = min((dotUp * -2.0 + 3.0) * dotUp * dotUp, 1.0);
				//densityVal += (1.0 - specular) * 0.2;
				//float coverageVal = densityVal - input.lightDirTSAndCoverage.w;
 
				//// Light
				//densityVal = saturate(dot(density1.xx, density1.yy));
				//densityVal = densityVal * densityVal;
				//baseCol += (densityVal * 0.5) * input.lightColor * _CloudCoverage;
    
				// Wisps
				float wispsVal = SAMPLE_TEXTURE2D(_CloudWispsTex, sampler_CloudWispsTex, input.curlAndWispsUV.zw).w;
				wispsVal = saturate((wispsVal - (1.0 - _CloudWispsCoverage)) / _CloudWispsCoverage);
				wispsVal *= _CloudWispsOpacity;
				float3 wispsCol = wispsVal * (input.lightColor - input.darkColor) + input.darkColor;
				float3 opaqueCol = wispsVal * wispsCol;
				//float3 transparentCol = baseCol - opaqueCol;
				
				//// Alhpa
				//float smoonthness = weatherVal.z * (_CloudSmoothness.y - _CloudSmoothness.x) + _CloudSmoothness.x;
				//smoonthness = densityVal * smoonthness + 0.05;
				//float opacity = specular * saturate(coverageVal / smoonthness) * _CloudOpacity;

				//coverageVal = saturate((_CloudCoverage - 0.5) * 5.0);
				//coverageVal = (coverageVal * -2.0 + 3.0) * coverageVal * coverageVal;
				//float alpha = coverageVal * (1.0 - opacity) + opacity;
				float alpha = 0.0;

				//float3 col = alpha * transparentCol + opaqueCol;
				float3 col = opaqueCol;
				col *= input.weatherUVAndDotVal.z * _CloudSunBrightenIntensity + 1.0;

                return float4(col, alpha);
			}

			ENDHLSL
		}
	}
} 