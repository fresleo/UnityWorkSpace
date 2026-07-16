#ifndef WATER_ALPHA_INCLUDED
#define WATER_ALPHA_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Water_Variables.hlsl"
#include "Water_Helpers.hlsl"

void ComputeAlpha(inout GlobalData data, Varyings IN)
{	
	float edgeMask = 1 - DistanceFade(data.sceneDepth, data.pixelDepth, 0, max(0, _EdgeSize));
	data.finalColor = lerp(data.clearColor, data.finalColor, edgeMask);
}

#endif