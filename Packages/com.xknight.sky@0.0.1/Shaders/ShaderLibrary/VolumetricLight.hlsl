#ifndef VOLUMETRIC_LIGHT
#define VOLUMETRIC_LIGHT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

#if 0
    #define APPLY_VOLUMETRIC_LIGHT(col, positionWS, positionSS) col += VolumetricLight(positionWS, positionSS);
#else
    #define APPLY_VOLUMETRIC_LIGHT(col, positionWS, positionSS)
#endif

#define MAX_MARCHING 8

float _VolumetricLightRange;
half3 _VolumetricLightColor;

TEXTURE2D(_DitheringTex);   SAMPLER(sampler_DitheringTex);

// 强制使用硬阴影，减小开销
half SimpleShadow(float3 positionWS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture,
        TransformWorldToShadowCoord(positionWS).xyz);
}

half3 VolumetricLight(float3 positionWS, float2 positionSS)
{
    float3 rayStart = _WorldSpaceCameraPos;
    float3 rayDir = positionWS - rayStart;
    float rayLength = length(rayDir);
    rayDir /= rayLength;
    float step = rayLength / MAX_MARCHING;
    half totalAttenuation = 0;

    float offset = SAMPLE_TEXTURE2D(_DitheringTex, sampler_DitheringTex, positionSS * _ScaledScreenParams.xy / 8).r;
    float3 p = rayStart + rayDir * step * offset;

    UNITY_LOOP
        for (int i = 0; i < MAX_MARCHING; ++i)
        {
            totalAttenuation += SimpleShadow(p);
            p += rayDir * step;
        }
    totalAttenuation /= MAX_MARCHING;

    float cosAngle = dot(rayDir, _MainLightPosition.xyz);
    return totalAttenuation * _VolumetricLightColor * smoothstep(_VolumetricLightRange, 1.0, cosAngle);
}

#endif