#ifndef WATER_COMMON_INCLUDED
#define WATER_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Water_Normals.hlsl"

Varyings vert(Attributes v)
{
	Varyings OUT;

	// TODO Gerstner波动

	float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
	OUT.positionWS = worldPos;

	OUT.pos = TransformObjectToHClip(v.vertex.xyz);

	OUT.positionVS = TransformWorldToView(worldPos);

	VertexNormalInputs vertexTBN = GetVertexNormalInputs(v.normal, v.tangent);

	OUT.normal = float4(vertexTBN.normalWS, worldPos.x);
	OUT.tangent = float4(vertexTBN.tangentWS, worldPos.y);
	OUT.bitangent = float4(vertexTBN.bitangentWS, worldPos.z);

	OUT.worldNormal = OUT.normal.xyz;
	OUT.screenCoord = ComputeScreenPos(OUT.pos);
	OUT.screenCoord.z = ComputePixelDepth(worldPos);

#if _WORLDUV_ON
	v.texcoord.xy = worldPos.xz * 0.1;
#endif

#if _FLOWMAP_ON
		OUT.texcoord.xy = v.texcoord.xy * _NormalMapATilings.xy;
		OUT.texcoord.zw = v.texcoord.xy * _FlowTiling.xy + _FlowTiling.zw;
#else
		OUT.texcoord = DualAnimatedUV(v.texcoord, _NormalMapATilings, _NormalMapASpeeds);
#endif

	OUT.texcoord1.xy = AnimatedUV(v.texcoord.xy, _NormalMapBTilings.xy, _NormalMapBSpeeds.xy).xy;

	UBPA_TRANSFER_FOG(OUT, worldPos);
	
	return OUT;
}

float4 frag(Varyings IN) : SV_Target
{
	GlobalData data;
	InitializeGlobalData(data, IN);
	
	ComputeNormals(data, IN);
	ComputeRefractionData(data);
	ComputeLighting(data, IN);
	ComputeReflections(data, IN);

	UBPA_APPLY_FOG(IN, data.finalColor);
	
	ComputeAlpha(data, IN);

#if _FLOWMAP_VISUALIZATION
	return GetFlowmapEditor(data.worldPosition, data.worldNormal);
#endif
	
	return float4(data.finalColor, 1.0f);
}

float4 frag_LOD1(Varyings IN) : SV_Target
{
	GlobalData data;
	InitializeGlobalData(data, IN);

	ComputeNormals(data, IN);
	ComputeRefractionData_LOD1(data);
	ComputeLighting_LOD1(data, IN);
	ComputeReflections(data, IN);

	UBPA_APPLY_FOG(IN, data.finalColor);
	
	ComputeAlpha(data, IN);
	
	// return data.debugInfo;

#if _FLOWMAP_VISUALIZATION
	return GetFlowmapEditor(data.worldPosition, data.worldNormal);
#endif
	
	return float4(data.finalColor, 1.0f);
}

#endif