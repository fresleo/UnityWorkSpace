#ifndef __WATER_INPUT__
#define __WATER_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _WavesNormal_ST;
    
    float4 _WaterColor;
    float _WavesSpeed;
    float _Tiling;
    float _NormalIntensity;
    float _RefractionFactor;
    float _Transparency;
    float _TransparencyFade;
    float _FoamTiling;
    float _FoamDistance;
    float _FoamOpacity;
    float _Smoothness;
    float _CoastalBlending;
CBUFFER_END

TEXTURE2D(_WavesNormal);    SAMPLER(sampler_WavesNormal);

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

float2 voronoihash61(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

float voronoi61(float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId)
{
    float2 n = floor(v);
    float2 f = frac(v);
    float F1 = 8.0;
    float F2 = 8.0;
    float2 mg = 0;
    for (int j = -1; j <= 1; j++)
    {
        for (int i = -1; i <= 1; i++)
        {
            float2 g = float2(i, j);
            float2 o = voronoihash61(n + g);
            o = (sin(time + o * 6.2831) * 0.5 + 0.5);
            float2 r = f - g - o;
            float d = 0.707 * sqrt(dot(r, r));
            if (d < F1)
            {
                F2 = F1;
                F1 = d;
                mg = g;
                mr = r;
                id = o;
            }
            else if (d < F2)
            {
                F2 = d;
            }
        }
    }
    return F1;
}

#endif
