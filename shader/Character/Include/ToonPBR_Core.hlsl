#ifndef TOONPBR_CORE
#define TOONPBR_CORE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

half StepFeatherToon(half value,half step, half feather)
{
    return saturate((value - step + feather) / feather);
}

half CellShadingDiffuse(half value,half step, half feather)
{
    return saturate(1 + (value - step - feather) / max(feather, 1e-3));
}

//高光类型->头发高光
//(HairSpecularTangent)：头发高光切线
//(HairSpecularViewNormal)：头发高光观察方向法线

half DirectSpecularHairViewNormalToon(half specularExponent, half3 normalWS, half3 viewDirectionWS, half specularStep, half specularFeather)
{
    half NdotV = saturate(dot(normalize(normalWS.xz), normalize(viewDirectionWS.xz)));
    half spec = pow(NdotV, specularExponent);

    return StepFeatherToon(spec, specularStep, specularFeather);
}

//AntiAliasing,we use to calculate shadow
half StepAntiAliasing(half x, half y)
{
    half v = x - y;
    return saturate(v / (fwidth(v)+6.103515625e-5)); 
}

//Use for HairSpecular,Roughness To BlinnPhong
float RoughnessToBlinnPhongSpecularExponent(float roughness)
{
    // return clamp(2 * rcp(roughness * roughness) - 2, FLT_EPS, rcp(FLT_EPS));
    return clamp(2 * rcp(roughness * roughness) - 2, 5.960464478e-8, rcp(5.960464478e-8));
    // FLT_EPS = 5.960464478e-8
}

//头发阴影遮罩衰减
half HairShadowMaskAtten(half hairShadowMask, half H_Lambert)
{
    half hairAtten = lerp(1, hairShadowMask, H_Lambert);
    return hairAtten;
}

// 旋转Cubemap
half3 RotateAround(half degree, half3 target)
{
    float rad = degree * PI/180;
    float2x2 m_rotate = float2x2(cos(rad), -sin(rad), sin(rad), cos(rad));
    float2 dir_rotate = mul(m_rotate, target.xz);
    target = float3(dir_rotate.x, target.y, dir_rotate.y);
    return target;
}

// ----------------------------------------------------------------------------------
// 颜色空间转换方法
// 来自 Packages/com.unity.render-pipelines.universal/Shaders/Extend/PostProcessing/Common.hlsl
// 因为 'Attributes' 结构的冲突，所以不能直接引用文件

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

real3 GetSRGBToLinear(real3 c)
{
    #if _USE_FAST_SRGB_LINEAR_CONVERSION
    return FastSRGBToLinear(c);
    #else
    return SRGBToLinear(c);
    #endif
}

real4 GetSRGBToLinear(real4 c)
{
    #if _USE_FAST_SRGB_LINEAR_CONVERSION
    return FastSRGBToLinear(c);
    #else
    return SRGBToLinear(c);
    #endif
}

real3 GetLinearToSRGB(real3 c)
{
    #if _USE_FAST_SRGB_LINEAR_CONVERSION
    return FastLinearToSRGB(c);
    #else
    return LinearToSRGB(c);
    #endif
}

real4 GetLinearToSRGB(real4 c)
{
    #if _USE_FAST_SRGB_LINEAR_CONVERSION
    return FastLinearToSRGB(c);
    #else
    return LinearToSRGB(c);
    #endif
}

float ComputeLocalFactor(float4x4 worldToLocal, float3 positionWS)
{
    float3 positionLS = mul(worldToLocal, float4(positionWS, 1)).xyz;
    float3 absPLS = abs(positionLS);
    float sd = max(max(absPLS.x - 1, absPLS.y - 1), absPLS.z - 1);

    float fadeWidth = 0.1;
    float localFactor = 1 - smoothstep(0, fadeWidth, sd);

    float currentState = worldToLocal[3][3];
    return localFactor * currentState;
}

#endif // TOONPBR_CORE
