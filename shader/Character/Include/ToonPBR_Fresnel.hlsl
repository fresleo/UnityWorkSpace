#ifndef TOONPBR_FRESNEL
#define TOONPBR_FRESNEL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"

half3 RimGlowLight(float3 lightDirWS, float3 viewDirWS, float3 normalWS
    , half3 rgColor
    , half rgScale, half rgBias, half rgShininess, half rgFeather
    , half rgDiffuseBlend, half rgDiffuseStep, half rgDiffuseFeather
    , half rgSoftFresnelMix
    , half rgSoftFresnelRangeMultiplier, half rgSoftFresnelRangeMin, half rgSoftFresnelStartOffset, half rgSoftFresnelPow)
{
    half nDotV = dot(viewDirWS, normalWS);
    half nDotLHalf = dot(normalWS, lightDirWS) * 0.5 + 0.5;
    
    half fresnelBase = clamp(1.001 - nDotV, 0.001, 1.0);
    half fresnelPow = pow(fresnelBase, rgShininess);
    
    // 硬的版本
    half hardFresnel = CheapSmoothStep(0, rgFeather + 0.001, fresnelPow);
    
    // 软的版本
    half softRange = max(rgFeather * rgSoftFresnelRangeMultiplier, rgSoftFresnelRangeMin);
    half softStart = -softRange * rgSoftFresnelStartOffset;
    half softFresnel = CheapSmoothStep(softStart, softRange, fresnelPow);
    softFresnel = pow(saturate(softFresnel), rgSoftFresnelPow);
    
    // 混合
    half mixFresnel = lerp(hardFresnel, softFresnel, rgSoftFresnelMix);
    half fresnel = saturate(rgBias + mixFresnel * rgScale);
    
    float diffuse = saturate((nDotLHalf - rgDiffuseStep + rgDiffuseFeather) / rgDiffuseFeather);
    fresnel *= lerp(1.0, diffuse, rgDiffuseBlend);
    
    half3 resultColor = rgColor * fresnel;
    return resultColor;
}



half3 RimGlowLight(float3 lightDirWS, float3 viewDirWS
    , ToonData toonData
    , float3 globalNormalWS, float3 localNormalWS, float3 positionWS
    )
{
    half3 globalResultColor = 0;
    
    #if defined(_RG_EFFECT_ON)
    globalResultColor = RimGlowLight(lightDirWS, viewDirWS, globalNormalWS
        , toonData.rimGlowColor
        , toonData.rimGlowScale, toonData.rimGlowBias, toonData.rimGlowShininess, toonData.rimGlowFeather
        , toonData.rimGlowDiffuseBlend, toonData.rimGlowDiffuseStep, toonData.rimGlowDiffuseFeather
        , toonData.rimGlowSoftFresnelMix
        , toonData.rimGlowSoftFresnelRangeMultiplier, toonData.rimGlowSoftFresnelRangeMin, toonData.rimGlowSoftFresnelStartOffset, toonData.rimGlowSoftFresnelPow);
    #endif
    
    half3 localResultColor = 0;
    float localFactor = 0;
    
    #if defined(_RG_EFFECT_LOCAL_ON)
    localResultColor = RimGlowLight(lightDirWS, viewDirWS, localNormalWS
        , toonData.localRGColor
        , toonData.localRGScale, toonData.localRGBias, toonData.localRGShininess, toonData.localRGFeather
        , toonData.localRGDiffuseBlend, toonData.localRGDiffuseStep, toonData.localRGDiffuseFeather
        , toonData.localRGSoftFresnelMix
        , toonData.localRGSoftFresnelRangeMultiplier, toonData.localRGSoftFresnelRangeMin, toonData.localRGSoftFresnelStartOffset, toonData.localRGSoftFresnelPow);
    
    float localFactor0 = ComputeLocalFactor(toonData.localRGWorldToLocal_0, positionWS);
    float localFactor1 = ComputeLocalFactor(toonData.localRGWorldToLocal_1, positionWS);
    localFactor = max(localFactor0, localFactor1);
    #endif
    
    half3 resultColor = lerp(globalResultColor, localResultColor, localFactor);
    return resultColor;
}

#endif
