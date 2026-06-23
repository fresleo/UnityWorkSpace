//author:calvin
//date:26/6/17
//description:
//            1.sss直接散射 
//            2.间接散射（球谐）
//            3.阴影 
//            4.表面散射

// haven't defined param: _ThickOffset , sufsurfaceLighting.Thickness

#ifndef _SUBSURFACELIGHTING_HLSL
#define _SUBSURFACELIGHTING_HLSL

// 显式指定阴影分级，防止打包时算法未定义
#ifndef SHADOW_HIGH
#define SHADOW_HIGH 1
#endif

#ifndef PUNCTUAL_SHADOW_HIGH
#define PUNCTUAL_SHADOW_HIGH 1
#endif

#ifndef DIRECTIONAL_SHADOW_HIGH
#define DIRECTIONAL_SHADOW_HIGH 1
#endif

#ifndef AREA_SHADOW_HIGH
#define AREA_SHADOW_HIGH 1
#endif

#define HALF_MIN 6.103515625e-5


// #pragma multi_compile SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"


float _ThickOffset;
float3 _TransmissionTint;
float2 _Knight_ThicknessRemap;
float4 _ShapeParams;

TEXTURE2D(_TempSSSDiscKernel);
float _Smoothness;
float _Fresnel0;


struct DirectSufsurfaceLighting
{
    float3 Albedo;
    float Thickness;
    float3 NormalWS;
    float3 PositionWS;
    float3 PositionRWS;
    float4 positionCS;
    float3 viewDir;
    float2 uv;
};

float3 ComputeTransmittanceProfile(float thickness, float3 S)
{
    float3 transmittance = max(exp(-thickness * S), 0.001);
    return transmittance * _TransmissionTint;
}

float3 SampleTransmitTexture(float map_01)
{
    // float3 S = float3(_ShapeParams.w * rcp(_ShapeParams.x), _ShapeParams.w * rcp(_ShapeParams.y),
    //                   _ShapeParams.w * rcp(_ShapeParams.z)); //d/s
    float3 S = float3(_ShapeParams.x, _ShapeParams.y, _ShapeParams.z);
    float t = lerp(_Knight_ThicknessRemap.x, _Knight_ThicknessRemap.y, saturate(map_01));
    return ComputeTransmittanceProfile(t, S);
}


float GetShadowAttenuation(DirectSufsurfaceLighting posInput)
{
    DirectionalLightData lightData = _DirectionalLightDatas[0];
    float shadowAttenuation = 1.0;

    if (lightData.shadowIndex < 0)
        return shadowAttenuation;

    float3 L = -lightData.forward.xyz;

    #if defined(SCREEN_SPACE_SHADOWS_ON)
    if ((lightData.screenSpaceShadowIndex & SCREEN_SPACE_SHADOW_INDEX_MASK) != INVALID_SCREEN_SPACE_SHADOW)
    {
        // shadowAttenuation = GetScreenSpaceShadow(posInput, lightData.screenSpaceShadowIndex);
    }
    else
    #endif
    {
        HDShadowContext shadowContext = InitShadowContext();
        shadowAttenuation = GetDirectionalShadowAttenuation(
            shadowContext,
            posInput.positionCS,
            posInput.PositionRWS,
            posInput.NormalWS,
            lightData.shadowIndex,
            L);
    }

    return shadowAttenuation;
}

void DirectLightSSS(DirectSufsurfaceLighting sufsurfaceLighting, out float3 DirectDiffuse, out float ShadowMask)
{
    float3 normal = sufsurfaceLighting.NormalWS;
    DirectionalLightData lightData = _DirectionalLightDatas[0];
    float3 lightDir = normalize(-lightData.forward.xyz);
    float NDL = dot(normal, lightDir);


    // float3 backColor = (1 - TransmitFactor) * sufsurfaceLighting.Albedo * _TransmissionTint;

    float Thickness = max(0, sufsurfaceLighting.Thickness);
    // half4 colorGama = IsGammaSpace() ? half4(0.5019608, 0.5019608, 1, 0) : half4(0.2158605, 0.2158605, 1, 0);

    //计算公式
    // float test = ComputeTransmittanceProfile(t, _ShapeParams);

    //========================shadow start ===========================


    float Shadow = GetShadowAttenuation(sufsurfaceLighting) * saturate(NDL);

    //=======================shadow end========================
    float halfNDL = (NDL + 1) * 0.5;

    half TransmitFactor = pow(halfNDL, abs(_ThickOffset));


    half ThichnessFactor = pow(Thickness, abs(_ThickOffset));

    float3 BackColor = SampleTransmitTexture(TransmitFactor) * (1 - ThichnessFactor);

    // float3 BackColor1 = SampleTransmitTexture(NDL * _ThickOffset + 0.5);

    float3 SSSColor = (Shadow + BackColor);
    //===================fresnel (模仿光滑表面)============================

    // float3 viewXDirWS = normalize(mul(_InvViewMatrix, float4(0, 0, 0,1)).xyz - sufsurfaceLighting.PositionRWS);
    float4 virtualViewDirVS = float4(.02, -0.02, 1.0, 0.0);
    float3 viewXDirWS = normalize(mul(_InvViewMatrix, virtualViewDirVS).xyz);
    float NDV = saturate(dot(viewXDirWS, sufsurfaceLighting.NormalWS));

    // NDV =(NDV +1)*.5;
    half3 fresnelTerm = saturate(F_Schlick(_Fresnel0, NDV)) * _Smoothness;

    float distanceToCamera = length(sufsurfaceLighting.PositionRWS);

    float distanceFade = smoothstep(0, 3.0, distanceToCamera);

    fresnelTerm *= distanceFade;

    // float3 final = SSSColor * (1 - fresnelTerm) + fresnelTerm;
    // float3 final = lerp(SSSColor,float3(1,1,1),fresnelTerm);

    float3 final = SSSColor * (1 - fresnelTerm) + (fresnelTerm);

    // ShadowMask = saturate(-((GetShadowAttenuation(sufsurfaceLighting) - 1) + NDL));
    ShadowMask = step(saturate(GetShadowAttenuation(sufsurfaceLighting) * NDL),0.25);
    // float Test = step(saturate(GetShadowAttenuation(sufsurfaceLighting) * NDL),0.25);
    // final = float3(Test, Test, Test);
    DirectDiffuse = float3(final);
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
