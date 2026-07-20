Shader "Hidden/ToonPostProcessing/SobelOutline"
{
    Properties
    {
        _OutlineColor ("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineData ("描边的外部参数", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque"
        }

        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "SobelOutline"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fragment _ _MRT_BUFFER

            #include "./SobelOutlineInput.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/PostProcessing/XKnightForwardBuffers.hlsl"

            float4 SamplePixel(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
            }
            
            float Sobel(float2 UV)
            {
                float3 x = 0;
                float3 y = 0;

                float2 texelSize = _BlitTexture_TexelSize.xy;

                x += SamplePixel(UV + float2(-texelSize.x, -texelSize.y)).rgb * -1.0f;
                x += SamplePixel(UV + float2(-texelSize.x, 0)).rgb * -2.0f;
                x += SamplePixel(UV + float2(-texelSize.x, texelSize.y)).rgb * -1.0f;

                x += SamplePixel(UV + float2(texelSize.x, -texelSize.y)).rgb * 1.0f;
                x += SamplePixel(UV + float2(texelSize.x, 0)).rgb * 2.0f;
                x += SamplePixel(UV + float2(texelSize.x, texelSize.y)).rgb * 1.0f;

                y += SamplePixel(UV + float2(-texelSize.x, -texelSize.y)).rgb * -1.0f;
                y += SamplePixel(UV + float2(0, -texelSize.y)).rgb * -2.0f;
                y += SamplePixel(UV + float2(texelSize.x, -texelSize.y)).rgb * -1.0f;

                y += SamplePixel(UV + float2(-texelSize.x, texelSize.y)).rgb * 1.0f;
                y += SamplePixel(UV + float2(0, texelSize.y)).rgb * 2.0f;
                y += SamplePixel(UV + float2(texelSize.x, texelSize.y)).rgb * 1.0f;

                float xLum = dot(x, float3(0.2126729, 0.7151522, 0.0721750));
                float yLum = dot(y, float3(0.2126729, 0.7151522, 0.0721750));

                return saturate(sqrt(xLum * xLum + yLum * yLum) * OUTLINE_THICKNESS);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 UV = input.texcoord;

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV);

                // 深度排除
                float cameraDepth = Sample_CameraDepth_01(UV);
                #if !defined( _MRT_BUFFER )
                float maskDepth = Sample_SceneSpaceOutlineMask_Depth_01(UV);
                UNITY_BRANCH if (cameraDepth < maskDepth * DEPTH_COMPARE_FACTOR) return col;
                #endif

                // 遮罩排除
                float mask = Sample_SceneSpaceOutlineMask(UV);
                UNITY_BRANCH if (mask == 0) return col;

                // 过滤边缘
                float edge = Sobel(UV);
                // 主要靠 pow 来增强高频细节，破坏线性趋势，使其不那么容易形成水波纹
                edge = pow(edge * OUTLINE_EDGE_MULTIPLIER, OUTLINE_EDGE_BIAS);

                // 深度渐隐
                float depthFade = max(0, (OUTLINE_DISTANCE_FADE - cameraDepth) / OUTLINE_DISTANCE_FADE);
                float outline = edge * depthFade;
                
                return lerp(col, _OutlineColor, outline);
            }
            ENDHLSL
        }
    }
}