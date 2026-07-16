// 雨的输入参数
#ifndef __RAINY_PUDDLES_INPUT__
#define __RAINY_PUDDLES_INPUT__

// CBUFFER 宏
#define RAINY_PUDDLES_CBUFFER \
	// 雨滴属性
	float	_RainDotsIntensity; \
	float	_RainDotsTiling; \
	float	_RainDotsSplashSpeed; \
	float	_RainDotsSize; \
	\
	// 遮罩属性
	float4	_RainMask_ST; \
	float	_RainExcludeBaseMapAlpha; \
	float	_RainMaskInvert; \
	\
	float	_RainMaskIntensity; \
	float	_RainMaskContrast; \
	float	_RainMaskSpread; \
	\
	// 反射属性
	float4	_RainCubemapColor; \
	float	_RainReflectionIntensity; \
	float	_RainBlurReflectionFactor; \
	float	_UseMainNormalToRainNormal; \
	\
	// 水坑属性
	float4	_WaveNormalMap_ST; \
	half4	_PuddleColor; \
	half	_PuddleMetallic; \
	half	_PuddleGlossiness; \
	float	_PuddleBlendMainNormal; \
	\
	float	_MainWave; \
	float	_NormalWaveIntensity1; \
	float	_TranslationSpeed1; \
	float	_RotationAngle1; \
	float	_TilingWave1; \
	\
	float	_DetailWave; \
	float	_NormalWaveIntensity2; \
	float	_TranslationSpeed2; \
	float	_RotationAngle2; \
	float	_TilingWave2; \
	\
	// 涟漪属性
	float4 _XColumnsYRowsZSpeedWStrartFrameNormal; \
	float _FlipBTilingNormal; \
	float _IntensityScaleNormal1; \
	\
	float _DuplicateRainDotsNormalAtlas; \
	float _IntensityScaleNormal2; \
	\
	float _ScaleFBDetailsNormal; \
	float _FBDetailsNormal; \
	float2 _OffsetFBDetailsNormal; \
	float _Distortion; \
	float _UseAoFromMainProperties; \
	float _UseEmissionFromMainProperties; \
	\
	\
	\
	// 雨的总控属性
	// 积水强度
	float _HydropsIntensity; \
	// 总的雨点强度
	float _TotalRainDotsIntensity; \

// 纹理采样宏
#define RAINY_PUDDLES_SAMPLE \
	TEXTURE2D(_RainRipplesAtlasNormal);		SAMPLER(sampler_RainRipplesAtlasNormal); \
	TEXTURE2D(_RainMask);					SAMPLER(sampler_RainMask); \
	TEXTURECUBE(_RainCubemap);				SAMPLER(sampler_RainCubemap); \
	TEXTURE2D(_WaveNormalMap);				SAMPLER(sampler_WaveNormalMap); \
	TEXTURE2D(_RainDotsGradientTex);		SAMPLER(sampler_RainDotsGradientTex); \

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/GlobalTimeControl.hlsl"

#endif // __RAINY_PUDDLES_INPUT__
