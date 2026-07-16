#ifndef __XKNIGHT_GLOBAL_ILLUMINATION__
#define __XKNIGHT_GLOBAL_ILLUMINATION__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "./RealtimeLights.hlsl"

#if USE_FORWARD_PLUS
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#endif

// 如果未定义 lightmap，则我们从 SH 评估 GI（环境 + 探针）

// Renamed -> LIGHTMAP_SHADOW_MIXING
#if !defined(_MIXED_LIGHTING_SUBTRACTIVE) && defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK)
    #define _MIXED_LIGHTING_SUBTRACTIVE
#endif

// Samples SH L0, L1 and L2 terms
half3 SampleSH(half3 normalWS)
{
    // LPPV is not supported in Ligthweight Pipeline
    real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
}

// SH 顶点评估。
// 根据目标，SH 采样可以完全按顶点完成，也可以与每个顶点的 L2 项和每个像素的 L0、L1 混合。
half3 SampleSHVertex(half3 normalWS)
{
    #if defined(EVALUATE_SH_VERTEX)
    return SampleSH(normalWS);
    #elif defined(EVALUATE_SH_MIXED)
    // no max since this is only L2 contribution
    return SHEvalLinearL2(normalWS, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC);
    #endif
    
    // 完全逐像素。无需计算。
    return half3(0.0, 0.0, 0.0);
}

// SH 像素评估。
// 根据目标 SH 采样，可能会混合或完全在像素中完成。
half3 SampleSHPixel(half3 L2Term, half3 normalWS)
{
    #if defined(EVALUATE_SH_VERTEX)
    return L2Term;
    #elif defined(EVALUATE_SH_MIXED)
    half3 res = L2Term + SHEvalLinearL0L1(normalWS, unity_SHAr, unity_SHAg, unity_SHAb);
    #ifdef UNITY_COLORSPACE_GAMMA
    res = LinearToSRGB(res);
    #endif
    return max(half3(0, 0, 0), res);
    #endif
    
    // 默认: 完全按像素评估 SH
    return SampleSH(normalWS);
}

#if defined(UNITY_DOTS_INSTANCING_ENABLED)
#define LIGHTMAP_NAME unity_Lightmaps
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapsInd
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmaps
#define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV, unity_LightmapIndex.x
#else
#define LIGHTMAP_NAME unity_Lightmap
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap
#define LIGHTMAP_SAMPLE_EXTRA_ARGS staticLightmapUV
#endif

// 采样烘焙或实时光照贴图。
// AHD 占用 lightmapDir，漫反射 GI 不再走 Unity directional lightmap。
half3 SampleLightmap(float2 staticLightmapUV, float2 dynamicLightmapUV, half3 normalWS)
{
    #ifdef UNITY_LIGHTMAP_FULL_HDR
    bool encodedLightmap = false;
    #else
    bool encodedLightmap = true;
    #endif

    half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);

    // The shader library sample lightmap functions transform the lightmap uv coords to apply bias and scale.
    // However, universal pipeline already transformed those coords in vertex. We pass half4(1, 1, 0, 0) and
    // the compiler will optimize the transform away.
    half4 transformCoords = half4(1, 1, 0, 0);

    float3 diffuseLighting = 0;

    // 现在 Diffuse GI 只采样 lightmap 颜色
    // 避免 DIRLIGHTMAP_COMBINED 的逻辑串过来
    /*
    // Lightmap 和 Direction Maps
    #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
    diffuseLighting = SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
        TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME),
        LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, normalWS, encodedLightmap, decodeInstructions);
    */
    //#elif defined(LIGHTMAP_ON)
    #if defined(LIGHTMAP_ON)
    diffuseLighting = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, encodedLightmap, decodeInstructions);
    #endif

    /*
    // Direction Maps
    #if defined(DYNAMICLIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
    diffuseLighting += SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
        TEXTURE2D_ARGS(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
        dynamicLightmapUV, transformCoords, normalWS, false, decodeInstructions);
    */
    //#elif defined(DYNAMICLIGHTMAP_ON)
    #if defined(DYNAMICLIGHTMAP_ON)
    diffuseLighting += SampleSingleLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
        dynamicLightmapUV, transformCoords, false, decodeInstructions);
    #endif

    return diffuseLighting;
}

// 不支持实时 GI 的旧版 SampleLightmap。
half3 SampleLightmap(float2 staticLightmapUV, half3 normalWS)
{
    float2 dummyDynamicLightmapUV = float2(0,0);
    half3 result = SampleLightmap(staticLightmapUV, dummyDynamicLightmapUV, normalWS);
    return result;
}

// 我们从烘焙光照贴图或探针中采样 GI。
// If lightmap: sampleData.xy = lightmapUV
// If probe: sampleData.xyz = L2 SH terms
#if defined(LIGHTMAP_ON) && defined(DYNAMICLIGHTMAP_ON)
#define SAMPLE_GI(staticLmName, dynamicLmName, shName, normalWSName) SampleLightmap(staticLmName, dynamicLmName, normalWSName)
#elif defined(DYNAMICLIGHTMAP_ON)
#define SAMPLE_GI(staticLmName, dynamicLmName, shName, normalWSName) SampleLightmap(0, dynamicLmName, normalWSName)
#elif defined(LIGHTMAP_ON)
#define SAMPLE_GI(staticLmName, shName, normalWSName) SampleLightmap(staticLmName, 0, normalWSName)
#else
#define SAMPLE_GI(staticLmName, shName, normalWSName) SampleSHPixel(shName, normalWSName)
#endif


// 不同引擎的全局光照实现
#include "./GlobalIllumination_Unity.hlsl"
#include "./GlobalIllumination_UE.hlsl"

#endif // __XKNIGHT_GLOBAL_ILLUMINATION__
