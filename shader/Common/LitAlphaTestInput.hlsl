#ifndef XKNIGHT_LIT_ALPHA_TEST_INPUT_INCLUDED
#define XKNIGHT_LIT_ALPHA_TEST_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/SurfaceInput_AlphaTestOn.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/OperatorInstead.hlsl"

CBUFFER_START(UnityPerMaterial)
	float4	_BaseMap_ST;
	//float4	_DetailMap_ST;
	float4	_WindowDetailMap_ST;
	half4	_BaseColor;
	
	half4	_EmissionColor;
	half	_EmissionColorScale;

	half	_EmissionTOD;
	half4	_EmissionColor_1, _EmissionColor_2, _EmissionColor_3, _EmissionColor_4;
	half	_EmissionColorScale_1, _EmissionColorScale_2, _EmissionColorScale_3, _EmissionColorScale_4;

	half	_BumpScale;

	half	_OcclusionStrength;
	half	_RoughnessStrength;

	half	_Surface;
	half	_Cutoff;

	float4	_BakerAOMap_ST;

	// 抖动
	half	_DitherIntensity, _DitherSize, _DitherAlpha;
	half	_DitherWithMatrix;
	float4  _DitherTexture_TexelSize;

	// 风场
	float	_WindVariation;
	float	_WindStrength;
	float	_TurbulenceStrength;

	half	_GIIndirectDiffuseBoost;
	half	_SpecularScaleBRDF;

	XKNIGHT_DEPTH_MASK_INPUT_1

	half	_BakedGITintIntensity; // TOD GI 调色强度

    half    _FrostIntensity;
    float4  _FrostTexture_ST;
	half    _IOR;
	half    _RefractStrengthPX;

	//Noise
	half4 _NoiseUVTiling;
	half4 _DetailFresnelRange;
	
CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMixMap);         SAMPLER(sampler_BumpMixMap);
TEXTURE2D(_BakerAOMap);			SAMPLER(sampler_BakerAOMap);
TEXTURE2D(_EmissionColorMap);	SAMPLER(sampler_EmissionColorMap);
// TEXTURE2D(_DetailMap);			SAMPLER(sampler_DetailMap);
TEXTURE2D(_AlphaTestMap);       SAMPLER(sampler_AlphaTestMap);
TEXTURE2D(_DitherTexture);
TEXTURE2D(_WindowDetailMap);			SAMPLER(sampler_WindowDetailMap);
TEXTURE2D(_FrostTexture);       SAMPLER(sampler_FrostTexture);

half4	_BakedGITint; // TOD GI 调色
half	_TODTimeIndex; // TOD 时间索引

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = 1.0f)
{
	half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
	return UnpackNormalScale(n, scale);
}

half3 SampleBakerAO(float2 uv)
{
	half3 ao = 1.0;
	ao = SAMPLE_TEXTURE2D(_BakerAOMap, sampler_BakerAOMap, uv);
	return ao;
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

half3 GetEmission(float2 uv)
{
	#if defined( _EMISSION )
	return GetEmissionColor() * SAMPLE_TEXTURE2D(_EmissionColorMap, sampler_EmissionColorMap, uv).rgb;
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

float3 SampleNormalMapMix(float2 uv, out float metallic, out float ao)
{
	float4 bmm = SAMPLE_TEXTURE2D(_BumpMixMap, sampler_BumpMixMap, uv);

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

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
	outSurfaceData = (SurfaceData)0;

	half outAlpha;
	half3 outAlbedo;
	half3 outSpecular;
	half outSmoothness;
	half outOcclusion;
	half outMetallic;
	half3 outNormalTS;
	half3 outEmission;
	
	half4 albedoRoughness = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
	
	half alphaTest = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_AlphaTestMap, sampler_AlphaTestMap)).a;
	outAlpha = Alpha(alphaTest, _BaseColor, _Cutoff);

	outAlbedo = albedoRoughness.rgb * _BaseColor.rgb;
	outSpecular = half3(0.0, 0.0, 0.0);

	outSmoothness = 1.0f - (albedoRoughness.a * _RoughnessStrength);
	
	float metallic, ao;
	float3 normalMix = SampleNormalMapMix(uv, metallic, ao);
	
	outOcclusion = ao;
	outMetallic = metallic;
	outNormalTS = normalMix;
	
	outEmission = GetEmission(uv);
	
	#if _DETAIL
	float2 detailUv = TRANSFORM_TEX(uv, _DetailMap);
	half4 detail = SampleDetail(detailUv, TEXTURE2D_ARGS(_DetailMap, sampler_DetailMap));
	
	outNormalTS = BlendNormalRNM(outNormalTS, detail.xyz);
	outAlbedo = BlendAlbedo(outAlbedo, detail.w);
	#endif // _DETAIL

	// 赋值
	outSurfaceData.alpha = outAlpha;
	outSurfaceData.albedo = outAlbedo;
	outSurfaceData.specular = outSpecular;
	outSurfaceData.smoothness = outSmoothness;
	outSurfaceData.occlusion = outOcclusion;
	outSurfaceData.metallic = outMetallic;
	outSurfaceData.normalTS = outNormalTS;
	outSurfaceData.emission = outEmission;
	
	outSurfaceData.clearCoatMask = half(0.0);
	outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif //XKNIGHT_LIT_ALPHA_TEST_INPUT_INCLUDED