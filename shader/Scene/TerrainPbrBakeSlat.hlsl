#ifndef __TERRAIN_PBR_SLAT__
#define __TERRAIN_PBR_SLAT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

half3 UnpackNormalScaleRG(half2 packNormal, half scale = 1.0)
{
    half3 normal;
    normal.xy = packNormal * 2.0 - 1.0;
    normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));

    normal.xy *= scale;
    return normalize(normal);
}

void NormalMapMix(float4 uvSplat01, float4 uvSplat23, inout half4 splatControl
    , inout half3 mixedNormal, inout half ao, inout half metallic)
{
    #if defined(_NORMALMAP)
    
    half4 normal0 = SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uvSplat01.xy);
    half4 normal1 = SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, uvSplat01.zw);
    half4 normal2 = SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, uvSplat23.xy);
    half4 normal3 = SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, uvSplat23.zw);

    half3 normal_add = half(0.0);
    normal_add += splatControl.r * UnpackNormalScaleRG(normal0.rg, _NormalScale0);
    normal_add += splatControl.g * UnpackNormalScaleRG(normal1.rg, _NormalScale1);
    normal_add += splatControl.b * UnpackNormalScaleRG(normal2.rg, _NormalScale2);
    normal_add += splatControl.a * UnpackNormalScaleRG(normal3.rg, _NormalScale3);

    // 避免 NaN
    #if HAS_HALF
    normal_add.z += half(0.01);
    #else
    normal_add.z += 1e-5f;
    #endif

    mixedNormal = normalize(normal_add.xyz);

    half ao_add = 0;
    ao_add += normal0.b * splatControl.r;
    ao_add += normal1.b * splatControl.g;
    ao_add += normal2.b * splatControl.b;
    ao_add += normal3.b * splatControl.a;
    ao = ao_add;

    half metallic_add = 0;
    metallic_add += normal0.a * splatControl.r;
    metallic_add += normal1.a * splatControl.g;
    metallic_add += normal2.a * splatControl.b;
    metallic_add += normal3.a * splatControl.a;
    metallic = metallic_add;

    #endif
}

void NormalMapMix(float4 uvSplat01, float4 uvSplat23, inout half4 splatControl
    , inout half3 mixedNormal)
{
    #if defined(_NORMALMAP)
    
    half4 normal0 = SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uvSplat01.xy);
    half4 normal1 = SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, uvSplat01.zw);
    half4 normal2 = SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, uvSplat23.xy);
    half4 normal3 = SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, uvSplat23.zw);
    
    half3 nrm = half(0.0);
    nrm += splatControl.r * UnpackNormalScale(normal0, _NormalScale0);
    nrm += splatControl.g * UnpackNormalScale(normal1, _NormalScale1);
    nrm += splatControl.b * UnpackNormalScale(normal2, _NormalScale2);
    nrm += splatControl.a * UnpackNormalScale(normal3, _NormalScale3);

    // avoid risk of NaN when normalizing.
    #if HAS_HALF
    nrm.z += half(0.01);
    #else
    nrm.z += 1e-5f;
    #endif

    mixedNormal = normalize(nrm.xyz);

    #endif
}

void SplatmapMix(float4 uvSplat01, float4 uvSplat23, inout half4 splatControl
    , out half weight, out half4 mixedDiffuse, out half4 defaultSmoothness
    , inout half3 mixedNormal, inout half ao, inout half metallic)
{
    half4 diffAlbedo[4];
    
    diffAlbedo[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplat01.xy);
    diffAlbedo[1] = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvSplat01.zw);
    diffAlbedo[2] = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvSplat23.xy);
    diffAlbedo[3] = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvSplat23.zw);

    // 如果是美术工作流的话，漫反射的 A 应该是粗糙度
    half smoothness0 = 0, smoothness1 = 0, smoothness2 = 0, smoothness3 = 0;
    #if defined( _USE_PACKED_TEXTURE_MDOE )
    smoothness0 = 1.0 - diffAlbedo[0].a;
    smoothness1 = 1.0 - diffAlbedo[1].a;
    smoothness2 = 1.0 - diffAlbedo[2].a;
    smoothness3 = 1.0 - diffAlbedo[3].a;
    #else
    smoothness0 = diffAlbedo[0].a;
    smoothness1 = diffAlbedo[1].a;
    smoothness2 = diffAlbedo[2].a;
    smoothness3 = diffAlbedo[3].a;
    #endif
    defaultSmoothness = half4(smoothness0, smoothness1, smoothness2, smoothness3);
    defaultSmoothness *= half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);

    #ifndef _TERRAIN_BLEND_HEIGHT // density blending
    if(_NumLayersCount <= 4)
    {
        // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
        half4 opacityAsDensity = saturate((half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a) - (1 - splatControl)) * 20.0);
        opacityAsDensity += 0.001h * splatControl;      // if all weights are zero, default to what the blend mask says
        
        half4 useOpacityAsDensityParam = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
        splatControl = lerp(opacityAsDensity, splatControl, useOpacityAsDensityParam);
    }
    #endif

    // Now that splatControl has changed, we can compute the final weight and normalize
    weight = dot(splatControl, 1.0h);

    #ifdef TERRAIN_SPLAT_ADDPASS
    clip(weight <= 0.005h ? -1.0h : 1.0h);
    #endif

    #ifndef _TERRAIN_BASEMAP_GEN
    // Normalize weights before lighting and restore weights in final modifier functions so that the overal
    // lighting result can be correctly weighted.
    splatControl /= (weight + HALF_MIN);
    #endif

    mixedDiffuse = 0.0h;
    mixedDiffuse += diffAlbedo[0] * half4(_DiffuseRemapScale0.rgb * splatControl.rrr, 1.0h);
    mixedDiffuse += diffAlbedo[1] * half4(_DiffuseRemapScale1.rgb * splatControl.ggg, 1.0h);
    mixedDiffuse += diffAlbedo[2] * half4(_DiffuseRemapScale2.rgb * splatControl.bbb, 1.0h);
    mixedDiffuse += diffAlbedo[3] * half4(_DiffuseRemapScale3.rgb * splatControl.aaa, 1.0h);

    #ifdef _USE_PACKED_TEXTURE_MDOE
    NormalMapMix(uvSplat01, uvSplat23, splatControl, mixedNormal, ao, metallic);
    #else
    NormalMapMix(uvSplat01, uvSplat23, splatControl, mixedNormal);
    #endif
}

half ControlMixValue(half4 splatControl, half val0, half val1, half val2, half val3)
{
    half result = 0;
    result += splatControl.r * val0;
    result += splatControl.g * val1;
    result += splatControl.b * val2;
    result += splatControl.a * val3;
    
    return result;
}

#endif //__TERRAIN_PBR_SLAT__
