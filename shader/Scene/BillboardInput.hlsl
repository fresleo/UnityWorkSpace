#ifndef __BILLBOARD_INPUT__
#define __BILLBOARD_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Input.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4	_BaseMap_ST;
    half4	_BaseColor;
    half	_Cutoff;
    
    half	_GIIndirectDiffuseBoost;

    XKNIGHT_DEPTH_MASK_INPUT_1
CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

#endif // __BILLBOARD_INPUT__
