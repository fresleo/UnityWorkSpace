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
TEXTURE2D (_SSSDiscKernel);
TEXTURE2D (_SSSDiffuse); // rgb = 漫反射辐照度, a = coverage(是否 SSS 像素)
SAMPLER(sampler_SSSDiffuse);
TEXTURE2D (_SSSAlbedo); // rgb = 漫反射反照率
SAMPLER(sampler_SSSAlbedo);


float4 _ShapeParams;
float _MaxRadius;
float _WorldScale;
uint _DiscKernelCount;
//--------------------------------------end =---------------------------------
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
    float3 irradiance = LOAD_TEXTURE2D_X(_SSSDiffuse, pixelCoord).rgb;
    float depth = LoadCameraDepth(pixelCoord* _RTHandleScale.xy);
    return float4(irradiance, depth);
}

float4 LoadSample(int2 pixelCoord, int2 cacheOffset)
{
    float4 value;

    //判断是否在 shared memory 缓存内
    int2 cacheCoord = pixelCoord - cacheOffset;
    value = LoadSampleFromCacheMemory(cacheCoord);
    value.a = LinearEyeDepth(value.a, _ZBufferParams);
    return value;
}

float3 ComputeBilateralWeightLUT(float r2D_mm, float relZ, 
                                  float mmPerUnit, 
                                  float3 S,
                                  float  d)   
{

    float zMm = relZ * mmPerUnit;//深度赋予权重
    float r3D = sqrt(r2D_mm * r2D_mm + zMm * zMm);
    
    float3 sd = S / d;   // 合并系数，EvaluateBurley 里的 s/d
    float3 exp_13_3D = exp2(LOG2_E * (-1.0/3.0) * r3D * sd);
    float3 expSum_3D = exp_13_3D * (1 + exp_13_3D * exp_13_3D);
    float3 exp_13_2D = exp2(LOG2_E * (-1.0/3.0) * r2D_mm * sd);
    float3 expSum_2D = exp_13_2D * (1 + exp_13_2D * exp_13_2D);

    float3 depthFactor = expSum_3D / max(expSum_2D, 1e-6);
    
    return depthFactor;
}


void EvaluateSample(uint i, uint n, int2 pixelCoord, 
                    int2 cacheOffset,float3 S,float d,
                    float mmPerUnit, float pixelsPerMm, float phase,
                    inout float3 totalIrradiance, inout float3 totalWeight, float linearDepth)
{
    float4 kernel = _SSSDiscKernel.Load(int3(i+0.5, 0, 0)); //当前lut的第几像素，相当于pdf
    float3 lutWeight = kernel.rgb;
    float r = kernel.a;

    // 2. 角度 + per-pixel phase 抖动(黄金角)
    float sinPhase, cosPhase;
    sincos(phase, sinPhase, cosPhase);
    float phi = SampleDiskGolden(i, n).y; //黄金角随机角度

    float sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);
    float sinPsi = cosPhase * sinPhi + sinPhase * cosPhi;
    float cosPsi = cosPhase * cosPhi - sinPhase * sinPhi;
    // 3. 采样位置
    float2 position = pixelCoord + (int2)round(pixelsPerMm * r * float2(cosPsi, sinPsi));
    float4 textureSample = LoadSample(position, cacheOffset);
    float3 irradiance = textureSample.rgb;

    if (TestLightingForSSS(irradiance))
    {
        // 4. depth-aware:LUT权重 × runtime depth penalty
        float viewZ = textureSample.a;
        float relZ = viewZ - linearDepth;
        
        float3 depthFactor = ComputeBilateralWeightLUT(r, relZ, mmPerUnit, S,d);
        float3 weight      = lutWeight * depthFactor;
        totalIrradiance += weight * irradiance;
        totalWeight += weight;
    }
    
}


#endif
