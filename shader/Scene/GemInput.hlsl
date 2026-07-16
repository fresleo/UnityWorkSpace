#ifndef GEM_INPUT_INCLUDED
#define GEM_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

CBUFFER_START(UnityPerMaterial)
    half _Smoothness;
    half _Alpha;
    half _ReflectionIntensity;
    half _Blur;
    half _IOR;
    half _FresnelIntensity;
    half _FresnelPower;

    half _EmissionStrength;

    half _Saturation;
    half _Contrast;
    half _Metallic;
    half _BumpScale;

    half _OutlineWidth;

    half4 _EmissionColor;
    half4 _BaseColor;
    half4 _CubemapColor;
    half4 _FresnelColor;
    half4 _OutlineColor;

    // Disolve
    half4 _DissolveTex_ST;
    half4 _DissolveTexChannel;
    half _DissolveFadingMin;
    half _DissolveFadingMax;
    half _EdgeWidth;
    half _DissolveCutoff;
    half4 _EdgeColor1;
    half4 _EdgeColor2;
    half3 _DissolveDir;

    float4 _BaseMap_ST;
    half4 _CubeMap_HDR;
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURECUBE(_CubeMap); SAMPLER(sampler_CubeMap);
TEXTURE2D(_DissolveTex); SAMPLER(sampler_DissolveTex);

half Fresnel(half power, half bias, half scale, half3 normalWS, half3 viewDir)
{
    half NdotV = dot(normalWS, viewDir);
    half fresnel = bias + scale * pow(saturate(1.0 - NdotV), power);

    return fresnel;
}

half3 SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(_BaseMap, sampler_BaseMap))
{
    #ifdef _EMISSION
    half4 emissionSampler = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    emissionSampler.rgb = emissionSampler.a * emissionColor * _EmissionStrength;
    return emissionSampler.rgb;
    #else
    return 0.0;
    #endif
}

float4 CalculateContrast(float contrastValue, float4 colorTarget)
{
    float t = 0.5 * (1.0 - contrastValue);
    return mul(float4x4(contrastValue, 0, 0, t, 0, contrastValue, 0, t, 0, 0, contrastValue, t, 0, 0, 0, 1), colorTarget);
}

#endif // GEM_INPUT_INCLUDED
