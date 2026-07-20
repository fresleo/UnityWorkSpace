Shader "Hidden/DrawDiffusionProfile"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _ShapeParam; float _MaxRadius; // See 'DiffusionProfile'

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------
            
            struct Attributes
            {
                float3 vertex     : POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex      : SV_POSITION;
                float2 texcoord    : TEXCOORD0;
            };

            float3 EvalBurleyDiffusionProfile(float r, float3 S)
            {
                float3 exp_13 = exp2(((LOG2_E * (-1.0/3.0)) * r) * S); // Exp[-S * r / 3]
                float3 expSum = exp_13 * (1 + exp_13 * exp_13);        // Exp[-S * r / 3] + Exp[-S * r]

                return (S * rcp(8 * PI)) * expSum; // S / (8 * Pi) * (Exp[-S * r / 3] + Exp[-S * r])
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
     
                output.vertex   = mul(unity_MatrixVP, float4(input.vertex, 1));
                output.texcoord = input.texcoord.xy;
                return output;
            }
            
            float4 Frag(Varyings input) : SV_Target
            {
            
                float  r = _MaxRadius * 0.5 * length(input.texcoord - 0.5); // (-0.25 * R, 0.25 * R)
                float3 S = _ShapeParam.rgb;
                float3 M;

                
                S = S * S;
                M = EvalBurleyDiffusionProfile(r, S) / r; 
                return float4(sqrt(M), 1);
            }
            ENDHLSL
        }
    }
}
