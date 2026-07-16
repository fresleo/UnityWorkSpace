#ifndef __TREE0_DEPTH_ONLY_PASS__
#define __TREE0_DEPTH_ONLY_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#define _TYPE_PLANT_ON
#include "./Wind.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/SurfaceInput.hlsl"

SurfaceInput DepthOnlyVertex(VertexAttributes input)
{
    SurfaceInput output = (SurfaceInput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.uv0.xy = TRANSFORM_TEX(input.uv0, _BaseMap);
    
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    #ifdef _WIND_ON
    Wind(input, output, output.positionWS, _WindStrength, _WindVariation, _TurbulenceStrength);
    #endif
    
    output.positionCS = TransformWorldToHClip(output.positionWS);
    
    return output;
}

half DepthOnlyFragment(SurfaceInput input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    half4 albedo = SampleAlbedoAlpha(input.uv0, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    clip(albedo.a * _BaseColor.a - _Cutoff);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    return input.positionCS.z;
}

#endif //__TREE0_DEPTH_ONLY_PASS__