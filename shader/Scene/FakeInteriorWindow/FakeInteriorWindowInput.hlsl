#ifndef __FAKE_INTERIOR_WINDOW_INPUT__
#define __FAKE_INTERIOR_WINDOW_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _Dust_Texture_ST, _Crack_Mask_ST, _Break_Mask_ST, _Curtain_Texture_ST, _BakerAOMap_ST;

    float2 _Room_Tiling, _Room_Offset;
    float _Room_Depth;
    float2 _Window_Number, _Frame_Thickness;

    half4 _Glass_Color;
    float _Smoothness;
    float _Metalic;
    float _Emission;

    float _Dust_Intensity;
    half4 _Dust_Color;
    float _Dust_Smoothness;

    half4 _Crack_Color;
    
    float _Noise_Intensity;
    half4 _Break_Noise_Color;

    float _Glass_Break;
    float _Glass_Thickness;
    half4 _Thickness_Color;
    float2 _Shadow_Offset;

    float2 _Curtain_Tiling;
    half4 _Curtain_Color;
    float _Curtain_Depth;
    
CBUFFER_END

SAMPLER(SamplerState_Linear_Repeat);

TEXTURECUBE(_Cubemap); SAMPLER(sampler_Cubemap);

TEXTURE2D(_Dust_Texture); SAMPLER(sampler_Dust_Texture);

TEXTURE2D(_Crack_Mask); SAMPLER(sampler_Crack_Mask);

TEXTURE2D(_Break_Mask); SAMPLER(sampler_Break_Mask);

TEXTURE2D(_Curtain_Texture); SAMPLER(sampler_Curtain_Texture);

TEXTURE2D(_BakerAOMap); SAMPLER(sampler_BakerAOMap);


struct Bindings_Parallax
{
    float3 TangentSpaceViewDirection;
    float4 uv;
};

void SamplePatallaxMap(
    float parallaxOffset, TEXTURE2D_PARAM(patallaxMap, sampler_patallaxMap),
    Bindings_Parallax IN,
    out float OUT)
{
    float3 offset = IN.TangentSpaceViewDirection * parallaxOffset.xxx;
    float2 uv = IN.uv.xy * float2(1, 1) + offset.xy;
    
    half4 patallaxCol = SAMPLE_TEXTURE2D(patallaxMap, sampler_patallaxMap, uv);
    
    OUT = patallaxCol.r;
}

float2 SampleParallaxMapUV(float parallaxOffset, Bindings_Parallax IN)
{
    float3 offset = IN.TangentSpaceViewDirection * parallaxOffset.xxx;
    float2 uv = IN.uv.xy * float2(1, 1) + offset.xy;
    return uv;
}

void Blend_Overlay(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

half3 SampleBakerAO(float2 uv)
{
    half3 ao = 1.0;
    #ifdef _BAKERAO
    ao = SAMPLE_TEXTURE2D(_BakerAOMap, sampler_BakerAOMap, uv);
    #endif
    return ao;
}

float3 SampleXyNormal(float2 uv, TEXTURE2D_PARAM(normal, sampler_normal))
{
    half4 col = SAMPLE_TEXTURE2D(normal, sampler_normal, uv);
    
    float2 nxy = col.xy * 2.0 - 1.0;

    float4 data = 0;
    data.xy = nxy;
    data.z = max(1.0e-16, sqrt(1.0 - saturate(dot(nxy, nxy))));

    float3 result = UnpackNormal(data);
    return result;
}

#endif // __FAKE_INTERIOR_WINDOW_INPUT__
