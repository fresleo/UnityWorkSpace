#ifndef __TREE0_VIEW_SPACE_NORMALS__
#define __TREE0_VIEW_SPACE_NORMALS__

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightViewSpaceNormals_Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"

Varyings ViewSpaceNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
    
    output.positionCS = vertexInput.positionCS;
    output.normalWS = normalInput.normalWS;
    
    return output;
}

half4 ViewSpaceNormalsFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 albedo = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    clip(albedo.a * _BaseColor.a - _Cutoff);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    // 法线
    half3 normalVS = mul(input.normalWS, (float3x3) UNITY_MATRIX_I_V);
    
    half3 remapNormal = 0;
    Remap(normalVS, float2(-1, 1), float2(0, 1), remapNormal);

    half4 col = half4(remapNormal, 1); // a是是否有写入的标记，所以这里给1
    return col;
}

#endif // __TREE0_VIEW_SPACE_NORMALS__
