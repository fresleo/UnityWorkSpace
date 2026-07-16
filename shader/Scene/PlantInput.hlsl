#ifndef __PLANT_INPUT__
#define __PLANT_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/OperatorInstead.hlsl"

// Properties
CBUFFER_START(UnityPerMaterial)
    half    _AlphaTestThreshold;

    // Maps
    float4  _Albedo_ST;
    half4   _MainColor;

    half   _WindVariation;
    half   _WindStrength;
    half   _TurbulenceStrength;

    half    _TranslucencyStrength;
    half    _TranslucencyDistortion;
    half    _TranslucencyScattering;
    half4   _TranslucencyColor;
    half    _TranslucencyAmbient;
    half    _TranslucencyShadow;

    half    _IntersectionIntensity;

    half4   _TopColor;
    half4   _BottomColor;
    half4   _ShadowColor;
    half    _ShadowRange;
    half    _ShadowSmooth;
    
    half    _RimThreshold;
    half    _RimOffset;
    
    half4   _RimColor;
    half    _RimIntensity;
    half    _RimTOD;
    half4   _RimColor_1, _RimColor_2, _RimColor_3, _RimColor_4;
    half    _RimIntensity_1, _RimIntensity_2, _RimIntensity_3, _RimIntensity_4;

    half    _ThecutAngle;
    half    _ThecutSmoothness;
    half    _ThecutStrength;
    // half    _IfDistance;
    // half    _thecutDistnce;

    XKNIGHT_DEPTH_MASK_INPUT_1

    half    _DitherIntensity, _DitherSize, _DitherAlpha;
    half    _DitherWithMatrix;
    float4  _DitherTexture_TexelSize;
CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

TEXTURE2D(_Albedo); SAMPLER(sampler_Albedo);
TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_DitherTexture);

half _TODTimeIndex; // TOD 时间索引

void GetTODIndex(out half index_1, out half index_2, out half index_3, out half index_4)
{
    index_1 = if_equal(_TODTimeIndex, 0);
    index_2 = if_equal(_TODTimeIndex, 1);
    index_3 = if_equal(_TODTimeIndex, 2);
    index_4 = if_equal(_TODTimeIndex, 3);
}

half3 GetRimColor(in half index_1, in half index_2, in half index_3, in half index_4)
{
    half3 noTodColor, todColor;
    
    noTodColor = _RimColor;
    
    half3 color_1 = _RimColor_1.rgb * index_1;
    half3 color_2 = _RimColor_2.rgb * index_2;
    half3 color_3 = _RimColor_3.rgb * index_3;
    half3 color_4 = _RimColor_4.rgb * index_4;
    todColor = color_1 + color_2 + color_3 + color_4;
    
    half3 color = lerp(noTodColor, todColor, _RimTOD);
    return color;
}

half GetRimIntensity(in half index_1, in half index_2, in half index_3, in half index_4)
{
    half noTodIntensity, todIntensity;
    
    noTodIntensity = _RimIntensity;
    
    half intensity_1 = _RimIntensity_1 * index_1;
    half intensity_2 = _RimIntensity_2 * index_2;
    half intensity_3 = _RimIntensity_3 * index_3;
    half intensity_4 = _RimIntensity_4 * index_4;
    todIntensity = intensity_1 + intensity_2 + intensity_3 + intensity_4;
    
    half intensity = lerp(noTodIntensity, todIntensity, _RimTOD);
    return intensity;
}

#endif // __PLANT_INPUT__