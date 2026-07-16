Shader "Hidden/PVS/PVSRendererForOcculudeeCombinedURP"
{
    SubShader
    {
		Tags{"LightMode" = "UniversalForward" "RenderPipeline" = "UniversalRenderPipeline" "Queue" = "Transparent"}

        Pass
        {
			Blend off
			ZWrite off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(PVSRendererForOcculudeeCombinedURP)
				float4 _PVSCameraPosition;
			CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 idAndDistance : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 idAndDistance : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            bool IsInBounds(float3 position, float3 center, float distance)
            {
                float3 boundsMin = center - float3(distance, distance, distance);
                float3 boundsMax = center + float3(distance, distance, distance);
                float3 validBounds = clamp(position, boundsMin, boundsMax);
                return all(validBounds == position);
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.idAndDistance = input.idAndDistance;
                o.positionWS = input.positionWS;
                return o;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return IsInBounds(input.positionWS, _PVSCameraPosition.xyz, input.idAndDistance.y) ? input.idAndDistance.x + 1e-2f : 0.0f;
            }
			ENDHLSL
        }
    }
}