#ifndef __TRANSPARENT_BY_DITHER__
#define __TRANSPARENT_BY_DITHER__

//Packages/com.unity.render-pipelines.universal@14.0.11/ShaderLibrary/Extend/TransparentByDither.hlsl

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

void HandleDifferentResolutions(out half screenAspect, out half resolutionScale)
{
    screenAspect = _ScreenParams.x / _ScreenParams.y;
    half baseAspect = 1920.0 / 1080.0;
    resolutionScale = screenAspect / baseAspect;
}

float2 ProduceUVOffset(float2 uv, half resolutionScale)
{
    float2 pixelPos = floor(uv);
    float randomOffset = frac(sin(dot(pixelPos, float2(12.9898, 78.233))) * 43758.5453);

    float offsetStrength = lerp(0.1, 0.02, smoothstep(1.0, 2.0, resolutionScale));
    randomOffset *= offsetStrength;

    return randomOffset;
}

// 2560x1440 2670x1200 等超宽屏
#define ULTRA_WIDE_SCREEN_ASPECT 1.5

// 使用多层矩阵叠加来减少重复
// 重复的问题主要出现在 2670x1200 这种奇葩的分辨率下
#define MULTI_LAYER_MATRIX_SUPERPOSITION(uv, size, matrix) \
    (matrix[int(fmod(uv.y, size)) * size + int(fmod(uv.x, size))] + \
    matrix[int(fmod(uv.y * 1.5, size)) * size + int(fmod(uv.x * 1.5, size))] + \
    matrix[int(fmod(uv.y * 0.75, size)) * size + int(fmod(uv.x * 0.75, size))]) / 3.0

// 用来处理阈值的浮点误差
#define DITHER_EPSILON 0.001

// 原神版，其实是一样的
void DitherWithMatrix_GenshinImpact(float4 screenPos, half ditherIntensity, half ditherSize)
{
    float4x4 thresholdMatrix =
    {
        1.0, 9.0, 3.0, 11.0,
        13.0, 5.0, 15.0, 7.0,
        4.0, 12.0, 2.0, 10.0,
        16.0, 8.0, 14.0, 6.0
    };

    if (ditherIntensity < 0.95f)
    {
        ditherIntensity *= 17;

        float2 uv = (screenPos.xy / screenPos.w) * _ScreenParams.xy;
        uv /= ditherSize;

        half curA = thresholdMatrix[fmod(uv.x, 4)][fmod(uv.y, 4)];
        clip(ditherIntensity - curA - DITHER_EPSILON);
    }
}

// https://ocias.com/blog/unity-stipple-transparency-shader
void DitherWithMatrix_4x4(float4 screenPos, half ditherIntensity, half ditherSize)
{
    // Screen-door transparency: Discard pixel if below threshold.
    float4x4 thresholdMatrix =
    {
        1.0 / 17.0,     9.0 / 17.0,     3.0 / 17.0,     11.0 / 17.0,
        13.0 / 17.0,    5.0 / 17.0,     15.0 / 17.0,    7.0 / 17.0,
        4.0 / 17.0,     12.0 / 17.0,    2.0 / 17.0,     10.0 / 17.0,
        16.0 / 17.0,    8.0 / 17.0,     14.0 / 17.0,    6.0 / 17.0
    };

    half screenAspect = 0, resolutionScale = 0;
    HandleDifferentResolutions(screenAspect, resolutionScale);

    // 考虑高宽比的分辨率适配
    half adjustedDitherSize = ditherSize * resolutionScale;

    float2 uv = (screenPos.xy / screenPos.w) * _ScreenParams.xy; // pixel position
    uv /= adjustedDitherSize;

    float2 uvOffset = ProduceUVOffset(uv, resolutionScale);
    uv += uvOffset;

    int ix = (int)fmod(uv.x, 4);
    int iy = (int)fmod(uv.y, 4);
    half threshold = thresholdMatrix[ix][iy];

    clip(ditherIntensity - threshold - DITHER_EPSILON);
}

void DitherWithMatrix_8x8(float4 screenPos, half ditherIntensity, half ditherSize)
{
    float thresholdMatrix[64] = {
        0.0/64.0,   32.0/64.0,  8.0/64.0,   40.0/64.0,  2.0/64.0,   34.0/64.0,  10.0/64.0,  42.0/64.0,
        48.0/64.0,  16.0/64.0,  56.0/64.0,  24.0/64.0,  50.0/64.0,  18.0/64.0,  58.0/64.0,  26.0/64.0,
        12.0/64.0,  44.0/64.0,  4.0/64.0,   36.0/64.0,  14.0/64.0,  46.0/64.0,  6.0/64.0,   38.0/64.0,
        60.0/64.0,  28.0/64.0,  52.0/64.0,  20.0/64.0,  62.0/64.0,  30.0/64.0,  54.0/64.0,  22.0/64.0,
        3.0/64.0,   35.0/64.0,  11.0/64.0,  43.0/64.0,  1.0/64.0,   33.0/64.0,  9.0/64.0,   41.0/64.0,
        51.0/64.0,  19.0/64.0,  59.0/64.0,  27.0/64.0,  49.0/64.0,  17.0/64.0,  57.0/64.0,  25.0/64.0,
        15.0/64.0,  47.0/64.0,  7.0/64.0,   39.0/64.0,  13.0/64.0,  45.0/64.0,  5.0/64.0,   37.0/64.0,
        63.0/64.0,  31.0/64.0,  55.0/64.0,  23.0/64.0,  61.0/64.0,  29.0/64.0,  53.0/64.0,  21.0/64.0
    };

    half screenAspect = 0, resolutionScale = 0;
    HandleDifferentResolutions(screenAspect, resolutionScale);

    // 考虑高宽比的分辨率适配
    half adjustedDitherSize = ditherSize * resolutionScale;

    float2 uv = (screenPos.xy / screenPos.w) * _ScreenParams.xy;
    uv /= adjustedDitherSize;

    float2 uvOffset = ProduceUVOffset(uv, resolutionScale);
    uv += uvOffset;

    int x = int(fmod(uv.x, 8));
    int y = int(fmod(uv.y, 8));
    int index = y * 8 + x;
    half simpleThreshold = thresholdMatrix[index];

    half complexThreshold = MULTI_LAYER_MATRIX_SUPERPOSITION(uv, 8, thresholdMatrix);

    half useComplexThreshold = step(ULTRA_WIDE_SCREEN_ASPECT, screenAspect);
    half threshold = lerp(simpleThreshold, complexThreshold, useComplexThreshold);

    clip(ditherIntensity - threshold - DITHER_EPSILON);
}

void DitherWithMatrix_16x16(float4 screenPos, half ditherIntensity, half ditherSize)
{
    float thresholdMatrix[256] = {
        0.0/256.0,      128.0/256.0,    32.0/256.0,     160.0/256.0,    8.0/256.0,      136.0/256.0,    40.0/256.0,     168.0/256.0,    2.0/256.0,      130.0/256.0,    34.0/256.0,     162.0/256.0,    10.0/256.0,     138.0/256.0,    42.0/256.0,     170.0/256.0,
        192.0/256.0,    64.0/256.0,     224.0/256.0,    96.0/256.0,     200.0/256.0,    72.0/256.0,     232.0/256.0,    104.0/256.0,    194.0/256.0,    66.0/256.0,     226.0/256.0,    98.0/256.0,     202.0/256.0,    74.0/256.0,     234.0/256.0,    106.0/256.0,
        48.0/256.0,     176.0/256.0,    16.0/256.0,     144.0/256.0,    56.0/256.0,     184.0/256.0,    24.0/256.0,     152.0/256.0,    50.0/256.0,     178.0/256.0,    18.0/256.0,     146.0/256.0,    58.0/256.0,     186.0/256.0,    26.0/256.0,     154.0/256.0,
        240.0/256.0,    112.0/256.0,    208.0/256.0,    80.0/256.0,     248.0/256.0,    120.0/256.0,    216.0/256.0,    88.0/256.0,     242.0/256.0,    114.0/256.0,    210.0/256.0,    82.0/256.0,     250.0/256.0,    122.0/256.0,    218.0/256.0,    90.0/256.0,
        12.0/256.0,     140.0/256.0,    44.0/256.0,     172.0/256.0,    4.0/256.0,      132.0/256.0,    36.0/256.0,     164.0/256.0,    14.0/256.0,     142.0/256.0,    46.0/256.0,     174.0/256.0,    6.0/256.0,      134.0/256.0,    38.0/256.0,     166.0/256.0,
        204.0/256.0,    76.0/256.0,     236.0/256.0,    108.0/256.0,    196.0/256.0,    68.0/256.0,     228.0/256.0,    100.0/256.0,    206.0/256.0,    78.0/256.0,     238.0/256.0,    110.0/256.0,    198.0/256.0,    70.0/256.0,     230.0/256.0,    102.0/256.0,
        60.0/256.0,     188.0/256.0,    28.0/256.0,     156.0/256.0,    52.0/256.0,     180.0/256.0,    20.0/256.0,     148.0/256.0,    62.0/256.0,     190.0/256.0,    30.0/256.0,     158.0/256.0,    54.0/256.0,     182.0/256.0,    22.0/256.0,     150.0/256.0,
        252.0/256.0,    124.0/256.0,    220.0/256.0,    92.0/256.0,     244.0/256.0,    116.0/256.0,    212.0/256.0,    84.0/256.0,     254.0/256.0,    126.0/256.0,    222.0/256.0,    94.0/256.0,     246.0/256.0,    118.0/256.0,    214.0/256.0,    86.0/256.0,
        3.0/256.0,      131.0/256.0,    35.0/256.0,     163.0/256.0,    11.0/256.0,     139.0/256.0,    43.0/256.0,     171.0/256.0,    1.0/256.0,      129.0/256.0,    33.0/256.0,     161.0/256.0,    9.0/256.0,      137.0/256.0,    41.0/256.0,     169.0/256.0,
        195.0/256.0,    67.0/256.0,     227.0/256.0,    99.0/256.0,     203.0/256.0,    75.0/256.0,     235.0/256.0,    107.0/256.0,    193.0/256.0,    65.0/256.0,     225.0/256.0,    97.0/256.0,     201.0/256.0,    73.0/256.0,     233.0/256.0,    105.0/256.0,
        51.0/256.0,     179.0/256.0,    19.0/256.0,     147.0/256.0,    59.0/256.0,     187.0/256.0,    27.0/256.0,     155.0/256.0,    49.0/256.0,     177.0/256.0,    17.0/256.0,     145.0/256.0,    57.0/256.0,     185.0/256.0,    25.0/256.0,     153.0/256.0,
        243.0/256.0,    115.0/256.0,    211.0/256.0,    83.0/256.0,     251.0/256.0,    123.0/256.0,    219.0/256.0,    91.0/256.0,     241.0/256.0,    113.0/256.0,    209.0/256.0,    81.0/256.0,     249.0/256.0,    121.0/256.0,    217.0/256.0,    89.0/256.0,
        15.0/256.0,     143.0/256.0,    47.0/256.0,     175.0/256.0,    7.0/256.0,      135.0/256.0,    39.0/256.0,     167.0/256.0,    13.0/256.0,     141.0/256.0,    45.0/256.0,     173.0/256.0,    5.0/256.0,      133.0/256.0,    37.0/256.0,     165.0/256.0,
        207.0/256.0,    79.0/256.0,     239.0/256.0,    111.0/256.0,    199.0/256.0,    71.0/256.0,     231.0/256.0,    103.0/256.0,    205.0/256.0,    77.0/256.0,     237.0/256.0,    109.0/256.0,    197.0/256.0,    69.0/256.0,     229.0/256.0,    101.0/256.0,
        63.0/256.0,     191.0/256.0,    31.0/256.0,     159.0/256.0,    55.0/256.0,     183.0/256.0,    23.0/256.0,     151.0/256.0,    61.0/256.0,     189.0/256.0,    29.0/256.0,     157.0/256.0,    53.0/256.0,     181.0/256.0,    21.0/256.0,     149.0/256.0,
        255.0/256.0,    127.0/256.0,    223.0/256.0,    95.0/256.0,     247.0/256.0,    119.0/256.0,    215.0/256.0,    87.0/256.0,     253.0/256.0,    125.0/256.0,    221.0/256.0,    93.0/256.0,     245.0/256.0,    117.0/256.0,    213.0/256.0,    85.0/256.0
    };

    half screenAspect = 0, resolutionScale = 0;
    HandleDifferentResolutions(screenAspect, resolutionScale);

    // 考虑高宽比的分辨率适配
    half adjustedDitherSize = ditherSize * resolutionScale;

    float2 uv = (screenPos.xy / screenPos.w) * _ScreenParams.xy;
    uv /= adjustedDitherSize;

    float2 uvOffset = ProduceUVOffset(uv, resolutionScale);
    uv += uvOffset;

    int x = int(fmod(uv.x, 16));
    int y = int(fmod(uv.y, 16));
    int index = y * 16 + x;
    half simpleThreshold = thresholdMatrix[index];

    half complexThreshold = MULTI_LAYER_MATRIX_SUPERPOSITION(uv, 16, thresholdMatrix);

    half useComplexThreshold = step(ULTRA_WIDE_SCREEN_ASPECT, screenAspect);
    half threshold = lerp(simpleThreshold, complexThreshold, useComplexThreshold);

    clip(ditherIntensity - threshold - DITHER_EPSILON);
}


// _DitherTexture 是材质上挂的普通 Texture2D，参数和采样宏统一用非 _X 版本，
// 避免 HDRP 下 TEXTURE2D_X 展开为 Texture2DArray 与 Tex2D 类型不匹配。
void DitherWithTexture(
    float4 screenPos, half ditherIntensity, half ditherSize, half patternSize,
    TEXTURE2D_PARAM(ditherTexture, sampler_ditherTexture), float4 ditherTexture_TexelSize)
{
    half screenAspect = 0, resolutionScale = 0;
    HandleDifferentResolutions(screenAspect, resolutionScale);

    // 考虑高宽比的分辨率适配
    half adjustedDitherSize = ditherSize * resolutionScale;

    float2 uv = (screenPos.xy / screenPos.w) * _ScreenParams.xy;
    uv /= adjustedDitherSize;

    float2 uvOffset = ProduceUVOffset(uv, resolutionScale);
    uv += uvOffset;

    float2 patternUV = fmod(uv, patternSize);
    int ix = (int)patternUV.x;
    int iy = (int)patternUV.y;
    float2 tuv = (float2(ix, iy) + 0.5) / ditherTexture_TexelSize.zw;

    half simpleThreshold = SAMPLE_TEXTURE2D(ditherTexture, sampler_ditherTexture, tuv).r;

    clip(ditherIntensity - simpleThreshold - DITHER_EPSILON);
}

#endif // __TRANSPARENT_BY_DITHER__
