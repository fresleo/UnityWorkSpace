#ifndef TOONPBR_LIGHTING_INCLUDED
#define TOONPBR_LIGHTING_INCLUDED

BRDFData ConvertBRDFDataToonTOBRDFData(BRDFDataToon brdfToon)
{
    BRDFData data;
    
    data.diffuse = brdfToon.diffuse;
    data.roughness = brdfToon.roughness;
    data.roughness2 = brdfToon.roughness2;
    data.specular = brdfToon.specular;
    data.grazingTerm = brdfToon.grazingTerm;
    data.normalizationTerm = brdfToon.normalizationTerm;
    data.perceptualRoughness = brdfToon.perceptualRoughness;
    data.roughness2MinusOne = brdfToon.roughness2MinusOne;

    return data;
}

inline void InitializeBRDFDataToon(SurfaceDataToon surfaceData, out BRDFDataToon outBRDFData)
{
    #ifdef _SPECULAR_SETUP
    half reflectivity = ReflectivitySpecular(surfaceData.specular);
    half oneMinusReflectivity = 1.0h - reflectivity;
    outBRDFData.diffuse = surfaceData.albedo;
    outBRDFData.specular = surfaceData.specular;
    #else
    half oneMinusReflectivity = OneMinusReflectivityMetallic(surfaceData.metallic);
    half reflectivity = 1.0 - oneMinusReflectivity;
    outBRDFData.diffuse = surfaceData.albedo;
    outBRDFData.specular = lerp(kDieletricSpec.rgb, surfaceData.albedo, surfaceData.metallic);
    #endif
    
    outBRDFData.grazingTerm = saturate(surfaceData.smoothness + reflectivity);
    outBRDFData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.smoothness);
    outBRDFData.roughness = max(PerceptualRoughnessToRoughness(outBRDFData.perceptualRoughness), HALF_MIN_SQRT);
    outBRDFData.roughness2 = max(outBRDFData.roughness * outBRDFData.roughness, HALF_MIN);
    
    #if defined _HAIRSPECULAR || defined _HAIRSPECULARVIEWNORMAL
    outBRDFData.specularExponent = RoughnessToBlinnPhongSpecularExponent(outBRDFData.roughness);
    #endif

    outBRDFData.normalizationTerm = outBRDFData.roughness * 4.0h + 2.0h;
    outBRDFData.roughness2MinusOne = outBRDFData.roughness2 - 1.0h;
}

half3 LightingToon(
    BRDFDataToon brdfData,
    SurfaceDataToon surfaceData,
    InputDataToon inputData,
    ToonData toonData,
    Light light)
{
    half3 color = 0;
    float diffuseLightMask = 0; // 亮度细节
    
    float lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;;
    
    float3 diffuseColor = CalculateDiffuse(
        surfaceData, inputData, toonData,
        brdfData, light.direction, lightAttenuation,
        /*out*/ diffuseLightMask);
    
    float3 specularColor = CalculateSpecular(brdfData, inputData, light, toonData);
    specularColor *= diffuseLightMask;
    
    color += diffuseColor;
    color += specularColor;
    
    float3 globalNormalWS = lerp(inputData.normalWS, inputData.vertexNormalWS, toonData.rimGlowMixVertexNormal);
    float3 localNormalWS = lerp(inputData.normalWS, inputData.vertexNormalWS, toonData.localRGMixVertexNormal);
    
    half3 rimGlowColor = RimGlowLight(light.direction, inputData.viewDirectionWS, toonData, globalNormalWS, localNormalWS, inputData.positionWS);
    rimGlowColor *= surfaceData.fresnelMask;
    
    color += rimGlowColor;
    
    color *= light.color;
    
    return color;
}

half3 AdditionalLightingToon(BRDFDataToon brdfData, SurfaceDataToon surfaceData, InputDataToon inputData, ToonData toonData, Light light)
{
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half3 lightColor = LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);

    lightColor *= surfaceData.albedo;

    return lightColor;
}

half4 FragmentLitToon(InputDataToon inputData, SurfaceDataToon surfaceData, ToonData toonData)
{
    BRDFDataToon brdfData;
    InitializeBRDFDataToon(surfaceData, brdfData);
    
    Light mainLight = GetMainLight();
    BRDFData pbrBrdfData = ConvertBRDFDataToonTOBRDFData(brdfData);

    #if defined( _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON )
    mainLight.direction = toonData.mainLightDirection;
    mainLight.color = toonData.mainLightColor;
    #endif
    
    half3 color = GlobalIllumination_UE(pbrBrdfData, (BRDFData)0, 0,
        inputData.bakedGI, 1.0h, inputData.positionWS,
        inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV,
        toonData.envReflect, toonData.envReflect);
    
    color += LightingToon(
        brdfData,
        surfaceData,
        inputData,
        toonData,
        mainLight);

    color += RimLight(
        color, toonData.frontRimColor, toonData.backRimColor,
        inputData.vertexNormalWS, mainLight.direction, inputData.screenPos,
        toonData.rimWidth, toonData.rimDepthCutOff,
        toonData.rimControlMask, toonData.rimWidth2, toonData.rimDepthCutOff2);

    // #if _ADDITIONAL_LIGHTS // 为了节省关键字，所以不判断了，直接开放
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
        color += AdditionalLightingToon(brdfData, surfaceData, inputData, toonData, light);
    }
    // #endif

    // 这种静态宏编译时已经将上述计算剔除掉
    #ifdef _EYE_MODE
    color = EyeRender(brdfData, surfaceData, inputData);
    #endif

    return half4(color, surfaceData.alpha);
}

half4 FragmentLitToon_LOD1(InputDataToon inputData, SurfaceDataToon surfaceData, ToonData toonData)
{
    BRDFDataToon brdfData;
    InitializeBRDFDataToon(surfaceData, brdfData);
    
    Light mainLight = GetMainLight();

    #if defined( _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON )
    mainLight.direction = toonData.mainLightDirection;
    mainLight.color = toonData.mainLightColor;
    #endif
    
    half3 color = inputData.bakedGI * brdfData.diffuse * toonData.envReflect;
    
    color += LightingToon(
        brdfData,
        surfaceData,
        inputData,
        toonData,
        mainLight);
    
    // V2 因为是专门单独启动的 pass 所以不需要额外判是不是脸，因为它肯定是脸
    #ifdef _HAS_CEL_HAIR_SHADOW_V2
        color *= toonData.hairShadowColor.rgb;
    #endif
    
    //color += RimLight(color, toonData.frontRimColor, toonData.backRimColor, inputData.vertexNormalWS, mainLight.direction, toonData.rimWidth, inputData.screenPos);

    //#if _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
        color += AdditionalLightingToon(brdfData, surfaceData, inputData, toonData, light);
    }
    //#endif

    // 这种静态宏编译时已经将上述计算剔除掉
    #ifdef _EYE_MODE
    color = EyeRender(brdfData, surfaceData, inputData);
    #endif

    return half4(color, surfaceData.alpha);
}

#endif // TOONPBR_LIGHTING_INCLUDED
