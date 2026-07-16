// 水坑
Shader "XKnight/Scene/Puddle"
{
	Properties
	{
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		[MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
		[MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
		_Smoothness ("Smoothness", Range(0.0, 1.0)) = 1.0
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "AlphaTest" }

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// -------------------------------------
            // Unity defined keywords
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

			// -------------------------------------
            // Pipeline keywords
			#pragma multi_compile _ _HEIGHT_FOG
			#pragma shader_feature _RECORDING_QUALITY

			//--------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			
			#include "../ShaderLibrary/Lighting.hlsl"
			#include "../ShaderLibrary/ExtraBlend.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			#if defined( LOD_FADE_CROSSFADE )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif

			CBUFFER_START(UnityPerMaterial)
				float4	_BaseMap_ST;
				half4	_BaseColor;
				half    _Smoothness;
				half	_Cutoff;
			CBUFFER_END

			TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

			struct Attributes
			{
				float4 positionOS			: POSITION;
				float3 normalOS				: NORMAL;
				float2 texcoord				: TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float2	uv						: TEXCOORD0;

				UBPA_FOG_COORDS(1)
				DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 2);

				float3	normalWS				: TEXCOORD3;
				float3	positionWS				: TEXCOORD4;
				float4	positionCS				: SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings LitPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

				output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

				output.normalWS = normalInput.normalWS;

				OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

				output.positionWS = vertexInput.positionWS;
				output.positionCS = vertexInput.positionCS;

				UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

				return output;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				
				half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
				clip(albedoAlpha.a * _BaseColor.a - _Cutoff);

				#if defined( LOD_FADE_CROSSFADE )
				LODFadeCrossFade(input.positionCS);
				#endif

				Light light = GetMainLight();
				half3 viewDir = GetWorldSpaceViewDir(input.positionWS);

				half3 reflectVector = reflect(-viewDir, input.normalWS);
				half mip = ComputeReflectionCaptureMipFromRoughness(1.0f - _Smoothness, 7);
				half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));
				half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);

				half NoV = saturate(dot(input.normalWS, viewDir));
				half fresnelTerm = 1.0 - Pow4(1.0 - NoV);
				
				half3 albedo = _BaseColor.rgb;

				half3 bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, input.normalWS);
				half3 color = albedo * bakedGI;

				UBPA_APPLY_FOG(input, color);

				return half4(irradiance, fresnelTerm);
			}

			ENDHLSL
		}
	}
}