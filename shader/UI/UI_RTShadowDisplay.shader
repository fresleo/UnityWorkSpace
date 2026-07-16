/*
为 UI 显示 RT 阴影
*/
Shader "XKnight/UI/UI_RTShadowDisplay"
{
    Properties
    {
        [NoScaleOffset] _BaseMap ("RT 图", 2D) = "black" {}
        [MainColor] _ShadowColor ("阴影的颜色", Color) = (0, 0, 0, 0.5)
        _AlphaThreshold ("Alpha 阈值，比它小的都看作是 0", Range(0, 0.1)) = 0.001
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        
        ZWrite Off
        ZTest LEqual
        Cull Back
        
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RTShadowDisplay"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _ShadowColor;
                half _AlphaThreshold;
            CBUFFER_END
            
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half texA = tex.a;
                half alphaFactor = step(_AlphaThreshold, texA) * texA;
                
                half4 outColor = 0;
                outColor.rgb = _ShadowColor.rgb;
                outColor.a = _ShadowColor.a * alphaFactor;
                
                return outColor;
            }
            ENDHLSL
        }
    }
}
