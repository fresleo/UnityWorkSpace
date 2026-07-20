//author:calvin
//date:26/7/13
//description:
//            1.散射拓展函数
//            2.cache定义

#ifndef SUBSURFACE_SCATTERING_INCLUDED
#define SUBSURFACE_SCATTERING_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Fibonacci.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#define GROUP_SIZE_1D               16   //group大小
#define GROUP_SIZE_2D               (GROUP_SIZE_1D * GROUP_SIZE_1D)//wave 大小
#define TEXTURE_CACHE_BORDER        2    //纹理边界，必须大于等于1，否则会有问题

//每条边界需要的像素(2行)
#define TEXTURE_CACHE_SIZE_1D       (GROUP_SIZE_1D + 2 * TEXTURE_CACHE_BORDER)//20

//一个Thread需要的总像素 ： 256主体像素 + border像素
#define TEXTURE_CACHE_SIZE_2D       (TEXTURE_CACHE_SIZE_1D * TEXTURE_CACHE_SIZE_1D)//400

#define SSS_PIXELS_PER_SAMPLE       4

groupshared float2 textureCache0[TEXTURE_CACHE_SIZE_2D]; //存储R和G
groupshared float2 textureCache1[TEXTURE_CACHE_SIZE_2D]; //{irradiance.b, deviceDepth}

#pragma multi_compile __ SSS_FORCE_DIRECT_LOAD
//----------------------------------------Varible----------------------------
TEXTURE2D(_SSSDiscKernel);
SAMPLER(sampler_SSSDiffuse);
SAMPLER(sampler_SSSAlbedo);
TEXTURE2D_X(_SSSDiffuse); 
TEXTURE2D_X(_SSSAlbedo);


int _SssSampleBudget;

//--------------------------------------end =---------------------------------


GLOBAL_CBUFFER_START(ShaderVariableDiffusionParams, b2)
    float4 _ShapeParamsAndFreePath[16];
    float4 _TransmissionTintAndFresnel[16];
    float4 _WorldScaleAndMaxRadiusAndThicknessRemaps[16];
    uint4 _HashAndShadowStrenthAndThicknessOffset[16]; //这里用uint否则丢失精度，x：hash y：shadowstrenth  z:thicknessoffset
    uint _DiffusionParametersCount;
    uint _pad0, _pad1, _pad2;
CBUFFER_END





void DecodeFromSSSBuffer(uint2 positionSS, out float4 albedo, out uint materailID,out real DiffuseMask)
{
    float4 sssBuffer = LOAD_TEXTURE2D_X(_SSSAlbedo, positionSS);

    albedo = sssBuffer;
    UnpackFloatInt8bit(sssBuffer.a, 16, DiffuseMask, materailID);
}

//检查是否需要sss
bool TestLightingForSSS(float3 subsurfaceLighting)
{
    return subsurfaceLighting.b > 0;
}


//将每个20*20的像素点都进行存入cache
void StoreSampleToCacheMemory(float4 value, int2 cacheCoord)
{
    //进行拍平：y * 宽度 + x
    int linearCoord = Mad24(TEXTURE_CACHE_SIZE_1D, cacheCoord.y, cacheCoord.x);
    //一个 float4 = 16 bytes
    //两个 float2 = 8 bytes × 2，bank 分布更均匀
    textureCache0[linearCoord] = value.rg;
    textureCache1[linearCoord] = value.ba;
}

float4 LoadSampleFromCacheMemory(int2 cacheCoord)
{
    int linearCoord = Mad24(TEXTURE_CACHE_SIZE_1D, cacheCoord.y, cacheCoord.x);

    return float4(textureCache0[linearCoord],
                  textureCache1[linearCoord]);
}

float4 LoadSampleFromVideoMemory(int2 pixelCoord)
{
    int2 p = clamp(pixelCoord, 0, (int2)_ScreenSize.xy - 1);
    float3 irradiance = LOAD_TEXTURE2D_X(_SSSDiffuse, p).rgb;
    float depth = LoadSceneDepth(p); // 不乘 _RTHandleScale
    return float4(irradiance, depth); // 返回原始设备深度，由调用方线性化
}

float4 LoadSample(int2 pixelCoord, int2 cacheOffset)
{
    float4 value;
    //判断是否在 shared memory 缓存内
    int2 cacheCoord = pixelCoord - cacheOffset;
    bool isInCache = max((uint)cacheCoord.x, (uint)cacheCoord.y) < TEXTURE_CACHE_SIZE_1D;

    if (isInCache)
    {
        value = LoadSampleFromCacheMemory(cacheCoord);
    }
    else
    {
        value = LoadSampleFromVideoMemory(pixelCoord);
    }

    value.a = LinearEyeDepth(value.a, _ZBufferParams);

    return value;
}


void SampleBurleyDiffusionProfile(float u, float rcpS, out float r, out float rcpPdf)
{
    u = 1 - u; // Convert CDF to CCDF

    float g = 1 + 4 * u * (2 * u + sqrt(1 + 4 * u * u));
    float n = exp2(log2(g) * (-1.0 / 3.0)); // g^(-1/3)
    float p = g * n * n; // g^(+1/3)
    float c = 1 + p + n; // 1 + g^(+1/3) + g^(-1/3)
    float d = 3 / LOG2_E * 2 + 3 / LOG2_E * log2(u); // 3 * Log[4 * u]
    float x = (3 / LOG2_E) * log2(c) - d; // 3 * Log[c / (4 * u)]


    float rcpExp = c * c * c * rcp(4 * u * (c * c + 4 * u * (4 * u)));

    r = x * rcpS;
    rcpPdf = 8 * PI * rcpS * rcpExp; // (8 * Pi) / s / (Exp[-s * r / 3] + Exp[-s * r])
}


float3 EvalBurleyDiffusionProfile(float r, float3 S)
{
    float3 exp_13 = exp2(LOG2_E * (-1.0 / 3.0) * r * S); // Exp[-S * r / 3]
    float3 expSum = exp_13 * (1 + exp_13 * exp_13); // Exp[-S * r / 3] + Exp[-S * r]

    return (S * rcp(8 * PI)) * expSum; // S / (8 * Pi) * (Exp[-S * r / 3] + Exp[-S * r])
}

float3 ComputeBilateralWeight(float xy2, float z, float mmPerUnit, float3 S, float rcpPdf)
{
    float r = sqrt(xy2 + (z * mmPerUnit) * (z * mmPerUnit));
    float area = rcpPdf;
    return EvalBurleyDiffusionProfile(r, S) * area;
}

float4 LoadSampleDirect(int2 pixelCoord)
{
    int2 p = clamp(pixelCoord, 0, (int2)_ScreenSize.xy - 1);
    float3 irradiance = LOAD_TEXTURE2D_X(_SSSDiffuse, p).rgb;
    float depth = LoadSceneDepth(p); // 原始设备深度
    float4 value = float4(irradiance, depth);
    value.a = LinearEyeDepth(value.a, _ZBufferParams); // 线性视空间深度
    return value;
}

float4 LoadSampleAuto(int2 pixelCoord, int2 cacheOffset)
{
    #if defined(SSS_FORCE_DIRECT_LOAD)
    float4 textureSample = LoadSampleDirect(pixelCoord);
    return textureSample;
    #else
    float4 textureSample = LoadSample(pixelCoord, cacheOffset);
    return textureSample;
    #endif
}

//pixelCoord ：中心像素

void EvaluateSample(uint i, uint n, int2 pixelCoord,
                    int2 cacheOffset, float3 S, float d,
                    float mmPerUnit, float pixelsPerMm, float phase,
                    inout float3 totalIrradiance, inout float3 totalWeight, float linearDepth)
{
    const float scale = rcp(n); // 1/n
    const float offset = rcp(n) * 0.5;

    float sinPhase, cosPhase;
    sincos(phase, sinPhase, cosPhase);

    float r, rcpPdf;

    SampleBurleyDiffusionProfile(i * scale + offset, d, r, rcpPdf);

    float phi = SampleDiskGolden(i, n).y;
    float sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);

    float sinPsi = cosPhase * sinPhi + sinPhase * cosPhi; // sin(phase + phi)
    float cosPsi = cosPhase * cosPhi - sinPhase * sinPhi; // cos(phase + phi)
    //position ：邻居采样点
    float2 position = pixelCoord + (int2)round(pixelsPerMm * r * float2(cosPsi, sinPsi));
    float xy2 = r * r;

    float4 textureSample = LoadSampleAuto(position, cacheOffset);

    float3 irradiance = textureSample.rgb;

    if (TestLightingForSSS(irradiance))
    {
        float viewZ = textureSample.a;
        float relZ = viewZ - linearDepth;
        float3 weight = ComputeBilateralWeight(xy2, relZ, mmPerUnit, S, rcpPdf);

        // Note: cdf权重叠加
        totalIrradiance += weight * irradiance;
        totalWeight += weight;
    }

    //此方法为kernel方法，保留
    // float4 kernel = _SSSDiscKernel.Load(int3(i + 0.5, 0, 0)); //当前lut的第几像素，相当于pdf
    // float3 lutWeight = kernel.rgb;
    // float r = kernel.a * MILLIMETERS_PER_METER;
    //
    // // 2. 角度 + per-pixel phase 抖动(黄金角)
    // float sinPhase, cosPhase;
    // sincos(phase, sinPhase, cosPhase);
    // float phi = SampleDiskGolden(i, n).y; //黄金角随机角度
    //
    // float sinPhi, cosPhi;
    // sincos(phi, sinPhi, cosPhi);
    // float sinPsi = cosPhase * sinPhi + sinPhase * cosPhi;
    // float cosPsi = cosPhase * cosPhi - sinPhase * sinPhi;
    // // 3. 采样位置
    // float2 position = pixelCoord + (int2)round(pixelsPerMm * r * float2(cosPsi, sinPsi)); //r半径对应的像素点
    // float4 textureSample = LoadSample(position, cacheOffset);
    // float3 irradiance = textureSample.rgb;
    //
    // if (TestLightingForSSS(irradiance))
    // {
    //     // 4. depth-aware:LUT权重 × runtime depth penalty
    //     float viewZ = textureSample.a;
    //     float relZ = viewZ - linearDepth;
    //
    //     float3 depthFactor = ComputeBilateralWeightLUT(r, relZ, mmPerUnit, S, d);
    //     float3 weight = lutWeight * depthFactor;
    //     totalIrradiance += weight * irradiance;
    //     totalWeight += weight;
    // }
}


#endif
