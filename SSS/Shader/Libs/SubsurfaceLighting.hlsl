#ifndef _SUBSURFACELIGHTING_HLSL
#define _SUBSURFACELIGHTING_HLSL


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#define HALF_MIN 6.103515625e-5

struct DirectSufsurfaceLighting
{
    float3 Albedo;
    float Thickness;
    float3 NormalWS;
    float3 PositionWS;
    float2 uv;
};

sampler2D _PreIntegrated; //PerMaterial
void DirectLightSSS(DirectSufsurfaceLighting sufsurfaceLighting, out float3 DirectDiffuse)
{
    float3 normal = sufsurfaceLighting.NormalWS;
    DirectionalLightData lightData = _DirectionalLightDatas[0];
    float3 lightDir = normalize(-lightData.forward.xyz);
    
    //计算公式
    float NDL = saturate(dot(normal,lightDir ));

    float3 PreIntegrated = tex2D(_PreIntegrated, sufsurfaceLighting.uv.xy).rgb;

    DirectDiffuse = float3(NDL, NDL, NDL);
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
