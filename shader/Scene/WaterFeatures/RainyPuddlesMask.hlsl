// 雨坑的遮罩
#ifndef __RAINY_PUDDLES_MASK__
#define __RAINY_PUDDLES_MASK__

// 调整水坑的蒙板
float4 AdjustPuddlesMask(
    float mask, float contrast, float spread,
    float rainExcludeBaseMapAlpha, float baseMapAlpha, float intensityMask)
{
    // 调整遮罩的强度
    float4 temp_cast = (mask + (-1.2 + spread * (0.7 - -1.2))).xxxx;
    float4 temp_output = CalculateContrast((contrast + 1.0), temp_cast);
    
    // BaseMap 的 Alpha 也一起参与进来
    float4 rawResult = (rainExcludeBaseMapAlpha ? (temp_output * (1.0 - baseMapAlpha)) : temp_output);
    
    float4 clampResult = clamp(rawResult, float4(0, 0, 0, 0), float4(1, 1, 1, 0));
    float4 result = clampResult * intensityMask;

    return result;
}

/**
 * 获取雨的遮罩
 * @param rainUV                   - tiling 过的 UV
 * @param invertRainMask           - 反转遮罩
 * @param contrast                 - 对比度
 * @param spread                   - 扩散
 * @param rainExcludeBaseMapAlpha  - 排除 BaseMap 的 Alpha
 * @param baseMapAlpha             - BaseMap 的 Alpha
 * @param intensityMask            - 遮罩的强度
 */
float4 GetRainMask(
    TEXTURE2D_PARAM(rainMask, sampler_rainMask), float2 rainUV,
    float invertRainMask, float contrast, float spread,
    float rainExcludeBaseMapAlpha, float baseMapAlpha, float intensityMask)
{
    half4 maskColor = half4(SAMPLE_TEXTURE2D(rainMask, sampler_rainMask, rainUV));
    half maskValue = maskColor.r;

    // 反转遮罩
    float new_maskValue = (invertRainMask ? (1.0 - maskValue) : maskValue);

    float4 maskResult = AdjustPuddlesMask(
        new_maskValue, contrast, spread,
        rainExcludeBaseMapAlpha, baseMapAlpha, intensityMask);
    return maskResult;
}

#endif // __RAINY_PUDDLES_MASK__
