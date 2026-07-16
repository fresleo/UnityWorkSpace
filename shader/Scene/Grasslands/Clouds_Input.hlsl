#ifndef __CLOUDS_INPUT__
#define __CLOUDS_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    float _ParticleSoftness;
CBUFFER_END

TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);

#endif