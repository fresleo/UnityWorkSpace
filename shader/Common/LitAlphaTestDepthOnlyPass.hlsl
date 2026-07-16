#ifndef __LIT_ALPHA_TEST_DEPTH_ONLY_PASS__
#define __LIT_ALPHA_TEST_DEPTH_ONLY_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthOnlyPass_Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/SurfaceInput_AlphaTestOn.hlsl"

Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    
    return output;
}

half DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_AlphaTestMap, sampler_AlphaTestMap)).a, _BaseColor, _Cutoff);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    return input.positionCS.z;
}

#endif // __LIT_ALPHA_TEST_DEPTH_ONLY_PASS__
