#ifndef __TOONPBR_DEPTH_MASK__
#define __TOONPBR_DEPTH_MASK__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "./ToonPBR_Dissolve.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

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
    
    TOONPBR_DISSOLVE_FACTOR(1)
    float4 positionSS   : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthMaskVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputsWithFOVFix(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;
    
    output.uv = input.uv;
    
    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, vertexInput.positionWS)

    return output;
}

void DepthMaskFragment(
    Varyings input
    , out half4 outColor : SV_Target
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    // 溶解
    half4 fakeColor = half4(0, 0, 0, 0);
    TOONPBR_DISSOLVE_APPLY(fakeColor, input.uv, input)
    
    // 遮罩
    half4 color = 0;
    #if defined( _BLOOMFACTORMASK )
    color.r = _BloomFactor;
    #endif
    outColor = color;
}

#endif // __TOONPBR_DEPTH_MASK__
