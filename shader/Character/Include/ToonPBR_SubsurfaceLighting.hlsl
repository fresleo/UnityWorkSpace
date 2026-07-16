//author:calvin
//date:26/6/17
//description:
//            1.sss直接散射 
//            2.间接散射
//            3.阴影 
//            4.表面散射

// doesn't have defined param: _ThickOffset , sufsurfaceLighting.Thickness

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

#define __XKNIGHT_SHADOWS__ 1

// #pragma multi_compile SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"

#include "./ToonPBR_Input.hlsl"
#include "./ToonPBR_VertexPull.hlsl"

#include "./ToonPBR_InputData.hlsl"
#include "./ToonPBR_Core.hlsl"
#include "./ToonPBR_Specular.hlsl"   
#include "./ToonPBR_Diffuse.hlsl"
#include "./ToonPBR_Rim.hlsl"        
#include "./ToonPBR_Fresnel.hlsl"    
#include "./ToonPBR_Eye.hlsl"       
#include "./ToonPBR_Lighting.hlsl"
#include "./ToonPBR_Forward.hlsl"


struct SSSData
{
    float4 _ShapeParamsAndFreePath;
    float4 _TransmissionTintAndFresnel;
    float4 _WorldScaleAndMaxRadiusAndThicknessRemaps;
    uint4 HashAndShadowStrenthAndThicknessOffset;
};

GLOBAL_CBUFFER_START(ShaderVariableDiffusionParams, b2)
    float4 _ShapeParamsAndFreePath[16];
    float4 _TransmissionTintAndFresnel[16];
    float4 _WorldScaleAndMaxRadiusAndThicknessRemaps[16];
    uint4 _HashAndShadowStrenthAndThicknessOffset[16]; //这里用uint否则丢失精度，x：hash y：shadowstrenth  z:thicknessoffset
    uint _DiffusionParametersCount;
    uint _pad0, _pad1, _pad2;
CBUFFER_END


// ================xknight==========


struct DirectSufsurfaceLighting
{
    float3 Albedo;
    float Thickness;
    float SSSMask;
    float TransmissionMask;
    float3 NormalWS;
    float3 PositionWS;
    float3 PositionRWS;
    float4 positionCS;
    float4 TangentWS;
    float3 viewDir;
    float2 uv;
};

float3 ComputeTransmittanceProfile(float thickness, float3 S, float3 _TransmissionTint)
{
    float3 transmittance = max(exp(-thickness * S), 0.001);
    return transmittance * _TransmissionTint;
}

SSSData DecodeFromSSSBuffer(uint MaterialIndex)
{
    SSSData sssData;
    sssData._ShapeParamsAndFreePath = _ShapeParamsAndFreePath[MaterialIndex];
    sssData._TransmissionTintAndFresnel = _TransmissionTintAndFresnel[MaterialIndex];
    sssData._WorldScaleAndMaxRadiusAndThicknessRemaps = _WorldScaleAndMaxRadiusAndThicknessRemaps[MaterialIndex];
    sssData.HashAndShadowStrenthAndThicknessOffset = _HashAndShadowStrenthAndThicknessOffset[MaterialIndex];
    return sssData;
}

void EncodeIntoSSSBuffer(DirectSufsurfaceLighting sssData, out float4 outSSSBuffer, uint diffusionParameterIndex,
                         real SSSMask)
{
    outSSSBuffer = float4(sssData.Albedo,
                          PackFloatInt8bit(SSSMask, diffusionParameterIndex, 16));
}

uint FindDiffusionParametersIndex(uint diffusionParametersHash)
{
    if (diffusionParametersHash == 0)
        return 0;

    uint diffusionParametersIndex = 0;
    uint i = 0;

    // Fetch the 4 bit index number by looking for the diffusion profile unique ID:
    for (i = 0; i < _DiffusionParametersCount; i++)
    {
        if (_HashAndShadowStrenthAndThicknessOffset[i].x == diffusionParametersHash)
        {
            diffusionParametersIndex = i;
            break;
        }
    }

    return diffusionParametersIndex;
}

// uint FindDiffusionParametersIndex(uint diffusionParametersHash)
// {
//     if (diffusionParametersHash == 0)
//         return 0xffffffff;
//
//     for (uint i = 0; i < _DiffusionParametersCount; i++)
//     {
//         if (_HashAndShadowStrenthAndThicknessOffset[i].x == diffusionParametersHash)
//             return i;
//     }
//
//     return 0xffffffff;
// }

float3 SampleTransmitTexture(float map_01, SSSData sss_data)
{
    // float3 S = float3(_ShapeParams.w * rcp(_ShapeParams.x), _ShapeParams.w * rcp(_ShapeParams.y),
    //                   _ShapeParams.w * rcp(_ShapeParams.z)); //d/s
    float3 S = float3(sss_data._ShapeParamsAndFreePath.r, sss_data._ShapeParamsAndFreePath.g,
                      sss_data._ShapeParamsAndFreePath.b);
    float t = lerp(sss_data._WorldScaleAndMaxRadiusAndThicknessRemaps.b,
                   sss_data._WorldScaleAndMaxRadiusAndThicknessRemaps.a, saturate(map_01));
    return ComputeTransmittanceProfile(t, S, sss_data._TransmissionTintAndFresnel.rgb);
}


//TOONDIRECT、TOONSHADOW
float3 ToonDirectLight(DirectSufsurfaceLighting sufsurfaceLighting, out float shadowMask)
{
    // BRDFDataToon brdfData;
    float diffuseLightMask = 0; // 亮度细节

    SurfaceDataToon surfaceData;
    Varyings input = (Varyings)0;
    input.positionCS = sufsurfaceLighting.positionCS;
    input.normalWS = sufsurfaceLighting.NormalWS;
    input.tangentWS = sufsurfaceLighting.TangentWS;
    input.texcoord = sufsurfaceLighting.uv;
    InitializeSurfaceDataToon(sufsurfaceLighting.uv, input, surfaceData);
    BRDFDataToon brdfData;
    InitializeBRDFDataToon(surfaceData, brdfData);

    Light light = GetMainLight();
    float lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;

    InputDataToon inputData;
    InitializeInputDataToon(input, surfaceData.normalTS, inputData);

    ToonData toonData;
    InitializeToonData(input.texcoord, surfaceData.envReflect, inputData.normalWS,
                       inputData.positionWS, toonData);
    //计算光照（只算漫反射）
    // float H_Lambert = dot(inputData.normalWS, light.direction) * 0.5 + 0.5;
    CalculateDiffuse(
        surfaceData, inputData, toonData,
        brdfData, light.direction, lightAttenuation,
        /*out*/ diffuseLightMask);

    shadowMask = diffuseLightMask;
    //original color #7D7573
    float diffuseColor = lerp(_Shadow1Color * light.color * sufsurfaceLighting.Albedo,
                              light.color * sufsurfaceLighting.Albedo, diffuseLightMask);

    // float3 diffuseColor = diffuseLightMask * light.color * sufsurfaceLighting.Albedo;
    return diffuseColor;
}

float ModifyNDLToCDF(float3 normalWS, float3 LightDirWS)
{
    // float NDL = dot(-float3(normalWS.x, 0, normalWS.z), float3(LightDirWS.x, 0, LightDirWS.z));
    // return float(NDL);

    float2 normalXZ = normalize(float2(normalWS.x, normalWS.z));
    float2 lightXZ = normalize(float2(LightDirWS.x, LightDirWS.z));
    float NDL = dot(normalXZ, lightXZ);
    return NDL * 0.5;
}

void DirectLightSSS(DirectSufsurfaceLighting sufsurfaceLighting, SSSData sssData, out float3 DirectDiffuse,
                    out float ShadowMask)
{
    float3 normal = sufsurfaceLighting.NormalWS;
    Light light = GetMainLight();
    float3 lightDir = normalize(-light.direction.xyz);
    float NDL = ModifyNDLToCDF(normal, lightDir);

    // float3 backColor = (1 - TransmitFactor) * sufsurfaceLighting.Albedo * _TransmissionTint;


    // half4 colorGama = IsGammaSpace() ? half4(0.5019608, 0.5019608, 1, 0) : half4(0.2158605, 0.2158605, 1, 0);
    //========================shadow start ===========================

    // PBR Shadow
    float3 Diffusion = ToonDirectLight(sufsurfaceLighting, ShadowMask);
    //Toon shadow
    // float Shadow = ToonDirectLight(sufsurfaceLighting, ShadowMask);
    float shadow = ShadowMask;
    //=======================shadow end========================
    float halfNDL = saturate(NDL);

    // half TransmitFactor = pow(halfNDL, abs(_ThickOffset));


    float darkThickness = (halfNDL + sufsurfaceLighting.Thickness) * (1 - shadow); //越黑得地方越透
    // darkThickness = halfNDL;
    half ThichnessFactor = pow(darkThickness, abs(asfloat(sssData.HashAndShadowStrenthAndThicknessOffset.z)));


    float3 BackColor = SampleTransmitTexture(ThichnessFactor, sssData) * (1 - shadow) * sufsurfaceLighting.
        TransmissionMask;


    float3 SSSColor = (Diffusion + BackColor);
    //===================fresnel (模仿光滑表面)============================

    float4 virtualViewDirVS = float4(.01, -0.01, 1.0, 0.0);
    float3 viewXDirWS = normalize(mul(_InvViewMatrix, virtualViewDirVS).xyz);
    float NDV = saturate(dot(viewXDirWS, sufsurfaceLighting.NormalWS));

    // NDV =(NDV +1)*.5;
    half3 fresnelTerm = saturate(F_Schlick(sssData._TransmissionTintAndFresnel.a, NDV)) * _Smoothness;


    //根据摄像机距离进行衰减
    // float distanceToCamera = length(sufsurfaceLighting.PositionRWS);
    //
    // float distanceFade = smoothstep(0, 3.0, distanceToCamera);
    //
    // fresnelTerm *= distanceFade;


    float3 final = SSSColor * (1 - fresnelTerm) + (fresnelTerm);
    ShadowMask = 1 - ShadowMask;

    //测试
    // ShadowMask = 1 - smoothstep(0, 0.5, saturate(GetShadowAttenuation(sufsurfaceLighting) * NDL));
    // float2 Test = sufsurfaceLighting.uv ;
    // final = float3(Test,0);
    // float2 Test = sufsurfaceLighting.uv ;
    // final = float3(Diffusion);

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
    // irradiance *= GetCurrentExposureMultiplier();
    Diffuse = irradiance;
}


#endif
