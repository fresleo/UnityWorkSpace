//referenced:https://github.com/you-ri/LiliumToonGraph/blob/master/Packages/jp.lilium.toongraph/Editor/ShaderGraph/ToonOutlinePass.hlsl
// https://zhuanlan.zhihu.com/p/95986273 解释乘以w的原因

#ifndef TOONPBR_OUTLINEPASS_INCLUDED
#define TOONPBR_OUTLINEPASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"
#include "./ToonPBR_OutlineLib.hlsl"
#include "./ToonPBR_Dissolve.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

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
    float4 color        : COLOR;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;

    TOONPBR_DISSOLVE_FACTOR(2)

    float4 positionSS   : TEXCOORD3;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    float3 normalV3 = DecodeOctOutlineNormal(input.outlineNormalOct);
    float4 normalV4 = float4(normalV3, input.color.a);
    
    VertexPositionInputs vertexInput;
    output.positionCS = OutlineVertexPhase(
        input.positionOS, _OutlineWidth, _OutlinePower,
        vertexInput,
        input.normalOS, input.tangentOS, normalV4, _MiOutline);

    output.uv = input.uv;
    output.positionSS = ComputeScreenPos(output.positionCS);
    output.positionWS = vertexInput.positionWS;

    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, output.positionWS)

    return output;
}

half4 Fragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // 抖动
    #ifdef _DITHER_ON
    DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif

    half4 albedoAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    
    float4 outlineColor=_OutlineColor;
    #ifdef _OUTLINELOCALCOLOR
    outlineColor = GetOutlineColor(outlineColor,input.positionWS);
    #endif
        
    half4 color = albedoAlpha * outlineColor;

    // 计算摄像机与像素之间的距离
    float3 cameraWS = _WorldSpaceCameraPos;
    //float3 pixelWS = GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23);
    float3 pixelWS = GetAbsolutePositionWS(input.positionWS);
    float dis = distance(cameraWS, pixelWS);

    // 根据距离，计算该何时丢弃像素
    float clipFactor = 0;
    Remap(dis, float2(_OutlineFadeStart, _OutlineFadeEnd), float2 (0, 1), clipFactor);
    clip(1 - clipFactor);

    TOONPBR_DISSOLVE_APPLY(color, input.uv, input)
    
    return color;
}

#endif // TOONPBR_OUTLINEPASS_INCLUDED
