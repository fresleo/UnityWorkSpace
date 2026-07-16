#ifndef XKNIGHT_CRYSTAL_INPUT
#define XKNIGHT_CRYSTAL_INPUT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

CBUFFER_START(UnityPerMaterial)
	float4	_BaseMap_ST;
	half4	_BaseColor;
	half4	_EmissionColor;
	half	_BumpScale;
	half	_OcclusionStrength;

	half	_DistortIntensity;
	half4   _NoiseColor;
	half	_NoiseColorPower;
	half	_NoiseColorMultiply;
	half	_NoiseRangePower;
	half	_NoiseRangeMultiply;

	float4  _DistortTex_ScaleOffset;
	float4	_NoiseTex_ScaleOffset;

	float4  _ReflectionCube_HDR;

	// IceDepth
	half4	_IceColor;
	half	_IceSaturation;
	half	_IceLayer;
	half	_IceOffset;
	half	_IceBlur;
	half	_IceLOD;

	// 旋涡
	half4   _SpiralFlowColor;
	half4   _TilingAndSpeed;
	half    _RadialStrength;

	// Disolve
	float4	_DissolveTex_ST;
	float4	_DissolveTexChannel;
	half	_DissolveFadingMin;
	half	_DissolveFadingMax;
	half	_EdgeWidth;
	half4	_EdgeColor1;
	half4	_EdgeColor2;
	half	_DissolveCutoff;
	half3	_DissolveDir;
CBUFFER_END

TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
TEXTURE2D(_MetallicGlossMap);	SAMPLER(sampler_MetallicGlossMap);
TEXTURECUBE(_ReflectionCube);	SAMPLER(sampler_ReflectionCube);
TEXTURE2D(_DistortTex);			SAMPLER(sampler_DistortTex);
TEXTURE2D(_NoiseTex);			SAMPLER(sampler_NoiseTex);
TEXTURE2D(_DissolveTex);        SAMPLER(sampler_DissolveTex);
TEXTURE2D(_SpiralFlowTex);      SAMPLER(sampler_SpiralFlowTex);
TEXTURE2D(_SpiralFlowMaskTex);  SAMPLER(sampler_SpiralFlowMaskTex);

half3 VolumeColor(float2 uv, float emissiveRange)
{
	// 扭曲的信息
	float2 distortUV = uv * _DistortTex_ScaleOffset.xy + _Time.x * _DistortTex_ScaleOffset.zw;
	float2 distort = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortUV).rg * _DistortIntensity;

	float2 noise1UV = uv * _NoiseTex_ScaleOffset.xy + _Time.x * _NoiseTex_ScaleOffset.zw + distort;
	half3 noiseColor = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noise1UV);
	noiseColor = pow(noiseColor, _NoiseColorPower) * _NoiseColorMultiply * _NoiseColor.rgb;
	float noiseRange = smoothstep(_NoiseRangePower, _NoiseRangeMultiply, emissiveRange);

	return noiseColor * noiseRange;
}


half3 SpiralFlowColor(float2 uv)
{
	float2 radialUV = uv * 2.0 - 1.0;
	radialUV = float2(frac(atan2(radialUV.x, radialUV.y) / TWO_PI), length(radialUV));
	radialUV = _TilingAndSpeed.xy  * (radialUV + _TimeParameters.xx * _TilingAndSpeed.zw);

	half3 spiralColor = SAMPLE_TEXTURE2D(_SpiralFlowTex, sampler_SpiralFlowTex, radialUV).x * _RadialStrength * _SpiralFlowColor;
	spiralColor = spiralColor * SAMPLE_TEXTURE2D(_SpiralFlowMaskTex, sampler_SpiralFlowMaskTex, uv).x;

	return spiralColor.rgb;
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = 1.0f)
{
	half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
	return UnpackNormalScale(n, scale);
}

half4 SampleMetallicAOSmoothnessEmission(float2 uv, half albedoAlpha)
{
	//smoothness ao metallic emission 
	half4 same = half4(0,0,0,0);
	
	same = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
	// roughness to smooth
	same.r = 1.0f - same.r;
	return same;
}

half3 GetEmission(float2 uv, half3 emissionColor, half emissionStrength)
{
	return emissionColor * emissionStrength;
}

half GetOcclusion(half occ)
{
	return LerpWhiteTo(occ, _OcclusionStrength);
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData, out float emissionRange)
{
	half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
	//albedoAlpha默认值为1，为了兼容美术录制高画质视频，需要增加冰面半透效果，相当于一个通道的两种用法： alpha or emission
	outSurfaceData.alpha = albedoAlpha.a;

	half4 same = SampleMetallicAOSmoothnessEmission(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
	
	outSurfaceData.metallic = same.b;
	outSurfaceData.specular = half3(0.0, 0.0, 0.0);
    outSurfaceData.smoothness = same.r;
	outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
	outSurfaceData.occlusion = GetOcclusion(same.g);
	outSurfaceData.emission = GetEmission(uv, _EmissionColor.rgb, same.a);
	emissionRange = same.a;
	
	outSurfaceData.clearCoatMask = half(0.0);
	outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // XKNIGHT_CRYSTAL_INPUT
