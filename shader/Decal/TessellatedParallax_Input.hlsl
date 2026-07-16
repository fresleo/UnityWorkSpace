#ifndef __TESSELLATED_PARALLAX_INPUT__
#define __TESSELLATED_PARALLAX_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;

half _AlphaClip;
half4 _BaseColor;
half _BumpScale;

half4 _SpecularColor;
half _Shininess;

half _Displacement;
half _Parallax;

half4 _EmissionColor;

half _DissolveCutoff, _DissolveFadingMin, _DissolveFadingMax;

// 其它设置
half _ShowProgressY;

// 镶嵌
half _Tess;
half _MinDistance;
half _MaxDistance;
half _TessEdgeLength;
half _TessMaxDisp;
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_SpecularMap); SAMPLER(sampler_SpecularMap);
TEXTURE2D(_ParallaxMap); SAMPLER(sampler_ParallaxMap);
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);


void ApplyPerPixelDisplacement(half3 viewDirTS, inout float2 uv)
{
    uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _Parallax, uv);
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
    half4 bumpMapCol = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    return UnpackNormalScale(bumpMapCol, scale);
}

half3 SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(emissionMap, sampler_emissionMap))
{
    #ifndef _EMISSION_ON
    return 0;
    #else
    return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * emissionColor;
    #endif
}

// 伪随机数生成器
float FakeRandom(float2 uv)
{
    return frac(sin(dot(uv.xy, float2(12.9898, 78.233))) * 43758.5453);
}

void InitializeStandardLitSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    #ifdef _REVERSEPROGRESS_ON
    if (uv.y < 1.0 - _ShowProgressY)
    {
        clip(-1);
    }
    #else
    if (uv.y > _ShowProgressY)
    {
        // 在 D3D 里，discard 会导致 Program 'LitPassFragment', internal error: argument pulled into unrelated predicate at line 150 (on d3d11)
        // discard;
        clip(-1);
    }
    #endif

    // 消融
    float dissolveNoise = FakeRandom(uv);
    float cutoff = lerp(-0.15, 1.01, _DissolveCutoff);
    float dissolve = dissolveNoise - cutoff;
    dissolve = saturate(smoothstep(_DissolveFadingMin, _DissolveFadingMax, dissolve));
    clip(dissolve - 0.01);
    
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

    half alpha = albedoAlpha.a * _BaseColor.a * dissolve;
    alpha = AlphaDiscard(alpha, _AlphaClip);
    outSurfaceData.alpha = alpha;

    outSurfaceData.metallic = half(1.0);
    
    half4 specularMap = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, uv);
    outSurfaceData.specular = specularMap.rgb * _SpecularColor.rgb;
    outSurfaceData.smoothness = specularMap.a * _Shininess;
    
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}

#endif // __TESSELLATED_PARALLAX_INPUT__
