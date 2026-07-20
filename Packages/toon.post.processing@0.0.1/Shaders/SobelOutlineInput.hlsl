#ifndef __SOBEL_OUTLINE_INPUT__
#define __SOBEL_OUTLINE_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

CBUFFER_START(UnityPerMaterial)
    #if UNITY_VERSION < 600000
    float4 _BlitTexture_TexelSize;
    #endif

    half4 _OutlineColor;
    float4 _OutlineData;
CBUFFER_END

#define OUTLINE_THICKNESS _OutlineData.x
#define OUTLINE_DISTANCE_FADE _OutlineData.y
#define OUTLINE_EDGE_MULTIPLIER _OutlineData.z
#define OUTLINE_EDGE_BIAS _OutlineData.w

#endif // __SOBEL_OUTLINE_INPUT__
