#ifndef URPWATER_VARIABLES_INCLUDED
#define URPWATER_VARIABLES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"   
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

TEXTURE2D(_CausticsTex);
TEXTURE2D(_NormalMapA);
TEXTURE2D(_NormalMapB);
TEXTURE2D(_FlowMap);

SAMPLER(sampler_CameraOpaqueTexture_linear_clamp);
SAMPLER(sampler_ScreenTextures_linear_clamp);
SAMPLER(sampler_pointTextures_point_clamp);

TEXTURECUBE(_CubemapTexture);
SAMPLER(sampler_CubemapTexture);

SamplerState URPWater_trilinear_repeat_sampler;
SamplerState URPWater_linear_repeat_sampler;
SamplerState URPWater_linear_clamp_sampler;

float4 _CameraOpaqueTexture_TexelSize;
float4 _CameraDepthTexture_TexelSize;

CBUFFER_START(UnityPerMaterial)

float4 _NormalMapA_ST;
float4 _NormalMapASpeeds;
float4 _NormalMapATilings;
float4 _NormalMapB_ST;
float4 _NormalMapBSpeeds;
float4 _NormalMapBTilings;
float4 _FlowTiling;
float4 _CausticsTiling;
float4 _Color;
float4 _DepthColor;
float4 _SpecColor;

float4 _CubemapTexture_HDR;
float4 _CausticsSpeed;
float _CausticsStart;
float _CausticsEnd;

float _Distortion;
float _Smoothness;
float _NormalMapAIntensity;
float _NormalMapBIntensity;
float _FlowSpeed;
float _FlowIntensity;
float _DepthStart;
float _DepthEnd;
float _ReflectionDistortion;
float _EdgeSize;
float _CausticsIntensity;
float _ReflectionIntensity;
float _ReflectionFarDistortion;
float _ReflectionFresnel;
float _PlanarReflectionDistortionIntensity;
float4 _PlanarReflectionLightDirection;

CBUFFER_END

struct Attributes
{
	float4 vertex		: POSITION;
	float4 color		: COLOR;
	float3 normal		: NORMAL;
	float4 tangent 		: TANGENT;
	float2 texcoord		: TEXCOORD0;
};

struct Varyings
{
	float4 pos				: SV_POSITION;
	float3 worldNormal		: NORMAL;
	float4 texcoord			: TEXCOORD0;
	float4 texcoord1		: TEXCOORD1;
	float4 screenCoord		: TEXCOORD2;

	float4 normal			: TEXCOORD3;    // xyz: normal, w: worldPos.x
	float4 tangent			: TEXCOORD4;    // xyz: tangent, w: worldPos.y
	float4 bitangent		: TEXCOORD5;    // xyz: binormal, w: worldPos.z
	float3 positionVS		: TEXCOORD6;
	float3 positionWS		: TEXCOORD7;
	
	UBPA_FOG_COORDS(8)
};

struct GlobalData 
{
	float depth;			// Remapped Depth
	float sceneDepth;		// Linear Depth
	float rawDepthDst;		// Raw Depth Distorted
	float pixelDepth;
	float2 refractionOffset;
	float2 refractionUV;
	float3 finalColor;
	float4 refractionData;	// RGB: Refraction Color A: Refraction Depth
	float3 clearColor;		// RGB: Clear Color
	float3 shadowColor;
	float3 worldPosition;
	float3 worldNormal;
	float3 worldViewDir;
	float4 screenUV;
	float3 tangentNormal;
	float4 debugInfo;
	
	Light mainLight;
	float3 addLight;
	real3x3 tangentToWorld;
};

void InitializeGlobalData(inout GlobalData data, Varyings IN)
{
	data.depth = 0;
	data.sceneDepth = 0;
	data.rawDepthDst = 0;
	data.pixelDepth = IN.screenCoord.z;
	data.refractionOffset = float2(0, 0);
	data.refractionUV = float2(0, 0);
	data.refractionData = float4(0, 0, 0 ,0);
	data.clearColor = float3(1, 1, 1);
	data.finalColor = float3(1, 1, 1);
	data.shadowColor = float3(1, 1, 1);
	data.debugInfo = float4(0,0,0,1);
	data.worldPosition = float3(IN.normal.w, IN.tangent.w, IN.bitangent.w);
	data.worldNormal = float3(0, 1, 0);
	data.worldViewDir = GetWorldSpaceNormalizeViewDir(data.worldPosition);
	data.screenUV = float4(IN.screenCoord.xyz / IN.screenCoord.w, IN.pos.z);
	data.mainLight = GetMainLight(TransformWorldToShadowCoord(data.worldPosition));
	data.addLight = float3(0, 0, 0);
	data.tangentToWorld = float3x3(IN.tangent.xyz, IN.bitangent.xyz, IN.normal.xyz);
	data.tangentNormal = float3(0, 0, 0);
}

#endif