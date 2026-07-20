Shader "Garena/RenderFeature/PerObjectShadowReceiver"
{
    Properties
    {
        _Color ("_Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "./GPerObjectShadow.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.positionWS = vertexInput.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half perObjectShadow = PerObjectRealtimeShadow(IN.positionWS);
                color.xyz *= _Color.xyz;
                return color * perObjectShadow;
            }

            ENDHLSL
        }
    }
}