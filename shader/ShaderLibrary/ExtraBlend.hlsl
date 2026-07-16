#ifndef EXTRA_BLEND
#define EXTRA_BLEND

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/CloudShadow.hlsl"
#include "ColorMask.hlsl"

void ExtraBlend(inout half3 col, float3 positionWS)
{
    //CloudShadow(col, positionWS);
    //BlendColorMask(col, positionWS);
}

#endif