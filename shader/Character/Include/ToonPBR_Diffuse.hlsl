#ifndef TOONPBR_DIFFUSE
#define TOONPBR_DIFFUSE

#include "ToonPBR_JujutsuKaisen.hlsl"
#include "Packages\com.garena.ta.perobjectshadow@1.0.0\Shaders\GPerObjectShadow.hlsl"

// 对灯光衰减做卡通化平滑
float SmoothLightAttenuation(float lightAttenuation, float feather)
{
    float result = lerp(StepAntiAliasing(lightAttenuation, 0.5), lightAttenuation, feather);
    result = saturate(result);
    return result;
}

// 读取材质 _ShadowmapReceiveWeight，控制角色接收场景 Shadowmap 的强度。
// 无 TOONPBR_HAS_SHADOWMAP_RECEIVE_WEIGHT 宏时恒为 1（完全接收）。
float GetShadowmapReceiveWeight()
{
    float shadowmapReceiveWeight = 1.0;
    
    #ifdef TOONPBR_HAS_SHADOWMAP_RECEIVE_WEIGHT
    shadowmapReceiveWeight = saturate(_ShadowmapReceiveWeight);
    #endif

    return shadowmapReceiveWeight;
}

// 根据场景阴影衰减计算固有色乘子，用于 Shadowmap 投影染色（非直接输出阴影色）。
half3 GetShadowmapTintMultiplier(float shadowAttenuation)
{
    #ifdef TOONPBR_HAS_SHADOWMAP_RECEIVE_WEIGHT
    float tintWeight = (1.0 - saturate(shadowAttenuation)) * GetShadowmapReceiveWeight();
    return lerp((half3)1.0h, _ShadowmapTintColor.rgb, (half)tintWeight);
    
    #else
    return (half3)1.0h;
    #endif
}

// 采样主光级联 Shadowmap
float GetSceneShadowAttenuation(InputDataToon inputData)
{
    float sceneShadow = MainLightShadow(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask, _MainLightOcclusionProbes);
    return sceneShadow;
}

// 计算二阶卡通漫反射的亮暗辐射度（半兰伯特 + 步进羽化）
// radiance.x: 一阶亮暗，含 SmoothLightAttenuation
// radiance.y: 二阶亮暗，不含灯光衰减
half2 RadianceToon(
    half3 normalWS, half3 lightDirectionWS, float lightAttenuation
    , half shadow1Step, half shadow1Feather
    , half shadow2Step, half shadow2Feather)
{
    lightAttenuation = SmoothLightAttenuation(lightAttenuation, shadow1Feather);
    
    half H_Lambert = dot(normalWS, lightDirectionWS) * 0.5 + 0.5;

    half2 radiance;
    radiance.x = StepFeatherToon(H_Lambert, shadow1Step, shadow1Feather) * lightAttenuation;
    radiance.y = StepFeatherToon(H_Lambert, shadow2Step, shadow2Feather);
    return radiance;
}

// 二阶阴影染色
// radiance.x: 控制整体回到固有色
// radiance.y: 在一阶/二阶阴影色间切换
half3 DoubleShadowToon(half3 shadow1, half3 shadow2, half3 baseColor, half2 radiance, half tintStrength)
{
    half3 darkReplace = lerp(shadow2, shadow1, radiance.y);
    half3 darkMultiply = lerp(baseColor * shadow2, baseColor * shadow1, radiance.y);
    half3 dark = lerp(darkMultiply, darkReplace, saturate(tintStrength));
    return lerp(dark, baseColor, radiance.x);
}

// 默认二阶阴影漫反射路径（无 SDF / 175 / Ramp 等 keyword 时走此分支）
half3 CalculateDoubleShadeDiffuse(
    half3 normalWS, half3 lightDirectionWS, float lightAttenuation
    , ToonData toonData, BRDFDataToon brdfData
    , out float diffuseLightMask)
{
    half2 radiance = RadianceToon(
        normalWS, lightDirectionWS, lightAttenuation,
        toonData.shadow1Step, toonData.shadow1Feather,
        toonData.shadow2Step, toonData.shadow2Feather);
    
    diffuseLightMask = radiance.x;
    
    return DoubleShadowToon(toonData.shadow1, toonData.shadow2, brdfData.diffuse, radiance,toonData.doubleShadeTintStrength);
}

// 读取 Shadow175 分档标记 mark175，用于 0.75 / 0.25 阈值切换叠层深度。
half GetShadow175Mark(SurfaceDataToon surfaceData, InputDataToon inputData, ToonData toonData)
{
    half mark175 = 0;
    int source175Shadow = (int)toonData.source175Shadow;
    if (source175Shadow == 1)
    {
        mark175 = surfaceData.toonShadowMask;
    }
    else
    {
        mark175 = inputData.vertexColor.r;
    }
    return mark175;
}

// 计算 175 法线方向羽化亮暗遮罩（不含 mark175 贴图分档）。
// 合成: CellShadingDiffuse(半兰伯特) × 灯光衰减羽化 × 逐对象实时阴影。
half GetShadow175FeatherLitMask(
    InputDataToon inputData, ToonData toonData
    , half3 normalWS, half3 lightDirectionWS, float lightAttenuation)
{
    half selfShadow = PerObjectCharacterRealtimeShadow(inputData.positionWS);
    float sla = SmoothLightAttenuation(lightAttenuation, toonData.shadow1Feather);
    float H_Lambert = dot(normalWS, lightDirectionWS) * 0.5 + 0.5;
    return CellShadingDiffuse(H_Lambert, toonData.shadow1Step, toonData.shadow1Feather) * sla * selfShadow;
}

// 在 HSV 空间按 tintBrightness 提升固有色明度，得到 175 阴影用的 Tint 基色。
half3 GetShadow175TintColor(half3 baseColor, half tintBrightness)
{
    float3 hsv = RgbToHsv(baseColor);
    hsv.b *= tintBrightness;
    return HsvToRgb(hsv);
}

// 175 与 SDF 主区共用的阴影染色核心
half3 ApplyUnifiedShadowDiffuse(
    half3 baseColor, half3 shadowColor, half3 tintColor
    , half litMask, half mark175, half tintShadowFactor)
{
    // 法线羽化底色
    half3 featherShade = lerp(tintColor * shadowColor, baseColor, saturate(litMask));

    // mark175 >= 0.75: darkColor = tintColor * tintShadowFactor
    // mark175 >= 0.25: darkColor *= baseColor
    half3 darkColor = lerp(tintColor, tintColor * tintShadowFactor, step(mark175, 0.75));
    darkColor = lerp(darkColor, darkColor * baseColor, step(mark175, 0.25));
    half3 overlayShade = darkColor * shadowColor;

    // mark175 >= 0.75 时 overlayShade 替换 featherShade，不受 litMask 约束（亮面可保留贴图结构线）
    return lerp(featherShade, overlayShade, step(mark175, 0.75));
}

// 纯 Shadow175 路径（keyword _ILM_SHADOW_MASK_ON）。
// 法线羽化 + mark175 分档叠层，阴影色使用 toonData.shadow1（_Shadow1Color）。
half3 CalculateShadow175Diffuse(
    SurfaceDataToon surfaceData, InputDataToon inputData, ToonData toonData
    , BRDFDataToon brdfData, half3 normalWS, half3 lightDirectionWS
    , float lightAttenuation, out float diffuseLightMask)
{
    half3 baseColor = brdfData.diffuse;
    half3 tintColor = GetShadow175TintColor(baseColor, toonData.tintBrightness);
    half mark175 = GetShadow175Mark(surfaceData, inputData, toonData);
    
    diffuseLightMask = GetShadow175FeatherLitMask(inputData, toonData, normalWS, lightDirectionWS, lightAttenuation);
    
    return ApplyUnifiedShadowDiffuse(
        baseColor, toonData.shadow1, tintColor, 
        diffuseLightMask, mark175, toonData.tintShadowFactor);
}

// FaceSDFShadow 合成（keyword _SDFSHADOWMAP）。
// sdfWeight（_SDFShadowMap.a）: 1=主区 A=1，0=遮罩区 A=0。
// 主区: mainLitMask = sdfMask × featherLitMask，经 ApplyUnifiedShadowDiffuse，
//       阴影色 sdfShadowColor，含 175 分档。
// 遮罩区: 仅 sdfMaskArea 控制亮暗，阴影色 sdfMaskShadowColor，不走 175。
half3 CalculateSdfCompositeDiffuse(
    SurfaceDataToon surfaceData, InputDataToon inputData, ToonData toonData
    , BRDFDataToon brdfData, half3 normalWS, half3 lightDirectionWS
    , float lightAttenuation, out float diffuseLightMask)
{
    half3 baseColor = brdfData.diffuse;
    half sdfWeight = saturate(toonData.sdfWeight);
    
    // sdfMask: 主区 SDF 亮暗；sdfMaskArea: 遮罩区 SDF 亮暗
    half sdfLitMain = saturate(toonData.sdfMask);
    half sdfLitMask = saturate(toonData.sdfMaskArea);
    
    half mark175 = GetShadow175Mark(surfaceData, inputData, toonData);
    half featherLitMask = GetShadow175FeatherLitMask(inputData, toonData, normalWS, lightDirectionWS, lightAttenuation);
    half3 tintColor = GetShadow175TintColor(baseColor, toonData.tintBrightness);
    
    // 主区: SDF 与 175 法线羽化相乘
    half mainLitMask = sdfLitMain * featherLitMask;
    half3 mainDiffuse = ApplyUnifiedShadowDiffuse(
        baseColor, toonData.sdfShadowColor, tintColor, 
        mainLitMask, mark175, toonData.tintShadowFactor);
    
    // 遮罩区: 独立颜色，不参与 mark175 分档
    half3 maskDiffuse = lerp(baseColor * toonData.sdfMaskShadowColor, baseColor, sdfLitMask);
    half3 toonDiffuseRgb = lerp(maskDiffuse, mainDiffuse, sdfWeight);
    diffuseLightMask = lerp(sdfLitMask, mainLitMask, sdfWeight);
    
    return toonDiffuseRgb;
}

// 卡通漫反射总入口，按 keyword 分发阴影模式。
// _SDFSHADOWMAP → CalculateSdfCompositeDiffuse
// _ILM_SHADOW_MASK_ON → CalculateShadow175Diffuse
// _DIFFUSE_OFFSET → 贴图 R + HSV 偏移染色
// _RAMP_MODE_ON → Ramp 纹理映射
// 默认 → CalculateDoubleShadeDiffuse
// _HAS_CEL_HAIR_SHADOW_V1: shadingModel==2（脸）时叠加头发屏幕空间投影
half3 CalculateDiffuse(
    SurfaceDataToon surfaceData, InputDataToon inputData, ToonData toonData
    , BRDFDataToon brdfData, half3 lightDirectionWS, float lightAttenuation
    , out float diffuseLightMask)
{
    diffuseLightMask = 0;
    
    half3 toonDiffuseRgb = 0;
    half3 normalWS = inputData.normalWS;

    // 面部 SDF 阴影：_SDFSHADOWMAP
    #if defined(_SDFSHADOWMAP)
    {
        toonDiffuseRgb = CalculateSdfCompositeDiffuse(
            surfaceData, inputData, toonData, brdfData,
            normalWS, lightDirectionWS, lightAttenuation, diffuseLightMask);
        
        // 这段会在暗部把高光压暗，暂时先去掉
        // half selfShadow = PerObjectCharacterRealtimeShadow(inputData.positionWS);
        // float sla = SmoothLightAttenuation(lightAttenuation, toonData.shadow1Feather);
        // diffuseLightMask = sdfMask * sla * selfShadow;
    }
    
    // 175 阴影：_ILM_SHADOW_MASK_ON
    #elif defined(_ILM_SHADOW_MASK_ON)
    {
        toonDiffuseRgb = CalculateShadow175Diffuse(
            surfaceData, inputData, toonData, brdfData,
            normalWS, lightDirectionWS, lightAttenuation, diffuseLightMask);
    }
    
    // 偏移染色：贴图 R + HSV 分档
    #elif defined(_DIFFUSE_OFFSET)
    {
        half selfShadow = PerObjectCharacterRealtimeShadow(inputData.positionWS);
        half3 BaseColor = brdfData.diffuse;

        float3 hsv = RgbToHsv(BaseColor);
        half3 TintColor = hsv;
        {
            TintColor.b *= toonData.tintBrightness;
            TintColor = HsvToRgb(TintColor);
        }
        half3 LightColor = hsv;
        {
            LightColor.gb = lerp(LightColor.gb, 1, half2(_LightColorHSV_S, _LightColorHSV_V));
            LightColor = HsvToRgb(LightColor);
        }
        
        half occlusion = min(1 - _DiffuseOcclusion + surfaceData.occlusion * _DiffuseOcclusion, selfShadow);

        DiffuseOffset diffData;

        diffData.lightmap = surfaceData.toonShadowMask;
        diffData.lightMode = _LightColorMode;
        diffData.occlusion = occlusion;

        diffData.shadowColorDiffuse = _ShadowColorDiffuse;
        diffData.diffuseFeather = _ShadowColorSmooth;
        diffData.darkFeather = _DarkColorSmooth;
        diffData.lightFeather = _LightColorSmooth;

        diffData.fresnelThreshold = half2(_LightColorRangeMin, _LightColorRangeMax);

        diffData.color = BaseColor;
        diffData.shadowColor = TintColor;
        diffData.darkColor = TintColor * toonData.tintShadowFactor;
        diffData.lightColor = LightColor;

        toonDiffuseRgb = CalculateDiffuseOffsetColor(diffData, normalWS, lightDirectionWS, inputData.viewDirectionWS);

        // 羽化结果
        float sla = SmoothLightAttenuation(lightAttenuation, toonData.shadow1Feather);
        float H_Lambert = dot(normalWS, lightDirectionWS) * 0.5 + 0.5;
        float lumi = CellShadingDiffuse(H_Lambert, toonData.shadow1Step, toonData.shadow1Feather) * sla;
        diffuseLightMask = lumi;
    }
    
    // Ramp 映射漫反射
    #elif defined(_RAMP_MODE_ON)
    {
        half selfShadow = PerObjectCharacterRealtimeShadow(inputData.positionWS);
        float nDotL = dot(normalWS, lightDirectionWS);
        float nDotLHalf = nDotL * 0.5 + 0.5;
        
        float3 rampColor = SAMPLE_TEXTURE2D(_DiffuseRampMap, sampler_DiffuseRampMap, float2(nDotLHalf, _DiffuseRampMapVertical)).rgb;
        {
            half gray = 0.2125 * rampColor.r + 0.7154 * rampColor.g + 0.0721 * rampColor.b;
            rampColor = saturate(lerp(gray, rampColor, _DiffuseRampSaturate + 1));
        }

        nDotLHalf = min(selfShadow, nDotLHalf);
        
        toonDiffuseRgb = brdfData.diffuse * _DiffuseRampColor.rgb * lerp(1, rampColor, _DiffuseRampIntensity);
        
        half diffuse = StepFeatherToon(nDotLHalf, toonData.shadow1Step, toonData.shadow1Feather);
        toonDiffuseRgb *= lerp(toonData.shadow1, 1, diffuse);
        
        float sla = SmoothLightAttenuation(lightAttenuation, 1);
        diffuseLightMask = CellShadingDiffuse(nDotLHalf, toonData.shadow1Step, toonData.shadow1Feather) * sla;
    }
    
    // 2阶阴影（默认）
    #else
    {
        toonDiffuseRgb = CalculateDoubleShadeDiffuse(
            normalWS, lightDirectionWS, lightAttenuation,
            toonData, brdfData, 
            diffuseLightMask);
    }
    #endif // 结束阴影处理

    
    // 头发在脸部的屏幕空间投影（仅 shadingModel=脸）
    #if defined(_HAS_CEL_HAIR_SHADOW_V1)
    {
        // 是脸时才处理
        if((int)surfaceData.shadingModel == 2)
        {
            float2 scrPos = inputData.screenPos.xy / inputData.screenPos.w;
            
            float ndcW = rcp(inputData.positionNDCw);
            float3 viewLightDir = normalize(TransformWorldToViewDir(lightDirectionWS)) * ndcW;
            viewLightDir += toonData.hairShadowOffset.xyz * ndcW;
            
            // 计算最终的采样点
            float scaleFactor = 0.001; // 一般不会改，所以这里选择写死
            float2 samplingPoint = scrPos + viewLightDir.xy * scaleFactor;
            float hairDepth = SAMPLE_TEXTURE2D_X(_CelHairShadowColor, sampler_CelHairShadowColor, samplingPoint).r;
            float depthContrast = hairDepth > 0 ? 0 : 1;
            
            // 头发在脸上的投影
            toonDiffuseRgb = lerp(brdfData.diffuse * toonData.hairShadowColor.rgb, toonDiffuseRgb, depthContrast);
        }
    }
    #endif
    
    
    return toonDiffuseRgb;
}

#endif // TOONPBR_DIFFUSE
