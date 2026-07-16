#ifndef TOONPBR_SPECULAR
#define TOONPBR_SPECULAR

//直接卡通高光 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
half DirectSpecularToon(BRDFDataToon brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS, half step, half feather)
{
    float3 halfDir = SafeNormalize(float3(lightDirectionWS) + float3(viewDirectionWS));
    float NoH = saturate(dot(normalWS, halfDir));
    half LoH = saturate(dot(lightDirectionWS, halfDir));
    half LoH2 = LoH * LoH;
    float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;
    half d2 = half(d * d);

    half specularTerm = brdfData.roughness2 / (d2 * max(0.1h, LoH2) * brdfData.normalizationTerm);
    half normalizeSpec = brdfData.roughness2 * brdfData.roughness2 * rcp(d2);

    specularTerm *= StepFeatherToon(normalizeSpec, step, feather);
    return specularTerm;
}

//卡基亚各向异性高光 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
struct AnisoLightingData
{
    half3 lightColor;
    half3 HalfDir;
    half3 lightDir;
    half NdotL;
    half NdotLClamp;
    half HalfLambert;
    half NdotVClamp;
    half NdotHClamp;
    half LdotHClamp;
    half VdotHClamp;
    half ShadowAttenuation;
};

struct AnisoSpecularData
{
    half3 specularColor;
    half3 specularSecondaryColor;
    half specularShift;
    half specularSecondaryShift;
    half specularStrength;
    half specularSecondaryStrength;
    half specularExponent;
    half specularSecondaryExponent;
    half spread1;
    half spread2;
};

AnisoLightingData InitializeAnisoLightingData(Light mainLight, half3 normalWS, half3 viewDirectionWS)
{
    AnisoLightingData lightData;
    lightData.lightColor = mainLight.color;

    lightData.NdotL = dot(normalWS, mainLight.direction.xyz);

    lightData.NdotLClamp = saturate(lightData.NdotL);
    lightData.HalfLambert = lightData.NdotL * 0.5 + 0.5;
    half3 halfDir = SafeNormalize(mainLight.direction + viewDirectionWS);
    lightData.LdotHClamp = saturate(dot(mainLight.direction.xyz, halfDir.xyz));
    lightData.NdotHClamp = saturate(dot(normalWS.xyz, halfDir.xyz));
    lightData.NdotVClamp = saturate(dot(normalWS.xyz, viewDirectionWS.xyz));
    lightData.HalfDir = halfDir;
    lightData.lightDir = mainLight.direction;

    #if defined(_RECEIVE_SHADOWS_OFF)
    lightData.ShadowAttenuation = 1;
    #else
    lightData.ShadowAttenuation = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    #endif

    return lightData;
}

inline void InitAnisoSpecularData(out AnisoSpecularData anisoSpecularData)
{
    #ifdef _KAJIYAHAIR
    
    anisoSpecularData.specularColor = _AnisoSpecularColor.rgb;
    anisoSpecularData.specularSecondaryColor = _AnisoSecondarySpecularColor.rgb;
    anisoSpecularData.specularShift = _AnsioSpeularShift;
    anisoSpecularData.specularSecondaryShift = _AnsioSecondarySpeularShift;
    anisoSpecularData.specularStrength = _AnsioSpeularStrength;
    anisoSpecularData.specularSecondaryStrength = _AnsioSecondarySpeularStrength;
    anisoSpecularData.specularExponent = _AnsioSpeularExponent;
    anisoSpecularData.specularSecondaryExponent = _AnsioSecondarySpeularExponent;
    anisoSpecularData.spread1 = _AnisoSpread1;
    anisoSpecularData.spread2 = _AnisoSpread2;

    #endif
}

inline half3 AnisotropyDoubleSpecular(
    half2 uv, half4 tangentWS, 
    BRDFDataToon brdfData, InputDataToon inputData, AnisoLightingData lightingData, AnisoSpecularData anisoSpecularData, 
    TEXTURE2D_PARAM(anisoDetailMap, sampler_anisoDetailMap))
{
    // TODO ADD Mask
    half specMask = 1;
    half4 detailNormal = SAMPLE_TEXTURE2D(anisoDetailMap, sampler_anisoDetailMap, uv);

    float2 jitter = (detailNormal.y - 0.5) * float2(anisoSpecularData.spread1, anisoSpecularData.spread2);

    float sgn = tangentWS.w;
    float3 T = normalize(sgn * cross(inputData.normalWS.xyz, tangentWS.xyz));
    //float3 T = normalize(tangentWS.xyz);

    float3 t1 = ShiftTangent(T, inputData.normalWS.xyz, anisoSpecularData.specularShift + jitter.x);
    float3 t2 = ShiftTangent(T, inputData.normalWS.xyz, anisoSpecularData.specularSecondaryShift + jitter.y);

    float3 hairSpec1 = anisoSpecularData.specularColor * anisoSpecularData.specularStrength *
        D_KajiyaKay(t1, lightingData.HalfDir, anisoSpecularData.specularExponent);
    float3 hairSpec2 = anisoSpecularData.specularSecondaryColor * anisoSpecularData.specularSecondaryStrength *
        D_KajiyaKay(t2, lightingData.HalfDir, anisoSpecularData.specularSecondaryExponent);

    float3 F = F_Schlick(half3(0.2, 0.2, 0.2), lightingData.LdotHClamp);
    half3 anisoSpecularColor = 0.25 * F * (hairSpec1 + hairSpec2) * lightingData.NdotLClamp * specMask * brdfData.specular;
    return anisoSpecularColor;
}


//计算高光
half3 CalculateSpecular(BRDFDataToon brdfData, InputDataToon inputData, Light light, ToonData toonData)
{
    half3 specularColor = 0;
    
    #if _KAJIYAHAIR

    float2 uv = inputData.texcoord * _BaseMap_ST.xy + _BaseMap_ST.zw;
    half2 anisoUV = half2(uv.x * _AnisoShiftScaleX, uv.y * _AnisoShiftScaleY);
    
    AnisoLightingData lightingData = InitializeAnisoLightingData(light, inputData.normalWS, inputData.viewDirectionWS);
    
    AnisoSpecularData anisoSpecularData;
    InitAnisoSpecularData(anisoSpecularData);
    
    specularColor = AnisotropyDoubleSpecular(anisoUV, inputData.tangentWS,
        brdfData, inputData, lightingData, anisoSpecularData,
        TEXTURE2D_ARGS(_AnisoShiftMap, sampler_AnisoShiftMap));
    
    #else //直接卡通高光
    
    half3 normalWS = inputData.normalWS;
    half3 viewDirectionWS = inputData.viewDirectionWS;
    
    half3 lightDirectionWS = light.direction;
    
    half specularStep = toonData.specularStep;
    half specularFeather = toonData.specularFeather;
    
    half specularTerm = DirectSpecularToon(brdfData, normalWS, lightDirectionWS, viewDirectionWS, specularStep, specularFeather);
    specularTerm = clamp(specularTerm - HALF_MIN, 0.0, 100.0); // Prevent FP16 overflow on mobiles
    specularColor = specularTerm * brdfData.specular;
    
    // 不适合当前的画风，算法也不太完善（没和角色全局的高光分离开），所以注掉节省性能
    // 从 sdf.g 中获取鼻子的高光 mask
    // #if defined(_SDFSHADOWMAP)
    // half noseSdfSpecular = GetNoseSdfSpecular(lightDirectionWS, toonData.sdfFaceU, toonData.noseSdfG);
    // specularColor += noseSdfSpecular * brdfData.specular;
    // #endif
    
    #endif

    // 为高光添加一层泛光
    float3 halfDir = normalize(light.direction + inputData.viewDirectionWS);
    half nDotH = dot(inputData.normalWS, halfDir);
    half lowSpecular = saturate(nDotH * nDotH * nDotH * nDotH) * toonData.floodlightIntensity * 0.1;
    specularColor += lowSpecular * light.color;

    return specularColor;
}

#endif //TOONPBR_SPECULAR
