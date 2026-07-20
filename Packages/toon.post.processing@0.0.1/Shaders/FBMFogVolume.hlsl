// https://www.shadertoy.com/view/7dfXR4

#ifndef __FBM_FOG_VOLUME__
#define __FBM_FOG_VOLUME__

#define FOG_PI 3.141592654
#define FOG_LOOP 4
#define FOG_F3 0.3333333
#define FOG_G3 0.1666667

float3 FogRandom3(float3 c)
{
    float j = 4096.0 * sin(dot(c, float3(17.0, 59.4, 15.0)));
    float3 r;
    r.z = frac(512.0 * j);
    j *= 0.125;
    r.x = frac(512.0 * j);
    j *= 0.125;
    r.y = frac(512.0 * j);
    return r - 0.5;
}

float Simplex3D(float3 p)
{
    float3 s = floor(p + dot(p, float3(FOG_F3, FOG_F3, FOG_F3)));
    float3 x = p - s + dot(s, float3(FOG_G3, FOG_G3, FOG_G3));
    float3 e = step(0.0, x - x.yzx);
    float3 i1 = e * (1.0 - e.zxy);
    float3 i2 = 1.0 - e.zxy * (1.0 - e);
    float3 x1 = x - i1 + FOG_G3;
    float3 x2 = x - i2 + 2.0 * FOG_G3;
    float3 x3 = x - 1.0 + 3.0 * FOG_G3;

    float4 w, d;
    w.x = dot(x, x);
    w.y = dot(x1, x1);
    w.z = dot(x2, x2);
    w.w = dot(x3, x3);
    w = max(0.6 - w, 0.0);
    d.x = dot(FogRandom3(s), x);
    d.y = dot(FogRandom3(s + i1), x1);
    d.z = dot(FogRandom3(s + i2), x2);
    d.w = dot(FogRandom3(s + 1.0), x3);
    w *= w;
    w *= w;
    d *= w;
    return dot(d, float4(52.0, 52.0, 52.0, 52.0));
}

float FogFbmMask(float3 p)
{
    float res = 1.0;
    float fre = 1.0;
    float ap = 0.5 + 0.5 / (1e-5 + (float)FOG_LOOP);
    for (int i = 0; i < FOG_LOOP; i++)
    {
        float temp = 0.5 + 0.5 * Simplex3D(p * fre);
        res *= 1.0 - 0.9 * ap + ap * temp;
        fre *= 3.0;
        ap *= 0.5;
    }
    res = pow(res, 5.0);
    return max(res, 0.0);
}

// 采样缩放
#define FOG_SAMPLER_SCALE 0.01
// 步数
#define FOG_STEP_COUNT 32
// 范围
#define FOG_TMIN 20.0
#define FOG_TMAX 120.0

void TraceFogVolume(
    float3 rayOrigin,
    float3 rayDir,
    float tMin,
    float tMax,
    float samplerScale,
    int stepCount,
    out float3 outColor,
    out float outAlpha)
{
    outColor = 0;
    outAlpha = 0;
    float stepT = (tMax - tMin) / (float)stepCount;
    float bottom = rayOrigin.y - 400.0;
    float top = rayOrigin.y + 70.0;

    for (float t = tMin; t < tMax;)
    {
        float3 pos = rayOrigin + t * rayDir;
        float density = FogFbmMask(samplerScale * pos);
        float heightFactor = saturate((pos.y - bottom) / (top - bottom));
        float k = 100.0 * pow(1.0 - heightFactor, 3.0);
        density = saturate(density * k);

        float d2 = density * density;
        float d05 = sqrt(density);
        float3 cloudColor = 2.0 - float3(0.85, 0.85, 0.99);
        float3 color = 2.2 * pow(float3(d2, d2, d2), cloudColor);
        outColor += color * (1.0 - outAlpha);
        outAlpha += density * (1.0 - outAlpha);

        if (outAlpha > 0.99) break;
        t += stepT * (1.0 - 0.99 * d05);
    }
}

#endif
