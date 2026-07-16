#ifndef TOONPBR_GHOST_FORWARD
#define TOONPBR_GHOST_FORWARD

#include "../Include/ToonPBR_Indirect.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;

    float4 color        : COLOR;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    
    float2 lightmapUV   : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS       : SV_POSITION;
    
    float4 color            : COLOR;
    
    float3 normalWS         : TEXCOORD0;
    float4 tangentWS        : TEXCOORD1;
    float3 bitangentWS      : TEXCOORD2;
    
    float4 positionSS       : TEXCOORD3;
    float3 positionWS       : TEXCOORD4;

    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
    
    float positionNDCw      : TEXCOORD6; // NDC 坐标的 w
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// 顶点阶段方法
Varyings VertexFunction(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.color = input.color;
    
    output.normalWS = normalInput.normalWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.bitangentWS = normalInput.bitangentWS;

    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
    
    output.positionSS = ComputeScreenPos(vertexInput.positionCS);
    output.positionSS.z = -vertexInput.positionVS.z; // 边缘光有用到 z

    output.positionWS = vertexInput.positionWS;
    output.positionNDCw = vertexInput.positionNDC.w;
    output.positionCS = vertexInput.positionCS;
    
    return output;
}

Varyings ToonForwardPassVertex(Attributes input)
{
    return VertexFunction(input);
}


// 初始化表面资源的数据
inline void InitializeSurfaceDataToon(
    Varyings input,
    out SurfaceDataToon outSurfaceData)
{
    // 处理金属性，光滑度纹理
    half4 specGloss = SampleMetallicSpecGloss(_Smoothness, 1.0, 1.0);
    
    // 给输出赋值
    outSurfaceData.albedo = _BaseColor.rgb;
    outSurfaceData.alpha = _BaseColor.a;
    outSurfaceData.fresnelMask = 1;
    
    // outSurfaceData.normalTS = UnpackNormalScale(half4(0.5, 0.5, 1.0, 1.0), 1.0);
    outSurfaceData.normalTS = half3(0, 0, 1);
    
    outSurfaceData.emission = 0;

    outSurfaceData.specular = specGloss.rgb;
    outSurfaceData.smoothness = specGloss.a;
    #if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0;
    #else
    outSurfaceData.metallic = specGloss.b;
    #endif
    
    outSurfaceData.envReflect = _EnvReflectStrength; // 环境反射

    outSurfaceData.shadingModel = _ShadingModel;

    outSurfaceData.toonShadowMask = 1.0;
}

// 初始化环境输入的数据
void InitializeInputDataToon(Varyings input, half3 normalTS, out InputDataToon inputData)
{
    inputData = (InputDataToon)0;
    inputData.positionWS = input.positionWS;

    float sgn = input.tangentWS.w; // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);

    // 经过对比，模型法线更好一些
    inputData.vertexNormalWS = normalize(input.normalWS);

    inputData.tangentWS = input.tangentWS;
    inputData.bitangentWS = input.bitangentWS.xyz;
    
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.viewDirTS = mul(tangentToWorld, inputData.viewDirectionWS);
    
    #if _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
    inputData.bakedGI = CharacterSampleSH(inputData.normalWS);
    #else
    // inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    inputData.bakedGI = SampleSH(inputData.normalWS);
    #endif
    
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    inputData.shadowMask = half4(1,1,1,1);
    inputData.screenPos = input.positionSS;

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    
    inputData.positionNDCw = input.positionNDCw;

    inputData.vertexColor = input.color;
}

// 片元阶段
void ToonForwardPassFragment(Varyings input
    , out half4 outColor : SV_Target0
    )
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // 抖动
    #ifdef _DITHER_ON
    DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif

    // 初始化卡通表面数据
    SurfaceDataToon surfaceData;
    InitializeSurfaceDataToon(input, surfaceData);
    
    // 初始化卡通输入数据
    InputDataToon inputData;
    InitializeInputDataToon(input, surfaceData.normalTS, inputData);

    // 初始化卡通数据
    ToonData toonData;
    InitializeToonData(surfaceData.envReflect, inputData.normalWS, inputData.positionWS, toonData);

    // 计算卡通光照
    // half4 color = FragmentLitToon(inputData, surfaceData, toonData);
    half4 color = FragmentLitToon_LOD1(inputData, surfaceData, toonData);

    // 加自发光效果
    color.rgb += surfaceData.emission;
    
    half outAlpha = color.a * _DitherAlpha;
    outColor = half4(color.rgb, outAlpha);
}

#endif
