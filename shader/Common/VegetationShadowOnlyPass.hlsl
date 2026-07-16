#ifndef XKNIGHT_VEGETATION_SHADOW_ONLY_PASS_INCLUDED
#define XKNIGHT_VEGETATION_SHADOW_ONLY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if defined(LOD_FADE_CROSSFADE)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define _TYPE_PLANT_ON
#include "../Scene/Wind.hlsl"
#include "../ShaderLibrary/InteractiveParams.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Cutoff;
    
    half _IntersectionIntensity;

    half _WindVariation, _WindStrength, _TurbulenceStrength;
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

float3 _LightDirection;
float3 _LightPosition;

float4 GetVegetationShadowPositionHClip(float3 positionWS, float3 normalWS)
{
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
    float3 lightDirectionWS = _LightDirection;
    #endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    return positionCS;
}

SurfaceInput ShadowPassVertex(VertexAttributes input)
{
    SurfaceInput output = (SurfaceInput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.uv0.xy = TRANSFORM_TEX(input.uv0, _BaseMap);

    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    #ifdef _INTERSECTION_ON
    output.positionWS += VegetationInteractiveWS(
        output.positionWS,
        clamp(input.uv1.x * 1.5, 0, 1),
        _IntersectionIntensity);
    #endif

    #ifdef _WIND_ON
    Wind(input, output, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
    #endif

    output.positionCS = GetVegetationShadowPositionHClip(output.positionWS, output.normalWS);

    return output;
}

half4 ShadowPassFragment(SurfaceInput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv0.xy).a;
    clip(alpha * _BaseColor.a - _Cutoff);

    #if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
    #endif

    return 0;
}

#endif
