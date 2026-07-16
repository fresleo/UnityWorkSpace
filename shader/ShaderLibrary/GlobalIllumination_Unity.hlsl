#ifndef __XKNIGHT_GLOBAL_ILLUMINATION_UNITY__
#define __XKNIGHT_GLOBAL_ILLUMINATION_UNITY__

//<MLS_PREPARED>
float3 _CustomBoxMaxOffset;
float3 _CustomBoxMinOffset;
float3 _CameraOffset;

// Box 投影立方体贴图方向
half3 BoxProjectedCubemapDirection(half3 reflectionWS, float3 positionWS, float4 cubemapPositionWS, float4 boxMin, float4 boxMax)
{
    // Is this probe using box projection?
    if (cubemapPositionWS.w > 0.0f)
    {
        boxMin.xyz+=_CustomBoxMinOffset;
        boxMax.xyz+=_CustomBoxMaxOffset;
        float3 boxMinMax = (reflectionWS > 0.0f) ? boxMax.xyz : boxMin.xyz;
        half3 rbMinMax = half3(boxMinMax - positionWS) / reflectionWS;

        half fa = half(min(min(rbMinMax.x, rbMinMax.y), rbMinMax.z));

        half3 worldPos = half3(positionWS - cubemapPositionWS.xyz+_CameraOffset);

        half3 result = worldPos + reflectionWS * fa;
        return result;
    }
    else
    {
        return reflectionWS;
    }
}

// 计算探针权重
float CalculateProbeWeight(float3 positionWS, float4 probeBoxMin, float4 probeBoxMax)
{
    float blendDistance = probeBoxMax.w;
    float3 weightDir = min(positionWS - probeBoxMin.xyz, probeBoxMax.xyz - positionWS) / blendDistance;
    return saturate(min(weightDir.x, min(weightDir.y, weightDir.z)));
}

// 计算探针体积模的平方根
half CalculateProbeVolumeSqrMagnitude(float4 probeBoxMin, float4 probeBoxMax)
{
    half3 maxToMin = half3(probeBoxMax.xyz - probeBoxMin.xyz);
    return dot(maxToMin, maxToMin);
}

// 从反射探针计算出的辐射率
half3 CalculateIrradianceFromReflectionProbes(half3 reflectVector, float3 positionWS, half perceptualRoughness, float2 normalizedScreenSpaceUV)
{
    half3 irradiance = half3(0.0h, 0.0h, 0.0h);
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    
    #if USE_FORWARD_PLUS
    float totalWeight = 0.0f;
    uint probeIndex;
    ClusterIterator it = ClusterInit(normalizedScreenSpaceUV, positionWS, 1);
    [loop] while (ClusterNext(it, probeIndex) && totalWeight < 0.99f)
    {
        probeIndex -= URP_FP_PROBES_BEGIN;

        float weight = CalculateProbeWeight(positionWS, urp_ReflProbes_BoxMin[probeIndex], urp_ReflProbes_BoxMax[probeIndex]);
        weight = min(weight, 1.0f - totalWeight);

        half3 sampleVector = reflectVector;
        #ifdef _REFLECTION_PROBE_BOX_PROJECTION
        sampleVector = BoxProjectedCubemapDirection(reflectVector, positionWS, urp_ReflProbes_ProbePosition[probeIndex], urp_ReflProbes_BoxMin[probeIndex], urp_ReflProbes_BoxMax[probeIndex]);
        #endif // _REFLECTION_PROBE_BOX_PROJECTION

        uint maxMip = (uint)abs(urp_ReflProbes_ProbePosition[probeIndex].w) - 1;
        half probeMip = min(mip, maxMip);
        float2 uv = saturate(PackNormalOctQuadEncode(sampleVector) * 0.5 + 0.5);

        float mip0 = floor(probeMip);
        float mip1 = mip0 + 1;
        float mipBlend = probeMip - mip0;
        float4 scaleOffset0 = urp_ReflProbes_MipScaleOffset[probeIndex * 7 + (uint)mip0];
        float4 scaleOffset1 = urp_ReflProbes_MipScaleOffset[probeIndex * 7 + (uint)mip1];

        half3 irradiance0 = half4(SAMPLE_TEXTURE2D_LOD(urp_ReflProbes_Atlas, samplerurp_ReflProbes_Atlas, uv * scaleOffset0.xy + scaleOffset0.zw, 0.0)).rgb;
        half3 irradiance1 = half4(SAMPLE_TEXTURE2D_LOD(urp_ReflProbes_Atlas, samplerurp_ReflProbes_Atlas, uv * scaleOffset1.xy + scaleOffset1.zw, 0.0)).rgb;
        irradiance += weight * lerp(irradiance0, irradiance1, mipBlend);
        totalWeight += weight;
    }
    
    #else // 没用 Forward+ 的情况
    
    half probe0Volume = CalculateProbeVolumeSqrMagnitude(unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    half probe1Volume = CalculateProbeVolumeSqrMagnitude(unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

    half volumeDiff = probe0Volume - probe1Volume;
    float importanceSign = unity_SpecCube1_BoxMin.w;

    // A probe is dominant if its importance is higher
    // Or have equal importance but smaller volume
    bool probe0Dominant = importanceSign > 0.0f || (importanceSign == 0.0f && volumeDiff < -0.0001h);
    bool probe1Dominant = importanceSign < 0.0f || (importanceSign == 0.0f && volumeDiff > 0.0001h);

    float desiredWeightProbe0 = CalculateProbeWeight(positionWS, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    float desiredWeightProbe1 = CalculateProbeWeight(positionWS, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);

    // Subject the probes weight if the other probe is dominant
    float weightProbe0 = probe1Dominant ? min(desiredWeightProbe0, 1.0f - desiredWeightProbe1) : desiredWeightProbe0;
    float weightProbe1 = probe0Dominant ? min(desiredWeightProbe1, 1.0f - desiredWeightProbe0) : desiredWeightProbe1;

    float totalWeight = weightProbe0 + weightProbe1;

    // If either probe 0 or probe 1 is dominant the sum of weights is guaranteed to be 1.
    // If neither is dominant this is not guaranteed - only normalize weights if totalweight exceeds 1.
    weightProbe0 /= max(totalWeight, 1.0f);
    weightProbe1 /= max(totalWeight, 1.0f);

    // 对第一个反射探针进行采样
    if (weightProbe0 > 0.01f)
    {
        half3 reflectVector0 = reflectVector;
        #ifdef _REFLECTION_PROBE_BOX_PROJECTION
        reflectVector0 = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
        #endif // _REFLECTION_PROBE_BOX_PROJECTION

        // XK: MLS 改造
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector0, mip));
        
        // half4 leftCol = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector0, mip));
        // half4 rightCol = half4(BlendTwoCubeTextures(0, reflectVector0, mip));
        // half4 encodedIrradiance = lerp(leftCol, rightCol, _MLS_ENABLE_REFLECTIONS_BLENDING);

        irradiance += weightProbe0 * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    }

    // 对第二个反射探针进行采样
    if (weightProbe1 > 0.01f)
    {
        half3 reflectVector1 = reflectVector;
        #ifdef _REFLECTION_PROBE_BOX_PROJECTION
        reflectVector1 = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);
        #endif // _REFLECTION_PROBE_BOX_PROJECTION

        // XK: MLS 改造
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube1, samplerunity_SpecCube1, reflectVector1, mip));

        // half4 leftCol = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube1, samplerunity_SpecCube1, reflectVector1, mip));
        // half4 rightCol = half4(BlendTwoCubeTextures(1, reflectVector1, mip));
        // half4 encodedIrradiance = lerp(leftCol, rightCol, _MLS_ENABLE_REFLECTIONS_BLENDING);

        irradiance += weightProbe1 * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube1_HDR);
    }
    #endif // 区分 Forward+ 的情况
    
    // 使用任何剩余权重混合到环境反射立方体贴图
    if (totalWeight < 0.99f)
    {
        // XK: MLS 改造
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, reflectVector, mip));

        // half4 leftCol = half4(SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, reflectVector, mip));
        // half4 rightCol = half4(BlendTwoCubeTextures(2, reflectVector, mip));
        // half4 encodedIrradiance = lerp(leftCol, rightCol, _MLS_ENABLE_REFLECTIONS_BLENDING);

        irradiance += (1.0f - totalWeight) * DecodeHDREnvironment(encodedIrradiance, _GlossyEnvironmentCubeMap_HDR);
    }

    return irradiance;
}

#if !USE_FORWARD_PLUS
// 从反射探针计算出的辐射率
half3 CalculateIrradianceFromReflectionProbes(half3 reflectVector, float3 positionWS, half perceptualRoughness)
{
    return CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness, float2(0.0f, 0.0f));
}
#endif


// 光泽环境反射
half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion, float2 normalizedScreenSpaceUV)
{
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
    half3 irradiance;

        #if defined(_REFLECTION_PROBE_BLENDING) || USE_FORWARD_PLUS
    irradiance = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness, normalizedScreenSpaceUV);
        #else
            #ifdef _REFLECTION_PROBE_BOX_PROJECTION
    reflectVector = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
            #endif // _REFLECTION_PROBE_BOX_PROJECTION
    
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);

    // XK: MLS 改造
    half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));

    // half4 leftCol = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));
    // half4 rightCol = BlendTwoCubeTextures(0, reflectVector, mip);
    // half4 encodedIrradiance = lerp(leftCol, rightCol, _MLS_ENABLE_REFLECTIONS_BLENDING);

    irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
        #endif // _REFLECTION_PROBE_BLENDING
    
    return irradiance * occlusion;
    #else
    return _GlossyEnvironmentColor.rgb * occlusion;
    #endif // _ENVIRONMENTREFLECTIONS_OFF
}

#if !USE_FORWARD_PLUS
// 光泽环境反射
half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion)
{
    return GlossyEnvironmentReflection(reflectVector, positionWS, perceptualRoughness, occlusion, float2(0.0f, 0.0f));
}
#endif

// 光泽环境反射
half3 GlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion)
{
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
    half3 irradiance;
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);

    // XK: MLS 改造
    half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));

    // half4 leftCol = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));
    // half4 rightCol = BlendTwoCubeTextures(0, reflectVector, mip);
    // half4 encodedIrradiance = lerp(leftCol, rightCol, _MLS_ENABLE_REFLECTIONS_BLENDING);

    irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);

    return irradiance * occlusion;
    #else
    return _GlossyEnvironmentColor.rgb * occlusion;
    #endif // _ENVIRONMENTREFLECTIONS_OFF
}


// 从光照贴图中减去直接主灯
half3 SubtractDirectMainLightFromLightmap(Light mainLight, half3 normalWS, half3 bakedGI)
{
    // Let's try to make realtime shadows work on a surface, which already contains
    // baked lighting and shadowing from the main sun light.
    // Summary:
    // 1) Calculate possible value in the shadow by subtracting estimated light contribution from the places occluded by realtime shadow:
    //      a) preserves other baked lights and light bounces
    //      b) eliminates shadows on the geometry facing away from the light
    // 2) Clamp against user defined ShadowColor.
    // 3) Pick original lightmap value, if it is the darkest one.


    // 1) Gives good estimate of illumination as if light would've been shadowed during the bake.
    // We only subtract the main direction light. This is accounted in the contribution term below.
    half shadowStrength = GetMainLightShadowStrength();
    half contributionTerm = saturate(dot(mainLight.direction, normalWS));
    half3 lambert = mainLight.color * contributionTerm;
    half3 estimatedLightContributionMaskedByInverseOfShadow = lambert * (1.0 - mainLight.shadowAttenuation);
    half3 subtractedLightmap = bakedGI - estimatedLightContributionMaskedByInverseOfShadow;

    // 2) Allows user to define overall ambient of the scene and control situation when realtime shadow becomes too dark.
    half3 realtimeShadow = max(subtractedLightmap, _SubtractiveShadowColor.xyz);
    realtimeShadow = lerp(bakedGI, realtimeShadow, shadowStrength);

    // 3) Pick darkest color
    return min(bakedGI, realtimeShadow);
}


// 全局照明
half3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half NoV = saturate(dot(normalWS, viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);

    half3 indirectDiffuse = bakedGI;
    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, positionWS, brdfData.perceptualRoughness, 1.0h, normalizedScreenSpaceUV);

    half3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);

    if (IsOnlyAOLightingFeatureEnabled())
    {
        color = half3(1,1,1); // "Base white" for AO debug lighting mode
    }

    #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half3 coatIndirectSpecular = GlossyEnvironmentReflection(reflectVector, positionWS, brdfDataClearCoat.perceptualRoughness, 1.0h, normalizedScreenSpaceUV);
    // TODO: "grazing term" causes problems on full roughness
    half3 coatColor = EnvironmentBRDFClearCoat(brdfDataClearCoat, clearCoatMask, coatIndirectSpecular, fresnelTerm);

    // Blend with base layer using khronos glTF recommended way using NoV
    // Smooth surface & "ambiguous" lighting
    // NOTE: fresnelTerm (above) is pow4 instead of pow5, but should be ok as blend weight.
    half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * fresnelTerm;
    return (color * (1.0 - coatFresnel * clearCoatMask) + coatColor) * occlusion;
    #else
    return color * occlusion;
    #endif
}

#if !USE_FORWARD_PLUS
// 全局照明
half3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS)
{
    return GlobalIllumination(brdfData, brdfDataClearCoat, clearCoatMask, bakedGI, occlusion, positionWS, normalWS, viewDirectionWS, float2(0.0f, 0.0f));
}
#endif

// 为了提供兼容性的 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
// 全局照明
half3 GlobalIllumination(BRDFData brdfData, half3 bakedGI, half occlusion, float3 positionWS, half3 normalWS, half3 viewDirectionWS)
{
    const BRDFData noClearCoat = (BRDFData)0;
    return GlobalIllumination(brdfData, noClearCoat, 0.0, bakedGI, occlusion, positionWS, normalWS, viewDirectionWS, 0);
}

// 全局照明
half3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion,
    half3 normalWS, half3 viewDirectionWS)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half NoV = saturate(dot(normalWS, viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);

    half3 indirectDiffuse = bakedGI;
    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, brdfData.perceptualRoughness, half(1.0));

    half3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);

    #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half3 coatIndirectSpecular = GlossyEnvironmentReflection(reflectVector, brdfDataClearCoat.perceptualRoughness, half(1.0));
    // TODO: "grazing term" causes problems on full roughness
    half3 coatColor = EnvironmentBRDFClearCoat(brdfDataClearCoat, clearCoatMask, coatIndirectSpecular, fresnelTerm);

    // Blend with base layer using khronos glTF recommended way using NoV
    // Smooth surface & "ambiguous" lighting
    // NOTE: fresnelTerm (above) is pow4 instead of pow5, but should be ok as blend weight.
    half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * fresnelTerm;
    return (color * (1.0 - coatFresnel * clearCoatMask) + coatColor) * occlusion;
    #else
    return color * occlusion;
    #endif
}

// 全局照明
half3 GlobalIllumination(BRDFData brdfData, half3 bakedGI, half occlusion, half3 normalWS, half3 viewDirectionWS)
{
    const BRDFData noClearCoat = (BRDFData)0;
    return GlobalIllumination(brdfData, noClearCoat, 0.0, bakedGI, occlusion, normalWS, viewDirectionWS);
}

// 混合实时和烘焙的 GI
void MixRealtimeAndBakedGI(inout Light light, half3 normalWS, inout half3 bakedGI)
{
    #if defined(LIGHTMAP_ON) && defined(_MIXED_LIGHTING_SUBTRACTIVE)
    bakedGI = SubtractDirectMainLightFromLightmap(light, normalWS, bakedGI);
    #endif
}

// 向后兼容性
// 混合实时和烘焙的 GI
void MixRealtimeAndBakedGI(inout Light light, half3 normalWS, inout half3 bakedGI, half4 shadowMask)
{
    MixRealtimeAndBakedGI(light, normalWS, bakedGI);
}

// 混合实时和烘焙的 GI
void MixRealtimeAndBakedGI(inout Light light, half3 normalWS, inout half3 bakedGI, AmbientOcclusionFactor aoFactor)
{
    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION))
    {
        bakedGI *= aoFactor.indirectAmbientOcclusion;
    }

    MixRealtimeAndBakedGI(light, normalWS, bakedGI);
}

#endif // __XKNIGHT_GLOBAL_ILLUMINATION_UNITY__
