#ifndef __XKNIGHT_BRDF_XK__
#define __XKNIGHT_BRDF_XK__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"

/*
 * 重新计算 BRDF 的 粗糙度项
 * 逻辑来自 BRDF.hlsl 62行的 InitializeBRDFDataDirect 方法
 */
void RecalculationBRDFDataRoughness(inout BRDFData brdfData)
{
    brdfData.roughness           = max(PerceptualRoughnessToRoughness(brdfData.perceptualRoughness), HALF_MIN_SQRT);
    brdfData.roughness2          = max(brdfData.roughness * brdfData.roughness, HALF_MIN);
    brdfData.normalizationTerm   = brdfData.roughness * half(4.0) + half(2.0);
    brdfData.roughness2MinusOne  = brdfData.roughness2 - half(1.0);
}

#endif
