// FBM 雾效 (remix of pontino's Fog: https://www.shadertoy.com/view/tst3zr)

#ifndef __FBM_FOG__
#define __FBM_FOG__

float2 FracRandom2(float2 st)
{
    st = float2(dot(st, float2(127.1, 311.7)), dot(st, float2(269.5, 183.3)));
    return -1.0 + 2.0 * frac(sin(st) * 7.0);
}

float FillNoise(float2 st)
{
    float2 i = floor(st);
    float2 f = frac(st);
    float2 u = f * f * (3.0 - 2.0 * f);
    
    return lerp(
        lerp(dot(FracRandom2(i + float2(0, 0)), f - float2(0, 0)),
             dot(FracRandom2(i + float2(1, 0)), f - float2(1, 0)), u.x),
        lerp(dot(FracRandom2(i + float2(0, 1)), f - float2(0, 1)),
             dot(FracRandom2(i + float2(1, 1)), f - float2(1, 1)), u.x), u.y);
}

float FillFBM(float2 coord)
{
    float value = 0.0;
    float scale = 0.5;
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        value += FillNoise(coord) * scale;
        coord *= 2.0;
        scale *= 0.5;
    }
    return value + 0.2;
}

#endif
