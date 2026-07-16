#ifndef __TERRAIN_PBR_DEPTH_NORMAL__
#define __TERRAIN_PBR_DEPTH_NORMAL__

#include "./TerrainPbrBakeSlat.hlsl"

struct AttributesDepthNormal
{
    float4 positionOS       : POSITION;
    float3 normalOS         : NORMAL;
    float2 texcoord         : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsDepthNormal
{
    float4 clipPos          : SV_POSITION;
    
    float4 uvMainAndLM      : TEXCOORD0; // xy: control, zw: lightmap
    
    #ifndef TERRAIN_SPLAT_BASEPASS
    float4 uvSplat01        : TEXCOORD1; // xy: splat0, zw: splat1
    float4 uvSplat23        : TEXCOORD2; // xy: splat2, zw: splat3
    #endif

    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half4 normal            : TEXCOORD3; // xyz: normal, w: viewDir.x
    half4 tangent           : TEXCOORD4; // xyz: tangent, w: viewDir.y
    half4 bitangent         : TEXCOORD5; // xyz: bitangent, w: viewDir.z
    #else
    half3 normal            : TEXCOORD3;
    #endif

    UNITY_VERTEX_OUTPUT_STEREO
};


VaryingsDepthNormal DepthNormalOnlyVertex(AttributesDepthNormal input)
{
    VaryingsDepthNormal output = (VaryingsDepthNormal)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if !defined(NOT_REQUIRES_TERRAIN_INSTANCING)
    TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);
    #endif

    const VertexPositionInputs attributes = GetVertexPositionInputs(input.positionOS.xyz);

    output.uvMainAndLM.xy = input.texcoord;
    output.uvMainAndLM.zw = input.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;
    #ifndef TERRAIN_SPLAT_BASEPASS
    output.uvSplat01.xy = TRANSFORM_TEX(input.texcoord, _Splat0);
    output.uvSplat01.zw = TRANSFORM_TEX(input.texcoord, _Splat1);
    output.uvSplat23.xy = TRANSFORM_TEX(input.texcoord, _Splat2);
    output.uvSplat23.zw = TRANSFORM_TEX(input.texcoord, _Splat3);
    #endif
    
    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(attributes.positionWS);
    float4 vertexTangent = float4(cross(float3(0, 0, 1), input.normalOS), 1.0);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, vertexTangent);

    output.normal = half4(normalInput.normalWS, viewDirWS.x);
    output.tangent = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);
    
    #else
    output.normal = TransformObjectToWorldNormal(input.normalOS);
    #endif

    output.clipPos = attributes.positionCS;

    return output;
}

void DepthNormalOnlyFragment(
    VaryingsDepthNormal input
    , out half4 outNormalWS : SV_Target0
    )
{
    float2 splatUV = (input.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

    half3 normalTS = half3(0.0h, 0.0h, 1.0h);
    half sampleAO = 1;
    half sampleMetallic = 0;
    #ifdef _USE_PACKED_TEXTURE_MDOE
    NormalMapMix(input.uvSplat01, input.uvSplat23, splatControl, normalTS, sampleAO, sampleMetallic);
    #else
    NormalMapMix(input.uvSplat01, input.uvSplat23, splatControl, normalTS);
    #endif

    #if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half3 normalWS = TransformTangentToWorld(normalTS, half3x3(-input.tangent.xyz, input.bitangent.xyz, input.normal.xyz));
    #else
    half3 normalWS = input.normal;
    #endif

    normalWS = NormalizeNormalPerPixel(normalWS);
    
    outNormalWS = half4(normalWS, 0.0);
}

#endif //__TERRAIN_PBR_DEPTH_NORMAL__