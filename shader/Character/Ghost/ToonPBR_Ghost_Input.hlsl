#ifndef TOONPBR_GHOST_INPUT
#define TOONPBR_GHOST_INPUT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "../Include/ToonPBR_Enum.hlsl"

CBUFFER_START(UnityPerMaterial)
    // 基本
    half    _ShadingModel;
    half4   _BaseColor;

    // 2阶阴影
    half3   _Shadow1Color;
    half    _Shadow1Step;
    half    _Shadow1Feather;
    half3   _Shadow2Color;
    half    _Shadow2Step;
    half    _Shadow2Feather;

    // 阴影遮罩控制
    half    _Source175Shadow;
    half    _TintBrightness;
    half    _TintShadowFactor;

    // 卡通高光
    half    _Metallic;
    half4   _SpecColor;
    half    _Smoothness;
    half    _SpecularStep;
    half    _SpecularFeather;

    // 抖动
    half    _DitherIntensity, _DitherSize, _DitherAlpha;
    half    _DitherWithMatrix;
    float4  _DitherTexture_TexelSize;

    // 高级设置
    half    _EnvReflectStrength;
    half    _TimelineMainLightIntensity;
CBUFFER_END

TEXTURE2D_X(_DitherTexture);

// 自定义角色的光照 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
float4  _CharacterSH[7];
half4   _CharacterMainLightColor;

half    _OverrideMainLightDir;
half4   _CustomMainLightDir;
half    _CustomMainLightIntensity;

half    _OverrideHairLightDir;
half4   _CustomHairLightDir;
half    _CustomHairLightIntensity;

half    _OverrideWeaponLightDir;
half4   _CustomWeaponLightDir;
half    _CustomWeaponLightIntensity;

// 数据结构
#include "../Include/ToonPBR_InputData.hlsl"

// r空，g是粗糙度，b是金属，a是自发光
half4 SampleMetallicSpecGloss(half smoothness, half roughness, half specularStrength)
{
    half4 specGloss = 0;
    
    #if _SPECULAR_SETUP
    specGloss.a = (2 - roughness) * smoothness; 
    specGloss.rgb = (half3)specularStrength * _SpecColor.rgb;
    #else
    specGloss.a = (1 - roughness) * smoothness; 
    specGloss.rgb = (half3)specularStrength * _Metallic;
    #endif
    
    return specGloss;
}

// 初始化卡通渲染的数据结构
inline void InitializeToonData(half envReflect, float3 normalWS, float3 positionWS, out ToonData outToonData)
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

    // unity 主灯光方向
    float3 mainLightDirection = _MainLightPosition.xyz;
    half4 mainLightColor = 1;

    // 覆盖角色光照
    #ifdef _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
    
    switch ((int)_ShadingModel)
    {
    case EShadingModel_Hair:
        mainLightDirection = lerp(_MainLightPosition.xyz, _CustomHairLightDir.xyz, _OverrideHairLightDir);
        mainLightColor = _CharacterMainLightColor * _CustomHairLightIntensity;
        break;
        
    case EShadingModel_Weapon:
        mainLightDirection = lerp(_MainLightPosition.xyz, _CustomWeaponLightDir.xyz, _OverrideWeaponLightDir);
        mainLightColor = _CharacterMainLightColor * _CustomWeaponLightIntensity;
        break;
        
    default:
        mainLightDirection = lerp(_MainLightPosition.xyz, _CustomMainLightDir.xyz, _OverrideMainLightDir);
        mainLightColor = _CharacterMainLightColor * _CustomMainLightIntensity;
        break;
    }
    
    #endif // _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
    
    outToonData.mainLightDirection = normalize(mainLightDirection);
    outToonData.mainLightColor = mainLightColor * _TimelineMainLightIntensity;

    // SDF 阴影
    outToonData.sdfMask = 0;
    outToonData.sdfMaskArea = 0;
    outToonData.sdfWeight = 1;
    
    // ILM 阴影遮罩
    outToonData.source175Shadow = _Source175Shadow;
    outToonData.tintBrightness = _TintBrightness;
    outToonData.tintShadowFactor = _TintShadowFactor;
    
    outToonData.specularStep = _SpecularStep;
    outToonData.specularFeather = _SpecularFeather;
    
    outToonData.frontRimColor = half3(1,1,1);
    outToonData.backRimColor = half3(1,1,1);
    
    outToonData.rimWidth = 0.07;
    outToonData.rimDepthCutOff = 0.05;
    
    outToonData.rimControlMask = 0;
    outToonData.rimWidth2 = 0.07;
    outToonData.rimDepthCutOff2 = 0.01;

    outToonData.hairShadowColor = 0;
    outToonData.hairShadowOffset = float4(0, 0, 0, 0);
}

#endif
