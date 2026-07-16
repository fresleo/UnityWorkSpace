#ifndef XKNIGHT_FORWARD_LIT_PASS_INCLUDED
#define XKNIGHT_FORWARD_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Lighting.hlsl"

#ifdef LOD_FADE_CROSSFADE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "../ShaderLibrary/ExtraBlend.hlsl" // TODO: 应该没地用了，后续找机会删掉

#include "../ShaderLibrary/EarlyOpaqueBlend.hlsl"
#include "./LitDissolve.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

struct Attributes
{
	float4 color				: COLOR;
	
	float4 positionOS			: POSITION;
	float3 normalOS				: NORMAL;
	float4 tangentOS			: TANGENT;
	
	float2 texcoord				: TEXCOORD0;
	float2 staticLightmapUV		: TEXCOORD1;
	float2 uv3					: TEXCOORD2;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 vertexColor				: COLOR;
	
	float2 uv						: TEXCOORD0;
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);
	float2 uv3						: TEXCOORD2;
	
	float3 positionWS				: TEXCOORD3;
	float3 normalWS					: TEXCOORD4;
	float4 tangentWS				: TEXCOORD5;
	
	#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
	float4 shadowCoord				: TEXCOORD6;
	#endif
	
	float4 positionSS				: TEXCOORD7;
	
	UBPA_FOG_COORDS(8)
	DISSOLVE_FACTOR(9)
	float objectId					: TEXCOORD10;

	float4 positionCS				: SV_POSITION;
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};


///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	float3 vertex = input.positionOS.xyz;
	
	VertexPositionInputs vertexInput = GetVertexPositionInputs(vertex);
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
	
	output.vertexColor = input.color;
	output.vertexColor.w = input.staticLightmapUV.x; // 强制使用 uv1 的 trick

	output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
	output.uv3 = TRANSFORM_TEX(input.uv3, _BakerAOMap);
	
	output.normalWS = normalInput.normalWS;
	
	real sign = input.tangentOS.w * GetOddNegativeScale();
	output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
	OUTPUT_SH(normalInput.normalWS.xyz, output.vertexSH);
	
	#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
	output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
	#endif

	output.positionWS = vertexInput.positionWS;
	output.positionSS = ComputeScreenPos(vertexInput.positionCS);
	output.positionCS = vertexInput.positionCS;
	
	UBPA_TRANSFER_FOG(output, vertexInput.positionWS);
	DISSOLVE_TRANSFER_FACTOR(output, vertex, _DissolveDir)

	// 基于中心点计算对象id
	VertexPositionInputs vertexInput0 = GetVertexPositionInputs(float3(0, 0, 0));
	float objectId = dot(vertexInput0.positionWS, 1);
	output.objectId = objectId;
	
	return output;
}


// 具备完整特性的表面数据阶段
inline void InitializeStandardLitSurfaceData(Varyings input, out SurfaceData outSurfaceData, out half maskRoughness)
{
	outSurfaceData = (SurfaceData)0;
	
	half3 Albedo;
	half Alpha;
	half3 Specular;
	half Smoothness;
	half Occlusion;
	half Metallic;
	half3 NormalTS;
	half3 Emission;
	
	half4 baseMapCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
	
	Albedo = baseMapCol.rgb * _BaseColor.rgb;
	maskRoughness = baseMapCol.a;
	
	// 合图模式
	#ifdef _USE_PACKED_TEXTURE_MDOE
	Alpha = _BaseColor.a;
	
	half metallic, ao;
	half3 normalMix = SampleNormalMapMix(input.uv, metallic, ao);

	Smoothness = 1.0h - (maskRoughness * _RoughnessStrength);
	Occlusion = ao;
	Metallic = metallic;
	
	NormalTS = normalMix;
	Emission = GetEmission(input.uv);
	
	#else // 不合图模式

	Alpha = maskRoughness * _BaseColor.a;
	
	half4 same = SampleMetallicAOSmoothnessEmission(input.uv);
	
	Smoothness = same.r;
	Occlusion = GetOcclusion(same.g);
	Metallic = same.b;
	
	NormalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
	Emission = GetEmission(input.uv, same.a);
	#endif // 合图模式

	Specular = half3(0, 0, 0);

	// 细节纹理
	#ifdef _DETAIL
	float2 detailUv = TRANSFORM_TEX(input.uv, _DetailMap);
	half4 detail = SampleDetail(detailUv, TEXTURE2D_ARGS(_DetailMap, sampler_DetailMap));

	Albedo = BlendAlbedo(Albedo, detail.w);
	NormalTS = BlendNormalRNM(NormalTS, detail.xyz);
	#endif // 细节纹理

	// 新的雨
	#if defined(_GLOBAL_RAIN_ON) && defined(_LOCAL_RAIN_ON)
	// 雨的涟漪法线
	float3 rainRipplesNormal = GetRainRipplesNormal(
		_XColumnsYRowsZSpeedWStrartFrameNormal, input.uv, _FlipBTilingNormal,
		TEXTURE2D_ARGS(_RainRipplesAtlasNormal, sampler_RainRipplesAtlasNormal), _IntensityScaleNormal1,
		_DuplicateRainDotsNormalAtlas,
		_ScaleFBDetailsNormal, _OffsetFBDetailsNormal, _FBDetailsNormal, _IntensityScaleNormal2);

	// 带着畸变采样水坑的基础贴图
	float distortion = lerp(0.0, 0.9, _Distortion);
	float2 uv_rainPuddles = input.uv + rainRipplesNormal.xy * (distortion).xx;
	half4 rainPuddlesBaseMapCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv_rainPuddles);
	half3 rainPuddlesAlbedo = rainPuddlesBaseMapCol.rgb * _PuddleColor.rgb;
	
	// 雨的遮罩
	float2 uv_RainMask = input.uv * _RainMask_ST.xy + _RainMask_ST.zw;
	float4 rainMask = GetRainMask(
					TEXTURE2D_ARGS(_RainMask, sampler_RainMask), uv_RainMask,
					_RainMaskInvert, _RainMaskContrast, _RainMaskSpread,
					_RainExcludeBaseMapAlpha, Alpha, _RainMaskIntensity * _HydropsIntensity);

	// 混合坑里坑外的反照率
	Albedo = lerp(Albedo, rainPuddlesAlbedo, rainMask);

	// 波纹法线
	float2 uv_WaveNormalMap = input.uv * _WaveNormalMap_ST.xy + _WaveNormalMap_ST.zw;

	// 主波纹
	float2 uv_Wave1 = WaveUV(uv_WaveNormalMap, _TilingWave1, _TranslationSpeed1, _RotationAngle1);
	half4 waveNormalMap1 = SAMPLE_TEXTURE2D(_WaveNormalMap, sampler_WaveNormalMap, uv_Wave1);
	float3 waveNormal1 = UnpackNormalScale(waveNormalMap1, _NormalWaveIntensity1);
	waveNormal1.z = lerp(1, waveNormal1.z, saturate(_NormalWaveIntensity1));
	
	float3 mainWaveNormal = _MainWave ? waveNormal1 : float3(0, 0, 1);

	// 副波纹
	float2 uv_Wave2 = WaveUV(uv_WaveNormalMap, _TilingWave2, _TranslationSpeed2, _RotationAngle2);
	half4 waveNormalMap2 = SAMPLE_TEXTURE2D(_WaveNormalMap, sampler_WaveNormalMap, uv_Wave2);
	float3 waveNormal2 = UnpackNormalScale(waveNormalMap2, _NormalWaveIntensity2);
	waveNormal2.z = lerp(1, waveNormal2.z, saturate(_NormalWaveIntensity2));

	// 混合波1波2
	float3 finalWaveNormal = _DetailWave ? BlendNormal( mainWaveNormal , waveNormal2 ) : mainWaveNormal;
	
	// 波纹法线和涟漪法线混合，形成雨坑的法线
	float3 rainPuddlesNormal = BlendNormal(finalWaveNormal, rainRipplesNormal);
	
	// 再和主法线混合
	float3 normal1 = lerp(NormalTS, rainPuddlesNormal, rainMask.rgb);
	float3 normal2 = lerp(NormalTS, BlendNormal(NormalTS, rainPuddlesNormal), rainMask.rgb);
	NormalTS = _PuddleBlendMainNormal ? normal2 : normal1;

	// 选择使用全局的自发光，还是水坑的自发光
	half3 selectEmission = FilterEmission(_UseEmissionFromMainProperties, Emission, rainMask.rgb);

	// 反射法线，使用来自主法线的结果，还是波法线的结果
	float3 reflectNormal = (_UseMainNormalToRainNormal ? NormalTS : finalWaveNormal);
	
	float3 viewDirectionWS = _WorldSpaceCameraPos.xyz - input.positionWS;
	viewDirectionWS = SafeNormalize(viewDirectionWS);

	float sgn = input.tangentWS.w; // should be either +1 or -1
	float3 bitangentWS = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

	half3x3 tangentToWorldMatrix = half3x3(input.tangentWS.xyz, bitangentWS.xyz, input.normalWS.xyz);
	float3 normalWS = TransformTangentToWorld(NormalTS, tangentToWorldMatrix);
	normalWS = NormalizeNormalPerPixel(normalWS);
	
	float3 tanToWorld0 = float3(input.tangentWS.x, bitangentWS.x, normalWS.x);
	float3 tanToWorld1 = float3(input.tangentWS.y, bitangentWS.y, normalWS.y);
	float3 tanToWorld2 = float3(input.tangentWS.z, bitangentWS.z, normalWS.z);

	// 采样雨的反射
	float4 reflectColor = GetRainReflect(
					viewDirectionWS, reflectNormal, tanToWorld0, tanToWorld1, tanToWorld2, 
					TEXTURECUBE_ARGS(_RainCubemap, sampler_RainCubemap), _RainBlurReflectionFactor, 
					_RainReflectionIntensity, _RainCubemapColor, rainMask);

	Emission = selectEmission + reflectColor.rgb;

	// 采样渐变纹理
	float2 panner;
	float voronoi;
	CalculateRainDotsGradientTexUV(
		_RainDotsSplashSpeed, input.uv, _RainDotsTiling,
		panner, voronoi);
	float gradientValue = SAMPLE_TEXTURE2D(_RainDotsGradientTex, sampler_RainDotsGradientTex, panner).g;

	// 雨点的粗糙度
	float rainDotsRoughness = CalculateRainDotsRoughness(gradientValue, voronoi, _RainDotsSize, _RainDotsIntensity * _TotalRainDotsIntensity);
	// 雨点的光滑度
	float rainDotsSmoothness = CalculateRainDotsSmoothness(Smoothness, rainDotsRoughness);
	Smoothness = lerp(rainDotsSmoothness, _PuddleGlossiness, rainMask.r);

	// 金属性
	Metallic = lerp(Metallic, _PuddleMetallic, rainMask.r);

	// AO
	if(!_UseAoFromMainProperties)
	{
		Occlusion = lerp(Occlusion, 1.0, rainMask.r);
	}
	#endif // 新的雨
	
	
	// 赋值
	outSurfaceData.albedo = Albedo;
	outSurfaceData.alpha = Alpha;
	outSurfaceData.specular = Specular;
	outSurfaceData.smoothness = Smoothness;
	outSurfaceData.occlusion = Occlusion;
	outSurfaceData.metallic = Metallic;
	outSurfaceData.normalTS = NormalTS;
	outSurfaceData.emission = Emission;
	
	outSurfaceData.clearCoatMask = half(0);
	outSurfaceData.clearCoatSmoothness = half(0);
}

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;

	inputData.positionCS = input.positionCS;
	inputData.positionWS = input.positionWS;

	float sgn = input.tangentWS.w; // should be either +1 or -1
	float3 bitangentWS = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

	inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangentWS.xyz, input.normalWS.xyz));
	inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
	inputData.viewDirectionWS = viewDirWS;

	#if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
	inputData.shadowCoord = input.shadowCoord;
	#elif defined( MAIN_LIGHT_CALCULATE_SHADOWS )
	inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
	#else
	inputData.shadowCoord = float4(0,0,0,0);
	#endif

	half3 bakedGITint = lerp(half3(1, 1, 1), _BakedGITint.rgb, _BakedGITintIntensity);
	
	// GI = lightmap * GI贡献比例 * 全局调色参数
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS) * _GIIndirectDiffuseBoost * bakedGITint;
	
	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

void MRTBufferPass(InputData inputData, float objectId, out half4 outForwardBuffer0, out half4 outForwardBuffer1, out half4 outForwardBuffer2)
{
	half4 color0 = 0;
	color0.r = _BloomFactor;
	color0.g = _WaterColorOn;
	color0.b = objectId * _SceneSpaceOutlineOn;
	outForwardBuffer0 = color0;

	half4 color1 = 0;
	color1.rgb = inputData.normalWS;
	outForwardBuffer1 = color1;

	half4 color2 = 0;
	color2.r = inputData.positionCS.z;
	outForwardBuffer2 = color2;
}

void LitPassFragment(Varyings input
	, out half4 outColor : SV_Target0

	#if defined( _WRITE_RENDERING_LAYERS )
	, out float4 outRenderingLayers : SV_Target1
	#elif defined( _MRT_BUFFER )
	, out half4 outForwardBuffer0 : SV_Target1
	, out half4 outForwardBuffer1 : SV_Target2
	, out half4 outForwardBuffer2 : SV_Target3
	#endif
	)
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	#ifdef LOD_FADE_CROSSFADE
	LODFadeCrossFade(input.positionCS);
	#endif

	// 抖动
	#ifdef _DITHER_ON
	DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
		TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
	#endif
	
	SurfaceData surfaceData;
	half maskRoughness;
	InitializeStandardLitSurfaceData(input, surfaceData, maskRoughness);
	
	InputData inputData;
	InitializeInputData(input, surfaceData.normalTS, inputData);

	ExtendData extendData = (ExtendData)0;
	extendData.specularScaleBRDF = _SpecularScaleBRDF;
	extendData.receiveShadowsOff = _ReceiveShadowsOff;
	
	#if defined(LIGHTMAP_ON)
	extendData.staticLightmapUV = input.staticLightmapUV;
	#endif
	
	extendData.AHDBakedSpecularScale = _AHDBakedSpecularScale;
	extendData.AHDBakedSpecularDirectionBlur = _AHDBakedSpecularDirectionBlur;
	extendData.AHDBakedSpecularRougheningMaxAmount = _AHDBakedSpecularRougheningMaxAmount;
	extendData.AHDBakedSpecularStrengthGateMin = _AHDBakedSpecularStrengthGateMin;
	extendData.AHDBakedSpecularStrengthGateMax = _AHDBakedSpecularStrengthGateMax;
	extendData.AHDBakedSpecularRougheningConfidenceMin = _AHDBakedSpecularRougheningConfidenceMin;
	extendData.AHDBakedSpecularRougheningConfidenceMax = _AHDBakedSpecularRougheningConfidenceMax;
	
	extendData.mainLightMinPerceptualRoughness = lerp(0, _MainLightMinPerceptualRoughness, _MainLightSpecularSoftClamp);
	
	half preserveMask = lerp(maskRoughness, 1 - maskRoughness, _MainLightClampPreserveBaseMapAlphaInvert);
	half preserveBinaryMask = step(_MainLightClampPreserveBaseMapAlphaThreshold, preserveMask) * _MainLightClampPreserveBaseMapAlpha;
	extendData.mainLightMinPerceptualRoughness = lerp(extendData.mainLightMinPerceptualRoughness, 0, preserveBinaryMask);
	
	half4 color = FragmentPBR(inputData, surfaceData, extendData);
	
	// 这里虽然名字叫 AO，但它其实不是 AO，更近似于 detail 纹理，是为了给物体增加卡通素描线效果的
	// FinalAO = LightmapAO + BakerAO
	float3 bakerAO = SampleBakerAO(input.uv3);
	color.rgb = lerp(color.rgb, color.rgb * bakerAO.rgb, _BakerAOMapScale);

	// 需要优先于混合，否则双重加黑
	ExtraBlend(color.rgb, input.positionWS);

	#ifdef _OPAQUE_BLEND
	float2 screenUV = input.positionSS.xy / input.positionSS.w;
	EARLY_OPAQUE_BLEND(screenUV, input.positionCS.w, _TerrainBlendFactor, color.rgb);
	#endif

	color.rgb = BlendVolumeColor(input.positionWS, color.rgb);
	// 目前仅用于泥泞路面的边缘柔化
	color.a = input.vertexColor.r;
	
	UBPA_APPLY_FOG(input, color);
	DISSOLVE_APPLY(color, input.uv, input.directionFactor)

	// 蘑菇觉得只靠 dither 不够像绝区零，所以加了这个来辅助
	half outAlpha = color.a * _DitherAlpha;
	outColor = half4(color.rgb, outAlpha);
	
	// half s = PerObjectRealtimeShadow(inputData.positionWS);
	// outColor = half4((1 - s).xxx, 1);
	//


	#if defined( _WRITE_RENDERING_LAYERS )
	uint renderingLayers = GetMeshRenderingLayer();
	outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
	#elif defined( _MRT_BUFFER )
	MRTBufferPass(inputData, input.objectId, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
	#endif
}

#endif //XKNIGHT_FORWARD_LIT_PASS_INCLUDED
