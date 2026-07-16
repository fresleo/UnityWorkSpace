#ifndef URPWATER_REFRACTION_INCLUDED
#define URPWATER_REFRACTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Water_Variables.hlsl"
#include "Water_Helpers.hlsl"

void ComputeOpaqueAndDepth(inout GlobalData data, out float4 clearData, out float4 refractionData, out float2 refractionOffset)
{
	float2 screenUV = data.screenUV.xy;

	clearData.rgb = SampleSceneColor(screenUV);
	clearData.a = SampleDepth(screenUV);

	// Distorted Data
	float2 distortionAmount = _CameraOpaqueTexture_TexelSize.xy * _Distortion.xx;

	// Far Distortion
	float farDistance = saturate(1 - length(data.worldPosition.rgb - _WorldSpaceCameraPos.xyz) / 50);
	distortionAmount = lerp(distortionAmount * 0.25, distortionAmount, farDistance);
	
	float2 offset = data.worldNormal.xz * distortionAmount;
	float2 GrabUV = OffsetUV(data.screenUV, offset);
	float2 DepthUV = OffsetDepth(data.screenUV, offset);

	float4 distortedData;
	float rawDepth = SampleRawDepth(DepthUV);

	distortedData.rgb = SampleSceneColor(GrabUV);
	distortedData.a = RawDepthToLinear(rawDepth);

	// 解决只有水面下的像素被扭曲
	refractionData = data.pixelDepth > distortedData.a ? clearData : distortedData;
	refractionOffset = offset;

	data.refractionUV = DepthUV;
	data.rawDepthDst = rawDepth;
}

void ComputeOpaqueAndDepth_LOD1(inout GlobalData data, out float4 clearData, out float4 refractionData, out float2 refractionOffset)
{
	float2 screenUV = data.screenUV.xy;

	clearData.rgb = SampleSceneColor(screenUV);
	clearData.a = SampleDepth(screenUV);

	// Distorted Data
	float2 distortionAmount = _CameraOpaqueTexture_TexelSize.xy * _Distortion.xx;

	// Far Distortion
	float farDistance = saturate(1 - length(data.worldPosition.rgb - _WorldSpaceCameraPos.xyz) / 50);
	distortionAmount = lerp(distortionAmount * 0.25, distortionAmount, farDistance);
	
	float2 offset = data.worldNormal.xz * distortionAmount;
	float2 GrabUV = OffsetUV(data.screenUV, offset);
	float2 DepthUV = OffsetDepth(data.screenUV, offset);

	// float4 distortedData;
	float rawDepth = SampleRawDepth(DepthUV);
	//
	// distortedData.rgb = SampleSceneColor(GrabUV);
	// distortedData.a = RawDepthToLinear(rawDepth);

	// 解决只有水面下的像素被扭曲
	refractionData = clearData;//data.pixelDepth > distortedData.a ? clearData : distortedData;
	refractionOffset = offset;

	data.refractionUV = DepthUV;
	data.rawDepthDst = rawDepth;
}

void ComputeRefractionData(inout GlobalData data)
{
	float4 clearData;
	float4 refractionData;
	float2 refractionOffset;

	ComputeOpaqueAndDepth(data, clearData, refractionData, refractionOffset);
	data.depth = DistanceFade(refractionData.a, data.pixelDepth, _DepthStart, _DepthEnd);

	data.refractionData = refractionData;
	data.clearColor.rgb = clearData.rgb;
	data.sceneDepth = clearData.a;
	data.refractionOffset = refractionOffset;
}

void ComputeRefractionData_LOD1(inout GlobalData data)
{
	float4 clearData;
	float4 refractionData;
	float2 refractionOffset;

	ComputeOpaqueAndDepth_LOD1(data, clearData, refractionData, refractionOffset);
	data.depth = DistanceFade(refractionData.a, data.pixelDepth, _DepthStart, _DepthEnd);

	data.refractionData = refractionData;
	data.clearColor.rgb = clearData.rgb;
	data.sceneDepth = clearData.a;
	data.refractionOffset = refractionOffset;
}

#endif