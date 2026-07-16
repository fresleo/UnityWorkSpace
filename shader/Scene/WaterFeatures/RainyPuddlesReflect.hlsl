// 雨坑的反射
#ifndef __RAINY_PUDDLES_REFLECT__
#define __RAINY_PUDDLES_REFLECT__

/*
 * 过滤自发光
 */
half3 FilterEmission(half useEmissionFromMainProperties, half3 emission, float3 mask)
{
    half3 black = (0.0).xxx;
    half3 lerpResult = lerp(emission, black, mask);

    half3 selectEmission = useEmissionFromMainProperties ? emission : lerpResult;
    return selectEmission;
}

/*
 * 获取雨的反射结果
 */
half4 GetRainReflect(
    float3 worldViewDirection, float3 reflectNormal, float3 tanToWorld0, float3 tanToWorld1, float3 tanToWorld2,
    TEXTURECUBE_PARAM(rainCubemap, sampler_rainCubemap), float rainBlurReflection,
    float rainReflectionIntensity, half4 rainCubemapColor, half4 rainMask)
{
    float3 worldRefl = normalize(reflect(
        -worldViewDirection, float3(dot(tanToWorld0, reflectNormal),
                                    dot(tanToWorld1, reflectNormal),
                                    dot(tanToWorld2, reflectNormal)
                                    )));
    float4 coord = float4(worldRefl, rainBlurReflection);
    
    half4 rcColor = SAMPLE_TEXTURECUBE_LOD(rainCubemap, sampler_rainCubemap, coord, 0);

    float clampResult = clamp(rainReflectionIntensity, 0.0, 100.0);
    
    float4 reflectColor = (rcColor * rcColor.a * clampResult * rainCubemapColor * rainMask);
    return reflectColor;
}

#endif // __RAINY_PUDDLES_REFLECT__
