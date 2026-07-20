Shader "Hidden/ToonPostProcessing/WaterColor"
{
    Properties
    {
        _WaterColor ("水彩调色", Color) = (1, 1, 1, 1)

        _XRadius ("X半径", Float) = 1
        _YRadius ("Y半径", Float) = 1
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float2 uv : TEXCOORD0;
        float4 positionCS : SV_POSITION;
        float4 screenPos : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert_Kawahara(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

        // 计算屏幕投影坐标
        // output.screenPos = ComputeGrabScreenPos(output.positionCS);
        output.screenPos = ComputeScreenPos(output.positionCS);
        output.uv = input.texcoord;

        return output;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "WaterColorKawahara"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vert_Kawahara
            #pragma fragment Frag_Kawahara

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor;
                half _XRadius;
                half _YRadius;
            CBUFFER_END

            // 摄像机的颜色纹理
            TEXTURE2D_X(_CameraColorTexture); SAMPLER(sampler_CameraColorTexture);
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/PostProcessing/XKnightForwardBuffers.hlsl"

            #define MAX_RANGE 8
            
            //不同平台坐标转换
            inline float4 ComputeGrabScreenPos(float4 pos)
            {
                #if UNITY_UV_STARTS_AT_TOP
                float scale = -1.0;
                #else
                float scale = 1.0;
                #endif //UNITY_UV_STARTS_AT_TOP

                float4 o = pos;
                o.y = pos.w * 0.5f;
                o.y = (pos.y - o.y) * _ProjectionParams.x * scale + o.y;

                return o;
            }

            //Gobal.hlsl
            half4 GetKernelMeanAndVariance(float2 UV, half4 Range, float2x2 RotationMatrix)
            {
                // float2 TexelSize = View.BufferSizeAndInvSize.zw;
                float2 TexelSize = _ScreenSize.zw;
                float3 Mean = float3(0, 0, 0);
                float3 Variance = float3(0, 0, 0);
                half Samples = 0;

                //水平-循环边界
                UNITY_UNROLLX(MAX_RANGE)
                for (int x = Range.x; x <= Range.y; x++)
                {
                    //垂直-循环边界
                    UNITY_UNROLLX(MAX_RANGE)
                    for (int y = Range.z; y <= Range.w; y++)
                    {
                        float2 Offset = mul(float2(x, y) * TexelSize, RotationMatrix);
                        // float3 PixelColor = SceneTextureLookup(UV + Offset, 14, false).rgb;
                        float2 uv = UV + Offset;

                        float3 PixelColor = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uv).rgb;

                        //平均值
                        Mean += PixelColor;
                        //方差
                        Variance += PixelColor * PixelColor;
                        Samples++;
                    }
                }

                //计算平均值与方差
                Mean /= Samples;
                Variance = Variance / Samples - Mean * Mean;
                //方差分布在RGB通道上，为了解决这个问题，第三行将通道相加得出总方差。
                float TotalVariance = Variance.r + Variance.g + Variance.b;
                //返回平均值与方差
                return half4(Mean.r, Mean.g, Mean.b, TotalVariance);
            }

            float GetPixelAngle(float2 UV)
            {
                // float2 TexelSize = View.BufferSizeAndInvSize.zw;
                float2 TexelSize = _ScreenSize.zw;
                float GradientX = 0;
                float GradientY = 0;
                float SobelX[9] = {-1, -2, -1, 0, 0, 0, 1, 2, 1};
                float SobelY[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
                int i = 0;

                UNITY_UNROLL
                for (int x = -1; x <= 1; x++)
                {
                    UNITY_UNROLL
                    for (int y = -1; y <= 1; y++)
                    {
                        // 1
                        float2 Offset = float2(x, y) * TexelSize;
                        // float3 PixelColor = SceneTextureLookup(UV + Offset, 14, false).rgb;
                        float2 uv = UV + Offset;

                        half3 PixelColor = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uv).rgb;
                        float PixelValue = dot(PixelColor, half3(0.3, 0.59, 0.11));

                        // 2
                        GradientX += PixelValue * SobelX[i];
                        GradientY += PixelValue * SobelY[i];
                        i++;
                    }
                }

                return atan(GradientY / GradientX);
            }
            
            half4 Frag_Kawahara(Varyings input) : SV_Target
            {
                // 将屏幕投影坐标归一化到0-1范围得到UV坐标
                float2 UV = input.screenPos.xy / input.screenPos.w;
                //UV：当前像素的UV坐标
                // float2 UV = GetDefaultSceneTextureUV(Parameters, 14);
                
                // 水彩的深度遮罩
                float mask = Sample_WaterColorMask(UV);
                UNITY_BRANCH
                if(mask <= 0)
                {
                    half4 pixelColor = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, UV);
                    return half4(pixelColor.rgb, 1);
                }
                
                //Range：用于保存当前内核的 for 循环边界
                half4 Range;
                //MeanAndVariance：保存每个内核的均值和方差的数组
                half4 MeanAndVariance[4];

                float Angle = GetPixelAngle(UV);
                float2x2 RotationMatrix = float2x2(cos(Angle), -sin(Angle), sin(Angle), cos(Angle));

                _XRadius = round(_XRadius);
                _YRadius = round(_YRadius);

                Range = half4(-_XRadius, 0, -_YRadius, 0);
                MeanAndVariance[0] = GetKernelMeanAndVariance(UV, Range, RotationMatrix);

                Range = half4(0, _XRadius, -_YRadius, 0);
                MeanAndVariance[1] = GetKernelMeanAndVariance(UV, Range, RotationMatrix);

                Range = half4(-_XRadius, 0, 0, _YRadius);
                MeanAndVariance[2] = GetKernelMeanAndVariance(UV, Range, RotationMatrix);

                Range = half4(0, _XRadius, 0, _YRadius);
                MeanAndVariance[3] = GetKernelMeanAndVariance(UV, Range, RotationMatrix);

                //选择方差最小的内核
                // 1
                half3 FinalColor = MeanAndVariance[0].rgb; //最终颜色,初始化为第一个内核的均值
                half MinimumVariance = MeanAndVariance[0].a; //最小方差,初始化为第一个内核的方差

                // 2
                UNITY_UNROLL
                for (int i = 1; i < 4; i++)
                {
                    half4 item = MeanAndVariance[i];
                    UNITY_FLATTEN
                    if (item.a < MinimumVariance)
                    {
                        FinalColor = item.rgb;
                        MinimumVariance = item.a;
                    }
                }

                // 最终水彩颜色
                half3 finalWaterColor = FinalColor.rgb * _WaterColor.rgb;
                return half4(finalWaterColor, 1);
            }
            ENDHLSL
        }
    }
}