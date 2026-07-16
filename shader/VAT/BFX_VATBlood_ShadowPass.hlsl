#ifndef __BFX_VATBLOOD_SHADOWPASS__
#define __BFX_VATBLOOD_SHADOWPASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS	: POSITION;
    float3 normalOS		: NORMAL;
    float2 texcoord		: TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS	: SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

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

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float timeInFrames = _TimeInFrames;
    float4 sampleUv = float4(input.texcoord.x, (timeInFrames + input.texcoord.y), 0, 0);
    
    float4 texturePos = SAMPLE_TEXTURE2D_LOD(_PosMap, sampler_PosMap, sampleUv, 0);
    texturePos.xyz = LinearToGamma22(texturePos.xyz);

    float expand = _BoundingMax - _BoundingMin;
    texturePos.xyz *= expand;
    texturePos.xyz += _BoundingMin;
    texturePos.x *= -1;
    input.positionOS.xyz = texturePos.xzy;
    input.positionOS.xyz += _HeightOffset.xyz;
    
    output.positionCS = GetShadowPositionHClip(input);
    
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    return 0;
}

#endif // __BFX_VATBLOOD_SHADOWPASS__