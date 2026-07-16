#ifndef __PLANT_SHADOW_CASTER_PASS__
#define __PLANT_SHADOW_CASTER_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define _TYPE_PLANT_ON
#include "./Wind.hlsl"
#include "../ShaderLibrary/InteractiveParams.hlsl"

float3 _LightDirection;
float3 _LightPosition;

float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
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

SurfaceInput vert(VertexAttributes input)
{
    SurfaceInput output = (SurfaceInput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.uv0.xy = TRANSFORM_TEX(input.uv0, _Albedo);
    
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    #ifdef _INTERSECTION_ON
    output.positionWS += VegetationInteractiveWS(output.positionWS, clamp(input.uv1.x * 1.5, 0.0, 1.0), _IntersectionIntensity);
    #endif

    #ifdef _WIND_ON
    Wind(input, output, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
    #endif
    
    output.positionCS = GetShadowPositionHClip(output.positionWS, output.normalWS);

    return output;
}

half4 frag(SurfaceInput input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, input.uv0.xy) * _MainColor;
    clip(albedo.a - _AlphaTestThreshold);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    return 0;
}

#endif // __PLANT_SHADOW_CASTER_PASS__
