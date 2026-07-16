#ifndef __TERRAIN_PBR_SHADOW_CASTER__
#define __TERRAIN_PBR_SHADOW_CASTER__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

struct AttributesLean
{
    float4 position     : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsLean
{
    float4 clipPos      : SV_POSITION;
    float2 texcoord     : TEXCOORD0;
    
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsLean ShadowPassVertex(AttributesLean input)
{
    VaryingsLean output = (VaryingsLean)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if !defined(NOT_REQUIRES_TERRAIN_INSTANCING)
    TerrainInstancing(input.position, input.normalOS, input.texcoord);
    #endif

    float3 positionWS = TransformObjectToWorld(input.position.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
    float3 lightDirectionWS = _LightDirection;
    #endif

    float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

    #if UNITY_REVERSED_Z
    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
    #else
    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
    #endif

    output.clipPos = clipPos;

    output.texcoord = input.texcoord;

    return output;
}

half4 ShadowPassFragment(VaryingsLean input) : SV_TARGET
{
    return 0;
}

#endif // __TERRAIN_PBR_SHADOW_CASTER__
