#ifndef __BILLBOARD_FORWARD_PASS__
#define __BILLBOARD_FORWARD_PASS__

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "../ShaderLibrary/Lighting.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 3);
    UBPA_FOG_COORDS(4)

    float objectId : TEXCOORD5;

    float4 positionCS : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    //Calculate new billboard vertex position and normal;
    float3 upCamVec = float3(0, 1, 0);
    float3 forwardCamVec = -normalize(UNITY_MATRIX_V._m20_m21_m22);
    float3 rightCamVec = normalize(UNITY_MATRIX_V._m00_m01_m02);
    float3x3 rotationCamMatrix = float3x3(rightCamVec, upCamVec, forwardCamVec);

    // TRS
    float3 scale = float3(
        length(GetObjectToWorldMatrix()._m00_m10_m20),
        length(GetObjectToWorldMatrix()._m01_m11_m21),
        length(GetObjectToWorldMatrix()._m02_m12_m22));
    float3 positionWS = mul(input.positionOS.xyz * scale, rotationCamMatrix);
    positionWS += GetObjectToWorldMatrix()._m03_m13_m23;
    output.positionWS = positionWS;

    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
    output.normalWS = normalInput.normalWS;

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.positionCS = TransformWorldToHClip(positionWS);

    UBPA_TRANSFER_FOG(output, output.positionWS);

    // 基于中心点计算对象id
    VertexPositionInputs vertexInput0 = GetVertexPositionInputs(float3(0, 0, 0));
    float objectId = dot(vertexInput0.positionWS, 1);
    output.objectId = objectId;

    return output;
}

void MRTBufferPass(Varyings input, float objectId, out half4 outForwardBuffer0, out half4 outForwardBuffer1, out half4 outForwardBuffer2)
{
    half4 color0 = 0;
    color0.r = _BloomFactor;
    color0.g = _WaterColorOn;
    color0.b = objectId * _SceneSpaceOutlineOn;
    outForwardBuffer0 = color0;

    half4 color1 = 0;
    color1.rgb = NormalizeNormalPerPixel(input.normalWS);
    outForwardBuffer1 = color1;

    half4 color2 = 0;
    color2.r = input.positionCS.z;
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

    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    clip(albedoAlpha.a * _BaseColor.a - _Cutoff);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    half3 albedo = albedoAlpha.rgb * _BaseColor.rgb;

    half3 bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, input.normalWS) * _GIIndirectDiffuseBoost;
    half3 color = albedo * bakedGI;

    UBPA_APPLY_FOG(input, color);

    outColor = half4(color, 1.0);
    
    #ifdef _MRT_BUFFER
    MRTBufferPass(input, input.objectId, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
    #endif // _MRT_BUFFER
}

#endif // __BILLBOARD_FORWARD_PASS__
