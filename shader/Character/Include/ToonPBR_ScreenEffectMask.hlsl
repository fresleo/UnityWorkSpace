#ifndef __TOONPBR_SCREEN_EFFECT_MASK__
#define __TOONPBR_SCREEN_EFFECT_MASK__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct ScreenEffectMaskAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ScreenEffectMaskVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ScreenEffectMaskVaryings ScreenEffectMaskVertex(ScreenEffectMaskAttributes input)
{
    ScreenEffectMaskVaryings output = (ScreenEffectMaskVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClipWithFOVFix(input.positionOS.xyz);
    return output;
}

half EncodeScreenEffMaskId(float effectId)
{
    return saturate(effectId * (1.0h / 8.0h));
}

half4 ScreenEffectMaskFragment(ScreenEffectMaskVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float effectId = clamp(floor(_ScreenEffMaskID + 0.5h), 0.0, 8.0);
    if (effectId < 0.5)
    {
        clip(-1.0h);
    }

    return half4(1.0h, EncodeScreenEffMaskId(effectId), 0, 0);
}

#endif // __TOONPBR_SCREEN_EFFECT_MASK__
