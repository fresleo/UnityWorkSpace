#ifndef __LIT_ALPHA_TEST_DEPTH_MASK_PASS__
#define __LIT_ALPHA_TEST_DEPTH_MASK_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/SurfaceInput_AlphaTestOn.hlsl"

Varyings DepthMaskVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

    // 基于中心点计算对象id
    VertexPositionInputs vertexInput0 = GetVertexPositionInputs(float3(0, 0, 0));
    float objectId = dot(vertexInput0.positionWS, 1);
    output.objectId = objectId;
    
    return output;
}

void DepthMaskFragment(
    Varyings input
    , out half4 outColor : SV_Target
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_AlphaTestMap, sampler_AlphaTestMap)).a, _BaseColor, _Cutoff);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    half4 color = 0;
    #if defined( _BLOOMFACTORMASK )
    color.r = _BloomFactor;
    
    #elif defined( _WATERCOLORMASK )
    float val = _WaterColorOn;
        #if defined( SHADER_API_GLES3 )
    val *= input.positionCS.z; // 变成深度遮罩
    val = val * _DepthScale - _DepthBias;
        #endif
    color.r = val;
    
    #elif defined( _SCENESPACEOUTLINEMASK )
    color.r = input.objectId * _SceneSpaceOutlineOn;
    #endif
    outColor = color;
}

#endif // __LIT_ALPHA_TEST_DEPTH_MASK_PASS__
