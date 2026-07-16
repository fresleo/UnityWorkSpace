#ifndef __TOONPBR_FROST_INITIALIZE_FUNCS__
#define __TOONPBR_FROST_INITIALIZE_FUNCS__

float GetYMaskTop(float worldY)
{
    float mask = saturate(worldY * _IceAmount * _YMaskTop);
    return mask;
}

float GetYMaskDown(float worldY)
{
    float mask = saturate(worldY * _IceAmount * _YMaskDown);
    return mask;
}

// 冰霜的表面数据方法
void InitializeSurfaceData_Frost(
    in float2 texcoord, in float3 positionWS, in float3 normalWS,
    in half4 baseAlbedo, in float3 baseNormals, in half3 baseEmission,
    inout half4 finalAlbedo, inout float3 finalNormals, inout half3 finalEmission)
{
    // 世界空间观察方向
    float3 worldViewDirection = _WorldSpaceCameraPos.xyz - positionWS;
    worldViewDirection = SafeNormalize(worldViewDirection);

    // uv
    float2 uv_FrostTexture = texcoord * _FrostTexture_ST.xy + _FrostTexture_ST.zw;
    float2 uv_FrostBumpMap = texcoord * _FrostBumpMap_ST.xy + _FrostBumpMap_ST.zw;

    #ifdef _ICE_OVERLAY_MASK_ON
    float2 uv_IceOverlayMask = texcoord * _IceOverlayMask_ST.xy + _IceOverlayMask_ST.zw;
    #endif

    // 纹理采样
    half4 frostTexRgba = SampleAlbedoAlpha(uv_FrostTexture, TEXTURE2D_ARGS(_FrostTexture, sampler_FrostTexture));
    float3 frostNormals = SampleNormal(uv_FrostBumpMap, TEXTURE2D_ARGS(_FrostBumpMap, sampler_FrostBumpMap), _FrostBumpScale);

    #ifdef _ICE_OVERLAY_MASK_ON
    half4 iceOverlayMaskRgba = SampleAlbedoAlpha(uv_IceOverlayMask, TEXTURE2D_ARGS(_IceOverlayMask, sampler_IceOverlayMask));
    float iceOverlayMaskValue = iceOverlayMaskRgba.r;
    #endif

    // y轴遮罩范围
    float yMaskDown = GetYMaskDown(normalWS.y);
    float yLerpValue = saturate( yMaskDown * _yIceMultiplier );
    
    // 反照率
    float4 lerpAlbedo1 = lerp( baseAlbedo , frostTexRgba , yLerpValue );
    float4 lerpAlbedo2 = lerp( lerpAlbedo1 , frostTexRgba , _IceSlider );
    finalAlbedo = lerpAlbedo2;
    
    #ifdef _ICE_OVERLAY_MASK_ON
    finalAlbedo = lerp(baseAlbedo, lerpAlbedo2, iceOverlayMaskValue);
    #endif
    
    // 法线
    float3 lerpNormals = lerp(baseNormals, frostNormals, _IceSlider);
    finalNormals = lerpNormals;
    #ifdef _ICE_OVERLAY_MASK_ON
    finalNormals = lerp(baseNormals, lerpNormals, iceOverlayMaskValue);
    #endif
    
    // 菲涅尔
    float fresnelNdv = dot( normalWS, worldViewDirection );
    fresnelNdv = saturate(fresnelNdv); // 需要确保它不会 <0，pow() 负数会导致 NaN
    float fresnelValue = _FrostEmissionFresnelIntensity * pow( 1.0 - fresnelNdv, _FrostEmissionFresnelPow );
    // 冰霜的自发光
    half3 frostEmission = frostTexRgba.rgb * fresnelValue * _FrostTint.rgb * _IceSlider;
    finalEmission = frostEmission;
    
    #ifdef _ICE_OVERLAY_MASK_ON
    finalEmission = lerp(baseEmission, frostEmission, iceOverlayMaskValue);
    #endif
}

void InitializeSurfaceData_Frost_LOD1(
    in float2 texcoord, in float3 positionWS, in float3 normalWS,
    in half4 baseAlbedo, in half3 baseEmission,
    inout half4 finalAlbedo, inout half3 finalEmission)
{
    // 世界空间观察方向
    float3 worldViewDirection = _WorldSpaceCameraPos.xyz - positionWS;
    worldViewDirection = SafeNormalize(worldViewDirection);

    // uv
    float2 uv_FrostTexture = texcoord * _FrostTexture_ST.xy + _FrostTexture_ST.zw;

    #ifdef _ICE_OVERLAY_MASK_ON
    float2 uv_IceOverlayMask = texcoord * _IceOverlayMask_ST.xy + _IceOverlayMask_ST.zw;
    #endif

    // 纹理采样
    half4 frostTexRgba = SampleAlbedoAlpha(uv_FrostTexture, TEXTURE2D_ARGS(_FrostTexture, sampler_FrostTexture));

    #ifdef _ICE_OVERLAY_MASK_ON
    half4 iceOverlayMaskRgba = SampleAlbedoAlpha(uv_IceOverlayMask, TEXTURE2D_ARGS(_IceOverlayMask, sampler_IceOverlayMask));
    float iceOverlayMaskValue = iceOverlayMaskRgba.r;
    #endif

    // y轴遮罩范围
    float yMaskDown = GetYMaskDown(normalWS.y);
    float yLerpValue = saturate( yMaskDown * _yIceMultiplier );
    
    // 反照率
    float4 lerpAlbedo1 = lerp( baseAlbedo , frostTexRgba , yLerpValue );
    float4 lerpAlbedo2 = lerp( lerpAlbedo1 , frostTexRgba , _IceSlider );
    finalAlbedo = lerpAlbedo2;
    
    #ifdef _ICE_OVERLAY_MASK_ON
    finalAlbedo = lerp(baseAlbedo, lerpAlbedo2, iceOverlayMaskValue);
    #endif

    // 菲涅尔
    float fresnelNdv = dot( normalWS, worldViewDirection );
    fresnelNdv = saturate(fresnelNdv); // 需要确保它不会 <0，pow() 负数会导致 NaN
    float fresnelValue = _FrostEmissionFresnelIntensity * pow( 1.0 - fresnelNdv, _FrostEmissionFresnelPow );
    // 霜冻的自发光
    half3 frostEmission = frostTexRgba.rgb * fresnelValue * _FrostTint.rgb * _IceSlider;
    finalEmission = frostEmission;
    
    #ifdef _ICE_OVERLAY_MASK_ON
    finalEmission = lerp(baseEmission, frostEmission, iceOverlayMaskValue);
    #endif
}

#endif // __TOONPBR_FROST_INITIALIZE_FUNCS__
