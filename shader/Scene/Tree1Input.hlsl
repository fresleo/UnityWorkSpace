#ifndef __TREE_1_INPUT__
#define __TREE_1_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Input.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4	_BaseMap_ST;
    half4	_BaseColor;
    half4   _SecondColor;
    half4   _SSSColor;
    half4   _TopColor;
    half	_Cutoff;
    half    _CutOffset;
    half    _ClipEnhanceDistance;
    half    _ClipEnhance;
    half    _Alpha;
    half    _GIExposure;
    half    _GIFalloff;
    half    _AOOffset;

    half  _PosOffset;
    half  _PosScale;

    float _TranslucencyStrength;
    float _TranslucencyDistortion;
    float _TranslucencyScattering;

    // half4 _TranslucencyFakeColor;
    // half _TranslucencyFakeLightIntensity;
    // half _TranslucencyFakeLightFalloff;

    float _WindVariation;
    float _WindStrength;
    float _TurbulenceStrength;

    XKNIGHT_DEPTH_MASK_INPUT_1
CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

#endif // __TREE_1_INPUT__
