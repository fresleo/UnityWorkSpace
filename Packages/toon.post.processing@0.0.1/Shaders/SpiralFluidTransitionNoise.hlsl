#if !defined(SPIRAL_FLUID_TRANSITION_NOISE_H)
#define SPIRAL_FLUID_TRANSITION_NOISE_H

half SpiralFluidHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return half(frac(p.x * p.y));
}

half SpiralFluidValueNoise(float2 uv)
{
    float2 cell = floor(uv);
    float2 localUV = frac(uv);
    localUV = localUV * localUV * (3.0 - 2.0 * localUV);

    half a = SpiralFluidHash21(cell);
    half b = SpiralFluidHash21(cell + float2(1.0, 0.0));
    half c = SpiralFluidHash21(cell + float2(0.0, 1.0));
    half d = SpiralFluidHash21(cell + float2(1.0, 1.0));

    half x0 = lerp(a, b, half(localUV.x));
    half x1 = lerp(c, d, half(localUV.x));
    return lerp(x0, x1, half(localUV.y));
}

half3 SpiralFluidSampleNoise(float2 uv)
{
    half n0 = SpiralFluidValueNoise(uv);
    half n1 = SpiralFluidValueNoise(uv * 1.73 + 17.31);
    half n2 = SpiralFluidValueNoise(uv * 2.11 + 3.17);
    return half3(n0, n1, n2);
}

#endif
