#ifndef __LIT_ALPHA_TEST_DEPTH_NORMALS_PASS__
#define __LIT_ALPHA_TEST_DEPTH_NORMALS_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthNormalsPass_Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/SurfaceInput_AlphaTestOn.hlsl"

Varyings DepthNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);

    output.positionCS = vertexInput.positionCS;
    output.normalWS = normalInput.normalWS;

    return output;
}

void DepthNormalsFragment(Varyings input
    , out half4 outNormalWS : SV_Target0
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_AlphaTestMap, sampler_AlphaTestMap)).a, _BaseColor, _Cutoff);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    float3 normalWS = input.normalWS;
    normalWS = NormalizeNormalPerPixel(normalWS);
    
    outNormalWS = half4(normalWS, 0.0);
}

#endif // __LIT_ALPHA_TEST_DEPTH_NORMALS_PASS__
