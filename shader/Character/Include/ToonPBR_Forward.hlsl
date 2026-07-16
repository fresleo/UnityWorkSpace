#ifndef TOONPBR_FORWARD_PASS_INCLUDED
#define TOONPBR_FORWARD_PASS_INCLUDED

#include "./ToonPBR_Dissolve.hlsl"
#include "./ToonPBR_Indirect.hlsl"
#include "./ToonPBR_CombatGlowSurface.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;

    float4 color        : COLOR;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    
    float2 texcoord     : TEXCOORD0;
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
    
    float4 screenPos        : TEXCOORD4;
    float3 positionWS       : TEXCOORD5;

    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);
    TOONPBR_DISSOLVE_FACTOR(7)
    UBPA_FOG_COORDS(8)

    float2 texcoord         : TEXCOORD9; // 原始 uv ，没做过 TRANSFORM_TEX 的

    float positionNDCw      : TEXCOORD10; // NDC 坐标的 w
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#include "./ToonPBR_FrostPass.hlsl"

// 顶点阶段方法
Varyings VertexFunction(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    // 顶点拉扯效果
    half3 direciton = TransformWorldToObjectDir(_VertexPullDirection, false);
    float3 direcitonPOS = VertexOffset(input.normalOS, direciton, _VertexPullIntensity, input.texcoord, TEXTURE2D_ARGS(_VertexPullNoiseTexture, sampler_VertexPullNoiseTexture), _VertexPullNoiseTexture_ST);
    input.positionOS.xyz += direcitonPOS;

    // 冰霜的顶点处理
    #if defined( _FROST_ON )
    FrostVertex(input);
    #endif
    
    VertexPositionInputs vertexInput = GetVertexPositionInputsWithFOVFix(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.texcoord = input.texcoord;
    //output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    output.normalWS = normalInput.normalWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.bitangentWS = normalInput.bitangentWS;

    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.positionWS = vertexInput.positionWS;

    output.color = input.color;
    output.screenPos = ComputeScreenPos(vertexInput.positionCS);
    output.screenPos.z = -vertexInput.positionVS.z; // 边缘光有用到 z
    output.positionNDCw = vertexInput.positionNDC.w;
    output.positionCS = vertexInput.positionCS;
    
    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, output.positionWS)
    UBPA_TRANSFER_FOG(output, vertexInput.positionWS)
    
    return output;
}

// 适配镶嵌的顶点阶段
#if defined( ASE_TESSELLATION )
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TessellationFuncs.hlsl"

struct VertexControl
{
    float4 positionOS : INTERNALTESSPOS;

    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    half4 color : COLOR;

    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

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

#include "./ToonPBR_TessellationFuncs.hlsl"

VertexControl ToonForwardPassVertex(Attributes v)
{
    VertexControl o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    o.positionOS = v.positionOS;

    o.normalOS = v.normalOS;
    o.tangentOS = v.tangentOS;
    o.color = v.color;

    o.texcoord = v.texcoord;
    o.lightmapUV = v.lightmapUV;

    return o;
}
#else
Varyings ToonForwardPassVertex(Attributes input)
{
    return VertexFunction(input);
}
#endif // ASE_TESSELLATION


// 写入 MRT 缓冲
void MRTBufferPass(Varyings input, out half4 outForwardBuffer0, out half4 outForwardBuffer1, out half4 outForwardBuffer2)
{
    half4 color0 = 0;
    color0.r = _BloomFactor;
    outForwardBuffer0 = color0;

    // 世界空间法线
    float sgn = input.tangentWS.w; // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);

    float2 uv = input.texcoord * _BaseMap_ST.xy + _BaseMap_ST.zw;
    float3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    normalWS = NormalizeNormalPerPixel(normalWS);

    half4 color1 = 0;
    color1.rgb = normalWS * _WriteDepthNormals_On;
    outForwardBuffer1 = color1;

    half4 color2 = 0;
    color2.g = input.positionCS.z;
    outForwardBuffer2 = color2;
}

#include "./ToonPBR_Transmission.hlsl"
#include "./ToonPBR_Translucency.hlsl"

// 初始化表面资源的数据
inline void InitializeSurfaceDataToon(
    float2 uv, Varyings input,
    out SurfaceDataToon outSurfaceData)
{
    float2 texcoord = input.texcoord;
    float3 positionWS = input.positionWS;
    float3 normalWS = normalize(input.normalWS);
    
    // 反照率
    half fresnel = 1;
    half4 baseAlbedo = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    {
        fresnel = baseAlbedo.a * _BaseMapA + (1 - _BaseMapA);
        baseAlbedo.a = baseAlbedo.a * (1 - _BaseMapA) + _BaseMapA;
    }
    half4 finalAlbedo = baseAlbedo;
    // 法线
    float3 baseNormals = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    float3 finalNormals = baseNormals;

    // 遮罩
    half4 pbrMaskCol = SAMPLE_TEXTURE2D_X(_PBRMaskMap, sampler_BaseMap, uv);
    
    #if defined(_DIFFUSE_OFFSET)
        // 自发光
        half3 baseEmission = SampleEmission(_EmissionColor.rgb, saturate(pbrMaskCol.a * 2 - 1));
        half3 finalEmission = baseEmission;
    #else
    // 自发光
    half3 baseEmission = SampleEmission(_EmissionColor.rgb, pbrMaskCol.a);
    half3 finalEmission = baseEmission;
    #endif

    // 处理金属性，光滑度纹理
    half4 specGloss = SampleMetallicSpecGloss(_Smoothness, pbrMaskCol.g, pbrMaskCol.b);

    // 冰霜效果
    #ifdef _FROST_ON
    InitializeSurfaceData_Frost(
        texcoord, positionWS, normalWS,
        baseAlbedo, baseNormals, baseEmission,
        finalAlbedo, finalNormals, finalEmission);
    #endif
    
    // 给输出赋值
    outSurfaceData.albedo = finalAlbedo.rgb * _BaseColor.rgb;
    outSurfaceData.alpha = Alpha(finalAlbedo.a, _BaseColor);
    outSurfaceData.fresnelMask = fresnel;
    
    outSurfaceData.normalTS = finalNormals;
    
    outSurfaceData.emission = finalEmission;

    outSurfaceData.specular = specGloss.rgb;
    outSurfaceData.smoothness = specGloss.a;
    #if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0f;
    #else
    outSurfaceData.metallic = specGloss.b;
    #endif
    
    // 环境反射
    outSurfaceData.envReflect = _EnvReflectStrength;

    outSurfaceData.shadingModel = _ShadingModel;

    outSurfaceData.toonShadowMask = pbrMaskCol.r;

    #if defined(_DIFFUSE_OFFSET)
        outSurfaceData.occlusion = saturate(pbrMaskCol.a * 2);
    #endif
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
    half3 unityProbeGI = SampleSH(inputData.normalWS);
    half3 characterGI = CharacterSampleSH(inputData.normalWS);
    inputData.bakedGI = unityProbeGI * _CharacterGIIntensityParams.x + characterGI * _CharacterGIIntensityParams.y;
    #else
    // inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    inputData.bakedGI = SampleSH(inputData.normalWS);
    #endif
    
    inputData.texcoord = input.texcoord;

    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
    inputData.screenPos = input.screenPos;

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    
    inputData.positionNDCw = input.positionNDCw;

    inputData.vertexColor = input.color;
}

// 片元阶段
void ToonForwardPassFragment(Varyings input
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
    
    // 抖动
    #ifdef _DITHER_ON
    DitherWithTexture(input.screenPos, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif

    float2 uv = input.texcoord * _BaseMap_ST.xy + _BaseMap_ST.zw;
    
    // 初始化卡通表面数据
    SurfaceDataToon surfaceData;
    InitializeSurfaceDataToon(uv, input, surfaceData);
    
    // 初始化卡通输入数据
    InputDataToon inputData;
    InitializeInputDataToon(input, surfaceData.normalTS, inputData);

    // 初始化卡通数据
    ToonData toonData;
    InitializeToonData(uv, surfaceData.envReflect, inputData.normalWS, inputData.positionWS, toonData);

    // 计算卡通光照
    half4 color = FragmentLitToon(inputData, surfaceData, toonData);
    
    // 叠加细节纹理
    // color.rgb *= toonData.detail;

    // 战斗辉光
    color.rgb = ApplyCombatSurfaceGlow(color.rgb, inputData, surfaceData, uv);

    // 加自发光效果
    color.rgb += surfaceData.emission;

    // 光传输
    #if defined( _TRANSMISSION_LIGHT_ON )
    float yMaskDown = GetYMaskDown(inputData.normalWS.y);
    ApplyTransmission(color, inputData, surfaceData.albedo, yMaskDown);
    ApplyTranslucency(color, inputData, surfaceData.albedo, yMaskDown);
    #endif

    color.rgb *= GetShadowmapTintMultiplier(GetSceneShadowAttenuation(inputData));

    // 角色 LUT 图调色
    // float character_LUT_Contribution = _Character_LUT_Params.w;
    // if (character_LUT_Contribution > 0.0)
    // {
    //     half3 tempColor = GetLinearToSRGB(color.rgb);
    //     half3 outLut = ApplyLut2D(TEXTURE2D_ARGS(_Character_LUT_Map, sampler_Character_LUT_Map), tempColor, _Character_LUT_Params.xyz);
    //     tempColor = lerp(tempColor, outLut, character_LUT_Contribution);
    //     color.rgb = GetSRGBToLinear(tempColor);
    // }
    
    TOONPBR_DISSOLVE_APPLY(color, uv, input) // 溶解
    UBPA_APPLY_FOG(input, color) // 雾效

    half outAlpha = color.a * _DitherAlpha;
    outColor = half4(color.rgb, outAlpha);
    
    #ifdef _MRT_BUFFER
    MRTBufferPass(input, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
    #endif // _MRT_BUFFER
}

#endif // TOONPBR_FORWARD_PASS_INCLUDED
