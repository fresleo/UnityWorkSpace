#ifndef SPIRAL_FLUID_TRANSITION_BRIGHTEN_INCLUDED
#define SPIRAL_FLUID_TRANSITION_BRIGHTEN_INCLUDED

half3 SpiralFluidSampleWarmBrightLut(half3 displayColor)
{
    float lutSize = max(_WarmBrightLutParams.x, 2.0);
    float lutWidth = max(_WarmBrightLutParams.y, lutSize * lutSize);
    float lutHeight = max(_WarmBrightLutParams.z, lutSize);
    float lutLast = lutSize - 1.0;
    float3 lutColor = saturate(float3(displayColor));
    float blue = lutColor.b * lutLast;
    float sliceLow = floor(blue);
    float sliceHigh = min(sliceLow + 1.0, lutLast);
    float sliceLerp = blue - sliceLow;

    float2 uvLow = float2(
        (sliceLow * lutSize + lutColor.r * lutLast + 0.5) / lutWidth,
        (lutColor.g * lutLast + 0.5) / lutHeight);
    float2 uvHigh = float2(
        (sliceHigh * lutSize + lutColor.r * lutLast + 0.5) / lutWidth,
        uvLow.y);

    half3 lowColor = SAMPLE_TEXTURE2D_LOD(_WarmBrightLut, sampler_WarmBrightLut, uvLow, 0).rgb;
    half3 highColor = SAMPLE_TEXTURE2D_LOD(_WarmBrightLut, sampler_WarmBrightLut, uvHigh, 0).rgb;
    return lerp(lowColor, highColor, half(sliceLerp));
}

half3 SpiralFluidApplyPreRevealBrighten(half3 fromColor, half preRevealBright)
{
    half preRevealGrade = saturate(preRevealBright);
    if (preRevealGrade <= 1.0e-4h || _WarmBrightLutParams.x < 1.5)
    {
        return fromColor;
    }

#if defined(UNITY_COLORSPACE_GAMMA)
    half3 lutInput = saturate(fromColor);
#else
    half3 lutInput = half3(LinearToSRGB(saturate(fromColor)));
#endif
    half3 lutColor = SpiralFluidSampleWarmBrightLut(lutInput);
    return lerp(fromColor, lutColor, preRevealGrade);
}

#endif
