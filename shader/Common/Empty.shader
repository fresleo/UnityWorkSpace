Shader "Hidden/XKnight/Empty"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
		
		Pass
		{
			Tags { "LightMode" = "UniversalForward" }

			ColorMask 0

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				float4	positionCS : SV_POSITION;
			};

			Varyings LitPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				return output;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				return 0;
			}
			
			ENDHLSL
		}
	}
}
