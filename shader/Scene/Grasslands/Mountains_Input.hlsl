#ifndef __MOUNTAINS_INPUT__
#define __MOUNTAINS_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    float _NormalScale;
    float _Smoothness;

    float4 _FogColor;
    float _Height;
    float _Density;
CBUFFER_END

TEXTURE2D(_Albedo);     SAMPLER(sampler_Albedo);
TEXTURE2D(_Normal);     SAMPLER(sampler_Normal);

#endif