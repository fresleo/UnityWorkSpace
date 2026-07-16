#ifndef __LIT_ALPHA_TEST_VIEW_SPACE_NORMALS__
#define __LIT_ALPHA_TEST_VIEW_SPACE_NORMALS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/SurfaceInput_AlphaTestOn.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;

    float2 uv           : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float4 positionSS   : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings ViewSpaceNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

    output.positionCS = vertexInput.positionCS;
    output.normalWS = normalInput.normalWS;
    
    return output;
}

half4 ViewSpaceNormalsFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_AlphaTestMap, sampler_AlphaTestMap)).a, _BaseColor, _Cutoff);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    // 法线
    half3 normalVS = mul(input.normalWS, (float3x3)UNITY_MATRIX_I_V);

    half3 remapNormal = 0;
    Remap(normalVS, float2(-1, 1), float2(0, 1), remapNormal);

    half4 col = half4(remapNormal, 1); // a是是否有写入的标记，所以这里给1
    return col;
}

#endif // __LIT_ALPHA_TEST_VIEW_SPACE_NORMALS__
