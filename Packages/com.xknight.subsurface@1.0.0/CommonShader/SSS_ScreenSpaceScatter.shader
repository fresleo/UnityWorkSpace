//author:calvin
//date:26/6/17
//description:
//          pass0：散射shader
//          pass1：合成shader


Shader "Hidden/ScreenSpaceScatter"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off
        ZTest Always
        Cull Off

        // ------------------------------------------------------------------ //
        Pass
        {
            Name "SSSScatterFallback"
            Blend Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment FragScatter
            #pragma target   4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "./Libs//SubsurfaceScattering.hlsl"

            #ifndef MILLIMETERS_PER_METER
            #define MILLIMETERS_PER_METER 1000.0
            #endif

            #ifndef SSS_PIXELS_PER_SAMPLE
            #define SSS_PIXELS_PER_SAMPLE 4
            #endif


            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }


            float4 FragScatter(Varyings input) : SV_Target
            {
                // SV_POSITION.xy 已是像素中心, 截断得到整数像素索引
                uint2 pixelCoord = (uint2)input.positionCS.xy;
                int2 sCoord = clamp((int2)pixelCoord, 0, (int2)_ScreenSize.xy - 1);

                float4 centerSample = LOAD_TEXTURE2D_X(_SSSDiffuse, sCoord);
                float3 centerIrradiance = centerSample.rgb;
                float centerShadow = centerSample.a;

                // stencil 等价: 无 SSS 光照的像素直接输出 0 (compute: StoreResult(0))
                if (!TestLightingForSSS(centerIrradiance))
                    return float4(0, 0, 0, 1);

                float centerDepth = LoadSceneDepth(sCoord);

                // 等价 GetPositionInput(pixelCoord, _ScreenSize.zw):
                //   positionNDC = (pixelCoord + 0.5) * invScreenSize 
                uint2 positionSS = pixelCoord;
                float2 centerPosNDC = (pixelCoord + 0.5) * _ScreenSize.zw;

                float4 albedo = 0;
                uint materialID = 0;
                real DiffuseMask = 0;
                DecodeFromSSSBuffer(positionSS, albedo, materialID, DiffuseMask);
                if (DiffuseMask <= 0)
                {
                    return float4(albedo.rgb * centerIrradiance, 1);
                }
                float3 S = _ShapeParamsAndFreePath[materialID].xyz;
                float d = _ShapeParamsAndFreePath[materialID].w;
                float metersPerUnit = _WorldScaleAndMaxRadiusAndThicknessRemaps[materialID].x;
                float filterRadius = _WorldScaleAndMaxRadiusAndThicknessRemaps[materialID].y;
                float shadowStrength = asfloat(_HashAndShadowStrenthAndThicknessOffset[materialID].y);

                float2 cornerPosNDC = centerPosNDC + 0.5 * _ScreenSize.zw;

                // 屏幕点 (NDC + 深度) 还原到相机空间, 用于估算屏幕上每毫米对应多少像素
                float3 centerPosVS = ComputeViewSpacePosition(centerPosNDC, centerDepth, _InvProjMatrix);
                float3 cornerPosVS = ComputeViewSpacePosition(cornerPosNDC, centerDepth, _InvProjMatrix);

                float mmPerUnit = MILLIMETERS_PER_METER * metersPerUnit;
                float unitsPerMm = rcp(mmPerUnit);

                // 透视: 远处 units/pixel 大, 近处小
                float unitsPerPixel = max(0.0001f, 2 * abs(cornerPosVS.x - centerPosVS.x));
                float pixelsPerMm = rcp(unitsPerPixel) * unitsPerMm;

                // 散射半径(mm) -> 像素半径 -> 覆盖面积 -> 采样数
                float filterArea = PI * Sq(filterRadius * pixelsPerMm);
                uint sampleCount = (uint)(filterArea * rcp(SSS_PIXELS_PER_SAMPLE));

                // 散射圆在屏上不足 1 像素: 直接输出 albedo * 中心辐照度
                if (sampleCount < 1)
                    return float4(albedo.rgb * centerIrradiance, 1);

                float phase = TWO_PI * GenerateHashedRandomFloat(uint3(pixelCoord, (uint)(centerDepth * 16777216)));

                // NOTE: compute 路径由 C# 设置 _SssSampleBudget=32; 回退路径同样需要设置
                //       这里加一个兜底默认值, 防止未设置时 n=0 导致 SSS 区域变黑。
                int budget = (_SssSampleBudget > 0) ? _SssSampleBudget : 32;
                uint n = min(sampleCount, (uint)budget);

                float3 totalIrradiance = 0;
                float3 totalWeight = 0;
                float linearDepth = LinearEyeDepth(centerDepth, _ZBufferParams);

                for (uint i = 0; i < n; i++)
                {
                    EvaluateSample(i, n, (int2)pixelCoord, 0,
                                   S, d, mmPerUnit, pixelsPerMm, phase,
                                   totalIrradiance, totalWeight, linearDepth);
                }

                totalWeight = max(totalWeight, FLT_MIN);

                // ==================== Shadow ====================
                float3 scattered = totalIrradiance / totalWeight;
                scattered *= 1 + centerShadow * shadowStrength;
                // ================== shadow end ==================

                // compute: StoreResult(albedo * albedo * scattered)
                return float4(albedo.rgb * albedo.rgb * scattered, 1);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ //

        Pass
        {

            Name "SSSComposite"

            Blend [_SrcBlend] [_DstBlend]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_SSSDiffuse);
            TEXTURE2D_X(_SSSScatterResult);
            float _SSS_Strenth;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            float4 FragComposite(Varyings i) : SV_Target
            {
                int2 size = (int2)_ScreenSize.xy;
                int2 px = clamp(size, int2(0, 0), size - 1);
                float3 c = LOAD_TEXTURE2D_X(_SSSScatterResult, px).rgb;
                float a = LOAD_TEXTURE2D_X(_SSSDiffuse, px).b;
                a = step(1e-4, a) * _SSS_Strenth;
                return float4(c, a);
            }
            ENDHLSL
        }


    }
    Fallback Off
}