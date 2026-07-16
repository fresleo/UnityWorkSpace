#ifndef __TOONPBR_FROST_PASS__
#define __TOONPBR_FROST_PASS__

// 冰霜的顶点方法
void FrostVertex(inout Attributes input)
{
    float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);
    
    // 冰柱遮罩
    float2 uv_IcicleMask = float2(worldNormal.x, worldNormal.z) * _IcicleMaskTile + 0.5; // [-0.5, 0.5] -> [0, 1]
    half4 icicleMaskRgba = SAMPLE_TEXTURE2D_X_LOD(_IcicleMask, sampler_IcicleMask, uv_IcicleMask, 0);

    float yMaskTop = GetYMaskTop(worldNormal.y);
    float yMaskDown = GetYMaskDown(worldNormal.y);
    float4 expandValue = yMaskDown * icicleMaskRgba * _IcicleLength + yMaskTop;

    // 冰的覆盖遮罩
    float2 uv_IceOverlayMask = input.texcoord.xy * _IceOverlayMask_ST.xy + _IceOverlayMask_ST.zw;
    half4 iceOverlayMaskRgba = SAMPLE_TEXTURE2D_X_LOD(_IceOverlayMask, sampler_IceOverlayMask, uv_IceOverlayMask, 0);
    float iceOverlayMaskValue = iceOverlayMaskRgba.r;

    // 沿着法线往外拉伸
    float4 vertexValue = 0;
    vertexValue += float4(input.normalOS, 0) * expandValue;
    #ifdef _ICE_OVERLAY_MASK_ON
    vertexValue *= iceOverlayMaskValue;
    #endif
    input.positionOS.xyz += vertexValue.xyz;
}

#endif // __TOONPBR_FROST_PASS__
