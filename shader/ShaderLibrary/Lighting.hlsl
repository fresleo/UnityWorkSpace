#ifndef __XKNIGHT_LIGHTING__
#define __XKNIGHT_LIGHTING__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalParameters.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "./BRDF_XK.hlsl"
#include "./GlobalIllumination.hlsl"
#include "./RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

// lightmap 和 SH 的宏定义
#if defined(LIGHTMAP_ON)
#define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) float2 lmName : TEXCOORD##index
#define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = lightmapUV.xy * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
#define OUTPUT_SH(normalWS, OUT)
#else
#define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) half3 shName : TEXCOORD##index
#define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT)
#define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
#endif


///////////////////////////////////////////////////////////////////////////////
//                                 光照函数                                   //
///////////////////////////////////////////////////////////////////////////////
#include "./Lighting_Unity.hlsl"
#include "./Lighting_XK.hlsl"


///////////////////////////////////////////////////////////////////////////////
//                                 片元功能                                   //
///////////////////////////////////////////////////////////////////////////////

struct ExtendData
{
    half specularScaleBRDF; // 镜面高光比例（BRDF）
    half mainLightMinPerceptualRoughness; // 仅主灯直接高光使用的最小感知粗糙度
    half receiveShadowsOff; // _ReceiveShadowsOff: 不接收逐对象阴影遮罩

    float2 staticLightmapUV; // 已应用 unity_LightmapST 的 lightmap UV

    // AHD 烘焙高光局部参数
    half AHDBakedSpecularScale;
    half AHDBakedSpecularDirectionBlur;
    half AHDBakedSpecularRougheningMaxAmount;
    half AHDBakedSpecularStrengthGateMin, AHDBakedSpecularStrengthGateMax;
    half AHDBakedSpecularRougheningConfidenceMin, AHDBakedSpecularRougheningConfidenceMax;
};

#define LOCAL_AHD_BAKED_SPECULAR_SCALE(extendData) (extendData.AHDBakedSpecularScale + GLOBAL_AHD_BAKED_SPECULAR_SCALE)
#define LOCAL_AHD_BAKED_SPECULAR_DIRECTION_BLUR(extendData) (extendData.AHDBakedSpecularDirectionBlur + GLOBAL_AHD_BAKED_SPECULAR_DIRECTION_BLUR)
#define LOCAL_AHD_BAKED_SPECULAR_ROUGHENING_MAX_AMOUNT(extendData) (extendData.AHDBakedSpecularRougheningMaxAmount + GLOBAL_AHD_BAKED_SPECULAR_ROUGHENING_MAX_AMOUNT)
#define LOCAL_AHD_BAKED_SPECULAR_STRENGTH_GATE_MIN(extendData) (extendData.AHDBakedSpecularStrengthGateMin) + GLOBAL_AHD_BAKED_SPECULAR_STRENGTH_GATE_MIN
#define LOCAL_AHD_BAKED_SPECULAR_STRENGTH_GATE_MAX(extendData) (extendData.AHDBakedSpecularStrengthGateMax) + GLOBAL_AHD_BAKED_SPECULAR_STRENGTH_GATE_MAX
#define LOCAL_AHD_BAKED_SPECULAR_ROUGHENING_CONFIDENCE_MIN(extendData) (extendData.AHDBakedSpecularRougheningConfidenceMin + GLOBAL_AHD_BAKED_SPECULAR_ROUGHENING_CONFIDENCE_MIN)
#define LOCAL_AHD_BAKED_SPECULAR_ROUGHENING_CONFIDENCE_MAX(extendData) (extendData.AHDBakedSpecularRougheningConfidenceMax + GLOBAL_AHD_BAKED_SPECULAR_ROUGHENING_CONFIDENCE_MAX)

#if defined(LIGHTMAP_ON) && defined(_BAKED_SPECULARHIGHLIGHTS)
struct AHDBakedSpecularDirectionSample
{
    half3 directionWS;
    half strength;
};

half4 SampleAHDBakedSpecularDirectionMap(float2 staticLightmapUV)
{
    return SAMPLE_TEXTURE2D_LIGHTMAP(
        LIGHTMAP_INDIRECTION_NAME,
        LIGHTMAP_SAMPLER_NAME,
        LIGHTMAP_SAMPLE_EXTRA_ARGS);
}

#define AHD_BAKED_SPECULAR_DIRECTION_DECODE_MIN 0.05h

half GetAHDBakedSpecularDirectionWeight(half strength)
{
    return strength * step(AHD_BAKED_SPECULAR_DIRECTION_DECODE_MIN, strength);
}

half3 DecodeAHDBakedSpecularDirection(half4 encodedDirection)
{
    half valid = step(AHD_BAKED_SPECULAR_DIRECTION_DECODE_MIN, encodedDirection.a);
    return (encodedDirection.xyz - half3(0.5h, 0.5h, 0.5h)) * (2.0h * valid);
}

float2 GetBakedSpecularDirectionTexelSize()
{
    uint width;
    uint height;

    #if defined(UNITY_DOTS_INSTANCING_ENABLED)
    uint elements;
    LIGHTMAP_INDIRECTION_NAME.GetDimensions(width, height, elements);
    #else
    LIGHTMAP_INDIRECTION_NAME.GetDimensions(width, height);
    #endif

    return rcp (max(float2 (width, height), 1));
}

AHDBakedSpecularDirectionSample SampleAHDBakedSpecularDirection(float2 staticLightmapUV, ExtendData extendData)
{
    half4 centerSample = SampleAHDBakedSpecularDirectionMap(staticLightmapUV);
    half3 centerDirRaw = DecodeAHDBakedSpecularDirection(centerSample);
    half centerStrength = centerSample.a;

    half3 dominantDirection = centerDirRaw;
    half directionStrength = GetAHDBakedSpecularDirectionWeight(centerStrength);

    half blurTexels = LOCAL_AHD_BAKED_SPECULAR_DIRECTION_BLUR(extendData);
    UNITY_BRANCH
    if (blurTexels > 0.001h)
    {
        float2 texelOffset = GetBakedSpecularDirectionTexelSize() * blurTexels;
        half4 r = SampleAHDBakedSpecularDirectionMap(staticLightmapUV + float2(texelOffset.x, 0));
        half4 l = SampleAHDBakedSpecularDirectionMap(staticLightmapUV - float2(texelOffset.x, 0));
        half4 u = SampleAHDBakedSpecularDirectionMap(staticLightmapUV + float2(0, texelOffset.y));
        half4 d = SampleAHDBakedSpecularDirectionMap(staticLightmapUV - float2(0, texelOffset.y));

        half wC = GetAHDBakedSpecularDirectionWeight(centerStrength);
        half wR = GetAHDBakedSpecularDirectionWeight(r.a);
        half wL = GetAHDBakedSpecularDirectionWeight(l.a);
        half wU = GetAHDBakedSpecularDirectionWeight(u.a);
        half wD = GetAHDBakedSpecularDirectionWeight(d.a);
        half3 weightedDir =
            centerDirRaw * wC +
            DecodeAHDBakedSpecularDirection(r) * wR +
            DecodeAHDBakedSpecularDirection(l) * wL +
            DecodeAHDBakedSpecularDirection(u) * wU +
            DecodeAHDBakedSpecularDirection(d) * wD;
        half weightSum = wC + wR + wL + wU + wD;
        dominantDirection = weightedDir / max(weightSum, HALF_MIN);

        // strength 在小范围内取最大值，避免邻居 dip 拉低中心。
        directionStrength = max(wC, max(max(wR, wL), max(wU, wD)));
    }

    // 把低于 min 的 strength 截到 0，避免接缝中线被 GGX 锐峰放大成亮带。
    directionStrength = smoothstep(
        LOCAL_AHD_BAKED_SPECULAR_STRENGTH_GATE_MIN(extendData),
        LOCAL_AHD_BAKED_SPECULAR_STRENGTH_GATE_MAX(extendData),
        directionStrength) * directionStrength;

    AHDBakedSpecularDirectionSample result;
    result.directionWS = SafeNormalize(dominantDirection);
    result.strength = saturate(directionStrength);

    return result;
}

half3 CompressAHDBakedSpecularLuminance(half3 specularColor, half3 tintColor)
{
    half maxLuminance = _AHDBakedSpecularMaxLuminance;
    UNITY_BRANCH
    if (maxLuminance <= 0.001h)
    {
        return specularColor;
    }

    half luminance = max(Luminance(specularColor), HALF_MIN);
    half compressedLuminance = maxLuminance * luminance / (luminance + maxLuminance);
    half3 compressedColor = specularColor * (compressedLuminance / luminance);

    half tintMax = max(max(tintColor.r, tintColor.g), tintColor.b);
    half tintMin = min(min(tintColor.r, tintColor.g), tintColor.b);
    half tintChroma = saturate((tintMax - tintMin) / max(tintMax, HALF_MIN));
    UNITY_BRANCH
    if (tintChroma <= 0.001h)
    {
        return compressedColor;
    }

    half3 tint = tintColor / max(tintMax, HALF_MIN);
    half tintLuminance = max(Luminance(tint), HALF_MIN);
    half3 tintPreservedColor = compressedLuminance * tint / tintLuminance;

    // 保持色调权重独立于局部压缩量以避免出现彩色环。
    return lerp(compressedColor, tintPreservedColor, saturate(tintChroma * AHD_BAKED_SPECULAR_TINT_PRESERVE_WEIGHT));
}

half3 EvaluateAHDBakedSpecularTerm(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
    return brdfData.specular * DirectBRDFSpecular(
        brdfData,
        normalWS,
        lightDirectionWS,
        viewDirectionWS);
}

half3 CalculateBakedSpecularHighlights(BRDFData brdfData, InputData inputData, ExtendData extendData, AmbientOcclusionFactor aoFactor)
{
    AHDBakedSpecularDirectionSample directionMap = SampleAHDBakedSpecularDirection(extendData.staticLightmapUV, extendData);
    UNITY_BRANCH
    if (directionMap.strength <= 0.0001h)
    {
        return half3(0.0h, 0.0h, 0.0h);
    }

    BRDFData specularBRDF = brdfData;
    specularBRDF.specular *= extendData.specularScaleBRDF * GLOBAL_SPECULAR_SCALE;

    // 低可信区域人为提升[感知粗糙度]，让 GGX 不至于把"不太可信的方向"放大成尖锐亮带。
    half rougheningFromConfidence = 1 - smoothstep(
        LOCAL_AHD_BAKED_SPECULAR_ROUGHENING_CONFIDENCE_MIN(extendData),
        LOCAL_AHD_BAKED_SPECULAR_ROUGHENING_CONFIDENCE_MAX(extendData),
        directionMap.strength);
    half rougheningAmount = rougheningFromConfidence * LOCAL_AHD_BAKED_SPECULAR_ROUGHENING_MAX_AMOUNT(extendData);
    half softenedPerceptualRoughness = lerp(specularBRDF.perceptualRoughness, 1, rougheningAmount);
    softenedPerceptualRoughness = max(softenedPerceptualRoughness, specularBRDF.perceptualRoughness);
    specularBRDF.perceptualRoughness = softenedPerceptualRoughness;

    RecalculationBRDFDataRoughness(specularBRDF);

    half specularMask = directionMap.strength * saturate(dot(inputData.normalWS, directionMap.directionWS));
    half3 specularTerm = EvaluateAHDBakedSpecularTerm(
        specularBRDF,
        inputData.normalWS,
        directionMap.directionWS,
        inputData.viewDirectionWS) * specularMask;

    half3 bakedSpecular = specularTerm * inputData.bakedGI * LOCAL_AHD_BAKED_SPECULAR_SCALE(extendData) * aoFactor.indirectAmbientOcclusion;
    return CompressAHDBakedSpecularLuminance(bakedSpecular, inputData.bakedGI);
}
#endif // AHD 烘焙高光逻辑结束


////////////////////////////////////////////////////////////////////////////////
/// PBR lighting...
////////////////////////////////////////////////////////////////////////////////
half4 FragmentPBR(InputData inputData, SurfaceData surfaceData, ExtendData extendData)
{
    bool specularHighlightsOff = false; // 镜面高光永远不会关

    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    #ifdef DEBUG_DISPLAY
    half4 debugColor;
    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return debugColor;
    }
    #endif

    // 清漆的 BRDF 数据，内部有宏控制
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);

    // shadowMask
    half4 shadowMask = CalculateShadowMask(inputData);
    // ao
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    // mesh 渲染器的层
    uint meshRenderingLayers = GetMeshRenderingLayer();
    // 主灯
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    half mainLightSpecularScale = lerp(extendData.specularScaleBRDF, 0, saturate(DISABLE_MAIN_LIGHT_REALTIME_SPECULAR));

    // 注意：我们在这里不将 AO 应用于 GI，因为它是在下面的光照计算中完成的......
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    // 创建主灯的抽象数据
    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    // 全局光照，就是间接光照
    // lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
    //                                           inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
    //                                           inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
    lightingData.giColor = GlobalIllumination_UE(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                                 inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                                 inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

    // 直接光照
    // 主灯
    #ifdef _LIGHT_LAYERS
    // 灯光层匹配逻辑
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    #endif
    {
        // 主灯光照
        // lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
        //                                                       mainLight,
        //                                                       inputData.normalWS, inputData.viewDirectionWS,
        //                                                       surfaceData.clearCoatMask, specularHighlightsOff);
        lightingData.mainLightColor = LightingPhysicallyBased_XK(brdfData, brdfDataClearCoat,
                                                                 mainLight,
                                                                 inputData.normalWS, inputData.viewDirectionWS,
                                                                 surfaceData.clearCoatMask, specularHighlightsOff,
                                                                 mainLightSpecularScale,
                                                                 extendData.mainLightMinPerceptualRoughness);
    }

    // 附加灯
    #ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();

    // Forward+ 的情况
    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

    #ifdef _LIGHT_LAYERS
    // 灯光层匹配逻辑
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
    {
        // lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
        //                                                               inputData.normalWS, inputData.viewDirectionWS,
        //                                                               surfaceData.clearCoatMask, specularHighlightsOff);
        lightingData.additionalLightsColor += LightingPhysicallyBased_XK(brdfData, brdfDataClearCoat, light,
                                                                         inputData.normalWS, inputData.viewDirectionWS,
                                                                         surfaceData.clearCoatMask, specularHighlightsOff,
                                                                         extendData.specularScaleBRDF,
                                                                         0);
    }
    }
    #endif // USE_FORWARD_PLUS

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

    #ifdef _LIGHT_LAYERS
    // 灯光层匹配逻辑
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
    {
        // lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
        //                                                               inputData.normalWS, inputData.viewDirectionWS,
        //                                                               surfaceData.clearCoatMask, specularHighlightsOff);
        lightingData.additionalLightsColor += LightingPhysicallyBased_XK(brdfData, brdfDataClearCoat, light,
                                                                         inputData.normalWS, inputData.viewDirectionWS,
                                                                         surfaceData.clearCoatMask, specularHighlightsOff,
                                                                         extendData.specularScaleBRDF,
                                                                         0);
    }
    LIGHT_LOOP_END
    #endif // _ADDITIONAL_LIGHTS

    // TODO: 以前遗留的 trick 代码
    /*
    // trick，可以在阴影区通过Lightmap方向图来产生高光，来避免添加额外光源
    #ifdef _BAKED_SPECULARHIGHLIGHTS
    // Bake Speuclar
    float3 lmDir = unity_LightmapInd.Sample(samplerunity_Lightmap, lightmapUV) * 2.0f - 1.0f;
    mainLight.direction = lmDir;
    mainLight.color = inputData.bakedGI;
    mainLight.shadowAttenuation = 1.0;
    #endif
    // 理论上来说，bakeGI应该是直接从lightmap读出来的信息为正确，可是会多一次读取，此处选择了经过half lambert的bakedGI，效果相差并不多
    half3 bakeSpecular = DirectBRDFSpecular(brdfData, inputData.normalWS, normalize(lmDir), inputData.viewDirectionWS) * brdfData.specular * inputData.bakedGI;
    color += bakeSpecular * mainLight.shadowAttenuation;
    */

    #if defined(LIGHTMAP_ON) && defined(_BAKED_SPECULARHIGHLIGHTS)
    lightingData.additionalLightsColor += CalculateBakedSpecularHighlights(brdfData, inputData, extendData, aoFactor);
    #endif

    // 最终颜色
    half4 color = 0;
    // 如果有需要，限制它不超过 half
    #if REAL_IS_HALF
    color = min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
    #else
    color = CalculateFinalColor(lightingData, surfaceData.alpha);
    #endif

    // 室内将逐对象阴影当做mask绘制
    half perObjectShadowMask = PerObjectRealtimeShadow(inputData.positionWS);
    half maskFactor = lerp(1, perObjectShadowMask, saturate(extendData.receiveShadowsOff));
    color *= maskFactor;

    return color;
}

// 晶体 PBR
half4 CrystalPBR(InputData inputData, SurfaceData surfaceData, ExtendData extendData,
                 TEXTURECUBE_PARAM(cubemap, sampler_cubemap), float4 cubemap_HDR, bool acceptDirectLight)
{
    bool specularHighlightsOff = false; // 镜面高光永远不会关

    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);

    #ifdef DEBUG_DISPLAY
    half4 debugColor;
    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return debugColor;
    }
    #endif

    // 清漆的 BRDF 数据，内部有宏控制
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);

    // shadowMask
    half4 shadowMask = CalculateShadowMask(inputData);
    // ao
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    // mesh 渲染器的层
    uint meshRenderingLayers = GetMeshRenderingLayer();
    // 主灯
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    half mainLightSpecularScale = lerp(extendData.specularScaleBRDF, 0, saturate(DISABLE_MAIN_LIGHT_REALTIME_SPECULAR));

    // 注意：我们在这里不将 AO 应用于 GI，因为它是在下面的光照计算中完成的......
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    // 创建主灯的抽象数据
    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    // 全局光照，就是间接光
    lightingData.giColor = GlobalIllumination_CustomCubeMap(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                                            inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                                            inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV,
                                                            TEXTURECUBE_ARGS(cubemap, sampler_cubemap), cubemap_HDR);

    // 根据开关决定是否接收直接光照
    UNITY_BRANCH
    if (acceptDirectLight)
    {
        // 主灯
        #ifdef _LIGHT_LAYERS
        // 灯光层匹配逻辑
        if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
        #endif
        {
            // 主灯光照
            // lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
            //                                                       mainLight,
            //                                                       inputData.normalWS, inputData.viewDirectionWS,
            //                                                       surfaceData.clearCoatMask, specularHighlightsOff);
            lightingData.mainLightColor = LightingPhysicallyBased_XK(brdfData, brdfDataClearCoat,
                                                                     mainLight,
                                                                     inputData.normalWS, inputData.viewDirectionWS,
                                                                     surfaceData.clearCoatMask, specularHighlightsOff,
                                                                     mainLightSpecularScale,
                                                                     extendData.mainLightMinPerceptualRoughness);
        }

        // 附加灯
        #ifdef _ADDITIONAL_LIGHTS
        uint pixelLightCount = GetAdditionalLightsCount();

        // Forward+ 的情况
        #if USE_FORWARD_PLUS
        for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
        {
            FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

        #ifdef _LIGHT_LAYERS
        // 灯光层匹配逻辑
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
        {
            // lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
            //                                                               inputData.normalWS, inputData.viewDirectionWS,
            //                                                               surfaceData.clearCoatMask, specularHighlightsOff);
            lightingData.additionalLightsColor += LightingPhysicallyBased_XK(brdfData, brdfDataClearCoat, light,
                                                                             inputData.normalWS, inputData.viewDirectionWS,
                                                                             surfaceData.clearCoatMask, specularHighlightsOff,
                                                                             extendData.specularScaleBRDF,
                                                                             0);
        }
        }
        #endif // USE_FORWARD_PLUS

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

        #ifdef _LIGHT_LAYERS
        // 灯光层匹配逻辑
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
        {
            // lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
            //                                                               inputData.normalWS, inputData.viewDirectionWS,
            //                                                               surfaceData.clearCoatMask, specularHighlightsOff);
            lightingData.additionalLightsColor += LightingPhysicallyBased_XK(brdfData, brdfDataClearCoat, light,
                                                                             inputData.normalWS, inputData.viewDirectionWS,
                                                                             surfaceData.clearCoatMask, specularHighlightsOff,
                                                                             extendData.specularScaleBRDF,
                                                                             0);
        }
        LIGHT_LOOP_END
        #endif // _ADDITIONAL_LIGHTS
    }

    // 最终颜色
    half4 color = 0;
    // 如果有需要，限制它不超过 half
    #if REAL_IS_HALF
    color = min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
    #else
    color = CalculateFinalColor(lightingData, surfaceData.alpha);
    #endif

    return color;
}

half4 FragmentPBR(InputData inputData, ExtendData extendData,
                  half3 albedo, half metallic, half smoothness, half occlusion, half3 emission, half alpha)
{
    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.specular = half3(0, 0, 0);
    surfaceData.metallic = metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = half3(0, 0, 1);
    surfaceData.emission = emission;
    surfaceData.occlusion = occlusion;
    surfaceData.alpha = alpha;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;

    return FragmentPBR(inputData, surfaceData, extendData);
}

half4 FragmentPBR(InputData inputData, ExtendData extendData,
                  half3 albedo, half3 specular, half metallic, half smoothness, half occlusion, half3 emission, half alpha)
{
    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.specular = specular;
    surfaceData.metallic = metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = half3(0, 0, 1);
    surfaceData.emission = emission;
    surfaceData.occlusion = occlusion;
    surfaceData.alpha = alpha;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;

    return FragmentPBR(inputData, surfaceData, extendData);
}

////////////////////////////////////////////////////////////////////////////////
/// Phong lighting...
////////////////////////////////////////////////////////////////////////////////
half4 FragmentBlinnPhong(InputData inputData, SurfaceData surfaceData)
{
    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    inputData.bakedGI *= surfaceData.albedo;

    LightingData lightingData = CreateLightingData(inputData, surfaceData);
    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    #endif
    {
        lightingData.mainLightColor += CalculateBlinnPhong(mainLight, inputData, surfaceData);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
    {
        lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
    }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    #ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
    #endif
    {
        lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
    }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
    #endif

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}

half4 FragmentBlinnPhong(InputData inputData, half3 diffuse, half4 specularGloss, half smoothness, half3 emission, half alpha, half3 normalTS)
{
    SurfaceData surfaceData;

    surfaceData.albedo = diffuse;
    surfaceData.alpha = alpha;
    surfaceData.emission = emission;
    surfaceData.metallic = 0;
    surfaceData.occlusion = 1;
    surfaceData.smoothness = smoothness;
    surfaceData.specular = specularGloss.rgb;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
    surfaceData.normalTS = normalTS;

    return FragmentBlinnPhong(inputData, surfaceData);
}


// TODO: 只计算兰伯特漫反射的极简单版本，当前在旧的 Terrain.shader 中还有使用，后面看情况是否还需要保留它

half3 AdditionLightLambert(float3 positionWS, half3 normalWS)
{
    half3 color = half3(0, 0, 0);

    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, positionWS);
        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        color += LightingLambert(attenuatedLightColor, light.direction, normalWS);
    }

    return color;
}

half3 FragmentDiffuse(half4 shadowMask, float4 shadowCoord, float3 positionWS, half3 normalWS, half3 bakedGI, half3 diffuse)
{
    // 主灯
    Light mainLight = GetMainLight(shadowCoord, positionWS, shadowMask);
    // 衰减的光颜色
    half3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
    // 漫反射颜色
    half3 diffuseColor = bakedGI + LightingLambert(attenuatedLightColor, mainLight.direction, normalWS);

    #ifdef _ADDITIONAL_LIGHTS
    diffuseColor += AdditionLightLambert(positionWS, normalWS);
    #endif

    half3 finalColor = diffuseColor * diffuse;
    return finalColor;
}

// 过时的方法，但是仍然有地方在用

#define REFLECTION_CAPTURE_ROUGHEST_MIP 1
#define REFLECTION_CAPTURE_ROUGHNESS_MIP_SCALE 1.2

// 在给定粗糙度的情况下，计算反射捕获立方体贴图的绝对 mip
half ComputeReflectionCaptureMipFromRoughness(half Roughness, half CubemapMaxMip)
{
    // 将粗糙度映射到 mip 级别的启发式
    // 这是以某种方式完成的，即无论纹理中有多少个 mip，某个 mip 级别将始终具有相同的粗糙度
    // 在立方体贴图中使用更多的 mip 只是支持更清晰的反射
    half LevelFrom1x1 = REFLECTION_CAPTURE_ROUGHEST_MIP - REFLECTION_CAPTURE_ROUGHNESS_MIP_SCALE * log2(max(Roughness, 0.001));
    return CubemapMaxMip - 1 - LevelFrom1x1;
}

#endif // __XKNIGHT_LIGHTING__
