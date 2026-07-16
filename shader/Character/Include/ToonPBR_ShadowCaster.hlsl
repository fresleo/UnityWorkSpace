#ifndef TOONPBR_SHADOWCASTER
#define TOONPBR_SHADOWCASTER

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

#include "./ToonPBR_Dissolve.hlsl"

float3 _LightDirection;

struct Attributes
{
    float4 positionOS   : POSITION;
    
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    
    float2 uv           : TEXCOORD0;
    
    TOONPBR_DISSOLVE_FACTOR(1)
    
    float4 screenPos    : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

void GetShadowPositionHClip(Attributes input, inout float4 positionCS, out float3 positionWS)
{
    positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 shadowPositionWS = ApplyShadowBias(positionWS, normalWS, _LightDirection);

    VertexPositionInputs vpi = (VertexPositionInputs)0;
    vpi.positionWS = shadowPositionWS;
    vpi.positionCS = TransformWorldToHClip(shadowPositionWS);
    ApplyCharacterFOVFixInPlace(vpi);
    positionCS = vpi.positionCS;
    
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    output.uv = input.texcoord;
    
    float3 positionWS = 0;
    GetShadowPositionHClip(input, output.positionCS, positionWS);
    
    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, positionWS)
    
    output.screenPos = ComputeScreenPos(output.positionCS);
    
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    // 抖动
    #ifdef _DITHER_ON
    DitherWithTexture(input.screenPos, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif
    
    half4 fakeColor = half4(0, 0, 0, 0);
    TOONPBR_DISSOLVE_APPLY(fakeColor, input.uv, input)
    
    return 0;
}

#endif // TOONPBR_SHADOWCASTER
