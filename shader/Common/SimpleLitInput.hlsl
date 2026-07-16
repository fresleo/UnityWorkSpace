#ifndef __SIMPLE_LIT_INPUT__
#define __SIMPLE_LIT_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Input.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4  _BaseMap_ST;
    half4   _BaseColor;

    // 抖动
    half    _DitherIntensity, _DitherSize, _DitherAlpha;
    half    _DitherWithMatrix;
    float4  _DitherTexture_TexelSize;

    XKNIGHT_DEPTH_MASK_INPUT_1
CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_DitherTexture);

#endif // __SIMPLE_LIT_INPUT__
