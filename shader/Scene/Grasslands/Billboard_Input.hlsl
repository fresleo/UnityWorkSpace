#ifndef __BILLBOARD_INPUT__
#define __BILLBOARD_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _MainTexture_ST, _NormalTexture_ST;

    half _OpacityCutoff;
    half4 _Color;
    half _Normal;
    half _Smoothness;
CBUFFER_END

TEXTURE2D(_MainTexture);    SAMPLER(sampler_MainTexture);
TEXTURE2D(_NormalTexture);  SAMPLER(sampler_NormalTexture);

#endif
