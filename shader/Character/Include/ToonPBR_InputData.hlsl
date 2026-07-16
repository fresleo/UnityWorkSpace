#ifndef __TOONPBR_INPUT_DATA__
#define __TOONPBR_INPUT_DATA__

struct SurfaceDataToon
{
    half3   albedo;
    half3   specular;
    half    metallic;
    half    smoothness;
    float3  normalTS;
    half3   emission;
    half    envReflect;
    half    alpha;
    half    fresnelMask;
    
    half    shadingModel;
    
    half    toonShadowMask;

    #if defined(_DIFFUSE_OFFSET)
    half    occlusion;
    #endif
};

struct InputDataToon
{
    float3  positionWS;
    float3  normalWS;
    float3  viewDirectionWS;
    float3  vertexNormalWS;
    half3   bakedGI;
    float4  tangentWS;
    float3  bitangentWS;
    float4  shadowCoord;
    half4   shadowMask;
    float3  viewDirTS;
    float4  screenPos;
    float2  normalizedScreenSpaceUV;
    
    float   positionNDCw;

    float2  texcoord;

    half4   vertexColor;
};

struct ToonData
{
    half3   detail;   // Detail
    
    half3   shadow1;
    half    shadow1Step;
    half    shadow1Feather;
    half3   shadow2;
    half    shadow2Step;
    half    shadow2Feather;
    half    doubleShadeTintStrength; // 0=原色乘法调色，1=阴影替换色
    
    half    sdfMask; // SDF 主区（A=1）
    half    sdfMaskArea; // SDF 遮罩区（A=0）
    half    sdfWeight; // _SDFShadowMap.a，1=主区 SDF+175 遮罩融合，0=遮罩区仅 SDF；R8 无 A 时采样为 1
    
    half    noseSdfG; // 鼻子的 sdf 高光
    half    sdfFaceU; // 面部 sdf uv 的 x（按光的方向翻转），作为左右半脸的 mask
    
    half3   sdfShadowColor;
    half    sdfShadowStep;
    half    sdfShadowFeather;
    half3   sdfMaskShadowColor;
    half    sdfMaskShadowFeather;

    // ILM 阴影遮罩
    half    source175Shadow;
    half    tintBrightness;
    half    tintShadowFactor;
    
    half    envReflect;

    half    specularStep;
    half    specularFeather;
    half    floodlightIntensity;
    
    half3   frontRimColor;
    half3   backRimColor;
    
    half    rimWidth;
    half    rimDepthCutOff;
    
    half    rimControlMask;
    half    rimWidth2;
    half    rimDepthCutOff2;

    half4   hairShadowColor;
    float4  hairShadowOffset;

    float3  mainLightDirection;
    half4   mainLightColor;
    
    // 菲涅尔边缘光（整体）
    half3   rimGlowColor;
    half    rimGlowScale, rimGlowBias, rimGlowShininess, rimGlowFeather;
    half    rimGlowMixVertexNormal;
    half    rimGlowDiffuseBlend, rimGlowDiffuseStep, rimGlowDiffuseFeather;
    half    rimGlowSoftFresnelMix;
    half    rimGlowSoftFresnelRangeMultiplier, rimGlowSoftFresnelRangeMin, rimGlowSoftFresnelStartOffset, rimGlowSoftFresnelPow;
    
    // 菲涅尔边缘光（局部）
    float4x4 localRGWorldToLocal_0, localRGWorldToLocal_1;
    
    half3   localRGColor;
    half    localRGScale, localRGBias, localRGShininess, localRGFeather;
    half    localRGMixVertexNormal;
    half    localRGDiffuseBlend, localRGDiffuseStep, localRGDiffuseFeather;
    half    localRGSoftFresnelMix;
    half    localRGSoftFresnelRangeMultiplier, localRGSoftFresnelRangeMin, localRGSoftFresnelStartOffset, localRGSoftFresnelPow;
};

struct BRDFDataToon
{
    half3   diffuse;
    half3   specular;
    half    perceptualRoughness;
    half    roughness;
    half    roughness2;
    half    grazingTerm;
    half    normalizationTerm;  // roughness * 4.0 + 2.0
    half    roughness2MinusOne; // roughness^2 - 1.0
};

#endif // __TOONPBR_INPUT_DATA__
