// 雨坑的雨点涟漪
#ifndef __RAINY_PUDDLES_RAIN_RIPPLES__
#define __RAINY_PUDDLES_RAIN_RIPPLES__

void SplitFlipbookInfo(
    in float4 flipbookArguments,
    out float columns, out float rows, out float speed, out float strartFrame)
{
    columns = flipbookArguments.x;
    rows = flipbookArguments.y;
    speed = (flipbookArguments.x * flipbookArguments.y * flipbookArguments.z);
    strartFrame = flipbookArguments.w;
}

float2 CalculateFlipBookUVTiling(float2 uv, float flipBookTiling)
{
    float2 temp_tiling = (flipBookTiling).xx;
    float2 temp_uv = uv * temp_tiling;
    float2 result_uv = float2(frac(temp_uv.x), frac(temp_uv.y));
    return result_uv;
}

void CalculateFlipbookUVOffset(
    in float columns, in float rows, in float speed, in float strartFrame,
    out float2 fbTiling, out float2 fbOffset)
{
    // Flipbook 纹理的总瓦片数
    float fbTotalTiles = columns * rows;
    // Flipbook 纹理列和行的偏移量
    float fbColsOffset = 1.0f / columns;
    float fbRowsOffset = 1.0f / rows;
    // 动画速度
    float fbSpeed = GET_GLOBAL_TIME[1] * speed;
    // UV Tiling (列和行的偏移)
    fbTiling = float2(fbColsOffset, fbRowsOffset);
    
    // UV 偏移 - 计算当前瓦片线性索引，并将其转换为 (X * 列偏移, Y * 行偏移)
    // 计算当前瓦片线性索引
    float fbCurrentTileIndex = round(fmod(fbSpeed + strartFrame, fbTotalTiles));
    fbCurrentTileIndex += (fbCurrentTileIndex < 0) ? fbTotalTiles : 0;
    
    // 从当前瓦片线性索引获取偏移 X 坐标
    float fbLinearIndexToX = round(fmod(fbCurrentTileIndex, columns));
    // 将偏移 X 乘以列偏移
    float fbOffsetX = fbLinearIndexToX * fbColsOffset;
    
    // 从当前瓦片线性索引获取偏移 Y 坐标
    float fbLinearIndexToY = round(fmod((fbCurrentTileIndex - fbLinearIndexToX) / columns, rows));
    // 反转 Y 以获得从上到下的瓦片
    fbLinearIndexToY = (int)(rows - 1) - fbLinearIndexToY;
    // 将偏移 Y 乘以行偏移
    float fbOffsetY = fbLinearIndexToY * fbRowsOffset;
    
    // UV 偏移
    fbOffset = float2(fbOffsetX, fbOffsetY);
}

float2 CalculateFlipbookUV(
    float2 uv, float flipBookTiling,
    float2 fbTiling, float2 fbOffset)
{
    // Flipbook UV
    float2 tilingUV = CalculateFlipBookUVTiling(uv, flipBookTiling);
    float2 fbUV = tilingUV * fbTiling + fbOffset;

    return fbUV;
}

float2 CalculateDuplicateRainRipplesAtlasNormalUV(
    float2 uv, float flipBookTiling,
    float fb_scale, float fb_offset, float fb_rotation,
    float2 fbTiling, float2 fbOffset)
{
    float2 tempCast = (flipBookTiling / fb_scale).xx;
    float2 texCoord = uv * tempCast + fb_offset;
    float fbCos = cos(fb_rotation);
    float fbSin = sin(fb_rotation);
    float2 rotator = mul(texCoord - float2(0.5, 0.5), float2x2(fbCos, -fbSin, fbSin, fbCos)) + float2(0.5, 0.5);
    float2 appendResult = float2(frac(rotator.x), frac(rotator.y));
    
    half2 fbUV = appendResult * fbTiling + fbOffset;
    return fbUV;
}

/*
 * 获取雨的涟漪法线
 */
float3 GetRainRipplesNormal(
    float4 flipbookArguments, float2 uv, float fbTilingNormal,
    TEXTURE2D_PARAM(atlasNormal, sampler_atlasNormal), float intensityScaleNormal1,
    float duplicate,
    float fb_scale, float fb_offset, float fb_rotation, float intensityScaleNormal2)
{
    float fbColumns, fbRows, fbSpeed, fbStrartFrame;
    SplitFlipbookInfo(flipbookArguments,
        fbColumns, fbRows, fbSpeed, fbStrartFrame);
    
    float2 fbTiling, fbOffset;
    CalculateFlipbookUVOffset(fbColumns, fbRows, fbSpeed, fbStrartFrame,
        fbTiling, fbOffset);
    
    float2 fbUV_1 = CalculateFlipbookUV(uv, fbTilingNormal,
        fbTiling, fbOffset);

    // 采样涟漪法线
    half4 atlasNormal_1 = half4(SAMPLE_TEXTURE2D(atlasNormal, sampler_atlasNormal, fbUV_1));
    float3 unpack_1 = UnpackNormalScale(atlasNormal_1, intensityScaleNormal1);
    unpack_1.z = lerp(1, unpack_1.z, saturate(intensityScaleNormal1));

    float3 rainDotsNormal = unpack_1;

    // 重复采样
    UNITY_BRANCH
    if(duplicate)
    {
        // 第2次采样的 uv
        float2 fbUV_2 = CalculateDuplicateRainRipplesAtlasNormalUV(uv, fbTilingNormal,
            fb_scale, fb_offset, fb_rotation,
            fbTiling, fbOffset);

        half4 atlasNormal_2 = half4(SAMPLE_TEXTURE2D(atlasNormal, sampler_atlasNormal, fbUV_2));
        float3 unpack_2 = UnpackNormalScale(atlasNormal_2, intensityScaleNormal2);
        unpack_2.z = lerp(1, unpack_2.z, saturate(intensityScaleNormal2));

        rainDotsNormal = BlendNormal(unpack_1, unpack_2);
    }
    
    return rainDotsNormal;
}

#endif // __RAINY_PUDDLES_RAIN_RIPPLES__
