#ifndef __GRASS_VIEW_SPACE_NORMALS_PASS__
#define __GRASS_VIEW_SPACE_NORMALS_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

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
    float3 normalWS     : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float4 positionSS   : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings ViewSpaceNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.positionSS = ComputeScreenPos(output.positionCS);
    output.normalWS = normalInput.normalWS;

    return output;
}

half4 ViewSpaceNormalsFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    ApplyGrassDitherClip(input.positionSS, input.positionWS);

    half3 normalVS = mul(input.normalWS, (float3x3) UNITY_MATRIX_I_V);
    half3 remapNormal = 0;
    Remap(normalVS, float2(-1, 1), float2(0, 1), remapNormal);

    return half4(remapNormal, 1);
}

#endif // __GRASS_VIEW_SPACE_NORMALS_PASS__
