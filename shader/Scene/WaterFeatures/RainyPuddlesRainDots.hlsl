// 雨坑的雨点
#ifndef __RAINY_PUDDLES_RAIN_DOTS__
#define __RAINY_PUDDLES_RAIN_DOTS__

/*
 * 计算雨点渐变图的 UV
 */
void CalculateRainDotsGradientTexUV(
    float splashSpeed, float2 uv, float tiling,
    out float2 panner, out float voronoi)
{
    float2 temp_cast = (splashSpeed).xx;
    
    float2 texCoord = uv * float2(1, 1) + float2(0, 0);
    float2 coords = texCoord * tiling;
    float temp_time = (1.0 * 0.001);
    float2 vId = 0;
    float2 vUv = 0;

    voronoi = 0;
    float fade = 0.5;
    float rest = 0;
    
    for (int it = 0; it < 2; it++)
    {
        voronoi += fade * Voronoi(coords, temp_time, vId, vUv);
        rest += fade;
        coords *= 2;
        fade *= 0.5;
    }
    voronoi /= rest;

    // 计算 panner 值
    panner = (1.0 * GET_GLOBAL_TIME.y * temp_cast + (step(voronoi, 0.1) * vId));
}

/*
 * 计算雨点的粗糙度
 */
float CalculateRainDotsRoughness(half gradientValue, float voroi, float size, float intensity)
{
    float result = saturate(gradientValue * step(voroi, (size * 0.05))) * intensity;
    return result;
}

/*
 * 计算雨点的光滑度
 */
float CalculateRainDotsSmoothness(float inputSmoothness, float roughness)
{
    float result = (inputSmoothness * (1.0 - roughness) + roughness);
    return result;
}

/*
 * 计算雨点的环境光遮蔽
 */
void CalculateRainDotsAmbientOcclusion(float aoChannel, float aoIntensity, float mask,
    out float aoResult, out float aoMaskResult)
{
    float blendOpSrc = aoChannel;
    float blendOpDest = (1.0 - aoIntensity);
    
    aoResult = saturate(1.0 - (1.0 - blendOpSrc) * (1.0 - blendOpDest));
    aoMaskResult = lerp(aoResult, 1.0, mask);
}

#endif // __RAINY_PUDDLES_RAIN_DOTS__
