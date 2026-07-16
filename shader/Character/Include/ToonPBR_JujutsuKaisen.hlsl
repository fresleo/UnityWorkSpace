#ifndef __TOON_PBR_JUJUTSUKAISEN__
#define __TOON_PBR_JUJUTSUKAISEN__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


// 漫反射偏移
struct DiffuseOffset
{
    // half nDotL;
    // half nDotV;
    half lightmap;
    half lightMode;
    half occlusion;

    //漫反射
    half shadowColorDiffuse;
    half diffuseFeather;
    half darkFeather;
    half lightFeather;

    // 菲涅尔阈值 (最大最小值)
    half2 fresnelThreshold;

    // 混合的颜色
    half3 color;
    half3 shadowColor;
    half3 darkColor;
    half3 lightColor;
};

// 获取平滑渐变效果
half GetSmoothLerp(half mask, half threshold, half feather)
{
    return 1 - smoothstep(threshold - feather, threshold, 1 - mask);
}

// 计算漫反射偏移并插值颜色
half3 CalculateDiffuseOffsetColor(DiffuseOffset i, half3 normal, half3 lightDir, half3 viewDir)
{
    float3 halfDir = normalize(lightDir + viewDir);

    half nDotL = dot(normal, lightDir);
    half nDotV = dot(normal, viewDir);
    half nDotH = dot(normal, halfDir);
    

    // 菲涅尔遮罩
    half vDotUp = 1 - abs(dot(viewDir, half3(0, 1, 0)));
    half fresnelThreshold = lerp(i.fresnelThreshold.x, i.fresnelThreshold.y, vDotUp);
    half fresnel = smoothstep(1 - fresnelThreshold * 0.999, 1, (1 - nDotV) * nDotL);
    half fresnelMask = smoothstep(0, 0.001 + i.lightFeather, fresnel);


    // 渐变阴影高光遮罩
    half s_nDotL = saturate(nDotL);
    half diffuseOffset = i.lightmap * 2 - 1;
    half diffuseMask = smoothstep(-0.001, i.diffuseFeather, nDotL);
    half diffuseOffsetDark = saturate(-diffuseOffset);
    half lightMode = lerp(s_nDotL, saturate(nDotH * nDotH * nDotH * nDotH * diffuseMask + fresnel), i.lightMode);

    half darkMask = GetSmoothLerp(diffuseOffsetDark, saturate(-nDotL), i.darkFeather);
    half shadowMask = GetSmoothLerp(diffuseOffsetDark, 1 - s_nDotL, i.diffuseFeather);
    half lightMask = GetSmoothLerp(saturate(diffuseOffset + fresnelMask * i.occlusion), lightMode, i.lightFeather);

    half3 shadowColor = lerp(i.shadowColor, i.color, saturate((nDotL + 0.5) * 2) * i.shadowColorDiffuse);
    half diffuse = min(1 - shadowMask, diffuseMask);

    
    // 混合颜色
    half3 result = lerp(shadowColor, i.color, diffuse);
          result = lerp(result, i.darkColor, darkMask);
          result = lerp(result, i.lightColor, lightMask);
          result *= i.occlusion;

    return result;
}






#endif