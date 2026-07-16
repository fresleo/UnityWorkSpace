#ifndef VEGETATION_UTILS
#define VEGETATION_UTILS

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

float2 Rotator(float2 uv, float2 anchor, float angle)
{
    float cos10 = cos(angle);
    float sin10 = sin(angle);
    float2 rotator = mul(uv - anchor, float2x2(cos10, -sin10, sin10, cos10)) + anchor;

    return rotator;
}

float2 Panner(float2 uv, float2 speed, float time)
{
    float2 pannerUV = time * speed.xy + uv;

    return pannerUV;
}

#endif // VEGETATION_UTILS
