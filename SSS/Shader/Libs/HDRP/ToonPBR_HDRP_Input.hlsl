#ifndef TOONPBR_INPUT
#define TOONPBR_INPUT

// [HDRP] 替换 URP Core.hlsl
#include "./ToonPBR_HDRP_URPCompat.hlsl"

#include "../ToonPBR_Enum.hlsl"

// [HDRP] 替换 URP 包路径为项目 Common 路径
#include "../../../Common/XKnightDepthMask_Input.hlsl"
#include "../ToonPBR_FrostInput.hlsl"

#include "../ToonPBR_SDF.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4  _BaseMap_ST;
    // float4  _DetailTex_ST;

    half    _ShadingModel;
    half4   _BaseColor;
    half    _BaseMapA;

    half3   _Shadow1Color;
    half    _Shadow1Step;
    half    _Shadow1Feather;
    half3   _Shadow2Color;
    half    _Shadow2Step;
    half    _Shadow2Feather;

    half    _DiffuseRampIntensity;
    half3   _DiffuseRampColor;
    half    _EnvReflectStrength;
    half    _DiffuseRampMapVertical;
    half    _DiffuseRampSaturate;

    half4   _EmissionColor;
    half    _EmissionStrength;

    half    _BumpScale;

    // 卡通高光
    half    _Metallic;
    half4   _SpecColor;
    half    _SpecularStep;
    half    _SpecularFeather;
    half    _FloodlightIntensity;
    half    _Smoothness;
    // 各向异性高光
    half    _AnisoShiftScaleX, _AnisoShiftScaleY;
    half3   _AnisoSpecularColor;
    half    _AnisoSpread1, _AnsioSpeularShift, _AnsioSpeularStrength, _AnsioSpeularExponent;
    half3   _AnisoSecondarySpecularColor;
    half    _AnisoSpread2, _AnsioSecondarySpeularShift, _AnsioSecondarySpeularStrength, _AnsioSecondarySpeularExponent;

    half3   _FrontRimColor;
    half3   _BackRimColor;

    half    _RimWidth;
    half    _RimDepthCutOff;

    float4  _RimControlMask_ST;
    half    _RimWidth2;
    half    _RimDepthCutOff2;

    half    _MiOutline;
    half    _OutlineWidth;
    half    _OutlinePower;
    half4   _OutlineColor;
    half    _OutlineFadeStart, _OutlineFadeEnd;

    // 消融
    half    _DissolveType;

    // 溶解的边缘
    half    _DissolveEdgeOn;
    half    _EdgeWidth;
    half4   _EdgeColor1, _EdgeColor2;

    float4  _DissolveTex_ST;
    half4   _DissolveTex_Channel;
    half    _DissolveFadingMin, _DissolveFadingMax;

    // 溶解系数
    half    _DissolveCutoff, _DissolveCutoffMultiplier;

    // 方向溶解
    half3   _DissolveDir;

    // 遮罩溶解
    float4  _DissolveMaskTex_ST;
    half4   _DissolveMaskTex_Channel;
    half    _DissolveMaskReverse;

    // 抖动
    half    _DitherIntensity, _DitherSize, _DitherAlpha;
    half    _DitherWithMatrix;
    float4  _DitherTexture_TexelSize;

    // 菲涅尔边缘光
    half3   _RGColor;
    half    _RGScale, _RGBias, _RGShininess, _RGFeather, _RGMixVertexNormal;
    half    _RGDiffuseBlend, _RGDiffuseStep, _RGDiffuseFeather;
    half    _RGSoftFresnelMix;
    half4   _RGSoftFresnelParameters;

    // 局部菲涅尔边缘光
    float4x4 _Local_RGWorldToLocal;

    half3   _Local_RGColor;
    half    _Local_RGScale, _Local_RGBias, _Local_RGShininess, _Local_RGFeather, _Local_RGMixVertexNormal;
    half    _Local_RGDiffuseBlend, _Local_RGDiffuseStep, _Local_RGDiffuseFeather;
    half    _Local_RGSoftFresnelMix;
    half4   _Local_RGSoftFresnelParameters;

    // 眼睛
    half    _PupilSize;
    half    _PupilSunken;
    half    _PupilMatcapIntensity;

    half3  _VertexPullDirection;
    half   _VertexPullIntensity;

    half4  _VertexPullNoiseTexture_ST;

    half    _TimelineMainLightIntensity;

    // SDF 阴影
    half    _SDFFullLight;

    // 阴影遮罩控制
    half    _Source175Shadow;
    half    _TintBrightness;
    half    _TintShadowFactor;

    // 漫反射偏移
    half    _DarkColorSmooth;
    half    _ShadowColorSmooth;
    half    _LightColorSmooth;
    half    _ShadowColorDiffuse;
    half    _LightColorHSV_S;
    half    _LightColorHSV_V;
    half    _DiffuseOcclusion;
    half    _LightColorMode;
    half    _LightColorRangeMin;
    half    _LightColorRangeMax;

    // 头发阴影
    half4   _HairShadowColor;
    half    _HairShadowOffsetX, _HairShadowOffsetY, _HairShadowOffsetZ;

    // 后处理遮罩
    XKNIGHT_DEPTH_MASK_INPUT_1
    half    _WriteDepthNormals_On;

    float4  _Character_LUT_Params; // 角色 LUT 参数

    // FOV
    float4  _FOV_PivotWS;
    float4  _FOV_Parameters;

    // 冰霜效果
    TOONPBR_FROST_CBUFFER

    // 战斗辉光ID：0=不写入，1..8=效果 ID
    half    _CombatGlowID;

    // 战斗时菲涅尔边缘光
    half    _CombatSurfaceGlowOn;
    half4   _CombatSurfaceGlowColorInner;
    half4   _CombatSurfaceGlowColorMid;
    half4   _CombatSurfaceGlowColorOuter;
    half    _CombatSurfaceGlowIntensity;
    half4   _CombatSurfaceGlowBand;
    float4  _CombatSurfaceGlowBreakupTex_ST;
    half4   _CombatSurfaceGlowBreakupParams;
    half4   _CombatSurfaceGlowFill;
CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

// 2代头帘阴影需要的
float4  _LightDirSS; // 主灯在相机空间的表示

// 自定义角色的光照 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
// [HDRP] _CharacterSH 统一在 URPCompat.hlsl 里声明，这里不重复
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

// 纹理采样 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_PBRMaskMap); SAMPLER(sampler_PBRMaskMap);

// TEXTURE2D(_DetailTex); SAMPLER(sampler_DetailTex);

TEXTURE2D(_DissolveTex); SAMPLER(sampler_DissolveTex);
TEXTURE2D(_DissolveMaskTex); SAMPLER(sampler_DissolveMaskTex);

TEXTURE2D(_DitherTexture);

TEXTURE2D(_SDFShadowMap); SAMPLER(sampler_SDFShadowMap);
TEXTURE2D(_AnisoShiftMap); SAMPLER(sampler_AnisoShiftMap);
TEXTURE2D(_DiffuseRampMap); SAMPLER(sampler_DiffuseRampMap);

TEXTURE2D(_PupilMatcap); SAMPLER(sampler_PupilMatcap);

TEXTURE2D(_VertexPullNoiseTexture); SAMPLER(sampler_VertexPullNoiseTexture);

#ifdef _HAS_CEL_HAIR_SHADOW_V1
TEXTURE2D_FLOAT(_CelHairShadowColor); SAMPLER(sampler_CelHairShadowColor); // 卡通头发投影遮罩
#endif

TEXTURE2D(_Character_LUT_Map); SAMPLER(sampler_Character_LUT_Map); // 角色 LUT 图

TEXTURE2D(_RimControlMask); SAMPLER(sampler_RimControlMask); // 深度边缘光局部控制遮罩

// 冰霜效果
TOONPBR_FROST_TEXTURE

// 战斗时菲涅尔边缘光 噪声纹理
TEXTURE2D(_CombatSurfaceGlowBreakupTex); SAMPLER(sampler_CombatSurfaceGlowBreakupTex);
// 纹理采样 <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

// 数据结构
#include "../ToonPBR_InputData.hlsl"

half Alpha(half albedoAlpha, half4 color)
{
    half alpha = albedoAlpha * color.a;
    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    half4 color = SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv);
    return color;
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_BaseMap), half scale = 1.0h)
{
    half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_BaseMap, uv);
    return UnpackNormalScale(n, scale);
}

half3 SampleEmission(half3 emissionColor, half mask)
{
    #ifdef _EMISSION
    half3 col = emissionColor * mask * _EmissionStrength;
    return col;
    #else
    return 0.0;
    #endif
}

// Detail
half4 SampleToonDetail(float2 uv, TEXTURE2D_PARAM(detailMap, sampler_detailMap))
{
    half4 color = SAMPLE_TEXTURE2D(detailMap, sampler_detailMap, uv);
    return color;
}

// 素描线 - UV旋转功能
float2 RotateUV(float2 uv, float angle)
{
    // Convert angle from degrees to radians
    float angleRad = angle * PI / 180.0;

    // Get sin and cos of angle
    float s = sin(angleRad);
    float c = cos(angleRad);

    // Create rotation matrix
    float2x2 rotationMatrix = float2x2(c, -s, s, c);

    // Center UV coordinates before rotation
    float2 uvCentered = uv - 0.5;

    // Apply rotation
    float2 rotatedUV = mul(rotationMatrix, uvCentered);

    // Move UV coordinates back
    return rotatedUV + 0.5;
}

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

#include "../ToonPBR_FrostInitializeFuncs.hlsl" // 冰霜初始化方法

// 初始化卡通渲染的数据结构
inline void InitializeToonData(float2 uv, float2 texcoord1, half envReflect, float3 normalWS, float3 positionWS, out ToonData outToonData)
{
    outToonData = (ToonData)0;

    outToonData.shadow1 = _Shadow1Color;
    outToonData.shadow1Step = _Shadow1Step;
    outToonData.shadow1Feather = _Shadow1Feather;
    outToonData.shadow2 = _Shadow2Color;
    outToonData.shadow2Step = _Shadow2Step;
    outToonData.shadow2Feather = _Shadow2Feather;
    outToonData.envReflect = envReflect;

    // 细节贴图
    // float2 uv1 = texcoord1 * _DetailTex_ST.xy + _DetailTex_ST.zw;
    // outToonData.detail = SampleToonDetail(uv1, TEXTURE2D_ARGS(_DetailTex, sampler_DetailTex)).rgb;

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
    #ifdef _SDFSHADOWMAP
        #ifdef _SDF_MESH_RENDER_MODE
    float3 sdfInfo = CalculateFaceShadowFactorMeshRender(mainLightDirection, uv, _SDFFullLight);
        #else
    float3 sdfInfo = CalculateFaceShadowFactorSkinnedMeshRender(mainLightDirection, uv, _SDFFullLight);
        #endif

    half4 faceSdfSample = SAMPLE_TEXTURE2D(_SDFShadowMap, sampler_LinearClamp, sdfInfo.xy);

    outToonData.sdfMask = GetToonFaceDiffuseMaskFromR(faceSdfSample.r, _Shadow1Step, _Shadow1Feather, sdfInfo.z);

    outToonData.noseSdfG = faceSdfSample.g;
    outToonData.sdfFaceU = sdfInfo.x;
    #endif // _SDFSHADOWMAP

    // ILM 阴影遮罩
    outToonData.source175Shadow = _Source175Shadow;
    outToonData.tintBrightness = _TintBrightness;
    outToonData.tintShadowFactor = _TintShadowFactor;

    outToonData.specularStep = _SpecularStep;
    outToonData.specularFeather = _SpecularFeather;

    outToonData.frontRimColor = _FrontRimColor;
    outToonData.backRimColor = _BackRimColor;

    outToonData.rimWidth = _RimWidth;
    outToonData.rimDepthCutOff = _RimDepthCutOff;

    float2 maskUV = uv * _RimControlMask_ST.xy + _RimControlMask_ST.zw;
    outToonData.rimControlMask = SAMPLE_TEXTURE2D(_RimControlMask, sampler_RimControlMask, maskUV).r;
    outToonData.rimWidth2 = _RimWidth2;
    outToonData.rimDepthCutOff2 = _RimDepthCutOff2;

    outToonData.hairShadowColor = _HairShadowColor;
    outToonData.hairShadowOffset = float4(_HairShadowOffsetX, _HairShadowOffsetY, _HairShadowOffsetZ, 0);

    // 菲涅尔边缘光（整体）
    outToonData.rimGlowColor = _RGColor;

    outToonData.rimGlowScale = _RGScale;
    outToonData.rimGlowBias = _RGBias;
    outToonData.rimGlowShininess = _RGShininess;
    outToonData.rimGlowFeather = _RGFeather;
    outToonData.rimGlowMixVertexNormal = _RGMixVertexNormal;

    outToonData.rimGlowDiffuseBlend = _RGDiffuseBlend;
    outToonData.rimGlowDiffuseStep = _RGDiffuseStep;
    outToonData.rimGlowDiffuseFeather = _RGDiffuseFeather;

    outToonData.rimGlowSoftFresnelMix = _RGSoftFresnelMix;
    outToonData.rimGlowSoftFresnelRangeMultiplier = _RGSoftFresnelParameters.x;
    outToonData.rimGlowSoftFresnelRangeMin = _RGSoftFresnelParameters.y;
    outToonData.rimGlowSoftFresnelStartOffset = _RGSoftFresnelParameters.z;
    outToonData.rimGlowSoftFresnelPow = _RGSoftFresnelParameters.w;

    // 菲涅尔边缘光（局部）
    outToonData.localRGWorldToLocal = _Local_RGWorldToLocal;

    outToonData.localRGColor = _Local_RGColor;

    outToonData.localRGScale = _Local_RGScale;
    outToonData.localRGBias = _Local_RGBias;
    outToonData.localRGShininess = _Local_RGShininess;
    outToonData.localRGFeather = _Local_RGFeather;
    outToonData.localRGMixVertexNormal = _Local_RGMixVertexNormal;

    outToonData.localRGDiffuseBlend = _Local_RGDiffuseBlend;
    outToonData.localRGDiffuseStep = _Local_RGDiffuseStep;
    outToonData.localRGDiffuseFeather = _Local_RGDiffuseFeather;

    outToonData.localRGSoftFresnelMix = _Local_RGSoftFresnelMix;
    outToonData.localRGSoftFresnelRangeMultiplier = _Local_RGSoftFresnelParameters.x;
    outToonData.localRGSoftFresnelRangeMin = _Local_RGSoftFresnelParameters.y;
    outToonData.localRGSoftFresnelStartOffset = _Local_RGSoftFresnelParameters.z;
    outToonData.localRGSoftFresnelPow = _Local_RGSoftFresnelParameters.w;
}

// [HDRP] 不 include FOVFix.hlsl，由 URPCompat.hlsl 里的同签名桩代替

#endif // TOONPBR_INPUT
