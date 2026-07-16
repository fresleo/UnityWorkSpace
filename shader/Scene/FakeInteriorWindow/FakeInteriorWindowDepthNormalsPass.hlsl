#ifndef __FAKE_INTERIOR_WINDOW_DEPTH_NORMALS_PASS__
#define __FAKE_INTERIOR_WINDOW_DEPTH_NORMALS_PASS__

#ifdef LOD_FADE_CROSSFADE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float3 positionOS : POSITION;
    
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    
    float4 uv0 : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    
    float3 normalWS : TEXCOORD0;
    float4 tangentWS : TEXCOORD1;
    
    float4 texCoord0 : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = vertexInput.positionCS;

    output.normalWS = normalInput.normalWS;
    output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
    
    output.texCoord0 = input.uv0;
    
    return output;
}

struct SurfaceDescriptionInputs
{
    float3 TangentSpaceNormal;
    float4 uv0;
};

struct SurfaceDescription
{
    float3 NormalTS;
};

SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output = (SurfaceDescriptionInputs)0;
    
    output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
    output.uv0 = input.texCoord0;

    return output;
}

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
    SurfaceDescription surface = (SurfaceDescription)0;

    float4 noNormal = float4(0, 0, 1, 1);
    float4 normalMap = noNormal;
    #ifdef _CRACK_MASK_ON
    float2 normalUV = IN.uv0.xy * _Crack_Mask_ST.xy + _Crack_Mask_ST.zw;
    normalMap.xyz = SampleXyNormal(normalUV, TEXTURE2D_ARGS(_Crack_Mask, sampler_Crack_Mask));
    #endif // _CRACK_MASK_ON

    float breakStep = 0; // 0 = 没破洞
    #ifdef _BREAK_MASK_ON
    half4 breakMask = SAMPLE_TEXTURE2D(_Break_Mask, sampler_Break_Mask, IN.uv0.xy * _Break_Mask_ST.xy + _Break_Mask_ST.zw);
    half breakMaskR = breakMask.r;
    
    float glassBreakFloor = floor(_Glass_Break * 50) * 0.02;
    breakStep = step(breakMaskR, glassBreakFloor);
    #endif // _BREAK_MASK_ON
    
    float4 normalTS = lerp(normalMap, noNormal, breakStep.xxxx);
    
    surface.NormalTS = normalTS.xyz;

    return surface;
}

void frag(
    Varyings input
    , out half4 outNormalWS : SV_Target0
    )
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
    #endif
    
    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(input);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
    
    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
    
    float3 normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
    
    outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
}

#endif // __FAKE_INTERIOR_WINDOW_DEPTH_NORMALS_PASS__
