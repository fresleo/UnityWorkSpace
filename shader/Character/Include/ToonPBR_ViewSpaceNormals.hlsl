#ifndef __TOONPBR_VIEW_SPACE_NORMALS__
#define __TOONPBR_VIEW_SPACE_NORMALS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"
#include "./ToonPBR_Dissolve.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    
    float3 normalOS : NORMAL;

    float2 uv : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    
    TOONPBR_DISSOLVE_FACTOR(2)
    float4 positionSS : TEXCOORD3;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings ViewSpaceNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    VertexPositionInputs vertexInput = GetVertexPositionInputsWithFOVFix(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;
    output.positionSS = ComputeScreenPos(output.positionCS);
    
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
    output.normalWS = normalInput.normalWS;

    output.uv = input.uv;
    
    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, vertexInput.positionWS)
    
    return output;
}

half4 ViewSpaceNormalsFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // 溶解
    half4 fakeColor = half4(0, 0, 0, 0);
    TOONPBR_DISSOLVE_APPLY(fakeColor, input.uv, input)

    // 法线
    half3 normalVS = mul(input.normalWS, (float3x3) UNITY_MATRIX_I_V);
    
    half3 remapNormal = 0;
    Remap(normalVS, float2(-1, 1), float2(0, 1), remapNormal);

    half4 col = half4(remapNormal, 1); // a是是否有写入的标记，所以这里给1
    return col;
}

#endif // __TOONPBR_VIEW_SPACE_NORMALS__
