#ifndef __INTERACTIVE_SNOW_INPUT__
#define __INTERACTIVE_SNOW_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Input.hlsl"

CBUFFER_START(UnityPerMaterial)
	float4	_BaseMap_ST, _BumpMap_ST, _DetailMap_ST;
	half4	_BaseColor, _EmissionColor;
	half	_EmissionColorScale;
	half	_BumpScale;
	half	_OcclusionStrength;
	half	_TerrainBlendFactor;
	half	_Cutoff;
	half	_GIIndirectDiffuseBoost;
	// 遮罩
	XKNIGHT_DEPTH_MASK_INPUT_1
	// 交互式顶点变形
	float4	_IVD_Mask_ST, _IVD_Noise_ST;
	float	_IVD_MaskIntensity, _IVD_VertexNoiseIntensity;
	half4	_IVD_SeamTintColor;
CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

TEXTURE2D(_BaseMap);				SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);				SAMPLER(sampler_BumpMap);
TEXTURE2D(_BumpMixMap);			SAMPLER(sampler_BumpMixMap);
TEXTURE2D(_MetallicGlossMap);		SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_EmissionColorMap);		SAMPLER(sampler_EmissionColorMap);
TEXTURE2D(_DetailMap);				SAMPLER(sampler_DetailMap);
TEXTURE2D(_IVD_Mask);				SAMPLER(sampler_IVD_Mask);
TEXTURE2D(_IVD_Noise);				SAMPLER(sampler_IVD_Noise);

// 采样法线纹理
half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = 1.0f)
{
	half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
	return UnpackNormalScale(n, scale);
}

// 采样遮罩纹理
half4 SampleMetallicAOSmoothnessEmission(float2 uv, half albedoAlpha)
{
	//smoothness ao metallic emission 
	half4 same = half4(0,0,0,0);

	same = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
	// roughness to smooth
	same.r = half(1.0) - same.r;
	return same;
}

half3 GetEmission(float2 uv, half3 emissionColor, half emissionStrength)
{
	#if defined( _EMISSION )
	return emissionColor * emissionStrength * SAMPLE_TEXTURE2D(_EmissionColorMap, sampler_EmissionColorMap, uv).rgb;
	#endif

	return 0;
}

half3 GetEmission(float2 uv, half3 emissionColor)
{
	#if defined( _EMISSION )
	return emissionColor * SAMPLE_TEXTURE2D(_EmissionColorMap, sampler_EmissionColorMap, uv).rgb;
	#endif

	return 0;
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
	float4 n = SAMPLE_TEXTURE2D(_BumpMixMap, sampler_BumpMixMap, uv);
	
	ao = LerpWhiteTo(n.b, _OcclusionStrength);
	metallic = n.a;

	float3 normal;
	normal.xy = n.xy * 2.0f - 1.0f;
	normal.z = max(1.0e-16, sqrt(1.0f - saturate(dot(normal.xy, normal.xy))));

	return normal;
}

// 采样标准光照表面数据 - meta pass 也会用到，所以被放在 input 里了
inline void InitializeStandardLitSurfaceData(float4 uv, float2 maskUV, out SurfaceData outSurfaceData)
{
	half4 albedoT2d = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy);
	outSurfaceData.alpha = albedoT2d.a * _BaseColor.a;

	#if defined(_ALPHATEST_ON)
	clip(outSurfaceData.alpha - _Cutoff);
	#endif

	half3 albedo = albedoT2d.rgb * _BaseColor.rgb;
	half4 same = SampleMetallicAOSmoothnessEmission(uv.xy, albedoT2d.a);

	outSurfaceData.albedo = albedo;
	outSurfaceData.specular = half3(0.0, 0.0, 0.0);
	
	outSurfaceData.metallic = same.b;
	outSurfaceData.smoothness = same.r;
	outSurfaceData.occlusion = GetOcclusion(same.g);

	outSurfaceData.normalTS = SampleNormal(uv.zw, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
	outSurfaceData.emission = GetEmission(uv.xy, _EmissionColor.rgb * _EmissionColorScale, same.a);

	// 临时代码，全部转换完毕后删除
	#ifdef _USE_PACKED_TEXTURE_MDOE
	half metallic, ao;
	half3 normalMix = SampleNormalMapMix(uv, metallic, ao);
	
	outSurfaceData.metallic = metallic;
	outSurfaceData.smoothness = 1.0f - albedoT2d.a;
	outSurfaceData.occlusion = ao;
	
	outSurfaceData.normalTS = normalMix;
	outSurfaceData.emission = GetEmission(uv, _EmissionColor.rgb * _EmissionColorScale);
	#endif
	
	#if defined(_DETAIL)
	float2 detailUv = TRANSFORM_TEX(uv.xy, _DetailMap);
	half4 detail = SampleDetail(detailUv, TEXTURE2D_ARGS(_DetailMap, sampler_DetailMap));

	outSurfaceData.albedo = BlendAlbedo(outSurfaceData.albedo, detail.w);
	outSurfaceData.normalTS = BlendNormalRNM(outSurfaceData.normalTS, detail.xyz);
	#endif

	#if defined(_IVD_ON)
	// 根据遮罩来决定使用的颜色
	float mask = SAMPLE_TEXTURE2D(_IVD_Mask, sampler_IVD_Mask, maskUV).r;
	half3 seamTintColor = lerp(1, _IVD_SeamTintColor, mask);
	outSurfaceData.albedo *= seamTintColor;
	#endif
	
	outSurfaceData.clearCoatMask = half(0.0);
	outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // __INTERACTIVE_SNOW_INPUT__
