#ifndef XKNIGHT_FORWARD_LIT_PASS_INCLUDED
#define XKNIGHT_FORWARD_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Lighting.hlsl"

#if defined( LOD_FADE_CROSSFADE )
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "../ShaderLibrary/ExtraBlend.hlsl"
#include "../ShaderLibrary/EarlyOpaqueBlend.hlsl"

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

#include "../Scene/Wind.hlsl"
#include "Packages/com.garena.ta.Glass@1.0.0/Shaders/Glass.hlsl"

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
	float4  vertexColor				: COLOR;
	
	float2	uv						: TEXCOORD0;
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 1);
	float4 uv3						: TEXCOORD2;

	#if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
	float4	shadowCoord				: TEXCOORD3;
	#endif
	
	float3	positionWS				: TEXCOORD4;
	float3	normalWS				: TEXCOORD5;
	float4	tangentWS				: TEXCOORD6;
	
	float3	viewDirWS				: TEXCOORD7;
	UBPA_FOG_COORDS(8)
	float4	positionSS				: TEXCOORD9;
	float objectId					: TEXCOORD10;
	
	float4	positionCS				: SV_POSITION;
	
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;

	inputData.positionCS = input.positionCS;
	inputData.positionWS = input.positionWS;

	float sgn = input.tangentWS.w; // should be either +1 or -1
	float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

	inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
	inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

	inputData.viewDirectionWS = SafeNormalize(input.viewDirWS);

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

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

	output.vertexColor = input.color;
	
	output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
	output.uv3.xy = TRANSFORM_TEX(input.uv3, _BakerAOMap);
    output.uv3.zw = TRANSFORM_TEX(input.texcoord, _FrostTexture);
	
	// already normalized from normal transform to WS.
	output.normalWS = normalInput.normalWS;
	output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

	real sign = input.tangentOS.w * GetOddNegativeScale();
	output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
	
	#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
	output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
	#endif

	output.positionWS = vertexInput.positionWS;
	output.positionSS = ComputeScreenPos(vertexInput.positionCS);
	//output.positionCS = vertexInput.positionCS;
	output.positionCS = TransformWorldToHClip(output.positionWS); // 为了把风场的 WS 应用过来
	
	#ifdef _WIND_ON
	Wind(output.uv, output.normalWS, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
	#endif
	
	UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

	// 基于中心点计算对象id
	VertexPositionInputs vertexInput0 = GetVertexPositionInputs(float3(0, 0, 0));
	float objectId = dot(vertexInput0.positionWS, 1);
	output.objectId = objectId;
	
	return output;
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

float hash21(float2 p)
{
	p = frac(p * float2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return frac(p.x * p.y);
}

float valueNoise(float2 uv)
{
	float2 i = floor(uv);
	float2 f = frac(uv);

	float a = hash21(i);
	float b = hash21(i + float2(1,0));
	float c = hash21(i + float2(0,1));
	float d = hash21(i + float2(1,1));

	float2 u = f * f * (3 - 2 * f);

	return lerp(
		lerp(a,b,u.x),
		lerp(c,d,u.x),
		u.y);
}

void LitPassFragment(Varyings input,bool face:SV_IsFrontFace
	, out half4 outColor : SV_Target0
	
	#ifdef _MRT_BUFFER
	, out half4 outForwardBuffer0 : SV_Target1
	, out half4 outForwardBuffer1 : SV_Target2
	, out half4 outForwardBuffer2 : SV_Target3
	#endif
	)
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	
	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);


	#if defined( LOD_FADE_CROSSFADE )
	LODFadeCrossFade(input.positionCS);
	#endif

	// 抖动
	#ifdef _DITHER_ON
	DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
		TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
	#endif

	InputData inputData;
	InitializeInputData(input, surfaceData.normalTS, inputData);
	
	//==========Noise=================
	#ifdef _WINDOW_DETAIL
	float3 normalWS=face?inputData.normalWS:-inputData.normalWS;
	half NoV = dot(normalWS, inputData.viewDirectionWS);
	NoV = smoothstep(_DetailFresnelRange.x, _DetailFresnelRange.y, NoV);
	half fresnelTerm = Pow4(1.0 - NoV);
	fresnelTerm = fresnelTerm*fresnelTerm*fresnelTerm*(10-15*fresnelTerm+6*fresnelTerm*fresnelTerm);
	float4 detailAlbedo = SAMPLE_TEXTURE2D(_WindowDetailMap,sampler_WindowDetailMap,input.uv*_WindowDetailMap_ST.xy);
	float noise = valueNoise(input.uv*_NoiseUVTiling.xy+_NoiseUVTiling.zw);
	surfaceData.albedo+=float4(1-noise*detailAlbedo)*fresnelTerm;;
	//==========Noise=================
	#endif
	
	ExtendData extendData = (ExtendData)0;
	extendData.specularScaleBRDF = _SpecularScaleBRDF;
	
	half4 color = FragmentPBR(inputData, surfaceData, extendData);

	// FinalAO = LightmapAO + BakerAO
	float3 bakerAO = SampleBakerAO(input.uv3);
	color.rgb *= bakerAO.rgb;

	// 需要优先于混合，否则双重加黑
	ExtraBlend(color.rgb, input.positionWS);
	
	color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
	half outAlpha = color.a * _DitherAlpha;
	UBPA_APPLY_FOG(input, color);
	#ifdef _WINDOW_DETAIL
	outAlpha= outAlpha+float4(1-noise*detailAlbedo)*fresnelTerm;
	
	#endif
	outColor = half4(color.rgb, outAlpha);
	// float  cosTheta = saturate(dot(normalize(inputData.normalWS), normalize(input.viewDirWS)));
	// float  f0 = pow(saturate((_IOR - 1.0) / (_IOR + 1.0)), 2.0);
	// float  F  = f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
	// outColor = half4(lerp(col * (1 - F), outColor.rgb, outColor.a), 1);

	#if _FROSTED_GLASS_ON & _REFRACTION_ON
	float2 refractUV = ComputeRefractionUV_Thin(input.positionSS.xy / input.positionSS.w, inputData.normalWS, input.viewDirWS, _IOR, _RefractStrengthPX);
	half4 col = GetFrostedColorFromScreenUV(input.uv3.zw, refractUV, _FrostTexture ,sampler_FrostTexture, _FrostIntensity);
	outColor = half4(lerp(col.xyz, outColor.xyz, outColor.a), 1);
	#elif _FROSTED_GLASS_ON
	half4 col = GetFrostedColorFromScreenUV(input.uv3.zw, input.positionSS.xy / input.positionSS.w, _FrostTexture ,sampler_FrostTexture, _FrostIntensity);
	outColor = half4(lerp(col.xyz, outColor.xyz, outColor.a), 1);
	#elif _REFRACTION_ON
	float2 refractUV = ComputeRefractionUV_Thin(input.positionSS.xy / input.positionSS.w, inputData.normalWS, input.viewDirWS, _IOR, _RefractStrengthPX);
	half4 col = GetColorFromScreenUV(refractUV);
	outColor = half4(lerp(col.xyz, outColor.xyz, outColor.a), 1);
	#endif
	
	#ifdef _MRT_BUFFER
	MRTBufferPass(inputData, input.objectId, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
	#endif // _MRT_BUFFER
}

#endif //XKNIGHT_FORWARD_LIT_PASS_INCLUDED