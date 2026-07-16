#ifndef GRASS_BLEND_WITH_TERRAIN_ALBEDO
#define GRASS_BLEND_WITH_TERRAIN_ALBEDO

TEXTURE2D(_GlobalGrassColorMap);            SAMPLER(sampler_GlobalGrassColorMap);

uniform half4 _GlobalGrassColorMapUV;
uniform half _GlobalGrassBlendTerrainStrength;

//UV Utilities
half2 BoundsToWorldUV(in float3 wPos, in half4 b)
{
    return (wPos.xz * b.z) - (b.xy * b.z);
}

//Color map UV
half2 GetColorMapUV(in float3 wPos)
{
    return BoundsToWorldUV(wPos, _GlobalGrassColorMapUV);
}

half4 SampleColorMapTexture(in float3 wPos) 
{
    half2 uv = GetColorMapUV(wPos);

    return SAMPLE_TEXTURE2D(_GlobalGrassColorMap, sampler_GlobalGrassColorMap, uv).rgba;
}

half3 ApplyColorMap(float3 wPos, half3 iColor, half localColorMapStrength)
{
    half3 colorMapColor = SampleColorMapTexture(wPos).rgb;
    
    return lerp(iColor, colorMapColor, localColorMapStrength * _GlobalGrassBlendTerrainStrength);
}

half3 ApplyColorMapPresampled(half3 colorMapColor, half3 iColor, half localColorMapStrength)
{
    return lerp(iColor, colorMapColor, localColorMapStrength * _GlobalGrassBlendTerrainStrength);
}

#endif // GRASS_BLEND_WITH_TERRAIN_ALBEDO
