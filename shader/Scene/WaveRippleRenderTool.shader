Shader "XKnight/Tool/RippleRenderTool"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "./Rain.hlsl"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = v.vertex;
                o.uv = v.uv;
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                float3 ripple = CalcuateOfflineRipple(i.uv);

                float3 wave = CalcuateOfflineWave(i.uv * 2.0f);
                // return float4(wave * 0.5 + 0.5, 1.0);

                // return float4(ripple.xyz * 0.5 + 0.5, 1.0f);
                // return float4(wave.xyz * 0.5 + 0.5, 1.0f);
                return float4(wave.xy * 0.5 + 0.5, ripple.xy * 0.5 + 0.5);
            }
            
            ENDHLSL
        }
    }
}
