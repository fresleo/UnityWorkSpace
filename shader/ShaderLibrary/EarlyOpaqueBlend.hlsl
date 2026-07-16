#ifndef EARLY_OPAQUE_BLEND_INCLUDED
#define EARLY_OPAQUE_BLEND_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

half3 EarlyOpaqueBlend(float2 screenUV, float thisZ, float depthBlend, half3 originalColor)
{
    float sceneZ = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
    float fade = saturate((sceneZ - thisZ) * depthBlend);
    half3 earlyColor = SampleSceneColor(screenUV);

    return lerp(earlyColor, originalColor, fade);
}

#define EARLY_OPAQUE_BLEND(screenUV,thisZ,depthBlend,inoutColor)\
    (inoutColor) = EarlyOpaqueBlend((screenUV), (thisZ), (depthBlend), (inoutColor));

#endif	//EARLY_OPAQUE_BLEND_INCLUDED