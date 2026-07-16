#ifndef __TOONPBR_DEPTH_NORMALS__
#define __TOONPBR_DEPTH_NORMALS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "./ToonPBR_Dissolve.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;

    float2 uv           : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS  : SV_POSITION;
    
    float2 uv          : TEXCOORD0;
    half3 normalWS     : TEXCOORD1;
    half4 tangentWS    : TEXCOORD2;    // xyz: tangent, w: sign
    
    TOONPBR_DISSOLVE_FACTOR(3)
    float4 positionSS  : TEXCOORD4;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    VertexPositionInputs vertexInput = GetVertexPositionInputsWithFOVFix(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;
    
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = half3(normalInput.normalWS);
    
    float sign = input.tangentOS.w * float(GetOddNegativeScale());
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.tangentWS = tangentWS;

    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    
    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, vertexInput.positionWS)

    return output;
}

void DepthNormalsFragment(
    Varyings input
    , out half4 outNormalWS : SV_TARGET
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // 溶解
    half4 fakeColor = half4(0, 0, 0, 0);
    TOONPBR_DISSOLVE_APPLY(fakeColor, input.uv, input)
    
    // 项目中普遍都是强制需要有法线的，所以这里直接读了
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    float3 normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    normalWS = NormalizeNormalPerPixel(normalWS);

    // 返回0的话，就相当于把角色的部分给扣掉了
    outNormalWS = half4(normalWS * _WriteDepthNormals_On, 0.0);
}

#endif // __TOONPBR_DEPTH_NORMALS__
