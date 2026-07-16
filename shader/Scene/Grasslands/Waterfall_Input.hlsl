#ifndef __WATERFALL_INPUT__
#define __WATERFALL_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _FoamColor;
    float4 _GradientColor;
    float4 _MainColor;
    float _Smoothness;
    float _FoamFade;
    float _FoamLevel;
    float _FoamOffset;
    float _FoamScale;
    float _FoamVoronoiSpeed;
    float _FoamTilingY;
    float _FoamTilingX;
    float _FlowSpeed;
    float _RefractionFactor;
    float _NormalScale;
    float _NormalTilingY;
    float _NormalTilingX;
    float _GradientFade;
    float _GradientLevel;
    float _VOIntensity;
    float _VOScale;
    float _OpacityLevel;
    float _OpacityFade;
CBUFFER_END

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

float3 mod2D289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float2 mod2D289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float3 permute(float3 x) { return mod2D289(((x * 34.0) + 1.0) * x); }

float snoise(float2 v)
{
    const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
    float2 i = floor(v + dot(v, C.yy));
    float2 x0 = v - i + dot(i, C.xx);
    float2 i1;
    i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
    float4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;
    i = mod2D289(i);
    float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
    float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
    m = m * m;
    m = m * m;
    float3 x = 2.0 * frac(p * C.www) - 1.0;
    float3 h = abs(x) - 0.5;
    float3 ox = floor(x + 0.5);
    float3 a0 = x - ox;
    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
    float3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot(m, g);
}

inline float4 ASE_ComputeGrabScreenPos(float4 pos)
{
    #if UNITY_UV_STARTS_AT_TOP
    float scale = -1.0;
    #else
    float scale = 1.0;
    #endif
    float4 o = pos;
    o.y = pos.w * 0.5f;
    o.y = (pos.y - o.y) * _ProjectionParams.x * scale + o.y;
    return o;
}

inline float2 UnityVoronoiRandomVector(float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y * +offset) * 0.5 + 0.5, cos(UV.x * offset) * 0.5 + 0.5);
}

//x - Out y - Cells
float3 UnityVoronoi(float2 UV, float AngleOffset, float CellDensity, inout float2 mr)
{
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 lattice = float2(x, y);
            float2 offset = UnityVoronoiRandomVector(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);

            if (d < res.x)
            {
                mr = f - lattice - offset;
                res = float3(d, offset.x, offset.y);
            }
        }
    }
    return res;
}

#endif
