#ifndef TOONPBR_HDRP_SHADOWCASTER
#define TOONPBR_HDRP_SHADOWCASTER

#include "./ToonPBR_HDRP_Macros.hlsl"
#include "./ToonPBR_HDRP_Input.hlsl"

#include "../../../Common/TransparentByDither.hlsl"

#include "../ToonPBR_Dissolve.hlsl"

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

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.uv = input.texcoord;

    float3 positionRWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(positionRWS);

#if UNITY_REVERSED_Z
    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, positionRWS)

    output.screenPos = ComputeScreenPos(output.positionCS);

    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    #ifdef _DITHER_ON
    DitherWithTexture(input.screenPos, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix,
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif

    // dissolve clip：CalculateFinalColor 内部会 clip(dissolve - 0.01)
    half4 fakeColor = half4(0, 0, 0, 0);
    TOONPBR_DISSOLVE_APPLY(fakeColor, input.uv, input)

    return 0;
}

#endif // TOONPBR_HDRP_SHADOWCASTER
