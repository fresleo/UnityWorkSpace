Shader "XKnight/Scene/PlanarReflectionVisualization"
{
    Properties
    {
    	_Brightness ("Brightness", Range(1.0, 5.0)) = 1.0
    }
	
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Transparent" "Queue" = "Transparent" }
		
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			
			ZWrite Off
			Cull Back
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

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

			// -------------------------------------
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			
			#include "../ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
			#if defined( LOD_FADE_CROSSFADE )
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif
			
			TEXTURE2D(_PlanarReflectionTexture); SAMPLER(sampler_PlanarReflectionTexture);

			struct Attributes
			{
				float4 positionOS			: POSITION;
				float3 normalOS				: NORMAL;
				float2 texcoord				: TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float3	positionWS				: TEXCOORD1;
				float3	normalWS				: TEXCOORD2;
				float3	viewDirWS				: TEXCOORD3;
				UBPA_FOG_COORDS(4)
				float4  positionSS				: TEXCOORD5;
				
				float4	positionCS				: SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			CBUFFER_START(UnityPerMaterial)
				float _Brightness;
			CBUFFER_END

			Varyings LitPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				half3 viewDirWS;
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
				
				// already normalized from normal transform to WS.
				output.normalWS = normalInput.normalWS;
				output.viewDirWS = normalize(GetCameraPositionWS() - vertexInput.positionWS);

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.positionSS = ComputeScreenPos(vertexInput.positionCS);

				//UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

				return output;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				#if defined( LOD_FADE_CROSSFADE )
				LODFadeCrossFade(input.positionCS);
				#endif
				
				half3 reflectionColor = SAMPLE_TEXTURE2D(_PlanarReflectionTexture, sampler_PlanarReflectionTexture, input.positionSS.xy / input.positionSS.w).rgb * _Brightness;

				half VdotN = saturate(dot(input.viewDirWS, input.normalWS));

				half alpha = smoothstep(0.05, 0.5, VdotN);

				//UBPA_APPLY_FOG(input, reflectionColor);
				
				return half4(reflectionColor, alpha);
			}

			ENDHLSL
		}
	}

	CustomEditor "LWGUI.LWGUI"
}
