#ifndef UNIVERSAL_ATLAS_INPUT_INCLUDED
#define UNIVERSAL_ATLAS_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;

half _AlphaClip;
half4 _BaseColor;
half _BumpScale;

half4 _SpecularColor;
half _Shininess;

half _AtlasWidth, _AtlasHeight;
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_SpecularMap); SAMPLER(sampler_SpecularMap);


half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    half alpha = albedoAlpha * color.a;
    alpha = AlphaDiscard(alpha, cutoff);

    return alpha;
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
    half4 bumpMapCol = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    return UnpackNormalScale(bumpMapCol, scale);
}

inline void InitializeStandardLitSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;
    
    // 从瓦片图集中选择瓦片
    float _Tile = floor((vertexColor.a * 2) / (1.0 / 128.0));
    
    float tileWidth = 1 / _AtlasWidth;
    float tileHeight = 1 / _AtlasHeight;
    float col = floor(_Tile / _AtlasWidth);
    float row = fmod(_Tile, _AtlasWidth);
    
    uv.x = row * tileWidth + uv.x * tileWidth;
    uv.y = col * -tileHeight + uv.y * tileHeight - tileHeight;
    
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);
    
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _AlphaClip);

    outSurfaceData.metallic = half(1.0);
    
    half4 specularMap = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, uv);
    outSurfaceData.specular = specularMap.rgb * _SpecularColor.rgb;
    outSurfaceData.smoothness = specularMap.a * _Shininess;
    
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = 0;
}

#endif // UNIVERSAL_ATLAS_INPUT_INCLUDED
