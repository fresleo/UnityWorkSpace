#ifndef TOONPBR_HDRP_DEPTH_FORWARD_ONLY
#define TOONPBR_HDRP_DEPTH_FORWARD_ONLY

#include "./ToonPBR_HDRP_Macros.hlsl"
#include "./ToonPBR_HDRP_Input.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

#include "./TransparentByDither.hlsl"

#include "./ToonPBR_VertexPull.hlsl"
#include "./ToonPBR_Dissolve.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;

    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;

    float2 texcoord     : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;

    float2 uv           : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float4 tangentWS    : TEXCOORD2;    // xyz: tangentWS, w: bitangent sign
    float3 positionWS   : TEXCOORD3;
    float4 screenPos    : TEXCOORD4;

    TOONPBR_DISSOLVE_FACTOR(5)

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthForwardOnlyVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    half3 pullDirOS = TransformWorldToObjectDir(_VertexPullDirection, false);
    float3 pullOffsetOS = VertexOffset(input.normalOS, pullDirOS, _VertexPullIntensity, input.texcoord,
        TEXTURE2D_ARGS(_VertexPullNoiseTexture, sampler_VertexPullNoiseTexture), _VertexPullNoiseTexture_ST);
    input.positionOS.xyz += pullOffsetOS;

    VertexPositionInputs vertexInput = GetVertexPositionInputsWithFOVFix(input.positionOS.xyz);
    VertexNormalInputs   normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    real sign = input.tangentOS.w * GetOddNegativeScale();

    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.uv         = input.texcoord;
    output.normalWS   = normalInput.normalWS;
    output.tangentWS  = half4(normalInput.tangentWS.xyz, sign);
    output.screenPos  = ComputeScreenPos(output.positionCS);

    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, output.positionWS)

    return output;
}

void DepthForwardOnlyFragment(
    Varyings input,
    out float4 outNormalBuffer : SV_Target0)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #ifdef _DITHER_ON
    DitherWithTexture(input.screenPos, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix,
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif

    half4 fakeColor = half4(0, 0, 0, 0);
    TOONPBR_DISSOLVE_APPLY(fakeColor, input.uv, input)

    float2 baseUV = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
    float3 normalTS = SampleNormal(baseUV, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    float sgn = input.tangentWS.w;
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

    float3 normalWS = TransformTangentToWorld(
        normalTS,
        half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    normalWS = NormalizeNormalPerPixel(normalWS);

    NormalData normalData;
    normalData.normalWS = normalWS;
    // 这里给一个粗略值（不精确采 _PBRMaskMap.g 是为了节省 prepass 的纹理带宽）。
    normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(saturate(_Smoothness));

    EncodeIntoNormalBuffer(normalData, outNormalBuffer);
}

#endif // TOONPBR_HDRP_DEPTH_FORWARD_ONLY
