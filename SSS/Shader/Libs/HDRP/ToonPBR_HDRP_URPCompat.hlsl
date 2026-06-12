#ifndef TOONPBR_HDRP_URPCOMPAT
#define TOONPBR_HDRP_URPCOMPAT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl" // SampleSH9
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl" // PerceptualSmoothnessToPerceptualRoughness / PerceptualRoughnessToRoughness
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl" // F_Schlick / D_KajiyaKay / ShiftTangent

// 必须在 include ProbeVolume.hlsl 之前定义，因为 ProbeVolume.hlsl 内部会引用它。
real3 EvaluateAmbientProbe(real3 normalWS)
{
    return SampleSH9(_AmbientProbeData, normalWS);
}

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#endif

#ifndef unity_CameraInvProjection
    #define unity_CameraInvProjection UNITY_MATRIX_I_P
#endif

struct Light
{
    half3 direction;       
    half3 color;
    half  distanceAttenuation;
    half  shadowAttenuation;
};

struct BRDFData
{
    half3 albedo;
    half3 diffuse;
    half3 specular;
    half  reflectivity;
    half  perceptualRoughness;
    half  roughness;
    half  roughness2;
    half  grazingTerm;
    half  normalizationTerm;
    half  roughness2MinusOne;
};

struct VertexPositionInputs
{
    float3 positionWS;   // Camera-Relative World Space（与 HDRP 原生一致）
    float3 positionVS;
    float4 positionCS;
    float4 positionNDC;
};

struct VertexNormalInputs
{
    real3 tangentWS;
    real3 bitangentWS;
    float3 normalWS;
};

#ifndef kDieletricSpec
    #define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04)
#endif

float4 GetCharacterMainLightPosition()
{
    if (_DirectionalLightCount > 0)
    {
        return float4(-_DirectionalLightDatas[0].forward, 0.0);
    }
    return float4(normalize(float3(0.3, 0.9, -0.3)), 0.0);
}

// 如果美术觉得太亮/太暗，可以单独暴露一个 _HDRPMainLightToURPIntensity 给材质或 CharacterOverrideGI 调。
#ifndef HDRP_CHARACTER_MAIN_LIGHT_TO_URP_INTENSITY
    #define HDRP_CHARACTER_MAIN_LIGHT_TO_URP_INTENSITY 1.0h
#endif

half4 GetCharacterMainLightColor()
{
    if (_DirectionalLightCount > 0)
    {
        half3 c = (half3)_DirectionalLightDatas[0].color;
        // 用最大通道做亮度归一化，保留色相。>1e-4 防 0 除。
        half maxC = max(max(c.r, c.g), max(c.b, (half)1e-4));
        half3 hue = c / maxC;
        return half4(hue * HDRP_CHARACTER_MAIN_LIGHT_TO_URP_INTENSITY, 1.0h);
    }
    return half4(1, 1, 1, 1);
}


#define _MainLightPosition  GetCharacterMainLightPosition()
#define _MainLightColor     GetCharacterMainLightColor()

Light GetMainLight()
{
    Light l;
    l.direction           = (half3)GetCharacterMainLightPosition().xyz;
    l.color               = GetCharacterMainLightColor().rgb;
    l.distanceAttenuation = 1.0h;
    l.shadowAttenuation   = 1.0h;
    return l;
}

Light GetMainLight(float4 shadowCoord)
{
    return GetMainLight();
}

Light GetMainLight(float4 shadowCoord, float3 positionWS, half4 shadowMask)
{
    return GetMainLight();
}

int GetAdditionalLightsCount()
{
    return 0;
}

Light GetAdditionalLight(uint i, float3 positionWS)
{
    Light l = (Light)0;
    l.distanceAttenuation = 0;
    l.shadowAttenuation = 0;
    return l;
}

Light GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
{
    return GetAdditionalLight(i, positionWS);
}

// --- Vertex Position / Normal ---
VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
{
    // HDRP 下 TransformObjectToWorld 返回 Camera-Relative World Space。
    float3 positionRWS = TransformObjectToWorld(positionOS);

    VertexPositionInputs o;
    o.positionWS  = positionRWS;
    o.positionVS  = TransformWorldToView(positionRWS);
    o.positionCS  = TransformWorldToHClip(positionRWS);

    // 仿 URP 的 ComputeScreenPos 实现
    float4 ndc = o.positionCS * 0.5f;
    ndc.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    ndc.zw = o.positionCS.zw;
    o.positionNDC = ndc;

    return o;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS)
{
    VertexNormalInputs n;
    n.tangentWS   = real3(1.0, 0.0, 0.0);
    n.bitangentWS = real3(0.0, 1.0, 0.0);
    n.normalWS    = TransformObjectToWorldNormal(normalOS);
    return n;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS, float4 tangentOS)
{
    VertexNormalInputs n;

    float sign = tangentOS.w * GetOddNegativeScale();
    n.normalWS    = TransformObjectToWorldNormal(normalOS);
    n.tangentWS   = real3(TransformObjectToWorldDir(tangentOS.xyz));
    n.bitangentWS = real3(cross(n.normalWS, float3(n.tangentWS)) * sign);
    return n;
}

// --- ComputeScreenPos（URP 风格的 homogeneous NDC 输出） ---
float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

// --- 像素法线归一化 ---
float3 NormalizeNormalPerPixel(float3 normalWS)
{
    // HDRP 的 SafeNormalize 对 0 向量返回 0，这里保持与 URP 行为一致
    return SafeNormalize(normalWS);
}

float4 _CharacterSH[7];

half3 CharacterSampleSH(half3 normalWS)
{
#if defined(_GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON)
    return max(half3(0, 0, 0), (half3)SampleSH9(_CharacterSH, normalWS));
#else
    return half3(0, 0, 0);
#endif
}


// 默认 reference = 1000（≈ 室外日间漫射 ambient 的 luminance 量级）。
// 美术想统一调亮/调暗角色环境光，可以改 HDRP_CHARACTER_GI_INTENSITY；
// 想适配场景整体曝光区间，可以改 HDRP_CHARACTER_GI_REFERENCE_LUMINANCE
#ifndef HDRP_CHARACTER_GI_REFERENCE_LUMINANCE
    #define HDRP_CHARACTER_GI_REFERENCE_LUMINANCE 1000.0h
#endif

#ifndef HDRP_CHARACTER_GI_INTENSITY
    #define HDRP_CHARACTER_GI_INTENSITY 1.0h
#endif

half3 NormalizeHDRPGIToURP(half3 luminance)
{
    return luminance * (HDRP_CHARACTER_GI_INTENSITY / HDRP_CHARACTER_GI_REFERENCE_LUMINANCE);
}

// HDRP 角色 GI：优先 APV，APV 关闭或采样无效时回退到 Ambient Probe。
// 两条路径出来的都是 luminance 量级，统一过 NormalizeHDRPGIToURP 归一化到 URP 量级。
half3 CharacterSampleAPV(half3 normalWS, float3 positionRWS, float2 positionSS)
{
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    float3 bakeDiffuseLighting = float3(0, 0, 0);
    float3 backBakeDiffuseLighting = float3(0, 0, 0);
    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionRWS);

    EvaluateAdaptiveProbeVolume(
        GetAbsolutePositionWS(positionRWS),
        (float3)normalWS,
        (float3)normalWS,
        viewDirWS,
        positionSS,
        bakeDiffuseLighting,
        backBakeDiffuseLighting);

    return max(half3(0, 0, 0), NormalizeHDRPGIToURP((half3)bakeDiffuseLighting));
#else
    half3 c = (half3)EvaluateAmbientProbe((real3)normalWS);
    return max(half3(0, 0, 0), NormalizeHDRPGIToURP(c));
#endif
}

#define OUTPUT_SH(normalWS, vertexSH)
#define DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, index)  half3 vertexSH : TEXCOORD##index

half ReflectivitySpecular(half3 specular)
{
    return max(max(specular.r, specular.g), specular.b);
}

half OneMinusReflectivityMetallic(half metallic)
{
    // r = lerp(0.04, 0, metallic)
    half oneMinusDielectricSpec = kDieletricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

half3 LightingLambert(half3 lightColor, half3 lightDir, half3 normal)
{
    half NdotL = saturate(dot(normal, lightDir));
    return lightColor * NdotL;
}

float2 GetNormalizedScreenSpaceUV(float4 positionCS)
{
    return positionCS.xy * _ScreenSize.zw;
}
float2 GetNormalizedScreenSpaceUV(float2 positionSS)
{
    return positionSS * _ScreenSize.zw;
}

half3 EnvBRDFApproxLazarov_HDRPCompat(half Roughness, half NoV)
{
    const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
    const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
    half4 r = Roughness * c0 + c1;
    half a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
    half2 AB = half2(-1.04, 1.04) * a004 + r.zw;
    return half3(AB.x, AB.y, 0);
}

half3 EnvironmentBRDF_UE_HDRPCompat(BRDFData brdfData, half3 indirectDiffuse, half3 indirectSpecular, half3 normalWS, half3 viewDirectionWS)
{
    half NoV = saturate(dot(normalWS, viewDirectionWS));
    half3 ab = EnvBRDFApproxLazarov_HDRPCompat(brdfData.perceptualRoughness, NoV);
    half F90 = saturate(half(50.0) * brdfData.specular.g);
    half3 c = indirectDiffuse * brdfData.diffuse;
    c += indirectSpecular * (brdfData.specular * ab.x + F90 * ab.y);
    return c;
}

half3 GlobalIllumination_UE(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV,
    half envReflectIntensity, half envReflectIntensityForSpec)
{
    half3 indirectDiffuse = bakedGI;
    half3 indirectSpecular = half3(0, 0, 0);

    half3 color = EnvironmentBRDF_UE_HDRPCompat(brdfData, indirectDiffuse, indirectSpecular, normalWS, viewDirectionWS);
    return color * occlusion;
}

half3 GlobalIllumination_UE(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    half3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV)
{
    return GlobalIllumination_UE(brdfData, brdfDataClearCoat, clearCoatMask,
        bakedGI, occlusion, positionWS, normalWS, viewDirectionWS, normalizedScreenSpaceUV,
        1.0h, 1.0h);
}

half3 GlobalIllumination_UE(BRDFData brdfData, half3 bakedGI, half occlusion, float3 positionWS, half3 normalWS, half3 viewDirectionWS)
{
    const BRDFData noClearCoat = (BRDFData)0;
    return GlobalIllumination_UE(brdfData, noClearCoat, 0.0, bakedGI, occlusion, positionWS, normalWS, viewDirectionWS, float2(0, 0), 1.0h, 1.0h);
}

// ToonPBR_FOVFix.hlsl 依赖 VertexPositionInputs 和 _FOV_PivotWS。由于 HDRP 下
// _FOV_PivotWS 是 C# 传入的绝对世界空间、而我们 shader 内部用 RWS，两者需要做
// AWS→RWS 转换。先不处理这个矫正（让 FOV-Fix 失效）

void ApplyCharacterFOVFixInPlace(inout VertexPositionInputs vertexInput)
{
    //no-op
}

VertexPositionInputs GetVertexPositionInputsWithFOVFix(float3 positionOS)
{
    return GetVertexPositionInputs(positionOS);
}

float4 TransformObjectToHClipWithFOVFix(float3 positionOS)
{
    return GetVertexPositionInputs(positionOS).positionCS;
}

float4 TransformWorldToShadowCoord(float3 positionWS)
{
    return float4(0, 0, 0, 0);
}


#define SAMPLE_GI(lightmapUV, vertexSH, normalWS) CharacterSampleSH(normalWS)

#endif // TOONPBR_HDRP_URPCOMPAT
