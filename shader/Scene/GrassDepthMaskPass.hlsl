#ifndef __GRASS_DEPTH_MASK_PASS__
#define __GRASS_DEPTH_MASK_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float4 positionOS   : POSITION;
    float2 texcoord     : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float4 positionSS   : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float objectId      : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthMaskVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.positionSS = ComputeScreenPos(output.positionCS);

    VertexPositionInputs vertexInput0 = GetVertexPositionInputs(float3(0, 0, 0));
    output.objectId = dot(vertexInput0.positionWS, 1);

    return output;
}

void DepthMaskFragment(
    Varyings input,
    out half4 outColor : SV_Target)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    ApplyGrassDitherClip(input.positionSS, input.positionWS);

    half4 color = 0;
    #if defined( _BLOOMFACTORMASK )
    color.r = _BloomFactor;

    #elif defined( _WATERCOLORMASK )
    float val = _WaterColorOn;
        #if defined( SHADER_API_GLES3 )
    val *= input.positionCS.z;
    val = val * _DepthScale - _DepthBias;
        #endif
    color.r = val;

    #elif defined( _SCENESPACEOUTLINEMASK )
    color.r = input.objectId * _SceneSpaceOutlineOn;
    #endif
    outColor = color;
}

#endif // __GRASS_DEPTH_MASK_PASS__
