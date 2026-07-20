#ifndef __PRE_OBJECT_ID_OUTLINE_INPUT__
#define __PRE_OBJECT_ID_OUTLINE_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    #if UNITY_VERSION < 600000
    float4 _BlitTexture_TexelSize;
    #endif

    half4 _OutlineColor;
    float4 _OutlineData;
    
    float _BlurScale;
CBUFFER_END

#define OUTLINE_INTENSITY_MULTIPLIER _OutlineData.x
#define OUTLINE_DISTANCE_FADE _OutlineData.y
#define OUTLINE_MIN_SEPARATION _OutlineData.z
#define OUTLINE_WIDTH _OutlineData.w

TEXTURE2D_X(_OutlineTexture); SAMPLER(sampler_OutlineTexture);

#endif // __PRE_OBJECT_ID_OUTLINE_INPUT__
