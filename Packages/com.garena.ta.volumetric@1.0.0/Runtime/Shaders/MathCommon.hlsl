#ifndef MY_MATH_COMMON
#define MY_MATH_COMMON

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

#endif