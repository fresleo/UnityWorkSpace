#ifndef __GRASS_DEPTH_NORMALS_PASS__
#define __GRASS_DEPTH_NORMALS_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normal       : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 normalWS     : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float4 positionSS   : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);

    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.positionSS = ComputeScreenPos(output.positionCS);
    output.normalWS = half3(normalInput.normalWS);

    return output;
}

void DepthNormalsFragment(
    Varyings input,
    out half4 outNormalWS : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    ApplyGrassDitherClip(input.positionSS, input.positionWS);

    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    outNormalWS = half4(normalWS, 0.0);
}

#endif // __GRASS_DEPTH_NORMALS_PASS__
