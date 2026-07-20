#ifndef SSR_COMMON
#define SSR_COMMON

#define dot2(x) dot(x, x)

//取明度
inline half getLuma(float3 rgb) 
{ 
    const half3 lum = float3(0.299, 0.587, 0.114);
    return dot(rgb, lum);
}

SamplerState sampler_PointClamp;
SamplerState sampler_PointRepeat;
SamplerState sampler_LinearClamp;
//降分辨率存储LinearDepth
TEXTURE2D_X_HALF(_DownscaledDepthRT);
float4 _DownscaledDepthRT_TexelSize;   

TEXTURE2D_X_HALF(_DownscaledBackDepthRT);
float4 _DownscaledBackDepthRT_TexelSize;

inline float GetLinearDepth(float2 uv) {
    float depth = SAMPLE_TEXTURE2D_X_LOD(_DownscaledDepthRT, sampler_PointClamp, uv, 0).r;
    return depth;
}

inline void GetLinearDepths(float2 uv, out float sceneDepth, out float sceneBackDepth)
{
    float2 depths = SAMPLE_TEXTURE2D_X_LOD(_DownscaledBackDepthRT, sampler_PointClamp, uv, 0).xy;
    sceneDepth = depths.x;
    sceneBackDepth = depths.y;
}
#endif