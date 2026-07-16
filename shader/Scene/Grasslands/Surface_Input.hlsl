#ifndef __SURFACE_INPUT__
#define __SURFACE_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    float4 _CovColor;
    float _CovSmoothnessTextureChannel;
    float _Glossiness;
    float _SmoothnessTextureChannel;
    float _CovMetallic;
    float _Metallic;
    float _NormalBlending;
    float _CovBumpScale;
    float _MaskContrast;
    float _MaskTilingY;
    float _MaskTilingX;
    float _CovBalance;
    float _CovOffset;
    float _CovOverlayMethod;
    float _BumpScale;
    float _CovTiling;
    float _Tiling;
    float _CovGlossiness;
    float _OcclusionStrength;
CBUFFER_END

TEXTURE2D(_MainTex);                        SAMPLER(sampler_MainTex);
TEXTURE2D(_CovMainTex);                     SAMPLER(sampler_CovMainTex);
TEXTURE2D(_BumpMap);                        SAMPLER(sampler_BumpMap);
TEXTURE2D(_CovMask);                        SAMPLER(sampler_CovMask);
TEXTURE2D(_CovBumpMap);                     SAMPLER(sampler_CovBumpMap);
TEXTURE2D(_MetallicGlossMap);               SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_CoverageMetallicSmoothness);     SAMPLER(sampler_CoverageMetallicSmoothness);
TEXTURE2D(_OcclusionMap);                   SAMPLER(sampler_OcclusionMap);

inline float4 TriplanarSampling421(TEXTURE2D_PARAM(topTexMap, sampler_topTexMap), float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index)
{
    float3 projNormal = pow(abs(worldNormal), falloff);
    projNormal /= (projNormal.x + projNormal.y + projNormal.z) + 0.00001;
    
    float3 nsign = sign(worldNormal);
    
    half4 xNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.zy * float2(nsign.x, 1.0) );
    half4 yNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.xz * float2(nsign.y, 1.0) );
    half4 zNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.xy * float2(-nsign.z, 1.0) );
    
    return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
}

inline float3 TriplanarSampling432(TEXTURE2D_PARAM(topTexMap, sampler_topTexMap), float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index)
{
    float3 projNormal = (pow(abs(worldNormal), falloff));
    projNormal /= (projNormal.x + projNormal.y + projNormal.z) + 0.00001;
    
    float3 nsign = sign(worldNormal);

    half4 xNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.zy * float2(nsign.x, 1.0) );
    half4 yNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.xz * float2(nsign.y, 1.0) );
    half4 zNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.xy * float2(-nsign.z, 1.0) );
    
    xNorm.xyz = half3(UnpackNormalScale(xNorm, normalScale.y).xy * float2(nsign.x, 1.0) + worldNormal.zy, worldNormal.x).zyx;
    yNorm.xyz = half3(UnpackNormalScale(yNorm, normalScale.x).xy * float2(nsign.y, 1.0) + worldNormal.xz, worldNormal.y).xzy;
    zNorm.xyz = half3(UnpackNormalScale(zNorm, normalScale.y).xy * float2(-nsign.z, 1.0) + worldNormal.xy, worldNormal.z).xyz;
    
    return normalize(xNorm.xyz * projNormal.x + yNorm.xyz * projNormal.y + zNorm.xyz * projNormal.z);
}

inline float4 TriplanarSampling430(TEXTURE2D_PARAM(topTexMap, sampler_topTexMap), float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale, float3 index)
{
    float3 projNormal = (pow(abs(worldNormal), falloff));
    projNormal /= (projNormal.x + projNormal.y + projNormal.z) + 0.00001;
    
    float3 nsign = sign(worldNormal);

    half4 xNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.zy * float2(nsign.x, 1.0) );
    half4 yNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.xz * float2(nsign.y, 1.0) );
    half4 zNorm = SAMPLE_TEXTURE2D( topTexMap, sampler_topTexMap, tiling * worldPos.xy * float2(-nsign.z, 1.0) );
    
    return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
}

#endif
