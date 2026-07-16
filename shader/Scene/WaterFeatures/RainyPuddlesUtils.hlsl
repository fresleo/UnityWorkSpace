// 雨坑的工具
#ifndef __RAINY_PUDDLES_UTILS__
#define __RAINY_PUDDLES_UTILS__

/*
 * 视差偏移
 */
inline float2 ParallaxOffset(half h, half height, half3 viewDir)
{
    h = h * height - height / 2.0;
    float3 v = normalize(viewDir);
    v.z += 0.42;
    return h * (v.xy / v.z);
}

/*
 * 计算对比度
 */
float4 CalculateContrast(float contrastValue, float4 colorTarget)
{
    float t = 0.5 * (1.0 - contrastValue);
    return mul(float4x4(contrastValue, 0, 0, t, 0, contrastValue, 0, t, 0, 0, contrastValue, t, 0, 0, 0, 1), colorTarget);
}

float2 VoronoiHash(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

/**
 * Voronoi 图 
 */
float Voronoi(float2 v, float time, inout float2 id, inout float2 mr)
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
            float2 o = VoronoiHash(n + g);
            o = (sin(time + o * 6.2831) * 0.5 + 0.5);
            float2 r = f - g - o;
            float d = 0.5 * dot(r, r);
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

#endif // __RAINY_PUDDLES_UTILS__
