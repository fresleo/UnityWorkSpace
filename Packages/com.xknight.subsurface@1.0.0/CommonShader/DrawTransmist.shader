Shader "Hidden/DrawTransmittance"
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

            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _ShapeParams, _TransmissionTint, _ThicknessRemap;

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 ComputeTransmittanceProfile(float thickness, float3 S)
            {
                float3 transmittance = exp(-thickness * S);
                return transmittance * _TransmissionTint;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float thickness = lerp(_ThicknessRemap.x, _ThicknessRemap.y, i.uv.x);
                float3 transmittance = ComputeTransmittanceProfile(thickness, _ShapeParams);
                
                return float4(transmittance, 1);
            }
            ENDCG
        }
    }
}
