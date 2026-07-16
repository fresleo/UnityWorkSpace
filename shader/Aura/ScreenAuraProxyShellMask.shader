Shader "XKnight/ScreenAura/ProxyShellMask"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ScreenEffMask"
            Tags { "LightMode" = "ScreenEffMask" }

            ZWrite On
            ZTest LEqual
            Cull Back
            ColorMask RG
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _ScreenEffMaskID;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half active = step(0.5h, _ScreenEffMaskID);
                clip(active - 0.5h);
                half encodedId = saturate(_ScreenEffMaskID * 0.125h);
                return half4(active, encodedId, 0.0h, 0.0h);
            }
            ENDHLSL
        }
    }
}
