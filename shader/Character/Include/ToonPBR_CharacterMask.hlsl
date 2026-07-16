#ifndef __TOONPBR_CHARACTER_MASK__
#define __TOONPBR_CHARACTER_MASK__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS	: POSITION;

    float2 uv           : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS	: SV_POSITION;
    
    float2 uv           : TEXCOORD0;
    float4 positionSS   : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings CharacterMaskVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClipWithFOVFix(input.positionOS.xyz);
    output.uv = input.uv;

    return output;
}

void CharacterMaskFragment(
    Varyings input
    , out half4 outColor : SV_Target
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    outColor = half4(1, 0, 0, 0);
}

#endif // __TOONPBR_CHARACTER_MASK__
