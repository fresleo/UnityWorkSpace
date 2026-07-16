#ifndef __TERRAIN_VT_LIT_FORWARD_PASS__
#define __TERRAIN_VT_LIT_FORWARD_PASS__

#include ".././ShaderLibrary/Lighting.hlsl"
#include ".././ShaderLibrary/ExtraBlend.hlsl"

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;

    float2 texcoord : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 uvMainAndLM : TEXCOORD0; // xy: control, zw: lightmap

    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 tangentWS : TEXCOORD3;
    float3 viewDirWS : TEXCOORD4;
    UBPA_FOG_COORDS(5)

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD6;
    #endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);

    float4 positionCS : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

//顶点阶段
Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uvMainAndLM.xy = TRANSFORM_TEX(input.texcoord, _Diffuse);

    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

    output.normalWS = normalInput.normalWS;
    output.viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

    return output;
}

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionCS = input.positionCS;
    inputData.positionWS = input.positionWS;

    float sgn = input.tangentWS.w; // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

    inputData.viewDirectionWS = SafeNormalize(input.viewDirWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
    inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

void InitializeSurfaceData(half3 albedo, half metallic, half smoothness, half occlusion, out SurfaceData surfaceData)
{
    surfaceData = (SurfaceData)0;

    surfaceData.albedo = albedo;
    surfaceData.metallic = metallic;
    surfaceData.occlusion = occlusion;
    surfaceData.smoothness = smoothness;
}

void MRTBufferPass(InputData inputData, out half4 outForwardBuffer0, out half4 outForwardBuffer1, out half4 outForwardBuffer2)
{
    half4 color0 = 0;
    outForwardBuffer0 = color0;

    half4 color1 = 0;
    color1.rgb = inputData.normalWS;
    outForwardBuffer1 = color1;

    half4 color2 = 0;
    color2.r = inputData.positionCS.z;
    outForwardBuffer2 = color2;
}

void LitPassFragment(Varyings input
    , out half4 outColor : SV_Target0
    #ifdef _MRT_BUFFER
    , out half4 outForwardBuffer0 : SV_Target1
    , out half4 outForwardBuffer1 : SV_Target2
    , out half4 outForwardBuffer2 : SV_Target3
    #endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 mixedDiffuse = SAMPLE_TEXTURE2D(_Diffuse, sampler_Diffuse, input.uvMainAndLM.xy);
    half4 mixedNormal = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, input.uvMainAndLM.xy);

    half3 normalTS = 0;
    normalTS.rg = mixedNormal.rg * 2 - 1;
    normalTS.b = sqrt(1 - normalTS.r * normalTS.r - normalTS.g * normalTS.g);
    normalTS = SafeNormalize(normalTS);

    half3 albedo = mixedDiffuse.rgb;
    half smoothness = mixedDiffuse.a;
    half occlusion = mixedNormal.b;
    half metallic = mixedNormal.a;

    InputData inputData;
    InitializeInputData(input, normalTS, inputData);

    SurfaceData surfaceData;
    InitializeSurfaceData(albedo, metallic, smoothness, occlusion, surfaceData);

    ExtendData extendData = (ExtendData)0;
    extendData.specularScaleBRDF = 1;
    
    half3 color = FragmentPBR(inputData, surfaceData, extendData);

    ExtraBlend(color, input.positionWS);
    UBPA_APPLY_FOG(input, color);

    outColor = half4(color, 1.0);

    #ifdef _MRT_BUFFER
    MRTBufferPass(inputData, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
    #endif // _MRT_BUFFER
}

#endif // __TERRAIN_VT_LIT_FORWARD_PASS__
