#ifndef __EXCLUDE_CHARACTER_LIB__
#define __EXCLUDE_CHARACTER_LIB__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define FAR_DEPTH_THRESHOLD     0.999h

void ExcludeCharacter(in float4 positionSS)
{
    #ifdef _EXCLUDE_CHARACTER_ON
    
    float2 screenUV = positionSS.xy / positionSS.w;

    half4 cameraCharacterDepthColor = SAMPLE_TEXTURE2D(_CameraCharacterDepthTexture, sampler_CameraCharacterDepthTexture, screenUV);
    half cameraCharacter01Depth = Linear01Depth(cameraCharacterDepthColor.r, _ZBufferParams);
    clip(cameraCharacter01Depth - FAR_DEPTH_THRESHOLD);
    
    #endif
}

#endif
