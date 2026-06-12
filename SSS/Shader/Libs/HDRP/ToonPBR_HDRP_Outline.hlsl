#ifndef TOONPBR_HDRP_OUTLINE
#define TOONPBR_HDRP_OUTLINE


#include "./ToonPBR_HDRP_Macros.hlsl"
#include "./ToonPBR_HDRP_Input.hlsl"

#include "../../../Common/MathFuncs.hlsl"        // Remap
#include "../../../Common/TransparentByDither.hlsl"

#include "../ToonPBR_OutlineLib.hlsl"            // OutlineVertexPhase / OUTLINE_ATTRIBUTES
#include "../ToonPBR_Dissolve.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;

    OUTLINE_ATTRIBUTES

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;        // Camera-Relative World Space

    TOONPBR_DISSOLVE_FACTOR(2)

    #ifdef _DITHER_ON
    float4 positionSS   : TEXCOORD3;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 normalV3 = input.color.rgb * 2.0 - 1.0;
    float4 normalV4 = float4(normalV3, input.color.a);

    VertexPositionInputs vertexInput;
    output.positionCS = OutlineVertexPhase(
        input.positionOS, _OutlineWidth, _OutlinePower,
        vertexInput,
        input.normalOS, input.tangentOS, normalV4, _MiOutline);

    output.uv         = input.uv;
    output.positionWS = vertexInput.positionWS;

    #ifdef _DITHER_ON
    output.positionSS = ComputeScreenPos(output.positionCS);
    #endif

    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, output.positionWS)

    return output;
}

half4 Fragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #ifdef _DITHER_ON
    DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix,
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif

    half4 albedoAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half4 color = albedoAlpha * _OutlineColor;

    // 距离淡出。HDRP 下 input.positionWS 是 RWS，需要转回绝对世界空间。
    float3 cameraWS = _WorldSpaceCameraPos;
    float3 pixelWS  = GetAbsolutePositionWS(input.positionWS);
    float  dis      = distance(cameraWS, pixelWS);

    float clipFactor = 0;
    Remap(dis, float2(_OutlineFadeStart, _OutlineFadeEnd), float2(0, 1), clipFactor);
    clip(1 - clipFactor);

    TOONPBR_DISSOLVE_APPLY(color, input.uv, input)

    return color;
}

#endif // TOONPBR_HDRP_OUTLINE
