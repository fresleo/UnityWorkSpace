#ifndef XKNIGHT_CRYSTAL_ICEDEPTH
#define XKNIGHT_CRYSTAL_ICEDEPTH

half3 IceDepth(TEXTURE2D_PARAM(MainTex, sampler_BaseMap), float2 uv, uint samples, float offset, float3 positionWS, int lod)
{
    half3 col = (half3)0;
    half u_off = 0;
    half v_off = 0;
    half2 viewOffset = normalize(_WorldSpaceCameraPos.xyz - positionWS.xyz).xz;

    UNITY_LOOP
    for(uint s = 0; s < samples; s++)
    {
        col += SAMPLE_TEXTURE2D_LOD(MainTex, sampler_BaseMap, uv + half2(u_off, v_off), lod).rgb;
        u_off -= offset * viewOffset.x;
        v_off -= offset * viewOffset.y;
    }
    
    return col / samples;
}

//冰面深度-顶点采样，效果略差，但是可以支持多层冰面-可以留给低配机器使用
// #if _ICE_DEPTH_ON
// 	half4 iceout = lerp(half4(surfaceData.albedo.rgb,surfaceData.alpha), input.iceColor, _IceBlur);
// 	iceout.rgb = iceout * _IceColor.rgb * _IceSaturation;
// 	color.rgb += iceout.rgb;
// #endif

#endif // XKNIGHT_CRYSTAL_ICEDEPTH
