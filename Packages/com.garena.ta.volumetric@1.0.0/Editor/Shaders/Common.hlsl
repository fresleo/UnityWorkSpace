#ifndef MY_COMMON
#define MY_COMMON
// Ported from GLM: https://github.com/g-truc/glm/blob/master/glm/gtc/noise.inl

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
//#define PI 3.141592654
#define OneOver2PI 0.159154943071114

float4 mod289(const float4 x) {
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 permute(const float4 v) {
    return mod289((v * 34.0 + 1.0) * v);
}

float4 taylorInvSqrt(const float4 r) {
    return 1.79284291400159 - 0.85373472095314 * r;
}

float4 fade(const float4 v) {
    return v * v * v * (v * (v * 6.0 - 15.0) + 10.0);
}

// Classic Perlin noise, periodic version
float perlin(const float4 position, const float4 rep) {
    float4 Pi0 = fmod(floor(position), rep); // Integer part modulo rep
    float4 Pi1 = fmod(Pi0 + 1.0, rep); // Integer part + 1 fmod rep
    float4 Pf0 = frac(position); // Fractional part for interpolation
    float4 Pf1 = Pf0 - 1.0; // Fractional part - 1.0
    float4 ix = float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
    float4 iy = float4(Pi0.y, Pi0.y, Pi1.y, Pi1.y);
    float4 iz0 = float4(Pi0.zzzz);
    float4 iz1 = float4(Pi1.zzzz);
    float4 iw0 = float4(Pi0.wwww);
    float4 iw1 = float4(Pi1.wwww);

    float4 ixy = permute(permute(ix) + iy);
    float4 ixy0 = permute(ixy + iz0);
    float4 ixy1 = permute(ixy + iz1);
    float4 ixy00 = permute(ixy0 + iw0);
    float4 ixy01 = permute(ixy0 + iw1);
    float4 ixy10 = permute(ixy1 + iw0);
    float4 ixy11 = permute(ixy1 + iw1);

    float4 gx00 = ixy00 / 7.0;
    float4 gy00 = floor(gx00) / 7.0;
    float4 gz00 = floor(gy00) / 6.0;
    gx00 = frac(gx00) - 0.5;
    gy00 = frac(gy00) - 0.5;
    gz00 = frac(gz00) - 0.5;
    float4 gw00 = float4(0.75, 0.75, 0.75, 0.75) - abs(gx00) - abs(gy00) - abs(gz00);
    float4 sw00 = step(gw00, float4(0, 0, 0, 0));
    gx00 -= sw00 * (step(0.0, gx00) - 0.5);
    gy00 -= sw00 * (step(0.0, gy00) - 0.5);

    float4 gx01 = ixy01 / 7.0;
    float4 gy01 = floor(gx01) / 7.0;
    float4 gz01 = floor(gy01) / 6.0;
    gx01 = frac(gx01) - 0.5;
    gy01 = frac(gy01) - 0.5;
    gz01 = frac(gz01) - 0.5;
    float4 gw01 = float4(0.75, 0.75, 0.75, 0.75) - abs(gx01) - abs(gy01) - abs(gz01);
    float4 sw01 = step(gw01, float4(0.0, 0, 0, 0));
    gx01 -= sw01 * (step(0.0, gx01) - 0.5);
    gy01 -= sw01 * (step(0.0, gy01) - 0.5);

    float4 gx10 = ixy10 / 7.0;
    float4 gy10 = floor(gx10) / 7.0;
    float4 gz10 = floor(gy10) / 6.0;
    gx10 = frac(gx10) - 0.5;
    gy10 = frac(gy10) - 0.5;
    gz10 = frac(gz10) - 0.5;
    float4 gw10 = float4(0.75, 0.75, 0.75, 0.75) - abs(gx10) - abs(gy10) - abs(gz10);
    float4 sw10 = step(gw10, float4(0.0, 0, 0, 0));
    gx10 -= sw10 * (step(0.0, gx10) - 0.5);
    gy10 -= sw10 * (step(0.0, gy10) - 0.5);

    float4 gx11 = ixy11 / 7.0;
    float4 gy11 = floor(gx11) / 7.0;
    float4 gz11 = floor(gy11) / 6.0;
    gx11 = frac(gx11) - 0.5;
    gy11 = frac(gy11) - 0.5;
    gz11 = frac(gz11) - 0.5;
    float4 gw11 = float4(0.75, 0.75, 0.75, 0.75) - abs(gx11) - abs(gy11) - abs(gz11);
    float4 sw11 = step(gw11, float4(0, 0, 0, 0));
    gx11 -= sw11 * (step(0.0, gx11) - 0.5);
    gy11 -= sw11 * (step(0.0, gy11) - 0.5);

    float4 g0000 = float4(gx00.x, gy00.x, gz00.x, gw00.x);
    float4 g1000 = float4(gx00.y, gy00.y, gz00.y, gw00.y);
    float4 g0100 = float4(gx00.z, gy00.z, gz00.z, gw00.z);
    float4 g1100 = float4(gx00.w, gy00.w, gz00.w, gw00.w);
    float4 g0010 = float4(gx10.x, gy10.x, gz10.x, gw10.x);
    float4 g1010 = float4(gx10.y, gy10.y, gz10.y, gw10.y);
    float4 g0110 = float4(gx10.z, gy10.z, gz10.z, gw10.z);
    float4 g1110 = float4(gx10.w, gy10.w, gz10.w, gw10.w);
    float4 g0001 = float4(gx01.x, gy01.x, gz01.x, gw01.x);
    float4 g1001 = float4(gx01.y, gy01.y, gz01.y, gw01.y);
    float4 g0101 = float4(gx01.z, gy01.z, gz01.z, gw01.z);
    float4 g1101 = float4(gx01.w, gy01.w, gz01.w, gw01.w);
    float4 g0011 = float4(gx11.x, gy11.x, gz11.x, gw11.x);
    float4 g1011 = float4(gx11.y, gy11.y, gz11.y, gw11.y);
    float4 g0111 = float4(gx11.z, gy11.z, gz11.z, gw11.z);
    float4 g1111 = float4(gx11.w, gy11.w, gz11.w, gw11.w);

    float4 norm00 = taylorInvSqrt(
        float4(dot(g0000, g0000), dot(g0100, g0100), dot(g1000, g1000), dot(g1100, g1100))
    );
    g0000 *= norm00.x;
    g0100 *= norm00.y;
    g1000 *= norm00.z;
    g1100 *= norm00.w;

    float4 norm01 = taylorInvSqrt(
        float4(dot(g0001, g0001), dot(g0101, g0101), dot(g1001, g1001), dot(g1101, g1101))
    );
    g0001 *= norm01.x;
    g0101 *= norm01.y;
    g1001 *= norm01.z;
    g1101 *= norm01.w;

    float4 norm10 = taylorInvSqrt(
        float4(dot(g0010, g0010), dot(g0110, g0110), dot(g1010, g1010), dot(g1110, g1110))
    );
    g0010 *= norm10.x;
    g0110 *= norm10.y;
    g1010 *= norm10.z;
    g1110 *= norm10.w;

    float4 norm11 = taylorInvSqrt(
        float4(dot(g0011, g0011), dot(g0111, g0111), dot(g1011, g1011), dot(g1111, g1111))
    );
    g0011 *= norm11.x;
    g0111 *= norm11.y;
    g1011 *= norm11.z;
    g1111 *= norm11.w;

    float n0000 = dot(g0000, Pf0);
    float n1000 = dot(g1000, float4(Pf1.x, Pf0.y, Pf0.z, Pf0.w));
    float n0100 = dot(g0100, float4(Pf0.x, Pf1.y, Pf0.z, Pf0.w));
    float n1100 = dot(g1100, float4(Pf1.x, Pf1.y, Pf0.z, Pf0.w));
    float n0010 = dot(g0010, float4(Pf0.x, Pf0.y, Pf1.z, Pf0.w));
    float n1010 = dot(g1010, float4(Pf1.x, Pf0.y, Pf1.z, Pf0.w));
    float n0110 = dot(g0110, float4(Pf0.x, Pf1.y, Pf1.z, Pf0.w));
    float n1110 = dot(g1110, float4(Pf1.x, Pf1.y, Pf1.z, Pf0.w));
    float n0001 = dot(g0001, float4(Pf0.x, Pf0.y, Pf0.z, Pf1.w));
    float n1001 = dot(g1001, float4(Pf1.x, Pf0.y, Pf0.z, Pf1.w));
    float n0101 = dot(g0101, float4(Pf0.x, Pf1.y, Pf0.z, Pf1.w));
    float n1101 = dot(g1101, float4(Pf1.x, Pf1.y, Pf0.z, Pf1.w));
    float n0011 = dot(g0011, float4(Pf0.x, Pf0.y, Pf1.z, Pf1.w));
    float n1011 = dot(g1011, float4(Pf1.x, Pf0.y, Pf1.z, Pf1.w));
    float n0111 = dot(g0111, float4(Pf0.x, Pf1.y, Pf1.z, Pf1.w));
    float n1111 = dot(g1111, Pf1);

    float4 fade_xyzw = fade(Pf0);
    float4 n_0w = lerp(float4(n0000, n1000, n0100, n1100), float4(n0001, n1001, n0101, n1101), fade_xyzw.w);
    float4 n_1w = lerp(float4(n0010, n1010, n0110, n1110), float4(n0011, n1011, n0111, n1111), fade_xyzw.w);
    float4 n_zw = lerp(n_0w, n_1w, fade_xyzw.z);
    float2 n_yzw = lerp(n_zw.xy, n_zw.zw, fade_xyzw.y);
    float n_xyzw = lerp(n_yzw.x, n_yzw.y, fade_xyzw.x);
    return 2.2 * n_xyzw;
}


// Based on the following work with slight modifications.
// https://github.com/sebh/TileableVolumeNoise

/**
 * The MIT License (MIT)
 *
 * Copyright(c) 2017 Sébastien Hillaire
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

float hash(const float n) {
    return frac(sin(n + 1.951) * 43758.5453);
}

float noise(const float3 x) {
    float3 p = floor(x);
    float3 f = frac(x);

    f = f * f * (3.0 - 2.0 * f);
    float n = p.x + p.y * 57.0 + 113.0 * p.z;
    return lerp(
        lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x), lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
        lerp(
            lerp(hash(n + 113.0), hash(n + 114.0), f.x),
            lerp(hash(n + 170.0), hash(n + 171.0), f.x),
            f.y
        ),
        f.z
    );
}



float getWorleyNoise(const float3 p, const float cellCount) {
    float3 cell = p * cellCount;
    float d = 1.0e10;
    for (int x = -1; x <= 1; ++x) {
        for (int y = -1; y <= 1; ++y) {
            for (int z = -1; z <= 1; ++z) {
                float3 tp = floor(cell) + float3(x, y, z);
                tp = cell - tp - noise(fmod(tp, cellCount / 1.0));
                d = min(d, dot(tp, tp));
            }
        }
    }
    return clamp(d, 0.0, 1.0);
}

// 周期性 hash 函数，返回 [0,1] 的伪随机 float3
float3 tileableHash33(float3 p, float period) {
    p = fmod(p, period); // wrap 到周期范围
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.xxy + p.yzz) * p.zyx);
}

// 四方连续的 Worley 噪声
float getTileableWorleyNoise(float2 uv, float cellCount) {
    // 将 UV 映射到环形表面（周期性域）
    float2 angle = uv * 2.0 * PI;

    // 创建 4D 点在环形表面上
    float4 p = float4(
        cos(angle.x), sin(angle.x),  // X轴上的圆
        cos(angle.y), sin(angle.y)   // Y轴上的圆
    ) * cellCount * OneOver2PI; //乘以OneOver2PI以保持和getWorleyNoise相同的cell参数尺寸

    float minDist = 1e10;

    // 在 4D 空间中搜索最近的 Worley 点
    for (int i = -1; i <= 1; i++) {
        for (int j = -1; j <= 1; j++) {
            for (int k = -1; k <= 1; k++) {
                for (int l = -1; l <= 1; l++) {
                    float4 cell = floor(p) + float4(i, j, k, l);
                    float4 noise4d = float4(
                        hash(cell.x + cell.y * 57.0 + cell.z * 113.0 + cell.w * 289.0),
                        hash(cell.x + cell.y * 57.0 + cell.z * 113.0 + cell.w * 289.0 + 1.0),
                        hash(cell.x + cell.y * 57.0 + cell.z * 113.0 + cell.w * 289.0 + 2.0),
                        hash(cell.x + cell.y * 57.0 + cell.z * 113.0 + cell.w * 289.0 + 3.0)
                    );

                    float4 diff = p - cell - noise4d;
                    float dist = dot(diff, diff);
                    minDist = min(minDist, dist);
                }
            }
        }
    }
    return clamp(minDist, 0.0, 1.0);
}

float getTileableWorleyNoise(float3 pos, float cellCount) {
    float3 angle = pos * 2.0 * PI;
    float3 p0 = float3(cos(angle.x), sin(angle.x), cos(angle.y));
    float3 p1 = float3(sin(angle.y), cos(angle.z), sin(angle.z));

    p0 *= cellCount * OneOver2PI;
    p1 *= cellCount * OneOver2PI;

    float minDist = 1e10;

    for (int i = -1; i <= 1; i++) {
        for (int j = -1; j <= 1; j++) {
            for (int k = -1; k <= 1; k++) {
                for (int l = -1; l <= 1; l++) {
                    for (int m = -1; m <= 1; m++) {
                        for (int n = -1; n <= 1; n++) {
                            float3 cell0 = floor(p0) + float3(i, j, k);
                            float3 cell1 = floor(p1) + float3(l, m, n);

                            float seed = dot(cell0, float3(1.0, 57.0, 113.0)) + dot(cell1, float3(289.0, 661.0, 911.0));

                            float3 jitter0 = float3(
                                hash(seed + 0.0),
                                hash(seed + 1.0),
                                hash(seed + 2.0)
                                );

                            float3 jitter1 = float3(
                                hash(seed + 3.0),
                                hash(seed + 4.0),
                                hash(seed + 5.0)
                                );

                            float3 diff0 = p0 - cell0 - jitter0;
                            float3 diff1 = p1 - cell1 - jitter1;

                            float dist = dot(diff0, diff0) + dot(diff1, diff1);
                            minDist = min(minDist, dist);
                        }
                    }
                }
            }
        }
    }

    return clamp(minDist, 0.0, 1.0);
}



float getPerlinNoise(const float3 pointPos, const float3 frequency, const int octaveCount) {
    // Noise frequency factor between octave, forced to 2.
    const float octaveFrequencyFactor = 2.0;

    // Compute the sum for each octave.
    float sum = 0.0;
    float roughness = 0.5;
    float weightSum = 0.0;
    float weight = 1.0;
    float3 nextFrequency = frequency;
    for (int i = 0; i < octaveCount; ++i) {
        float4 p = float4(pointPos.x, pointPos.y, pointPos.z, 0.0) * float4(nextFrequency, 1.0);
        float value = perlin(p, float4(nextFrequency, 1.0));
        sum += value * weight;
        weightSum += weight;
        weight *= roughness;
        nextFrequency *= octaveFrequencyFactor;
    }

    return sum / weightSum; // Intentionally skip clamping.
}

float getPerlinNoise(const float3 pointPos, const float frequency, const int octaveCount) {
    return getPerlinNoise(pointPos, float3(frequency, frequency, frequency), octaveCount);
}
/*
//-----------------math-----------------
float remap(const float x, const float min1, const float max1, const float min2, const float max2) {
    return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float2 remap(const float2 x, const float2 min1, const float2 max1, const float2 min2, const float2 max2) {
    return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float3 remap(const float3 x, const float3 min1, const float3 max1, const float3 min2, const float3 max2) {
    return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float4 remap(const float4 x, const float4 min1, const float4 max1, const float4 min2, const float4 max2) {
    return min2 + (x - min1) / (max1 - min1) * (max2 - min2);
}

float remapClamped(
    const float x,
    const float min1,
    const float max1,
    const float min2,
    const float max2
) {
    return clamp(min2 + (x - min1) / (max1 - min1) * (max2 - min2), min2, max2);
}

float2 remapClamped(
    const float2 x,
    const float2 min1,
    const float2 max1,
    const float2 min2,
    const float2 max2
) {
    return clamp(min2 + (x - min1) / (max1 - min1) * (max2 - min2), min2, max2);
}

float3 remapClamped(
    const float3 x,
    const float3 min1,
    const float3 max1,
    const float3 min2,
    const float3 max2
) {
    return clamp(min2 + (x - min1) / (max1 - min1) * (max2 - min2), min2, max2);
}

float4 remapClamped(
    const float4 x,
    const float4 min1,
    const float4 max1,
    const float4 min2,
    const float4 max2
) {
    return clamp(min2 + (x - min1) / (max1 - min1) * (max2 - min2), min2, max2);
}

// Implicitly remap to 0 and 1
float remap(const float x, const float min1, const float max1) {
    return (x - min1) / (max1 - min1);
}

float2 remap(const float2 x, const float2 min1, const float2 max1) {
    return (x - min1) / (max1 - min1);
}

float3 remap(const float3 x, const float3 min1, const float3 max1) {
    return (x - min1) / (max1 - min1);
}

float4 remap(const float4 x, const float4 min1, const float4 max1) {
    return (x - min1) / (max1 - min1);
}

float remapClamped(const float x, const float min1, const float max1) {
    return saturate((x - min1) / (max1 - min1));
}

float2 remapClamped(const float2 x, const float2 min1, const float2 max1) {
    return saturate((x - min1) / (max1 - min1));
}

float3 remapClamped(const float3 x, const float3 min1, const float3 max1) {
    return saturate((x - min1) / (max1 - min1));
}

float4 remapClamped(const float4 x, const float4 min1, const float4 max1) {
    return saturate((x - min1) / (max1 - min1));
}
*/
#endif