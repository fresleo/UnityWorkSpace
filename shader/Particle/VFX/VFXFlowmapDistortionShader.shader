Shader "XKnight/Particle/VFXFlowmapDistortionShader"
{
    Properties
    {
        [ToggleOff] _UseCustomData("UseCustomData", Float) = 1
        _Strength("Strength", Range(-0.5, 0.5)) = 0.0
        _FlowmapTexture("Flowmap Texture", 2D) = "black" {}
        _MaskTexture("Mask Texture", 2D) = "white" {}
        [Enum(R,0,G,1,B,2,A,3)] _DistortionMaskChannel("Mask Channel", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            ZTest Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv     : TEXCOORD0;   // 使用customdata1.x 作为强度
            };

            struct v2f
            {
                float4 uv        : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float4 vertex    : SV_POSITION;
            };

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

			CBUFFER_START(UnityPerMaterial)
                half _Strength;
                half _UseCustomData;
                half _DistortionMaskChannel;
			CBUFFER_END

            TEXTURE2D(_MaskTexture);        SAMPLER(sampler_MaskTexture);
            TEXTURE2D(_FlowmapTexture);     SAMPLER(sampler_FlowmapTexture);
            
            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;

                o.screenPos = ComputeScreenPos(o.vertex);

                return o;
            }

            float2 inflate(float2 uv, float2 center, float radius, float strength)
            {
                float dist = distance(uv , center);
                float2 dir = normalize(uv - center);
                float scale = 1.-strength + strength *smoothstep(0.,1.,dist/radius);
                float newDist = dist * scale;
                return center + newDist * dir;
            }

            float3 SampleFlowMap(float2 texUV, float4 uv, float strength)
            {
                half localStrength = _UseCustomData ? uv.z : strength;
                float2 flowVal = (SAMPLE_TEXTURE2D(_FlowmapTexture, sampler_FlowmapTexture, uv.xy).xy * 2.0f - 1.0f) * localStrength;

                float3 baseColor = SampleSceneColor(texUV);
	            float3 col1 = SampleSceneColor(texUV - flowVal);
                half mask = SAMPLE_TEXTURE2D(_MaskTexture, sampler_MaskTexture, uv.xy)[_DistortionMaskChannel];
                
                return lerp(baseColor, col1, mask);
            }

            half4 frag (v2f i) : SV_Target
            {
                half3 color = SampleFlowMap(i.screenPos.xy / i.screenPos.w, i.uv, _Strength);
                
				return half4(color, 1.0f);
            }
            
            ENDHLSL
        }
    }
}
