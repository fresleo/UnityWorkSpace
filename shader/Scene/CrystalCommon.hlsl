#ifndef XKNIGHT_CRYSTAL_COMMON
#define XKNIGHT_CRYSTAL_COMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#if defined( LOD_FADE_CROSSFADE )
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/ExtraBlend.hlsl"
#include "../ShaderLibrary/EarlyOpaqueBlend.hlsl"
#include "CrystalInput.hlsl"
#include "CrystalDissolve.hlsl"
#include "IceDepth.hlsl"

struct Attributes
{
	float4 positionOS			: POSITION;
	float3 normalOS				: NORMAL;
	float4 tangentOS			: TANGENT;
	float2 texcoord				: TEXCOORD0;
	float2 staticLightmapUV		: TEXCOORD1;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float2	uv						: TEXCOORD0;
	float3	positionWS				: TEXCOORD1;

	float3	normalWS				: TEXCOORD2;
	float4	tangentWS				: TEXCOORD3;	

	float3	viewDirWS				: TEXCOORD4;
	UBPA_FOG_COORDS(5)
	
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4	shadowCoord				: TEXCOORD6;
	#endif

	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);

	float4	positionSS				: TEXCOORD8;

	DISSOLVE_FACTOR(9)

	float4	positionCS				: SV_POSITION;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = SafeNormalize(input.viewDirWS);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0,0,0,0);
#endif
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	
	// normalWS and tangentWS already normalize.
	// this is required to avoid skewing the direction during interpolation
	// also required for per-vertex lighting and SH evaluation
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
	
	//output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

	output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

	// already normalized from normal transform to WS.
	output.normalWS = normalInput.normalWS;
	output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

	real sign = input.tangentOS.w * GetOddNegativeScale();
	output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

	output.positionSS = ComputeScreenPos(vertexInput.positionCS);

	output.positionWS = vertexInput.positionWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
#endif

	output.positionCS = vertexInput.positionCS;

	UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

	DISSOLVE_TRANSFER_FACTOR(output, input.positionOS, _DissolveDir)
	
	return output;
}

half4 LitPassFragment(Varyings input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	
	SurfaceData surfaceData;
	float emissionRange;
	InitializeStandardLitSurfaceData(input.uv, surfaceData, emissionRange);

	#if defined( LOD_FADE_CROSSFADE )
	LODFadeCrossFade(input.positionCS);
	#endif
	
	InputData inputData;
	InitializeInputData(input, surfaceData.normalTS, inputData);

	half4 color = (half4)0;

	//获取材质的 alpha 值 -录制视频
	float materialAlpha = surfaceData.alpha;

	ExtendData extendData = (ExtendData)0;
	extendData.specularScaleBRDF = 1;

#if _RECEIVE_DIRECTLIGHT_ON	
	color = CrystalPBR(inputData, surfaceData, extendData, TEXTURECUBE_ARGS(_ReflectionCube, sampler_ReflectionCube), _ReflectionCube_HDR, true);
#else
	color = CrystalPBR(inputData, surfaceData, extendData, TEXTURECUBE_ARGS(_ReflectionCube, sampler_ReflectionCube), _ReflectionCube_HDR, false);
#endif

#if _ICE_DEPTH_ON
	half3 icecolor = IceDepth(TEXTURE2D_ARGS(_BaseMap,sampler_BaseMap), input.uv, _IceLayer, _IceOffset, input.positionWS, _IceLOD);
	half3 iceout = lerp(surfaceData.albedo.rgb, icecolor, _IceBlur);
	color.rgb += iceout * _IceSaturation * _IceColor.rgb;
#endif

#if _SPIRAL_FLOW_ON
	half3 spiralFlowColor = SpiralFlowColor(input.uv);
	color.rgb += spiralFlowColor;
#endif	

	half3 volumeColor = VolumeColor(input.uv, emissionRange);

	color.rgb += volumeColor;
	
	ExtraBlend(color.rgb, input.positionWS);

	UBPA_APPLY_FOG(input, color);

	DISSOLVE_APPLY(color, input.uv, input.directionFactor)

	//获取材质的 alpha 值 -录制视频
    color.a = materialAlpha;
	
    return color;		
}

#endif // XKNIGHT_CRYSTAL_COMMON
