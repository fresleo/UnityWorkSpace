Shader "Hidden/PVS/PVSRendererDebugURP"
{
    Properties
    {
		[HideInInspector][Enum(UnityEngine.Rendering.CullMode)] _Cull ("_Cull", Float) = 2
    }

    SubShader
    {
		Tags{"LightMode" = "UniversalForward" "RenderPipeline" = "UniversalRenderPipeline"}

        Pass
        {
            Cull [_Cull]

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(PVSRendererDebugURP)
				float4 _DebugColor;
			CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
				float4 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
				float4 diffuseColor : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normalOS.xyz);
				float ndl = dot(normalWS, float3(0.214, 0.3420, 0.9145));

                o.diffuseColor = ndl * _DebugColor;
                return o;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return input.diffuseColor;
            }
			ENDHLSL
        }
    }
}