#ifndef URPWATER_HELPERS_INCLUDED
#define URPWATER_HELPERS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"  
#include "Water_Variables.hlsl"

float4 DualAnimatedUV(float2 uv, float4 tilings, float4 speeds) 
{
	float4 coords;

	coords.xy = uv * tilings.xy;
	coords.zw = uv * tilings.zw;

#if _WORLDUV_ON
	coords += speeds * _Time.x;
#else
	coords += frac(speeds * _Time.x);
#endif

	return coords;
}

float2 AnimatedUV(float2 uv, float2 tilings, float2 speeds)
{
	float2 coords;

	coords.xy = uv * tilings.xy;

#if _WORLDUV_ON
	coords += speeds * _Time.xx;
#else
	coords += frac(speeds * _Time.xx);
#endif

	return coords;
}

float UVEdgeMask(float2 uv, float maskSize) 
{
	float2 edgeMaskUV = abs(uv * 2 - 1);
	float edgeMask = 1 - max(edgeMaskUV.x, edgeMaskUV.y);
	return smoothstep(0, maskSize, edgeMask);
}

float ComputePixelDepth(float3 worldPos) 
{
	return -TransformWorldToView(worldPos).z;
}

float SampleRawDepth(float2 uv)
{
	return SampleSceneDepth(uv);
}

float RawDepthToLinear(float rawDepth) 
{
	return LinearEyeDepth(rawDepth, _ZBufferParams);
}

float SampleDepth(float2 uv) 
{
	return RawDepthToLinear(SampleRawDepth(uv));
}

float3 ProjectedWorldPos(GlobalData data, Varyings IN)
{
	float3 rawWorldViewDir = _WorldSpaceCameraPos.xyz - data.worldPosition;
	float4 rawScreenPos = ComputeScreenPos(IN.pos, _ProjectionParams.x);
	float eyeDepth = data.refractionData.a;

	float3 pos = rawWorldViewDir / rawScreenPos.a;
	pos *= eyeDepth;
	pos -= _WorldSpaceCameraPos.xyz;

	return pos;
}

float3 WorldNormal(float3 t0, float3 t1, float3 t2, float3 bump)
{
	return normalize(float3(dot(t0, bump), dot(t1, bump), dot(t2, bump)));
}

float DistanceFade(float depth, float pixelDepth, float start, float end)
{
	float dist = ((depth - pixelDepth) - end) / (start - end);
	return saturate(dist);
}

float2 OffsetUV(float4 uv, float2 offset)
{
#ifdef UNITY_Z_0_FAR_FROM_CLIPSPACE
		uv.xy = offset * uv.z + uv.xy;
#else
		uv.xy = offset * uv.z + uv.xy;
#endif

	return uv.xy;
}

float2 OffsetDepth(float4 uv, float2 offset)
{
	uv.xy = offset * uv.z + uv.xy;
	return uv.xy;
}

half2 DistortionUVs(half depth, float3 normalWS)
{
	half3 viewNormal = mul((float3x3)GetWorldToHClipMatrix(), -normalWS).xyz;

	return viewNormal.xz * saturate((depth) * 0.005);
}

float smootherstep(float x) {
	x = saturate(x);
	return saturate(x * x * x * (x * (6 * x - 15) + 10));
}

float4 InverseLerp(float4 A, float4 B, float4 T)
{
	return (T - A) / (B - A);
}

float2 ProjectionUV(float4 captureParams, float3 worldPosition ) 
{
	float3 simPos = worldPosition - captureParams.xyz;
	float3 simScale = simPos / captureParams.w;
	return simScale.xz + 0.5;
}

#endif