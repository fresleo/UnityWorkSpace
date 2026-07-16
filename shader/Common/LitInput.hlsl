#ifndef XKNIGHT_LIT_INPUT_INCLUDED
#define XKNIGHT_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Input.hlsl"

#include "../Scene/Rain.hlsl"
#include "../Scene/WaterFeatures/RainyPuddlesPack.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/OperatorInstead.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MaterialVolume.hlsl"

CBUFFER_START(UnityPerMaterial)
	float4	_BaseMap_ST;
	// float4	_DetailMap_ST;
	
	half4	_BaseColor;
	
	half4	_EmissionColor;
	half	_EmissionColorScale;

	half	_EmissionTOD;
	half4	_EmissionColor_1, _EmissionColor_2, _EmissionColor_3, _EmissionColor_4;
	half	_EmissionColorScale_1, _EmissionColorScale_2, _EmissionColorScale_3, _EmissionColorScale_4;

	half	_BumpScale;

	half	_OcclusionStrength;
	half	_RoughnessStrength;

	half	_TerrainBlendFactor;
	// half	_Cutoff;

	// Disolve
	float4	_DissolveTex_ST;
	float4	_DissolveTexChannel;
	half	_DissolveFadingMin;
	half	_DissolveFadingMax;
	half	_EdgeWidth;
	half	_DissolveCutoff;
	half4	_EdgeColor1;
	half4	_EdgeColor2;
	half3	_DissolveDir;

	// 抖动
	half	_DitherIntensity, _DitherSize, _DitherAlpha;
	half	_DitherWithMatrix;
	float4  _DitherTexture_TexelSize;

	half	_GIIndirectDiffuseBoost;
	half	_SpecularScaleBRDF;
	half4	_GIBakingAlbedoColor;
	half	_GIBakingSpecularScale;
	
	half	_AHDBakedSpecularScale, _AHDBakedSpecularDirectionBlur, _AHDBakedSpecularRougheningMaxAmount;
	half	_AHDBakedSpecularStrengthGateMin, _AHDBakedSpecularStrengthGateMax;
	half	_AHDBakedSpecularRougheningConfidenceMin, _AHDBakedSpecularRougheningConfidenceMax;

	half	_MainLightSpecularSoftClamp;
	half	_MainLightMinPerceptualRoughness;
	half	_MainLightClampPreserveBaseMapAlpha;
	half	_MainLightClampPreserveBaseMapAlphaInvert;
	half	_MainLightClampPreserveBaseMapAlphaThreshold;
	half	_ReceiveShadowsOff;

	float4	_BakerAOMap_ST;
	half	_BakerAOMapScale;

	XKNIGHT_DEPTH_MASK_INPUT_1 // 深度遮罩
	RAINY_PUDDLES_CBUFFER // 雨坑

	half	_BakedGITintIntensity; // TOD GI 调色强度

CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

TEXTURE2D(_BaseMap);			SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);			SAMPLER(sampler_BumpMap);
TEXTURE2D(_BumpMixMap);			SAMPLER(sampler_BumpMixMap);
TEXTURE2D(_BakerAOMap);			SAMPLER(sampler_BakerAOMap);
TEXTURE2D(_MetallicGlossMap);	SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_EmissionColorMap);	SAMPLER(sampler_EmissionColorMap);
// TEXTURE2D(_DetailMap);			SAMPLER(sampler_DetailMap);
TEXTURE2D(_DissolveTex);        SAMPLER(sampler_DissolveTex);
TEXTURE2D(_DitherTexture);

RAINY_PUDDLES_SAMPLE // 雨坑

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"

half4	_BakedGITint; // TOD GI 调色
half	_TODTimeIndex; // TOD 时间索引

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = 1.0f)
{
	half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
	return UnpackNormalScale(n, scale);
}

half4 SampleMetallicAOSmoothnessEmission(float2 uv)
{
	half4 same = 0;
	same = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
	// roughness to smooth
	same.r = half(1.0) - same.r;
	return same;
}

half3 SampleBakerAO(float2 uv)
{
	half3 result = 1;
	result = SAMPLE_TEXTURE2D(_BakerAOMap, sampler_BakerAOMap, uv);
	return result;
}

half3 GetEmissionColor()
{
	half3 noTodColor, todColor;

	noTodColor = _EmissionColor.rgb * _EmissionColorScale;
	
	half index_1 = if_equal(_TODTimeIndex, 0);
	half index_2 = if_equal(_TODTimeIndex, 1);
	half index_3 = if_equal(_TODTimeIndex, 2);
	half index_4 = if_equal(_TODTimeIndex, 3);
	half3 color_1 = _EmissionColor_1.rgb * _EmissionColorScale_1 * index_1;
	half3 color_2 = _EmissionColor_2.rgb * _EmissionColorScale_2 * index_2;
	half3 color_3 = _EmissionColor_3.rgb * _EmissionColorScale_3 * index_3;
	half3 color_4 = _EmissionColor_4.rgb * _EmissionColorScale_4 * index_4;
	todColor = color_1 + color_2 + color_3 + color_4;
	
	half3 color = lerp(noTodColor, todColor, _EmissionTOD);
	return color;
}

half3 GetEmission(float2 uv, half emissionStrength)
{
	#if defined( _EMISSION )
	half3 finalColor = GetEmissionColor();
	finalColor *= emissionStrength;
	finalColor *= SAMPLE_TEXTURE2D(_EmissionColorMap, sampler_EmissionColorMap, uv).rgb;
	return finalColor;
	#endif
	
	return 0;
}

half3 GetEmission(float2 uv)
{
	return GetEmission(uv, 1);
}

half GetOcclusion(half occ)
{
	return LerpWhiteTo(occ, _OcclusionStrength);
}

half4 SampleDetail(float2 uv, TEXTURE2D_PARAM(detailMap, sampler_detailMap))
{
	half4 res = half4(0, 0, 0, 0);
	half4 n = SAMPLE_TEXTURE2D(detailMap, sampler_detailMap, uv);
	res.w = n.z;
	
	half2 normal= n.xy * 2.0 - 1.0;
	n.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));

	res.xyz = UnpackNormal(n);
	return res;
}

half3 BlendAlbedo(half3 albedo, half detail)
{
	//half3 l = 1.0 - 2.0 * (1.0 - albedo) * (1.0 - detail);
	//half3 r = 2.0 * albedo * detail;
	//return albedo > 0.5 ? l : r;
	return 2.0 * albedo * detail;
}

half3 SampleNormalMapMix(float2 uv, out half metallic, out half ao)
{
	half4 bmm = SAMPLE_TEXTURE2D(_BumpMixMap, sampler_BumpMixMap, uv);

	// 当选了合图模式，但是没给图时，没有这个判断，灯光的显示会出错，但是加了就没事，所以严格来讲，这一段逻辑并不是必须的，算是一个蚊子腿
	bool isAllOnes = (bmm.x == 1.0 && bmm.y == 1.0 && bmm.z == 1.0 && bmm.w == 1.0);
	if (isAllOnes)
	{
		ao = 1.0;
		metallic = 1.0;
		return half3(0, 0, 1);
	}
	
	ao = LerpWhiteTo(bmm.b, _OcclusionStrength);
	metallic = bmm.a;

	float3 normal = half3(0, 0, 1);
	normal.xy = bmm.xy * 2.0f - 1.0f;
	normal.z = max(1.0e-16, sqrt(1.0f - saturate(dot(normal.xy, normal.xy))));
	
	return normal;
}

/**
 * 这个方法是给 meta pass 用的简化版
 */
inline void InitializeStandardLitSurfaceData(float2 uv, float3 positionWS, out SurfaceData outSurfaceData)
{
	outSurfaceData = (SurfaceData)0;

	half Alpha;
	half3 Albedo;
	half3 Specular;
	half Smoothness;
	half Occlusion;
	half Metallic;
	half3 NormalTS;
	half3 Emission;

	
	half4 baseMapCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
	
	Albedo = baseMapCol.rgb * _BaseColor.rgb;
	
	// 合图模式
	#ifdef _USE_PACKED_TEXTURE_MDOE
	Alpha = _BaseColor.a;
	
	half metallic, ao;
	half3 normalMix = SampleNormalMapMix(uv, metallic, ao);

	Smoothness = 1.0h - (baseMapCol.a * _RoughnessStrength);
	Occlusion = ao;
	Metallic = metallic;
	
	NormalTS = normalMix;
	Emission = GetEmission(uv);
	
	#else // 不合图模式

	Alpha = baseMapCol.a * _BaseColor.a;
	
	half4 same = SampleMetallicAOSmoothnessEmission(uv);
	
	Smoothness = same.r;
	Occlusion = GetOcclusion(same.g);
	Metallic = same.b;
	
	NormalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
	Emission = GetEmission(uv, same.a);
	#endif // 合图模式

	Specular = half3(0, 0, 0);

	// 细节纹理
	// #ifdef _DETAIL
	// float2 detailUv = TRANSFORM_TEX(uv, _DetailMap);
	// half4 detail = SampleDetail(detailUv, TEXTURE2D_ARGS(_DetailMap, sampler_DetailMap));
	//
	// Albedo = BlendAlbedo(Albedo, detail.w);
	// NormalTS = BlendNormalRNM(NormalTS, detail.xyz);
	// #endif // 细节纹理


	// 赋值
	outSurfaceData.alpha = Alpha;
	outSurfaceData.albedo = Albedo;
	outSurfaceData.specular = Specular;
	outSurfaceData.smoothness = Smoothness;
	outSurfaceData.occlusion = Occlusion;
	outSurfaceData.metallic = Metallic;
	outSurfaceData.normalTS = NormalTS;
	outSurfaceData.emission = Emission;
	
	outSurfaceData.clearCoatMask = half(0);
	outSurfaceData.clearCoatSmoothness = half(0);
}

#endif  //XKNIGHT_LIT_INPUT_INCLUDED
