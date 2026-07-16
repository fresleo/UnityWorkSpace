#ifndef __TERRAIN_VT_LIT_DEPTH_NORMALS__
#define __TERRAIN_VT_LIT_DEPTH_NORMALS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;

    float2 texcoord : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 uvMainAndLM : TEXCOORD0; // xy: control, zw: lightmap

    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 tangentWS : TEXCOORD3;
    float3 viewDirWS : TEXCOORD4;

    float4 positionCS : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


Varyings DepthNormalOnlyVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uvMainAndLM.xy = TRANSFORM_TEX(input.texcoord, _Diffuse);

    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

    output.normalWS = normalInput.normalWS;
    output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

    return output;
}

void DepthNormalOnlyFragment(
    Varyings input
    , out half4 outNormalWS : SV_TARGET
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    half4 mixedNormal = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, input.uvMainAndLM.xy);

    half3 normalTS = 0;
    normalTS.rg = mixedNormal.rg * 2 - 1;
    normalTS.b = sqrt(1 - normalTS.r * normalTS.r - normalTS.g * normalTS.g);
    normalTS = SafeNormalize(normalTS);
    
    half occlusion = mixedNormal.b;
    half metallic = mixedNormal.a;

    float sgn = input.tangentWS.w; // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    
    half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    normalWS = NormalizeNormalPerPixel(normalWS);

    outNormalWS = half4(normalWS, 0.0);
}

#endif //__TERRAIN_VT_LIT_DEPTH_NORMALS__
