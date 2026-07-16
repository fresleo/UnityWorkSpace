#ifndef __TERRAIN_PBR_DEPTH_MASK__
#define __TERRAIN_PBR_DEPTH_MASK__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 clipPos      : SV_POSITION;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthMaskVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if !defined( NOT_REQUIRES_TERRAIN_INSTANCING )
    TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);
    #endif

    output.clipPos = TransformObjectToHClip(input.positionOS.xyz);
    
    return output;
}

void DepthMaskFragment(Varyings input
    , out half4 outColor : SV_Target
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    // 一般地形的作用就是遮挡的，所以直接返回黑色就好
    outColor = half4(0, 0, 0, 0);
}

#endif //__TERRAIN_PBR_DEPTH_MASK__