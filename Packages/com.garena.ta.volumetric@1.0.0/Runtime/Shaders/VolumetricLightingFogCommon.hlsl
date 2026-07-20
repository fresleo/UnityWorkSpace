#ifndef VOLUMETRIC_LIGHTING_FOG_COMMON
#define VOLUMETRIC_LIGHTING_FOG_COMMON
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

float4 _BlitTexture_TexelSize;
#define KERNEL_RADIUS 4
#define BLUR_DEPTH_FALLOFF 0.5
//1/(4*PI)
#define RECIPROCAL_PI4 0.07957747154594767
#define GLOBAL_TILING 0.0001

static const float KernelWeights[] = { 0.2026, 0.1790, 0.1240, 0.0672, 0.0285 };

TEXTURE2D_FLOAT(_DepthTexture);
float4 _DepthTexture_TexelSize;
            
// Samples the downsampled camera depth texture.
float SampleDownsampledSceneDepth(float2 uv)
{
    return SAMPLE_TEXTURE2D(_DepthTexture, sampler_PointClamp, uv).r;
}

float4 BSMGaussianBlur(float2 uv, float2 dir, TEXTURE2D(textureToBlur), SAMPLER(sampler_TextureToBlur), float2 textureToBlurTexelSizeXy)
{
    float4 centerSample = SAMPLE_TEXTURE2D(textureToBlur, sampler_TextureToBlur, uv);

    int i = 0;
    float2 rgResult = centerSample.g * KernelWeights[i];
    float weights = KernelWeights[i];

    float2 texelSizeTimesDir = textureToBlurTexelSizeXy * dir;

    UNITY_UNROLL
        for (i = -KERNEL_RADIUS; i < 0; ++i)
        {
            float2 uvOffset = (float)i * texelSizeTimesDir;
            float2 uvSample = uv + uvOffset;

            float weight = KernelWeights[-i];

            float2 rg = SAMPLE_TEXTURE2D(textureToBlur, sampler_TextureToBlur, uvSample).rg;
            rgResult += (rg * weight);
            weights += weight;
        }

    UNITY_UNROLL
        for (i = 1; i <= KERNEL_RADIUS; ++i)
        {
            float2 uvOffset = (float)i * texelSizeTimesDir;
            float2 uvSample = uv + uvOffset;

            float weight = KernelWeights[i];

            float2 rg = SAMPLE_TEXTURE2D(textureToBlur, sampler_TextureToBlur, uvSample).rg;
            rgResult += (rg * weight);
            weights += weight;
        }
    //centerSample.r,
    return float4(centerSample.r, rgResult.g * rcp(weights), centerSample.ba);//
}
            
// Blurs the RGB channels of the given texture using depth aware gaussian blur, which uses the downsampled camera depth to apply weights to the blur.
// The alpha channel is not blurred so the original value is returned.
float4 DepthAwareGaussianBlur(float2 uv, float2 dir, TEXTURE2D(textureToBlur), SAMPLER(sampler_TextureToBlur), float2 textureToBlurTexelSizeXy)
{
    float4 centerSample = SAMPLE_TEXTURE2D(textureToBlur, sampler_TextureToBlur, uv);
    float centerDepth = SampleDownsampledSceneDepth(uv);
    float centerLinearEyeDepth = LinearEyeDepth(centerDepth, _ZBufferParams);

    int i = 0;
    float3 rgbResult = centerSample.rgb * KernelWeights[i];
    float weights = KernelWeights[i];

    float2 texelSizeTimesDir = textureToBlurTexelSizeXy * dir;

    UNITY_UNROLL
    for (i = -KERNEL_RADIUS; i < 0; ++i)
    {
        float2 uvOffset = (float)i * texelSizeTimesDir;
        float2 uvSample = uv + uvOffset;

        float depth = SampleDownsampledSceneDepth(uvSample);
        float linearEyeDepth = LinearEyeDepth(depth, _ZBufferParams);
        float depthDiff = abs(centerLinearEyeDepth - linearEyeDepth);
        float r2 = BLUR_DEPTH_FALLOFF * depthDiff;
        float g = exp(-r2 * r2);
        float weight = g * KernelWeights[-i];

        float3 rgb = SAMPLE_TEXTURE2D(textureToBlur, sampler_TextureToBlur, uvSample).rgb;
        rgbResult += (rgb * weight);
        weights += weight;
    }

    UNITY_UNROLL
    for (i = 1; i <= KERNEL_RADIUS; ++i)
    {
        float2 uvOffset = (float)i * texelSizeTimesDir;
        float2 uvSample = uv + uvOffset;

        float depth = SampleDownsampledSceneDepth(uvSample);
        float linearEyeDepth = LinearEyeDepth(depth, _ZBufferParams);
        float depthDiff = abs(centerLinearEyeDepth - linearEyeDepth);
        float r2 = BLUR_DEPTH_FALLOFF * depthDiff;
        float g = exp(-r2 * r2);
        float weight = g * KernelWeights[i];

        float3 rgb = SAMPLE_TEXTURE2D(textureToBlur, sampler_TextureToBlur, uvSample).rgb;
        rgbResult += (rgb * weight);
        weights += weight;
    }

    return float4(rgbResult.rgb * rcp(weights), centerSample.a);//
}


// Upsamples the given texture using both the downsampled and full resolution depth information.
float4 DepthAwareUpsample(float2 uv, TEXTURE2D(textureToUpsample), TEXTURE2D(depthTexture))
{
    float2 downsampledTexelSize = _DepthTexture_TexelSize.xy;
    float2 downsampledTopLeftCornerUv = uv - (downsampledTexelSize * 0.5);
    float2 uvs[4] =
    {
        downsampledTopLeftCornerUv + float2(0.0, downsampledTexelSize.y),
        downsampledTopLeftCornerUv + downsampledTexelSize.xy,
        downsampledTopLeftCornerUv + float2(downsampledTexelSize.x, 0.0),
        downsampledTopLeftCornerUv
    };

    float4 downsampledDepths;
    
#if SHADER_TARGET >= 45
    downsampledDepths = GATHER_RED_TEXTURE2D(depthTexture, sampler_PointClamp, uv);
#else
    downsampledDepths.x = SAMPLE_TEXTURE2D(depthTexture, sampler_PointClamp, uvs[0]).r;
    downsampledDepths.y = SAMPLE_TEXTURE2D(depthTexture, sampler_PointClamp, uvs[1]).r;
    downsampledDepths.z = SAMPLE_TEXTURE2D(depthTexture, sampler_PointClamp, uvs[2]).r;
    downsampledDepths.w = SAMPLE_TEXTURE2D(depthTexture, sampler_PointClamp, uvs[3]).r;
#endif

    float fullResDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
    float fullResLinearEyeDepth = LinearEyeDepth(fullResDepth, _ZBufferParams);
    float relativeDepthThreshold = fullResLinearEyeDepth * 0.1;
    
    float linearEyeDepth = LinearEyeDepth(downsampledDepths[0], _ZBufferParams);
    float minLinearEyeDepthDist = abs(fullResLinearEyeDepth - linearEyeDepth);
    
    float2 nearestUv = uvs[0];
    int numValidDepths = minLinearEyeDepthDist < relativeDepthThreshold;
    
    UNITY_UNROLL
    for (int i = 1; i < 4; ++i)
    {
        linearEyeDepth = LinearEyeDepth(downsampledDepths[i], _ZBufferParams);
        float linearEyeDepthDist = abs(fullResLinearEyeDepth - linearEyeDepth);

        bool updateNearest = linearEyeDepthDist < minLinearEyeDepthDist;
        minLinearEyeDepthDist = updateNearest ? linearEyeDepthDist : minLinearEyeDepthDist;
        nearestUv = updateNearest ? uvs[i] : nearestUv;
        
        numValidDepths += (linearEyeDepthDist < relativeDepthThreshold);
    }
    //return SAMPLE_TEXTURE2D(textureToUpsample, sampler_LinearClamp, uv);
    UNITY_BRANCH
    if (numValidDepths == 4)
        return SAMPLE_TEXTURE2D(textureToUpsample, sampler_LinearClamp, uv);
    else
        return SAMPLE_TEXTURE2D(textureToUpsample, sampler_PointClamp, nearestUv);
}


/*
real CornetteShanksPhasePartConstant(real anisotropy)
{
    real g = anisotropy;

    return (3 / (8 * PI)) * (1 - g * g) / (2 + g * g);
}

// Similar to the RayleighPhaseFunction.
real CornetteShanksPhasePartSymmetrical(real cosTheta)
{
    real h = 1 + cosTheta * cosTheta;
    return h;
}

real CornetteShanksPhasePartAsymmetrical(real anisotropy, real cosTheta)
{
    real g = anisotropy;
    real x = 1 + g * g - 2 * g * cosTheta;
    real f = rsqrt(max(x, REAL_EPS)); // x^(-1/2)
    return f * f * f;                 // x^(-3/2)
}

real CornetteShanksPhasePartVarying(real anisotropy, real cosTheta)
{
    return CornetteShanksPhasePartSymmetrical(cosTheta) *
            CornetteShanksPhasePartAsymmetrical(anisotropy, cosTheta); // h * x^(-3/2)
}

// A better approximation of the Mie phase function.
// Ref: Henyey-Greenstein and Mie phase functions in Monte Carlo radiative transfer computations
//更精确的mie相位函数计算
real CornetteShanksPhaseFunction(real anisotropy, real cosTheta)
{
    return CornetteShanksPhasePartConstant(anisotropy) *
            CornetteShanksPhasePartVarying(anisotropy, cosTheta);
}
*/

float2 HenyeyGreenstein(float2 g, float cosTheta) {
    float2 g2 = g * g;

    float2 x1 = pow(abs(1 + g2 - 2 * g * cosTheta), float2(1.5, 1.5));

    float2 b = max(float2(REAL_EPS, REAL_EPS), x1);

    return RECIPROCAL_PI4 * ( (1.0 - g2) / b);
    //return RECIPROCAL_PI4 * ((1.0 - g2) / max(float2(REAL_EPS, REAL_EPS), pow(1.0 + g2 - 2.0 * g * cosTheta, float2(1.5))));
}

float PhaseFunction(const float cosTheta, const float attenuation) {
    const float2 scatterAnisotropy = float2(0.7, -0.2);//前向 后向  mie前向较大
    const float scatterAnisotropyMix = 0.5;

    const float2 g = scatterAnisotropy;
    const float2 weights = float2(1.0 - scatterAnisotropyMix, scatterAnisotropyMix);
    // A similar approximation is described in the Frostbite's paper, where phase
    // angle is attenuated instead of anisotropy.
    return dot(HenyeyGreenstein(g * attenuation, cosTheta), weights);
}


//多重散射近似
float ApproximateMultipleScattering(float opticalDepth, float cosTheta) {
    // Multiple scattering approximation
    // See: https://fpsunflower.github.io/ckulla/data/oz_volumes.pdf
    // a: attenuation, b: contribution, c: phase attenuation
    float3 coeffs = float3(1, 1, 1); // [a, b, c]
    const float3 attenuation = float3(0.5, 0.5, 0.5); // Should satisfy a <= b
    float scattering = 0.0;
    float beerLambert;
    //
    for (int i = 0; i < 8; ++i) {
        beerLambert = exp(-opticalDepth * coeffs.y);
        scattering += coeffs.x * beerLambert * PhaseFunction(cosTheta, coeffs.z);
        coeffs *= attenuation;
    }

    return scattering;
}


//ApproximateMultipleScattering的拟合
float ApproximateMultipleScatteringFit(float opticalDepth, float cosTheta) {

    float od = opticalDepth;

    // 核心拟合：将几何级数转换为简单表达式
    // 基于数学分析：Σ[a^i × exp(-x × a^i)] ≈ f(x, a)
    float baseAttenuation = 2 / (1.0 + od * 0.3);  // 主要衰减项
    float multiScatterBoost = exp(-od * 0.1);         // 多重散射增强

    // 相位函数：用有效衰减参数
    float effectiveG = 0.6 * exp(-od * 0.05);  // 随光学深度衰减的有效各向异性
    float phase = PhaseFunction(cosTheta, effectiveG);

    return baseAttenuation * multiScatterBoost * phase;

}

// Gets the fog density at the given world height.
float GetHeightFogDensity(const float posWSy, const float minHeight, const float maxHeight)
{
    float t = saturate((posWSy - minHeight) / (maxHeight - minHeight));
    t = 1.0 - t;

    return t;
}

float2 GetGlobalUv(float3 position)
{
    return position.xz * GLOBAL_TILING;
}

//黑白对比度调整
float3 LuminanceContrast(float3 col, float contrast)
{
    float oldLuminance = dot(col, float3(0.299, 0.587, 0.114));
    float newLuminance = (oldLuminance - 0.5) * contrast + 0.5;
    newLuminance = saturate(newLuminance);

    return col * (newLuminance / max(oldLuminance, 0.001));
}
//
int IntersectHeight(const float3 rayOrigin, const  float3 rayDir, const float height, out float t)
{
    // 射线方向垂直于 Y 轴时，不可能与水平面相交
    if (abs(rayDir.y) < 1e-5)
    {
        t = 0;
        return 0;
    }

    // 计算与两个平面的交点距离
    t = (height - rayOrigin.y) / rayDir.y;

    return 1;
}
//
int IntersectHeightRange(const float3 rayOrigin, const  float3 rayDir, const float minHeight, const float maxHeight, out float tMin, out float tMax)
{
    // 射线方向垂直于 Y 轴时，不可能与水平面相交
    if (abs(rayDir.y) < 1e-5)
    {
        bool inside = rayOrigin.y >= minHeight && rayOrigin.y <= maxHeight;
        if (inside)
        {
            tMin = 0;
            tMax = 10000;
            return 1;
        }
        else
        {
            tMin = tMax = 0;
            return 0;
        }
    }

    // 计算与两个平面的交点距离
    float t1 = (minHeight - rayOrigin.y) / rayDir.y;
    float t2 = (maxHeight - rayOrigin.y) / rayDir.y;

    // 排序，确保 tMin <= tMax
    tMin = min(t1, t2);
    tMax = max(t1, t2);

    // 如果两个交点都在射线后，则无交点
    if (tMax < 0)
    {
        tMin = tMax = 0;
        return 0;
    }

    // 如果起点在平面之间，tMin 会是负数，表示从内部射出
    // 我们将 tMin clamp 到 0，表示从当前点开始
    tMin = max(tMin, 0.0);

    return 1;
}



float4 IntersectOBBRay(
    float3 rayOriginWS,
    float3 rayDirWS,
    float4x4 localToWorld,
    float4x4 worldToLocal)
{
    // 将射线转换到OBB的局部空间
    float3 rayOriginLS = mul(worldToLocal, float4(rayOriginWS, 1.0)).xyz;
    float3 rayDirLS = mul((float3x3)worldToLocal, rayDirWS);

    // OBB在局部空间中是一个标准的AABB，范围从-0.5到0.5
    float3 boxMin = float3(-0.5, -0.5, -0.5);
    float3 boxMax = float3(0.5, 0.5, 0.5);

    // 计算射线与AABB各面的交点参数t
    float3 invRayDir = 1.0 / (rayDirLS + 1e-7); // 添加小值避免除零
    float3 t1 = (boxMin - rayOriginLS) * invRayDir;
    float3 t2 = (boxMax - rayOriginLS) * invRayDir;

    // 确保t1是较小的值，t2是较大的值
    float3 tNear = min(t1, t2);
    float3 tFar = max(t1, t2);

    // 找到进入和离开AABB的t值
    float tEnter = max(max(tNear.x, tNear.y), tNear.z);
    float tExit = min(min(tFar.x, tFar.y), tFar.z);

    // 检查是否有有效的交点
    bool hasIntersection = (tExit >= tEnter) && (tExit > 0.0);

    if (hasIntersection)
    {
        // 确保tEnter不为负数（射线起点在盒子内部的情况）
        tEnter = max(tEnter, 0.0);

        // 计算世界空间中的入射点和出射点
        float3 enterPointLS = rayOriginLS + rayDirLS * tEnter;
        float3 exitPointLS = rayOriginLS + rayDirLS * tExit;

        float3 enterPointWS = mul(localToWorld, float4(enterPointLS, 1.0)).xyz;
        float3 exitPointWS = mul(localToWorld, float4(exitPointLS, 1.0)).xyz;

        // 计算距离
        float enterDistance = length(enterPointWS - rayOriginWS);
        float exitDistance = length(exitPointWS - rayOriginWS);

        return float4(1.0, enterDistance, exitDistance, 0.0);
    }
    else
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }
}

float ShapeAlteringFunction(const float heightFraction) {

    //float biased = pow(heightFraction, bias);           // 应用偏差(bias=0.5)
    float biased = sqrt(heightFraction);
    
    float x = clamp(biased * 2.0 - 1.0, -1.0, 1.0);   // 映射到[-1,1]
    return 1.0 - x * x;                               // 半圆形变换
}

float SampleWeather(const float height, float weather, float coverage)
{
    float heightScale = ShapeAlteringFunction(height);

    coverage *= 2;
    float factor = 1 - coverage * heightScale;

    //float density = remapClamped(weather * 0.5 + 0.5, factor, factor + 0.5);
    float density = saturate(weather + 1.0 - 2.0 * factor);
    return density;
}

#endif