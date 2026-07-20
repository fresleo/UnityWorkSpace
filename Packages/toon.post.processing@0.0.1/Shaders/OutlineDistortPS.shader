Shader "Hidden/ToonPostProcessing/OutlineDistortPS"
{
    Properties
    {
        _MainTex ("目标遮罩纹理", 2D) = "white" { }
        _DistortTex ("扭曲纹理", 2D) = "white" { }
        
        [HDR] _OutlineColor ("轮廓颜色", Color) = (1, 1, 1, 1)
        _OutlineAlpha ("轮廓 Alpha", Range(0, 1)) = 1
        
        _DistortUVScrollSpeed ("扭曲纹理的 UV 滚动速度（有方向性）", Vector) = (0, -1, 0, 0) // c#里使用
        _DistortScreenScale ("扭曲的屏幕缩放（有方向性）", Vector) = (0, -1, 0, 0)
        
        _AccumulatedUVOffset ("累积 UV 偏移", Vector) = (0, 0, 0, 0) // 界面不显示
        _AccumulatedUVOffset2 ("累积 UV 偏移 2", Vector) = (0, 0, 0, 0) // 界面不显示
        _AccumulatedUVOffset3 ("累积 UV 偏移 3", Vector) = (0, 0, 0, 0) // 界面不显示
        
        _MultipleSampleOn ("启用多次采样", Range(0, 1)) = 0 // c#使用
        _OffsetSampleUV ("偏移采样 UV，xy = 第2次，zw = 第3次", Vector) = (0.2, 0.2, -0.2, -0.2)
        _OffsetSampleTime ("偏移时间 x = 第2次，y = 第3次", Vector) = (0.3, 0.7, 0, 0) // c#使用
        _AppendDistortStrength ("叠加扰动强度", Float) = 1
        
        _DisturbanceIntensity ("扰动强度", Float) = 1
        _YAxisStretch ("Y 轴拉伸", Float) = 1
        
        _GradientMaskOn ("启用渐变遮罩", Range(0, 1)) = 0 // 这个是跟着 setting 中的状态变化的
        _GradientIntensity ("渐变强度", Float) = 1
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }
        
        Cull Off ZWrite Off ZTest Always
        Blend One OneMinusSrcAlpha // 预乘 Alpha
        //Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            Name "OutlineDistortPS"
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #pragma multi_compile_local_fragment _ _MULTIPLE_SAMPLE_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _DistortTex_ST;
                
                half4 _OutlineColor;
                half _OutlineAlpha;
                
                half4 _DistortUVScrollSpeed, _DistortScreenScale;
                half4 _AccumulatedUVOffset, _AccumulatedUVOffset2, _AccumulatedUVOffset3;
                half4 _OffsetSampleUV;
                half _AppendDistortStrength;
                
                half _DisturbanceIntensity;
                
                half _YAxisStretch;
                
                half _GradientMaskOn;
                half _GradientIntensity;
            CBUFFER_END
            
            TEXTURE2D(_CameraCharacterDepthTexture); SAMPLER(sampler_CameraCharacterDepthTexture);
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_DistortTex); SAMPLER(sampler_DistortTex);
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };
            
            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                
                output.uv = input.uv;
                
                return output;
            }
            
            float2 Panner(float2 uv, float2 speed, float time)
            {
                return uv + speed * time;
            }
            
            float2 Panner(float2 uv, float2 accumulatedOffset)
            {
                return uv + accumulatedOffset;
            }
            
            #define FAR_DEPTH_THRESHOLD     0.999h
            
            half4 Fragment(Varyings input) : SV_Target
            {
                // 先确保把角色不会被挡住
                half4 cameraCharacterDepthColor = SAMPLE_TEXTURE2D(_CameraCharacterDepthTexture, sampler_CameraCharacterDepthTexture, input.uv);
                half cameraCharacter01Depth = Linear01Depth(cameraCharacterDepthColor.r, _ZBufferParams);
                clip(cameraCharacter01Depth - FAR_DEPTH_THRESHOLD); // cameraCharacter01Depth < FAR_DEPTH_THRESHOLD , clip(-1)
                
                float nearPlane = _ProjectionParams.y;
                float farPlane = _ProjectionParams.z;
                float screenY = input.uv.y;
                
                float2 distortUV = input.uv * _DistortTex_ST.xy + _DistortTex_ST.zw;
                // float time = _TimeParameters.x;
                
                // 多次采样噪波
                #if defined(_MULTIPLE_SAMPLE_ON)
                // 第1次
                // float2 distortUV1 = Panner(distortUV, _DistortUVScrollSpeed.xy, time);
                float2 distortUV1 = Panner(distortUV, _AccumulatedUVOffset);
                half4 distortColor1 = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortUV1);
                half distortValue1 = distortColor1.r;

                // 第2次
                float2 distortUV2 = distortUV + _OffsetSampleUV.xy;
                // distortUV2 = Panner(distortUV2, _DistortUVScrollSpeed.xy, time + _OffsetSampleTime.x);
                distortUV2 = Panner(distortUV2, _AccumulatedUVOffset2);
                half4 distortColor2 = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortUV2);
                half distortValue2 = distortColor2.r;

                // 第3次
                float2 distortUV3 = distortUV + _OffsetSampleUV.zw;
                // distortUV3 = Panner(distortUV3, _DistortUVScrollSpeed.xy, time + _OffsetSampleTime.y);
                distortUV3 = Panner(distortUV3, _AccumulatedUVOffset3);
                half4 distortColor3 = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortUV3);
                half distortValue3 = distortColor3.r;
                
                // 叠加多次结果
                half distortValue = distortValue1;
                half appendDistortValue = (distortValue2 + distortValue3) * _AppendDistortStrength;
                distortValue += appendDistortValue;
                
                // 单次采样噪波
                #else
                // distortUV = Panner(distortUV, _DistortUVScrollSpeed.xy, time);
                distortUV = Panner(distortUV, _AccumulatedUVOffset);
                half4 distortColor = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortUV);
                half distortValue = distortColor.r;
                
                #endif
                
                // 最终扰动值
                distortValue *= _DisturbanceIntensity; 
                
                // 沿着屏幕的 Y 轴拉伸。从 1 开始 lerp 是为了产生底部小，顶部大的效果。
                // todo: 这不是最好的选择，不如直接在 mask 那边操作
                half stretchFactor = lerp(1.0, _YAxisStretch, screenY); 
                
                float2 appendUV = distortValue * _DistortScreenScale.xy;
                appendUV.y *= stretchFactor;
                
                float2 mainUV = input.uv + appendUV;
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV);
                half mainRawDepth = mainColor.r;
                
                // 主要是为了用来兼容 OpenGlES 的，在远近平面之间的深度才是有效的
                half mainEyeDepth = LinearEyeDepth(mainRawDepth, _ZBufferParams);
                half validDepth = step(nearPlane + 0.1, mainEyeDepth) * step(mainEyeDepth, farPlane - 0.1);
                
                // 计算 alpha
                half main01Depth = Linear01Depth(mainRawDepth, _ZBufferParams);
                half outlineAlpha = (1.0 - main01Depth) * validDepth;
                
                // 颜色渐变
                half fadeValue = mainColor.g * _GradientIntensity;
                fadeValue = lerp(1, fadeValue, _GradientMaskOn);
                outlineAlpha *= fadeValue;
                
                // 控制全局透明度
                outlineAlpha *= _OutlineAlpha;
                
                // 输出颜色
                half4 color;
                color = half4(_OutlineColor.rgb * outlineAlpha, 0);
                // color = half4(_OutlineColor.rgb, outlineAlpha);
                return color;
            }
            ENDHLSL
        }
    }
}