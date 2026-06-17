//author:calvin
//date:26/6/17
//description:
//            1.sss直接散射 
//            2.间接散射（球谐）
//            3.阴影 
//            4.表面散射


#ifndef _SUBSURFACELIGHTING_HLSL
#define _SUBSURFACELIGHTING_HLSL


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#define HALF_MIN 6.103515625e-5


// haven't defined param: _ThickOffset , sufsurfaceLighting.Thickness

float _ThickOffset;
float3 _TransmissionTint;
float2 _Knight_ThicknessRemap;
float3 _ShapeParams;


float _Smoothness;
float _Fresnel0;


struct DirectSufsurfaceLighting
{
    float3 Albedo;
    float Thickness;
    float3 NormalWS;
    float3 PositionWS;
    float3 viewDir;
    float2 uv;
};

float3 ComputeTransmittanceProfile(float thickness, float3 S)
{
    float3 transmittance = exp(-thickness * S);
    return transmittance * _TransmissionTint;
}

real F_Schlick(real f0, real f90, real u)
{
    real x = 1.0 - u;
    real x2 = x * x;
    real x5 = x * x2 * x2;
    return (f90 - f0) * x5 + f0;
}

real F_Schlick(real f0, real u)
{
    return F_Schlick(f0, 1.0, u);
}

void DirectLightSSS(DirectSufsurfaceLighting sufsurfaceLighting, out float3 DirectDiffuse)
{
    float3 normal = sufsurfaceLighting.NormalWS;
    DirectionalLightData lightData = _DirectionalLightDatas[0];
    float3 lightDir = normalize(-lightData.forward.xyz);
    float NDL = dot(normal, lightDir);


    half TransmitFactor = saturate((NDL * _ThickOffset + 0.5));
    // float3 backColor = (1 - TransmitFactor) * sufsurfaceLighting.Albedo * _TransmissionTint;

    float Thickness = max(0, sufsurfaceLighting.Thickness);
    Thickness = 1;
    // half4 colorGama = IsGammaSpace() ? half4(0.5019608, 0.5019608, 1, 0) : half4(0.2158605, 0.2158605, 1, 0);

    float t = lerp(_Knight_ThicknessRemap.x, _Knight_ThicknessRemap.y, TransmitFactor);
    //计算公式
    // float test = ComputeTransmittanceProfile(t, _ShapeParams);

    float3 Transmist = sufsurfaceLighting.Albedo * saturate(
            Thickness * ComputeTransmittanceProfile(t, _ShapeParams) * (NDL * 0.85 + 0.27)) +
        saturate(Thickness * ComputeTransmittanceProfile(t, _ShapeParams) * (1 - TransmitFactor));

  
    
    //===================fresnel (模仿光滑表面)============================

    float3 viewXDirWS = normalize(mul(_InvViewMatrix, float4(0, 0, 0,1)).xyz - sufsurfaceLighting.PositionRWS);

    float NDV = dot(viewXDirWS, sufsurfaceLighting.NormalWS);
    // NDV =(NDV +1)*.5;
    half3 fresnelTerm = F_Schlick(_Fresnel0, NDV);
    fresnelTerm *= _Smoothness;
    fresnelTerm = 1 - fresnelTerm;

    float3 final = Transmist * fresnelTerm + (1 - fresnelTerm);
    
    DirectDiffuse =float3(1-fresnelTerm) ;
}


float3 DecodeSH(float3 normalWS)
{
    float3 irradiance = float3(0.0, 0.0, 0.0);

    #if defined(LIGHTMAP_ON)
    #if defined(DIRLIGHTMAP_COMBINED)
    // 方向性 Lightmap：结果受法线影响，更准确
    irradiance = SampleDirectionalLightmap(
        TEXTURE2D_LIGHTMAP_ARGS(unity_Lightmap, samplerunity_Lightmap),
        TEXTURE2D_LIGHTMAP_ARGS(unity_LightmapInd, samplerunity_LightmapInd),
        lightmapUV,
        float4(1.0, 1.0, 0.0, 0.0), // ST 已在顶点处理，传 identity
        normalWS,
        false, // HDRP Lightmap 是 Full HDR，不是 RGBM
        float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
    );
    #else
    // 非方向性 Lightmap：不受法线影响
    irradiance = SampleSingleLightmap(
        TEXTURE2D_LIGHTMAP_ARGS(unity_Lightmap, samplerunity_Lightmap),
        lightmapUV,
        float4(1.0, 1.0, 0.0, 0.0),
        false,
        float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
    );
    #endif
    #else

    float4 n = float4(normalWS, 1.0);

    // L0 + L1
    irradiance.r = dot(unity_SHAr, n);
    irradiance.g = dot(unity_SHAg, n);
    irradiance.b = dot(unity_SHAb, n);

    // L2
    float3 n2 = normalWS * normalWS;
    float4 b = float4(
        normalWS.x * normalWS.y,
        normalWS.y * normalWS.z,
        n2.z,
        normalWS.x * normalWS.z
    );
    irradiance.r += dot(unity_SHBr, b);
    irradiance.g += dot(unity_SHBg, b);
    irradiance.b += dot(unity_SHBb, b);

    // L2 ZZ 项
    float c = n2.x - n2.y;
    irradiance += unity_SHC.rgb * c;

    irradiance = max(float3(0.0, 0.0, 0.0), irradiance);
    #endif

    return irradiance;
}


void irradianceSSS(float3 normalWS, out float3 Diffuse)
{
    //test
    float3 irradiance = DecodeSH(normalWS);
    irradiance *= GetCurrentExposureMultiplier();
    Diffuse = irradiance;
}

#endif
