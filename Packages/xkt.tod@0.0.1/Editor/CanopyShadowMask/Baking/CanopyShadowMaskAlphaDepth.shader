Shader "Hidden/XKnight/CanopyShadowMask/AlphaDepth"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _Cutoff ("Cutoff", Float) = 0.5
        _AlphaTestThreshold ("Alpha Test Threshold", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Name "CanopyAlphaDepth"
            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask RGBA

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float _Cutoff;
            float _AlphaTestThreshold;
            
            float4x4 _CanopyWorldToCamera;
            float _CanopyFarClip;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float depth01 : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float4 worldPos = mul(unity_ObjectToWorld, input.vertex);
                float3 cameraPos = mul(_CanopyWorldToCamera, worldPos).xyz;
                
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.depth01 = saturate(-cameraPos.z / max(_CanopyFarClip, 0.0001));
                
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half alpha = tex2D(_BaseMap, input.uv).a;
                half cutoff = max(_Cutoff, _AlphaTestThreshold);
                clip(alpha - cutoff);
                
                return half4(input.depth01, 1, 0, 1);
            }
            ENDCG
        }
    }

    FallBack Off
}
