// 对基本物理照明方法的扩展
#ifndef __XKNIGHT_LIGHTING_XK__
#define __XKNIGHT_LIGHTING_XK__

half3 LightingPhysicallyBased_XK(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, half3 lightDirectionWS, half lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlightsOff,
    half specularScale, half minPerceptualRoughness)
{
    BRDFData directSpecBRDF = brdfData;
    
    // 自定义的全局高光缩放
    directSpecBRDF.specular = directSpecBRDF.specular * specularScale * GLOBAL_SPECULAR_SCALE;
    
    // 钳制最小感知粗糙度
    if (minPerceptualRoughness > directSpecBRDF.perceptualRoughness)
    {
        directSpecBRDF.perceptualRoughness = minPerceptualRoughness;
        
        RecalculationBRDFDataRoughness(directSpecBRDF);
    }
    
    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half3 radiance = lightColor * (lightAttenuation * NdotL);
    
    half3 brdf = brdfData.diffuse;
    #ifndef _SPECULARHIGHLIGHTS_OFF
    [branch] if (!specularHighlightsOff)
    {
        brdf += directSpecBRDF.specular * DirectBRDFSpecular(directSpecBRDF, normalWS, lightDirectionWS, viewDirectionWS);

        #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
        // Clear coat evaluates the specular a second timw and has some common terms with the base specular.
        // We rely on the compiler to merge these and compute them only once.
        half brdfCoat = kDielectricSpec.r * DirectBRDFSpecular(brdfDataClearCoat, normalWS, lightDirectionWS, viewDirectionWS);

        // Mix clear coat and base layer using khronos glTF recommended formula
        // https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Khronos/KHR_materials_clearcoat/README.md
        // Use NoV for direct too instead of LoH as an optimization (NoV is light invariant).
        half NoV = saturate(dot(normalWS, viewDirectionWS));
        // Use slightly simpler fresnelTerm (Pow4 vs Pow5) as a small optimization.
        // It is matching fresnel used in the GI/Env, so should produce a consistent clear coat blend (env vs. direct)
        half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * Pow4(1.0 - NoV);

        brdf = brdf * (1.0 - clearCoatMask * coatFresnel) + brdfCoat * clearCoatMask;
        #endif // _CLEARCOAT
    }
    #endif // _SPECULARHIGHLIGHTS_OFF

    return brdf * radiance;
}

half3 LightingPhysicallyBased_XK(BRDFData brdfData, BRDFData brdfDataClearCoat,
    Light light,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlightsOff,
    half specularScale, half minPerceptualRoughness)
{
    half lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;
    
    return LightingPhysicallyBased_XK(brdfData, brdfDataClearCoat,
        light.color, light.direction, lightAttenuation,
        normalWS, viewDirectionWS,
        clearCoatMask, specularHighlightsOff,
        specularScale, minPerceptualRoughness);
}

#endif // __XKNIGHT_LIGHTING_XK__
