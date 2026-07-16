#ifndef __TERRAIN_PBR_DEPTH_ONLY__
#define __TERRAIN_PBR_DEPTH_ONLY__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 position     : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 clipPos      : SV_POSITION;
    float2 texcoord     : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if !defined( NOT_REQUIRES_TERRAIN_INSTANCING )
    TerrainInstancing(input.position, input.normalOS);
    #endif
    
    output.clipPos = TransformObjectToHClip(input.position.xyz);
    output.texcoord = input.texcoord;
    
    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    return input.clipPos.z;
}

#endif //__TERRAIN_PBR_DEPTH_ONLY__
