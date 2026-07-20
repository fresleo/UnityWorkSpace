#ifndef SPIRAL_FLUID_TRANSITION_CORE_INCLUDED
#define SPIRAL_FLUID_TRANSITION_CORE_INCLUDED

half SpiralFluidEaseOutCubic(half x)
{
    half inv = 1.0h - saturate(x);
    return 1.0h - inv * inv * inv;
}

half SpiralFluidTriangleWave(float phase)
{
    half t = half(frac(phase));
    return 1.0h - abs(t * 2.0h - 1.0h) * 2.0h;
}

half2 SpiralFluidRotateApprox(half2 value, float turns)
{
    half s = SpiralFluidTriangleWave(turns - 0.25h);
    half c = SpiralFluidTriangleWave(turns);
    return half2(value.x * c - value.y * s, value.x * s + value.y * c);
}

half2 SpiralFluidRotate(half2 value, float angle)
{
#if defined(_SPIRAL_FLUID_LOW_QUALITY)
    return SpiralFluidRotateApprox(value, angle * 0.15915494);
#else
    float s = sin(angle);
    float c = cos(angle);
    return half2(value.x * half(c) - value.y * half(s), value.x * half(s) + value.y * half(c));
#endif
}

half SpiralFluidLogSpiralWave(float2 value, float time, float ratio, float rate, float scale, float phase)
{
    float r = max(length(value), 1.0e-3);
    float theta = atan2(value.y, value.x);
    float logSpiral = log(r) / max(ratio, 1.0e-3) + theta;
    return half(sin(time * rate + scale * logSpiral + phase));
}

half3 SpiralFluidLinearToSRGB(half3 color)
{
#if _USE_FAST_SRGB_LINEAR_CONVERSION
    return half3(FastLinearToSRGB(color));
#else
    return half3(LinearToSRGB(color));
#endif
}

float2 SpiralFluidPolarUV(float2 uv, float2 center)
{
    float2 offset = uv - center;
    float r = length(offset) * 2.0;
    float t = atan2(offset.y, offset.x) * (1.0 / 6.2831853);
    return float2(t, r);
}

half2 SpiralFluidSampleTextureDistortion(float2 polarUV, half aspect)
{
    float2 distortionUV = polarUV * _DistortionTilingFlow.xy;
    distortionUV += frac(_DistortionTilingFlow.zw * _TransitionParams.z);
    half2 distortion = SAMPLE_TEXTURE2D(_DistortionTex, sampler_DistortionTex, distortionUV).rg;
    distortion = distortion * 2.0h - 1.0h;
    distortion.x /= max(aspect, 1.0e-4h);
    return distortion;
}

#endif
