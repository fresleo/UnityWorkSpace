#ifndef __LIT_DEPTH_NORMALS_PASS__
#define __LIT_DEPTH_NORMALS_PASS__

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthNormalsPass_Core.hlsl"

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

Varyings DepthNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

    #if defined( _ALPHATEST_ON )
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    #endif
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);

    output.normalWS = half3(normalInput.normalWS);
    
    float sign = input.tangentOS.w * float(GetOddNegativeScale());
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.tangentWS = tangentWS;

    return output;
}

void DepthNormalsFragment(
    Varyings input
    , out half4 outNormalWS : SV_Target0
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined( _ALPHATEST_ON )
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    // 项目中普遍都是强制需要有法线的，所以这里直接读了
    float sgn = input.tangentWS.w; // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    float3 normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    normalWS = NormalizeNormalPerPixel(normalWS);
    
    outNormalWS = half4(normalWS, 0.0);
}

#endif //__LIT_DEPTH_NORMALS_PASS__