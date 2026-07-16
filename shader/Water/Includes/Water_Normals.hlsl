#ifndef URPWATER_NORMALS_INCLUDED
#define URPWATER_NORMALS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"   
#include "./Water_Helpers.hlsl"
#include "./Water_Variables.hlsl"

// 为了flow map编辑器而存在
uniform float _FlowMapSize;
uniform TEXTURE2D(_FlowMapVisualization);

inline half4 GetFlowmapEditor(float3 worldPos, half3 normal)
{
	float2 flowMapUV = worldPos.xz / _FlowMapSize + 0.5;
	if (flowMapUV.x < 0 || flowMapUV.x > 1 || flowMapUV.y < 0 || flowMapUV.y > 1) return float4(0.5, 0, 0, 1);
	return half4(pow((normal.xz + 0.75), 7), 1, 1);
}

// Flow map
float4 SampleFlowMap(inout GlobalData data, Texture2D tex,	float2 texUV, Texture2D flowMap,float2 uv, float speed,	float intensity)
{
	float4 flowVal = (float4)0;
#if _FLOWMAP_VISUALIZATION
	flowVal = (SAMPLE_TEXTURE2D(_FlowMapVisualization, URPWater_trilinear_repeat_sampler, uv) * 2 - 1) * intensity;
#else
	flowVal = (SAMPLE_TEXTURE2D(flowMap, URPWater_trilinear_repeat_sampler, uv) * 2 - 1) * intensity;
#endif

	float dif1 = frac(_Time.x * speed + 0.5);
	float dif2 = frac(_Time.x * speed);

	float lerpVal = abs((0.5 - dif1) / 0.5);
	
	float4 col1 = SAMPLE_TEXTURE2D(tex, URPWater_trilinear_repeat_sampler, texUV - flowVal.xy * dif1);
	float4 col2 = SAMPLE_TEXTURE2D(tex, URPWater_trilinear_repeat_sampler, texUV - flowVal.xy * dif2);

	return lerp(col1, col2, lerpVal);
}

float3 BlendNormals(float3 n1, float3 n2)
{
	return normalize(half3(n1.xy + n2.xy, n1.z * n2.z));
}

float3 NormalBlendReoriented(float3 A, float3 B)
{
	float3 t = A.xyz + float3(0.0, 0.0, 1.0);
	float3 u = B.xyz * float3(-1.0, -1.0, 1.0);
	return (t / t.z) * dot(t, u) - u;
}

void ComputeNormals(inout GlobalData data, Varyings IN)
{
	float3 tangentNormal;
	
#if _FLOWMAP_ON
	float4 flowNormal = SampleFlowMap(data, _NormalMapA, IN.texcoord.xy, _FlowMap, IN.texcoord.zw, _FlowSpeed, _FlowIntensity);
	tangentNormal = UnpackNormalScale(flowNormal, _NormalMapAIntensity);
#else
	float4 nA = SAMPLE_TEXTURE2D(_NormalMapA, URPWater_trilinear_repeat_sampler, IN.texcoord.xy);
	float4 nB = SAMPLE_TEXTURE2D(_NormalMapA, URPWater_trilinear_repeat_sampler, IN.texcoord.zw);

	float3 normalA = UnpackNormalScale(nA, _NormalMapAIntensity);
	float3 normalB = UnpackNormalScale(nB, _NormalMapAIntensity);

	tangentNormal = NormalBlendReoriented(normalA, normalB);

	float3 normalC = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMapB, URPWater_trilinear_repeat_sampler, IN.texcoord1.xy), _NormalMapBIntensity);
	tangentNormal = NormalBlendReoriented(tangentNormal, normalC);
	data.tangentNormal = tangentNormal;
#endif
	
	float3 normalWS = TransformTangentToWorld(tangentNormal, data.tangentToWorld);
	data.worldNormal = normalize(normalWS);
}

#endif // URPWATER_NORMALS_INCLUDED
