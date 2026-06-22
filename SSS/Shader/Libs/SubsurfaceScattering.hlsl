#ifndef SUBSURFACE_SCATTERING_INCLUDED
#define SUBSURFACE_SCATTERING_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Fibonacci.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"
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


//----------------------------------------Varible----------------------------
TEXTURE2D(_SSSDiscKernel);
SAMPLER(sampler_SSSDiffuse);
SAMPLER(sampler_SSSAlbedo);
TEXTURE2D_X(_SSSDiffuse); // ← 改
TEXTURE2D_X(_SSSAlbedo);

float4 _ShapeParams;
float _MaxRadius;
float _WorldScale;
uint _DiscKernelCount;
int _SssSampleBudget;
//--------------------------------------end =---------------------------------


//==============================debug========================================
RW_TEXTURE2D_X(float4, _SSSDebugOutput);

void StoreDebug(uint2 pixelCoord, float4 value)
{
    _SSSDebugOutput[COORD_TEXTURE2D_X(pixelCoord)] = value;
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
    float depth = LoadCameraDepth(p); // 不乘 _RTHandleScale
    return float4(irradiance, depth); // 返回原始设备深度，由调用方线性化
}

float4 LoadSample(int2 pixelCoord, int2 cacheOffset)
{
    float4 value;
    //判断是否在 shared memory 缓存内
    int2 cacheCoord = pixelCoord - cacheOffset;
    bool isInCache  = max((uint)cacheCoord.x, (uint)cacheCoord.y) < TEXTURE_CACHE_SIZE_1D;

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

    float2 position = pixelCoord + (int2)round(pixelsPerMm * r * float2(cosPsi, sinPsi));
    float xy2 = r * r;

    float4 textureSample = LoadSample(position, cacheOffset);
    float3 irradiance = textureSample.rgb;

    if (TestLightingForSSS(irradiance))
    {
        // Apply bilateral weighting.
        float viewZ = textureSample.a;
        float relZ = viewZ - linearDepth;
        float3 weight = ComputeBilateralWeight(xy2, relZ, mmPerUnit, S, rcpPdf);

        // Note: if the texture sample if off-screen, (z = 0) -> (viewZ = far) -> (weight ≈ 0).
        totalIrradiance += weight * irradiance;
        totalWeight += weight;
    }

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
