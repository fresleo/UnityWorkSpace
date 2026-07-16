Shader "XKnight/Scene/Skybox/Moon"
{
    Properties
    {
        [HDR]_MoonColor("月亮颜色", Color) = (1, 1, 1, 1)
        _MoonTex("月亮纹理 (A通道为遮罩)", 2D) = "white" {}
        _MoonScale("月亮视觉大小调节", Range(0.1, 100)) = 1.0
        _GlowScale("月亮光晕",Range(0,10)) = 1
        _GlowIntensity("光晕强度",Range(0,10)) = 1
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+10" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100

        Pass
        {
            Name "FarPlaneBillboardPass"
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MoonColor;
                float4 _MoonTex_ST;
                float _MoonScale;
                float _GlowScale;
                float _GlowIntensity;
            CBUFFER_END

            TEXTURE2D(_MoonTex);
            SAMPLER(sampler_MoonTex);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                // 1. 获取当前 Quad 物体（月亮中心点）在世界空间的位置
                float3 worldCenterPos = TransformObjectToWorld(float3(0, 0, 0));
                // 2. 将世界空间的中心点转换到观察空间 (View Space)
                float3 viewCenterPos = TransformWorldToView(worldCenterPos);
                // 3. 核心 Billboard 矩阵消除法：
                // 直接在观察空间（正对屏幕的平面）应用顶点局部偏移 (input.positionOS.xy)
                // 这样操作强行让面片的面法线永远垂直于屏幕玻璃，消除了物体的任何 3D 旋转畸变
                float2 vertexOffset = input.positionOS.xy * _MoonScale;
                float3 viewPos = viewCenterPos + float3(vertexOffset, 0.0);
                // 4. 强行将 Z 轴（深度值）推到远裁剪平面
                // _ProjectionParams.z 存储的是当前摄像机的 Far Clip Plane 距离
                // 我们将 Z 设为接近远平面的值（减去一个极小的偏移以防止被 Far Clip 裁剪掉）
                #if defined(UNITY_REVERSED_Z)
                    // 在 DirectX 等使用 Reversed-Z 的平台上，近平面为 1，远平面为 0
                    // 为了让它在天空盒的极远处，我们需要让 View 空间的 Z 与远平面对齐
                    viewPos.z = max(viewPos.z, -_ProjectionParams.z + 0.1);
                #else
                    // 在 OpenGL 等非 Reversed-Z 平台上，Z 越正越远
                    viewPos.z = min(viewPos.z, _ProjectionParams.z - 0.1);
                #endif
                // 5. 最后将计算完的、贴在远平面玻璃上的观察空间坐标，转换到裁剪空间 (Clip Space)
                output.positionCS = TransformWViewToHClip(viewPos);
                // 确保通过非线性反转 Z 轴渲染时，其屏幕深度依然正确地待在天空盒背景层
                #if defined(UNITY_REVERSED_Z)
                    output.positionCS.z = 0.00001; // 极限接近 0（远平面）
                #else
                    output.positionCS.z = output.positionCS.w - 0.00001; // 极限接近 W（远平面）
                #endif

                output.uv = TRANSFORM_TEX(input.uv, _MoonTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MoonTex, sampler_MoonTex, input.uv);
                float2 centerUV = input.uv - float2(0.5, 0.5);
                float dist =  length(centerUV);
                float glow = exp(-dist * _GlowScale); 
                half3 moonBody = texColor.rgb * _MoonColor.rgb;
                half3 moonGlow = _MoonColor.rgb * glow * _GlowIntensity;
                half3 finalRGB = moonBody + moonGlow;
                half finalAlpha = max(texColor.a * _MoonColor.a, glow * 0.5);
                // finalAlpha = 1.0;
                // return half4(_MoonColor.rgb, 1);
                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}