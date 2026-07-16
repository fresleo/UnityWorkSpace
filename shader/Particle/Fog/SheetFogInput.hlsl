#ifndef __SHEET_FOG_INPUT__
#define __SHEET_FOG_INPUT__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

CBUFFER_START(UnityPerMaterial)
    float2 _SimpleNoiseAnimation;
    float _SimpleNoiseScale;
    float _SimpleNoiseAmount;
    float _SimpleNoiseRemap;

    float3 _SimplexNoiseAnimation;
    float _SimplexNoiseScale;
    float _SimplexNoiseAmount;
    float _SimplexNoiseRemap;

    float3 _VoronoiNoiseAnimation;
    float _VoronoiNoiseScale;
    float _VoronoiNoiseAmount;
    float _VoronoiNoiseRemap;

    float _CombinedNoiseRemap;
    float4 _Albedo;

    float _SurfaceDepthFade;
    float _CameraDepthFadeRange;
    float _CameraDepthFadeOffset;

    float _ViewAngleFading;
    float _CameraDistanceFading;
CBUFFER_END

#endif // __SHEET_FOG_INPUT__
