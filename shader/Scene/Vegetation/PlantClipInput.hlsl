#ifndef TREE_INPUT_INCLUDED
#define TREE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

    half _MotionFacingValue;

    half _MotionAmplitude_10;     // Motion Bending
    half _MotionPosition_10; // Motion Rigidity,主干刚度
    half _MotionSpeed_10;
    half _MotionScale_10;
    half _MotionVariation_10;
    
    half _InteractionAmplitude;
    half _InteractionMaskValue;

    half _MotionAmplitude_20;
    half _MotionAmplitude_22;
    half _MotionSpeed_20;
    half _MotionScale_20;
    half _MotionVariation_20;

    half _MotionAmplitude_32;
    half _MotionSpeed_32;
    half _MotionScale_32;
    half _MotionVariation_32;
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

#endif // TREE_INPUT_INCLUDED
