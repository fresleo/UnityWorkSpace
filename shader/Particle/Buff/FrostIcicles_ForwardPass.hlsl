#ifndef __FROST_ICICLES_FORWARD_PASS__
#define __FROST_ICICLES_FORWARD_PASS__

#include "../../ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TessellationFuncs.hlsl"

#include "../../Character/Include/ToonPBR_Core.hlsl"
#include "../../Character/Include/ToonPBR_Specular.hlsl"
#include "../../Character/Include/ToonPBR_Diffuse.hlsl"
#include "../../Character/Include/ToonPBR_Rim.hlsl"
#include "../../Character/Include/ToonPBR_Fresnel.hlsl"
#include "../../Character/Include/ToonPBR_Lighting.hlsl"

#include "../../Character/Include/ToonPBR_Indirect.hlsl"

#include "./FrostIcicles_Transmission.hlsl"
#include "./FrostIcicles_Translucency.hlsl"


struct Attributes
{
    float4 positionOS   : POSITION;
    
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float4 color        : COLOR;
    
    float4 texcoord     : TEXCOORD0;
    float4 lightmapUV   : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS               : SV_POSITION;

    float3 normalWS                 : TEXCOORD0;
    float4 tangentWS                : TEXCOORD1;
    float3 bitangentWS              : TEXCOORD2;
    
    float3 positionWS               : TEXCOORD3;
    float2 uv                       : TEXCOORD4;
    float4 lightmapUVOrVertexSH     : TEXCOORD5;
    half4 fogFactorAndVertexLight   : TEXCOORD6;

    float4 color                    : TEXCOORD7;
    float4 screenPos                : TEXCOORD8;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


// 顶点阶段 --------------------------------------------------------------------------------
// 冰霜的顶点方法
void FrostVertex(inout Attributes input)
{
    float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);

    // 冰柱遮罩
    float2 uv_IcicleMask = float2(worldNormal.x, worldNormal.z) * _IcicleMaskTile + 0.5; // [-0.5, 0.5] -> [0, 1]
    half4 icicleMaskRgba = SAMPLE_TEXTURE2D_X_LOD(_IcicleMask, sampler_IcicleMask, uv_IcicleMask, 0);

    float yMaskTop = GetYMaskTop(worldNormal.y);
    float yMaskDown = GetYMaskDown(worldNormal.y);
    float4 expandValue = yMaskDown * icicleMaskRgba * _IcicleLength + yMaskTop;

    // 冰的覆盖遮罩
    float2 uv_IceOverlayMask = input.texcoord.xy * _IceOverlayMask_ST.xy + _IceOverlayMask_ST.zw;
    half4 iceOverlayMaskRgba = SAMPLE_TEXTURE2D_X_LOD(_IceOverlayMask, sampler_IceOverlayMask, uv_IceOverlayMask, 0);
    float iceOverlayMaskValue = iceOverlayMaskRgba.r;

    // 沿着法线往外拉伸
    float4 vertexValue = 0;
    vertexValue += float4(input.normalOS, 0) * expandValue;
    #ifdef _ICE_OVERLAY_MASK_ON
    vertexValue *= iceOverlayMaskValue;
    #endif
    input.positionOS.xyz += vertexValue.xyz;
}

Varyings VertexFunction(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    FrostVertex(input);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = vertexInput.positionCS;
    
    output.normalWS = normalInput.normalWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.bitangentWS = normalInput.bitangentWS;

    output.positionWS = vertexInput.positionWS;
    output.uv = input.texcoord.xy;

    OUTPUT_SH( normalInput.normalWS.xyz, output.lightmapUVOrVertexSH.xyz );

    half fogFactor = 0;
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    output.color = input.color;
    output.screenPos = ComputeScreenPos(vertexInput.positionCS);
    output.screenPos.z = -vertexInput.positionVS.z;
    
    return output;
}

// 镶嵌相关逻辑
#if defined( ASE_TESSELLATION )
struct VertexControl
{
    float4 positionOS : INTERNALTESSPOS;
    
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    
    float4 texcoord : TEXCOORD0;
    float4 lightmapUV : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

VertexControl vert(Attributes input)
{
    VertexControl output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionOS = input.positionOS;
    output.normalOS = input.normalOS;
    output.tangentOS = input.tangentOS;
    output.color = input.color;
    output.texcoord = input.texcoord;
    output.lightmapUV = input.lightmapUV;

    return output;
}

Attributes PatchToOutput(OutputPatch<VertexControl, 3> patch, float3 barycentricCoordinates)
{
    Attributes output = (Attributes)0;

    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, positionOS);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, normalOS);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, tangentOS);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, color);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, texcoord);
    MY_DOMAIN_PROGRAM_INTERPOLATE(barycentricCoordinates, output, patch, lightmapUV);

    return output;
}

#include "./FrostIcicles_TessellationFuncs.hlsl"

#else
Varyings vert(Attributes input)
{
    return VertexFunction(input);
}
#endif // ASE_TESSELLATION


// 片元阶段 --------------------------------------------------------------------------------
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
    
    inputData.texcoord = input.uv;
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    inputData.shadowMask = half4(1,1,1,1);
    inputData.screenPos = input.screenPos;

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
}

half4 frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // 初始化卡通表面数据
    SurfaceDataToon surfaceData;
    InitializeSurfaceDataToon(input.uv, input.positionWS, input.normalWS, surfaceData);
    
    // 初始化卡通输入数据
    InputDataToon inputData;
    InitializeInputDataToon(input, surfaceData.normalTS, inputData);

    // 初始化卡通数据
    ToonData toonData;
    InitializeToonData(input.uv, surfaceData.envReflect, inputData.normalWS, inputData.positionWS, toonData);

    // 计算卡通光照
    half4 color = FragmentLitToon(inputData, surfaceData, toonData);
    color.rgb += surfaceData.emission;

    // 光传输
    #if defined( _TRANSMISSION_LIGHT_ON )
    float yMaskDown = GetYMaskDown(inputData.normalWS.y);
    ApplyTransmission(color, inputData, surfaceData.albedo, yMaskDown);
    ApplyTranslucency(color, inputData, surfaceData.albedo, yMaskDown);
    #endif

    return color;
}

#endif // __FROST_ICICLES_FORWARD_PASS__
