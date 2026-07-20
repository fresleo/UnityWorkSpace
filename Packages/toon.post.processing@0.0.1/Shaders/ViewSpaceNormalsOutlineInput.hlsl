#ifndef __VIEW_SPACE_NORMALS_OUTLINE_INPUT__
#define __VIEW_SPACE_NORMALS_OUTLINE_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _OutlineColor;
    float4 _OutlineData_0, _OutlineData_1, _OutlineData_2;
CBUFFER_END

TEXTURE2D_X(_OutlineTexture); SAMPLER(sampler_OutlineTexture);

#define OUTLINE_DISTANCE_FADE _OutlineData_0.x
#define OUTLINE_SCALE _OutlineData_0.y

#define DEPTH_THRESHOLD _OutlineData_0.z
#define DEPTH_DIFF_MULTIPLIER _OutlineData_0.w

#define NORMAL_THRESHOLD _OutlineData_1.x
#define STEEP_ANGLE_THRESHOLD _OutlineData_1.y
#define STEEP_ANGLE_MULTIPLIER _OutlineData_1.z

#endif // __VIEW_SPACE_NORMALS_OUTLINE_INPUT__
