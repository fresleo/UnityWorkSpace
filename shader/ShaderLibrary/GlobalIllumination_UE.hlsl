// 之前移植的 UE 版本全局光照代码
#ifndef __XKNIGHT_GLOBAL_ILLUMINATION_UE__
#define __XKNIGHT_GLOBAL_ILLUMINATION_UE__

//<MLS_PREPARED>
TEXTURE2D(_PlanarReflectionTextureV2);
TEXTURE2D(_PlanarReflectionTextureV3);

half2 EnvBRDFApproxLazarov(half Roughness, half NoV)
{
    // Lazarov 2013, 《在使命召唤：黑色行动 II》 中更加物理
    // 以适应我们的 G 项
    const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
    const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
    half4 r = Roughness * c0 + c1;
    half a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
    half2 AB = half2(-1.04, 1.04) * a004 + r.zw;
    return AB;
}

half3 EnvBRDFApprox(half3 SpecularColor, half Roughness, half NoV)
{
    half2 AB = EnvBRDFApproxLazarov(Roughness, NoV);
    
    // 任何小于 2% 的东西在物理上都是不可能的，而是被认为是影子
    // 注意：这是 'specular' 显示标志工作所必需的，因为它使用的 SpecularColor 为 0
    half F90 = saturate(half(50.0) * SpecularColor.g);

    return SpecularColor * AB.x + F90 * AB.y;
}

// 环境 BRDF - UE
half3 EnvironmentBRDF_UE(BRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half3 normalWS, half3 viewDirectionWS)
{
    half3 c = indirectDiffuse * brdfData.diffuse;
    c += indirectSpecular * EnvBRDFApprox(brdfData.specular, brdfData.perceptualRoughness, saturate(dot(normalWS, viewDirectionWS)));
    return c;
}

// 混合清漆
half3 MixClearCoat(half3 color,
    BRDFData brdfDataClearCoat, float clearCoatMask,
    half occlusion, float3 positionWS, float2 normalizedScreenSpaceUV,
    half3 reflectVector, half fresnelTerm)
{
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


// 光泽环境反射 - UE
half3 GlossyEnvironmentReflection_CustomSpecCube0(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion, float2 normalizedScreenSpaceUV,
    TEXTURECUBE_PARAM(customSpecCube0, sample_customSpecCube0), half4 customSpecCube0_HDR)
{
    #if !defined( _ENVIRONMENTREFLECTIONS_OFF ) // 环境反射 - 开 - 当前这个必开
    half3 irradiance;
    
        #if defined(_REFLECTION_PROBE_BLENDING) || USE_FORWARD_PLUS
    irradiance = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness, normalizedScreenSpaceUV);
        #else
    
    // 这个方法当前应该是宝石材质在用，它会使用自定义的 Cubemap，和引擎的 SpecCube0 是冲突的
    //        #ifdef _REFLECTION_PROBE_BOX_PROJECTION
    //reflectVector = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    //        #endif // _REFLECTION_PROBE_BOX_PROJECTION

    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);

    // XK: MLS 改造
    half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(customSpecCube0, sample_customSpecCube0, reflectVector, mip));

    // half4 leftCol = half4(SAMPLE_TEXTURECUBE_LOD(customSpecCube0, sample_customSpecCube0, reflectVector, mip));
    // half4 rightCol = BlendTwoCubeTextures(0, reflectVector, mip);
    // half4 encodedIrradiance = lerp(leftCol, rightCol, _MLS_ENABLE_REFLECTIONS_BLENDING);
    
    irradiance = DecodeHDREnvironment(encodedIrradiance, customSpecCube0_HDR);
        #endif // _REFLECTION_PROBE_BLENDING

    return irradiance * occlusion;
    #else // 环境反射 - 关
    return _GlossyEnvironmentColor.rgb * occlusion;
    #endif //_ENVIRONMENTREFLECTIONS_OFF 
}



// 全局照明 - UE
// 角色的 GI
half3 GlobalIllumination_UE(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV,
    half indirectDiffuseIntensity, half indirectSpecularIntensity)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half NoV = saturate(dot(normalWS, viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);

    half3 indirectDiffuse = bakedGI;
    indirectDiffuse *= indirectDiffuseIntensity;
    #ifndef _PlANARREFLECTION
        half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, positionWS, brdfData.perceptualRoughness, 1.0h, normalizedScreenSpaceUV);
    #else
        #ifndef _PlANARREFLECTION_FLIPX
            half3 indirectSpecular =SAMPLE_TEXTURE2D(_PlanarReflectionTextureV2,sampler_LinearRepeat,normalizedScreenSpaceUV);
        #else
            half3 indirectSpecular =SAMPLE_TEXTURE2D(_PlanarReflectionTextureV3,sampler_LinearRepeat,normalizedScreenSpaceUV);
       #endif
    
    #endif
    
    
    indirectSpecular *= indirectSpecularIntensity;
    
    // Unity
    // half3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);
    // UE
    half3 color = EnvironmentBRDF_UE(brdfData, indirectDiffuse, indirectSpecular, normalWS, viewDirectionWS);

    if (IsOnlyAOLightingFeatureEnabled())
    {
        color = half3(1, 1, 1); // "Base white" 用于 AO 调试照明模式
    }

    // 混合清漆
    color = MixClearCoat(color,
        brdfDataClearCoat, clearCoatMask,
        occlusion, positionWS, normalizedScreenSpaceUV,
        reflectVector, fresnelTerm);
    
    return color;
}


// 场景中物体的 GI
half3 GlobalIllumination_UE(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV,
    half environmentIntensity)
{
    return GlobalIllumination_UE(
        brdfData, brdfDataClearCoat, clearCoatMask,
        bakedGI, occlusion, positionWS,
        normalWS, viewDirectionWS, normalizedScreenSpaceUV,
        GLOBAL_BAKE_GI_SCALE, environmentIntensity);
}

// 场景中物体的 GI
half3 GlobalIllumination_UE(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV)
{
    return GlobalIllumination_UE(
        brdfData, brdfDataClearCoat, clearCoatMask,
        bakedGI, occlusion, positionWS,
        normalWS, viewDirectionWS, normalizedScreenSpaceUV,
        1.0h);
}

// 全局照明 - 自定义 Cubemap
half3 GlobalIllumination_CustomCubeMap(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV,
    TEXTURECUBE_PARAM(customSpecCube0, sample_customSpecCube0), real4 customSpecCube0_HDR,
    half environmentIntensity = 1.0)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half NoV = saturate(dot(normalWS, viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);

    half3 indirectDiffuse = bakedGI;
    indirectDiffuse *= GLOBAL_BAKE_GI_SCALE;
    half3 indirectSpecular = GlossyEnvironmentReflection_CustomSpecCube0(reflectVector, positionWS, brdfData.perceptualRoughness, 1.0h, normalizedScreenSpaceUV, 
        TEXTURECUBE_ARGS(customSpecCube0, sample_customSpecCube0), customSpecCube0_HDR);
    indirectSpecular *= environmentIntensity;
    
    // Unity
    // half3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);
    // UE
    half3 color = EnvironmentBRDF_UE(brdfData, indirectDiffuse, indirectSpecular, normalWS, viewDirectionWS);

    if (IsOnlyAOLightingFeatureEnabled())
    {
        color = half3(1, 1, 1); // "Base white" 用于 AO 调试照明模式
    }

    // 混合清漆
    color = MixClearCoat(color,
        brdfDataClearCoat, clearCoatMask,
        occlusion, positionWS, normalizedScreenSpaceUV,
        reflectVector, fresnelTerm);
    
    return color;
}

#endif // __XKNIGHT_GLOBAL_ILLUMINATION_UE__
