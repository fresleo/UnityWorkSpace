#ifndef __BFX_VATBLOOD_FORWARDPASS__
#define __BFX_VATBLOOD_FORWARDPASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Assets/OutputRes/shader/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

struct Attributes
{
    float4 positionOS			: POSITION;
    float3 normalOS				: NORMAL;
    float4 tangentOS			: TANGENT;
    float4 color                : COLOR;
    float2 texcoord				: TEXCOORD0;
    float2 staticLightmapUV		: TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2	uv						: TEXCOORD0;
    float3	positionWS				: TEXCOORD1;

    float3	normalWS				: TEXCOORD2;
    float4	tangentWS				: TEXCOORD3;
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4	shadowCoord				: TEXCOORD4;
    #endif
    
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);

    float3	positionVS				: TEXCOORD6;
    
    UBPA_FOG_COORDS(7)
    
    float4	positionCS				: SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ForwardPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    // uv
    float timeInFrames = _TimeInFrames;
    float4 sampleUv = float4(input.texcoord.x, (timeInFrames + input.texcoord.y), 0, 0);

    float4 texturePos = SAMPLE_TEXTURE2D_LOD(_PosMap, sampler_PosMap, sampleUv, 0);
    float3 textureN = SAMPLE_TEXTURE2D_LOD(_BumpMap, sampler_BumpMap, sampleUv, 0).rgb;
    // HDR 应该是 2.2，其实需要判断一下
    texturePos.xyz = LinearToGamma22(texturePos.xyz);
    textureN = LinearToGamma22(textureN);

    // 顶点
    float expand = _BoundingMax - _BoundingMin;
    texturePos.xyz *= expand;
    texturePos.xyz += _BoundingMin;
    texturePos.x *= -1;
    input.positionOS.xyz = texturePos.xzy;
    input.positionOS.xyz += _HeightOffset.xyz;

    // 世界法线
    output.normalWS = textureN.xzy * 2 - 1;
    output.normalWS.x *= -1;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    // 世界切线
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
    
    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
    #endif

    output.positionVS = vertexInput.positionVS;
    
    output.positionWS = vertexInput.positionWS;
    UBPA_TRANSFER_FOG(output, vertexInput.positionWS);
    
    output.positionCS = vertexInput.positionCS;
    
    return output;
}

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

    #if defined(_GLOBAL_RAIN_ON) && defined(_LOCAL_RAIN_ON)
    //	inputData.normalWS = ComputeRainWorldNormal(input.positionWS, normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    #else
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    #endif

    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
    inputData.shadowCoord = float4(0,0,0,0);
    #endif
    
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
}

void ForwardPassFragment(Varyings input
    , out half4 outColor : SV_Target0
    )
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.positionVS, input.normalWS, surfaceData);
    
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    ExtendData extendData = (ExtendData)0;
    extendData.specularScaleBRDF = 1;
    
    half4 color = FragmentPBR(inputData, surfaceData, extendData);
    UBPA_APPLY_FOG(input, color);
    
    outColor = color;
}

#endif // __BFX_VATBLOOD_FORWARDPASS__