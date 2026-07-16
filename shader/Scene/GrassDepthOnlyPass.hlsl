#ifndef __GRASS_DEPTH_ONLY_PASS__
#define __GRASS_DEPTH_ONLY_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define _TYPE_GRASS_ON
#include "./Wind.hlsl"
#include "../ShaderLibrary/InteractiveParams.hlsl"

SurfaceInput DepthOnlyVertex(VertexAttributes input)
{
    SurfaceInput output = (SurfaceInput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    #ifdef _INTERSECTION_ON
    output.positionWS += VegetationInteractiveWS(output.positionWS, input.uv0.y);
    #endif

    #ifdef _WIND_ON
    Wind(input, output, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
    #endif
    
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.positionSS = ComputeScreenPos(output.positionCS);
    
    return output;
}

half DepthOnlyFragment(SurfaceInput input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    #if defined(_DISABLE_DEPTHONLY)
    clip(-1);
    #endif
    
    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    //ApplyGrassDitherClip(input.positionSS, input.positionWS);

    return input.positionCS.z;
}

#endif
