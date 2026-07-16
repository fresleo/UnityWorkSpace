#ifndef __FROST_ICICLES_INPUT__
#define __FROST_ICICLES_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    // 卡通照明 -------------------------------------
    float4  _PBRMaskMap_ST;

    // 高光工作流
    float   _Metallic;
    half4   _SpecColor;
    float   _Smoothness;
    
    half    _SpecularStep;
    half    _SpecularFeather;

    // 各向异性高光 - 没用，只是为了解报错
    half    _AnisoShiftScaleX, _AnisoShiftScaleY;
    half3   _AnisoSpecularColor;
    half    _AnisoSpread1, _AnsioSpeularShift, _AnsioSpeularStrength, _AnsioSpeularExponent;
    half3   _AnisoSecondarySpecularColor;
    half    _AnisoSpread2, _AnsioSecondarySpeularShift, _AnsioSecondarySpeularStrength, _AnsioSecondarySpeularExponent;

    // 边缘光 - 没用，只是为了解报错
    half    _RGBias;
    half    _RGShininess;
    half    _RGScale;
    half3   _RGColor;

    half3   _Shadow1Color;
    half    _Shadow1Step;
    half    _Shadow1Feather;
    half3   _Shadow2Color;
    half    _Shadow2Step;
    half    _Shadow2Feather;

    half    _EnvReflectStrength;

    half    _TimelineMainLightIntensity;

    // 霜冻 -------------------------------------
    float4  _FrostTexture_ST, _FrostBumpMap_ST, _IcicleMask_ST, _IceOverlayMask_ST;
        
    float4  _FrostTint;
    float   _FrostBumpScale;
    float   _IcicleMaskTile;

    float   _IceSlider;
    float   _IceAmount;
    float   _YMaskTop, _YMaskDown;
    float   _IcicleLength;
    float   _yIceMultiplier;

    float   _FrostEmissionFresnelIntensity;
    float   _FrostEmissionFresnelPow;

    // 光传输
    float   _TransmissionShadow;
    float   _TransStrength;
    float   _TransNormal;
    float   _TransScattering;
    float   _TransDirect;
    float   _TransAmbient;
    float   _TransShadow;

    // 镶嵌
    float   _TessValue;
    float   _TessMin;
    float   _TessMax;
    float   _TessEdgeLength;
    float   _TessMaxDisp;
CBUFFER_END

// 自定义角色的光照 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
float4  _CharacterSH[7];

// 纹理采样 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
TEXTURE2D_X(_PBRMaskMap); SAMPLER(sampler_PBRMaskMap);
// 角色定制的环境光
TEXTURECUBE(_Character_SpecCube0); SAMPLER(sampler_Character_SpecCube0);

TEXTURE2D_X(_FrostTexture); SAMPLER(sampler_FrostTexture);
TEXTURE2D_X(_FrostBumpMap); SAMPLER(sampler_FrostBumpMap);
TEXTURE2D_X(_IcicleMask); SAMPLER(sampler_IcicleMask);
TEXTURE2D_X(_IceOverlayMask); SAMPLER(sampler_IceOverlayMask);

// 数据结构
#include "../../Character/Include/ToonPBR_InputData.hlsl"

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    half4 color = SAMPLE_TEXTURE2D_X(albedoAlphaMap, sampler_albedoAlphaMap, uv);
    return color;
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_BaseMap), half scale = 1.0h)
{
    half4 n = SAMPLE_TEXTURE2D_X(bumpMap, sampler_BaseMap, uv);
    return UnpackNormalScale(n, scale);
}

// r空，g是粗糙度，b是金属，a是自发光
half4 SampleMetallicSpecGloss(float2 uv, half smoothness)
{
    half4 specGloss = SAMPLE_TEXTURE2D(_PBRMaskMap, sampler_PBRMaskMap, uv);
    
    #if _SPECULAR_SETUP
    specGloss.a = (2 - specGloss.g) * smoothness; 
    specGloss.rgb = half3(specGloss.b, specGloss.b, specGloss.b) * _SpecColor.rgb;
    #else
    specGloss.a = (1 - specGloss.g) * smoothness; 
    specGloss.rgb = half3(specGloss.b , specGloss.b, specGloss.b) * _Metallic;
    #endif
    
    return specGloss;
}

#include "./FrostIcicles_InitializeFuncs.hlsl"

// 初始化表面资源的数据
inline void InitializeSurfaceDataToon(
    float2 uv, float3 positionWS, float3 normalWS,
    out SurfaceDataToon outSurfaceData)
{
    normalWS = normalize(normalWS);

    // 反照率
    half4 baseAlbedo = 0;
    half4 finalAlbedo = baseAlbedo;

    // 法线
    float3 baseNormals = 0;
    float3 finalNormals = baseNormals;
    
    // 自发光
    half3 baseEmission = 0;
    half3 finalEmission = baseEmission;

    float2 uv_PBRMaskMap = uv * _PBRMaskMap_ST.xy + _PBRMaskMap_ST.zw;
    
    // 处理金属性，光滑度纹理
    half4 specGloss = SampleMetallicSpecGloss(uv_PBRMaskMap, _Smoothness);

    // 冰霜效果
    InitializeSurfaceData_Frost(
        uv, positionWS, normalWS,
        baseAlbedo, baseNormals, baseEmission,
        finalAlbedo, finalNormals, finalEmission);
    
    // 给输出赋值
    outSurfaceData = (SurfaceDataToon)0;
    
    outSurfaceData.albedo = finalAlbedo.rgb;
    outSurfaceData.alpha = finalAlbedo.a;
    outSurfaceData.normalTS = finalNormals;
    outSurfaceData.emission = finalEmission;
    
    outSurfaceData.smoothness = specGloss.a;
    #if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0;
    outSurfaceData.specular = specGloss.rgb;
    #else
    outSurfaceData.metallic = specGloss.b;
    outSurfaceData.specular = specGloss.rgb;
    #endif
    
    // 环境反射
    outSurfaceData.envReflect = _EnvReflectStrength;
}

// 初始化卡通渲染的数据结构
inline void InitializeToonData(float2 uv, half envReflect, float3 normalWS, float3 positionWS, out ToonData outToonData)
{
    outToonData = (ToonData)0;
    
    outToonData.shadow1 = _Shadow1Color;
    outToonData.shadow1Step = _Shadow1Step;
    outToonData.shadow1Feather = _Shadow1Feather;
    outToonData.shadow2 = _Shadow2Color;
    outToonData.shadow2Step = _Shadow2Step;
    outToonData.shadow2Feather = _Shadow2Feather;
    outToonData.doubleShadeTintStrength = 1.0h;
    
    outToonData.envReflect = envReflect;
    
    outToonData.specularStep = _SpecularStep;
    outToonData.specularFeather = _SpecularFeather;
}

#endif // __FROST_ICICLES_INPUT__
