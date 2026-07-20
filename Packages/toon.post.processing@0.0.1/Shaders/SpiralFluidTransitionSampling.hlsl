#ifndef SPIRAL_FLUID_TRANSITION_SAMPLING_INCLUDED
#define SPIRAL_FLUID_TRANSITION_SAMPLING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

half4 SpiralFluidSampleFromTex(float2 uv)
{
    half4 color = SAMPLE_TEXTURE2D_X(_FromTex, sampler_FromTex, uv);
#if defined(_FROM_TEX_DISPLAY_SRGB)
    color.rgb = SRGBToLinear(saturate(color.rgb));
#endif
    return color;
}

half4 SpiralFluidSampleToTex(float2 uv)
{
    half4 color = SAMPLE_TEXTURE2D_X(_ToTex, sampler_ToTex, uv);
#if defined(_TO_TEX_DISPLAY_SRGB)
    color.rgb = SRGBToLinear(saturate(color.rgb));
#endif
#if _USE_FAST_SRGB_LINEAR_CONVERSION
    color.rgb = FastLinearToSRGB(color.rgb);
#else
    color.rgb = LinearToSRGB(color.rgb);
#endif
    return color;
}

half4 SpiralFluidDecodeToTexSample(half4 color)
{
#if defined(_TO_TEX_DISPLAY_SRGB)
    color.rgb = SRGBToLinear(saturate(color.rgb));
#endif
    return color;
}

// FromRT 轻量 5-tap 十字模糊 (preReveal / membrane 等, radius 为 UV 偏移)
half4 SpiralFluidSampleFromRTBlur(float2 uv, half radius)
{
    half4 color = SpiralFluidSampleFromTex(uv) * 0.36h;
    color += SpiralFluidSampleFromTex(saturate(uv + float2(radius, 0.0))) * 0.16h;
    color += SpiralFluidSampleFromTex(saturate(uv - float2(radius, 0.0))) * 0.16h;
    color += SpiralFluidSampleFromTex(saturate(uv + float2(0.0, radius))) * 0.16h;
    color += SpiralFluidSampleFromTex(saturate(uv - float2(0.0, radius))) * 0.16h;
    return color;
}

// ToTex 9-tap 星形模糊，用于模糊→清晰过渡（权重总和 = 1.00）
// center=0.28, 十字轴×4=0.40, 对角×4=0.32
half4 SpiralFluidSampleToTexBlur(float2 uv, half radius)
{
    half d = radius * 0.707h;
    half4 c = SpiralFluidSampleToTex(uv) * 0.28h;
    c += SpiralFluidSampleToTex(saturate(uv + float2( radius,  0.0h))) * 0.10h;
    c += SpiralFluidSampleToTex(saturate(uv + float2(-radius,  0.0h))) * 0.10h;
    c += SpiralFluidSampleToTex(saturate(uv + float2( 0.0h,  radius))) * 0.10h;
    c += SpiralFluidSampleToTex(saturate(uv + float2( 0.0h, -radius))) * 0.10h;
    c += SpiralFluidSampleToTex(saturate(uv + float2( d,  d))) * 0.08h;
    c += SpiralFluidSampleToTex(saturate(uv + float2(-d,  d))) * 0.08h;
    c += SpiralFluidSampleToTex(saturate(uv + float2( d, -d))) * 0.08h;
    c += SpiralFluidSampleToTex(saturate(uv + float2(-d, -d))) * 0.08h;
    return c;
}

#endif
