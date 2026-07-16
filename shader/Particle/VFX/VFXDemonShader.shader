Shader "XKnight/Particle/Demon"
{
    Properties
    {
        _FresnelPower("Fresnel Power", Range(0.1, 20)) = 1
        _FresnelScale("Fresnel Scale", Range(0, 20)) = 1  
        
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Range(0.1, 2)) = 1
        
        _NoiseTexture("Noise Texture", 2D) = "black" {}
        [HDR] _NoiseColor("Noise Color", Color) = (1,1,1,1)
        _NoisePower("Noise Power", Range(0.1, 20)) = 1
        _NoiseScale("Noise Scale", Range(0.1, 20)) = 1
        
        _StarTexture("Star Texture", 2D) = "black" {}
        [HDR] _StarColor("Star Color", Color) = (1,1,1,1)
        _StarPower("Star Power", Range(0.1, 20)) = 1
        _StarScale("Star Scale", Range(0.1, 20)) = 1
        
        _Speed("Speed", Range(0.1, 5)) = 1
    }
	
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 positionOS : POSITION;
	            float3 normalOS	  : NORMAL;
	            float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv            : TEXCOORD0;
                float4 screenPos     : TEXCOORD1;
                float3 positionWS    : TEXCOORD2;
	            float3 normalWS	     : TEXCOORD3;
	            float4 tangentWS	 : TEXCOORD4;
                float3 positionVS    : TEXCOORD5;
            	float3 viewDirWS	 : TEXCOORD6;
                float4 positionCS    : SV_POSITION;
            };

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
                half  _NoisePower;
                half  _NoiseScale;
                half  _StarPower;
                half  _StarScale;
                half  _FresnelPower;
                half  _FresnelScale;
                half  _Speed;
                half  _NormalIntensity;

				half4 _StarColor;
                half4 _NoiseColor;
                half4 _NormalMap_ST;
                half4 _NoiseTexture_ST;
                half4 _StarTexture_ST;
				
			CBUFFER_END

            TEXTURE2D(_NormalMap);          SAMPLER(sampler_NormalMap);
            TEXTURE2D(_NoiseTexture);       SAMPLER(sampler_NoiseTexture);
            TEXTURE2D(_StarTexture);        SAMPLER(sampler_StarTexture);

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"

            #define OBJECT_POSITION UNITY_MATRIX_M._m03_m13_m23
            
            Varyings vert (appdata input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionWS = vertexInput.positionWS;
                output.positionVS = vertexInput.positionVS;
                output.normalWS = normalInput.normalWS;
	            real sign = input.tangentOS.w * GetOddNegativeScale();
	            output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
            	output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                
                return output;
            }

			half Fresnel(half power, half bias, half scale, half3 normalWS, half3 viewDir)
			{
				half NdotV = dot(normalWS, viewDir);
				half fresnel = bias + scale * pow(saturate(1.0 - NdotV), power);

				return fresnel;
			}

            half4 frag (Varyings input) : SV_Target
            {
				input.viewDirWS = SafeNormalize(input.viewDirWS);
                float2 uv = input.positionWS.xy - OBJECT_POSITION.xy;
                float2 normalUV = uv * _NormalMap_ST.xy + _NormalMap_ST.zw * GET_GLOBAL_TIME.yy * _Speed;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV), _NormalIntensity);

                float sgn = input.tangentWS.w;      // should be either +1 or -1
	            float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

                half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
                normalWS = NormalizeNormalPerPixel(normalWS);

                half3 normalVS = TransformWorldToViewDir(normalWS);

                float2 noiseUV = (input.positionVS - TransformWorldToView(OBJECT_POSITION) + normalVS.xy) * _NoiseTexture_ST.xy + _NoiseTexture_ST.zw;
                half3 noiseColor = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, noiseUV) * _NoiseColor;
                noiseColor = pow(noiseColor, _NoisePower) * _NoiseScale;

                half revertFresnel = 1.0 - saturate(Fresnel(_FresnelPower, 0.0, _FresnelScale, input.normalWS, input.viewDirWS));
                noiseColor *= revertFresnel;

            	float2 starUV = uv * _StarTexture_ST.xy + _StarTexture_ST.zw * GET_GLOBAL_TIME.x * _Speed;
            	half3 starColor = SAMPLE_TEXTURE2D(_StarTexture, sampler_StarTexture, starUV) * _StarColor;
            	starColor = pow(starColor, _StarPower) * _StarScale;

            	return half4(noiseColor + starColor, 1.0);
            }
            
            ENDHLSL
        }
    }
}
