#ifndef __KNIFE_MARK__INPUT__
#define __KNIFE_MARK__INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;

    half4 _BaseColor;
    half _Cutoff;
    half _AlphaControl;

    half _BumpScale;
    half _Parallax;

    half _Metallic;
    half _Smoothness;

    float4 _UVTilingOffset;

    half4 _HighTempColor_1;
    half _HighTempStrength_1;

    half4 _HighTempColor_2;
    half _HighTempStrength_2;
    half _HighTempSmoothingFactor;

    float4x4 _PM;
    half _CurvatureCorrection;
    half _UVCorrection;

    // half _GIIndirectDiffuseBoost;
    // half _SpecularScaleBRDF;
    
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
float4 _BaseMap_TexelSize;
float4 _BaseMap_MipInfo;
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

TEXTURE2D(_ParallaxMap2D); SAMPLER(sampler_ParallaxMap2D);
TEXTURE2D(_HighTempMap); SAMPLER(sampler_HighTempMap);

half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    #if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) && !defined(_GLOSSINESS_FROM_BASE_ALPHA)
    half alpha = albedoAlpha * color.a;
    #else
    half alpha = color.a;
    #endif

    alpha = AlphaDiscard(alpha, cutoff);
    
    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
    #ifdef _NORMALMAP
    half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    return UnpackNormalScale(n, scale);
    #else
    return half3(0.0h, 0.0h, 1.0h);
    #endif
}

void SampleHighTempEmission(float2 uv, TEXTURE2D_PARAM(maskMap, sampler_maskMap), out half3 finalEmission)
{
    // 高温区遮罩
    half4 maskMapT2d = SAMPLE_TEXTURE2D(maskMap, sampler_maskMap, uv);
    half highTemp = maskMapT2d.r;
    
    // half highTempMask = step(0.01, highTemp); // 硬过渡
    half highTempMask = saturate(highTemp * _HighTempSmoothingFactor); // 平滑过渡
    
    // 检查是否存在高温区，没有的话，不能阻碍低温区的显示
    half haveHighTemp = step(_HighTempStrength_2, 0.0001);
    half lowTempMask = saturate(1 - highTempMask + haveHighTemp);

    // 低温区+高温区的自发光颜色
    half3 lowTempEmission = _HighTempColor_1.rgb * _HighTempStrength_1 * lowTempMask;
    half3 highTempEmission = _HighTempColor_2.rgb * _HighTempStrength_2 * highTempMask;
    finalEmission = lowTempEmission + highTempEmission;
}

#endif // __KNIFE_MARK__INPUT__
