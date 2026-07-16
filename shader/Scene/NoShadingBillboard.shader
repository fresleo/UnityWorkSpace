Shader "XKnight/Scene/NoShadingBillboard"
{
	Properties
	{
		[MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
		[MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			// -------------------------------------
            // Pipeline keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			
			// -------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#include "../ShaderLibrary/Lighting.hlsl"
			#if defined( LOD_FADE_CROSSFADE )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif
			
			CBUFFER_START(UnityPerMaterial)
				float4	_BaseMap_ST;
				half4	_BaseColor;
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
				float4	positionCS				: SV_POSITION;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings LitPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				//Calculate new billboard vertex position and normal;
				float3 upCamVec = float3( 0, 1, 0 );
				float3 forwardCamVec = -normalize ( UNITY_MATRIX_V._m20_m21_m22 );
				float3 rightCamVec = normalize( UNITY_MATRIX_V._m00_m01_m02 );
				float3x3 rotationCamMatrix = float3x3( rightCamVec, upCamVec, forwardCamVec );

				// TRS
				float3 scale = float3(
					length( GetObjectToWorldMatrix()._m00_m10_m20 ),
					length( GetObjectToWorldMatrix()._m01_m11_m21 ),
					length( GetObjectToWorldMatrix()._m02_m12_m22 ));
				float3 positionWS = mul( input.positionOS.xyz * scale, rotationCamMatrix );
				positionWS += GetObjectToWorldMatrix()._m03_m13_m23;
				
				output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
				output.positionCS = TransformWorldToHClip(positionWS);
				return output;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				#if defined( LOD_FADE_CROSSFADE )
				LODFadeCrossFade(input.positionCS);
				#endif

				half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
				return albedoAlpha;
			}

			ENDHLSL
		}
	}
}
