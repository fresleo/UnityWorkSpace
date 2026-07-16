#ifndef ___XKTVFXICECORE___
#define ___XKTVFXICECORE___

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


// 平滑度转粗糙度
float RoughnessToSmoothness(float roughness)
{
    return 1 - sqrt(roughness);
}

// 平滑度转粗糙度
float SmoothnessToRoughness(float smoothness)
{
    half perceptualRoughness = 1 - saturate(smoothness);
    return perceptualRoughness * perceptualRoughness;
}

// 视差贴图uv
float2 ParallaxUV(float2 uv, float3 viewDirTS, float depth)
{
    float z = max(abs(viewDirTS.z), 1e-4);
    float2 parallaxOffset = viewDirTS.xy * depth / z;
    return uv - parallaxOffset;
}

// 获取MatcapUV
float2 GetMatcapUV(float3 normal)
{
    float3 viewNormal = normalize(mul(UNITY_MATRIX_V, float4(normal, 0.0)).xyz);
    return viewNormal.xy * 0.5 + 0.5;
}

// 计算Mip等级
half CalculateMipLevel(float smoothness)
{
    half perceptualRoughness = 1 - smoothness;
    half mip = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness) * 7;
    return floor(mip);
}

//Cubemap采样
half3 Sample_ReflectProbeCubeMap(TextureCube cube, sampler s_cube, half4 hdr, float3 uv)
{
    half4 cubeColor = SAMPLE_TEXTURECUBE(cube, s_cube, uv);
    half3 decode = DecodeHDREnvironment(cubeColor, hdr);
    return decode;
}

//Cubemap采样
half3 Sample_ReflectProbeCubeMap(TextureCube cube, sampler s_cube, half4 hdr, float3 uv, float mip)
{
    half4 cubeColor = SAMPLE_TEXTURECUBE_LOD(cube, s_cube, uv, mip);
    half3 decode = DecodeHDREnvironment(cubeColor, hdr);
    return decode;
}

//BRDF高光(PBR高光部分)
half BRDF_Specular(float roughness, half nDotH, float lDotH)
{
    nDotH = saturate(nDotH);
    lDotH = saturate(lDotH);

    float roughness2 = max(0.0001, roughness * roughness);
    float d = nDotH * nDotH * (roughness2 - 1) + 1.00001f;
    float normalizationTerm  = roughness * 4.0h + 2.0h;
    half lDotH2 = lDotH * lDotH;
    half specularTerm = roughness2 / ((d * d) * max(0.1h, lDotH2) * normalizationTerm);

    specularTerm = clamp(specularTerm - (1e-5), 0.0, 100.0);

    return specularTerm;
}

//计算漫反射通用变量
float CalculateDiffuse(float nDotL, float offset, float feather)
{
    float index = nDotL * 0.5 + 0.5;
    float diffuse = smoothstep(offset - 0.001, offset + feather, index);
    return diffuse;
}

//环境反射效果
half3 EnvironmentBRDF(float3 diffuseColor, float3 specularColor, half3 ambient, half3 indirectSpecular, half metallic, half smoothness, 
    half roughness, half nDotV)
{
    float surfaceReduction = 1.0 / (roughness * roughness + 1.0);
    float grazingTerm = saturate(smoothness + 1 - (0.96 - metallic * 0.96));
    float fresnelTerm = 1;
    {
        fresnelTerm = 1 - nDotV;
        fresnelTerm = fresnelTerm * fresnelTerm * fresnelTerm * fresnelTerm;
    }
    half3 c = 1;
    {
        c = diffuseColor * ambient;
        c += indirectSpecular * surfaceReduction * lerp(specularColor, grazingTerm, fresnelTerm);
    }
    return c;
}


//反射盒0的颜色采样
half3 Box0ReflectColor(half3 vrDirWS, float smoothness)
{
    half mip = CalculateMipLevel(smoothness);

    //基于反射偏移获取ReflectProbe0的颜色
    half3 box0 = Sample_ReflectProbeCubeMap(unity_SpecCube0, samplerunity_SpecCube0, unity_SpecCube0_HDR, vrDirWS, mip);
    return box0;
}

//反射盒0的颜色采样,带偏移
half3 Box0ReflectColor(float3 vertexWS, half3 vrDirWS, float smoothness)
{
    vrDirWS = BoxProjectedCubemapDirection(vrDirWS, vertexWS, unity_SpecCube0_ProbePosition,  unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    half3 box0 = Box0ReflectColor(vrDirWS, smoothness);
    return box0;
}

// 菲涅尔边缘光
float FresnelRimLight(float nDotV, float offset, float feather, float reverse)
{
    nDotV = saturate(nDotV);
    feather = max(0.01, feather);
    
    float fresnel = smoothstep(offset, offset + feather, nDotV);
    fresnel = lerp(1 - fresnel, fresnel, reverse);

    return saturate(fresnel);
}

void ApplyFresnel(inout half4 mainColor, half fresnel, half4 fresnelColor, half scale, half power)
{
    fresnel = scale * pow(max(fresnel, 0.001), power);
    half4 fresnelNode = fresnel * fresnelColor;
    mainColor.rgb += fresnelNode.rgb;
}



#endif