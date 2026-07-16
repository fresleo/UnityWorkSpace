#ifndef TRANSLUCENCY
#define TRANSLUCENCY

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "RealtimeLights.hlsl"

struct TranslucencyProperty
{
    float translucencyStrength;
    float translucencyDistortion;
    float translucencyScattering;
    float4 translucencyColor;
    float translucencyAmbient;
    float translucencyShadow;
};

half3 Translucency(float3 bakedGI, float3 surfaceAlbedo, float3 surfaceNormal, float3 viewDirectionWS, Light light, half thickness, TranslucencyProperty transProp)
{
    half3 lightDir = light.direction + surfaceNormal * transProp.translucencyDistortion;
    half angle = saturate( dot( viewDirectionWS, -lightDir ));
    angle = smoothstep(0.0, 1.0, angle);
    half transVdotL = pow(angle, transProp.translucencyScattering ) * transProp.translucencyStrength;
    half3 translucency = (transVdotL + bakedGI * transProp.translucencyAmbient) * (1 - thickness) * lerp(1, light.shadowAttenuation, transProp.translucencyShadow) * light.distanceAttenuation;
    return half3( surfaceAlbedo * light.color * translucency * transProp.translucencyColor);
}

#endif