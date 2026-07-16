#ifndef TOONPBR_FORWARD_PASS_LOD1_INCLUDED
#define TOONPBR_FORWARD_PASS_LOD1_INCLUDED

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
    
    half3 normalWS          : TEXCOORD0;
    half4 tangentWS         : TEXCOORD1;
    half3 bitangentWS       : TEXCOORD2;
    
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

#include "./Include/ToonPBR_FrostPass.hlsl"

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
    output.screenPos.z = -vertexInput.positionVS.z;
    output.positionNDCw = vertexInput.positionNDC.w;
    output.positionCS = vertexInput.positionCS;
    
    TOONPBR_DISSOLVE_TRANSFER_FACTOR(output, output.positionWS)
    UBPA_TRANSFER_FOG(output, vertexInput.positionWS)
    
    return output;
}

// 顶点阶段
Varyings ToonForwardPassVertex(Attributes input)
{
    return VertexFunction(input);
}


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
        fresnel = baseAlbedo.a * (1 - _BaseMapA) + _BaseMapA;
        baseAlbedo.a = baseAlbedo.a * _BaseMapA + (1 - _BaseMapA);
    }
    half4 finalAlbedo = baseAlbedo;

    // 遮罩
    half4 pbrMaskCol = SAMPLE_TEXTURE2D_X(_PBRMaskMap, sampler_BaseMap, uv);
    
    // 自发光
    half3 baseEmission = SampleEmission(_EmissionColor.rgb, pbrMaskCol.a);
    half3 finalEmission = baseEmission;
    
    // 处理金属性，光滑度纹理
    half4 specGloss = SampleMetallicSpecGloss(_Smoothness, pbrMaskCol.g, pbrMaskCol.b);

    // 冰霜效果
    #ifdef _FROST_ON
    InitializeSurfaceData_Frost_LOD1(
        texcoord, positionWS, normalWS,
        baseAlbedo, baseEmission,
        finalAlbedo, finalEmission);
    #endif
    
    // 给输出赋值
    outSurfaceData.albedo = finalAlbedo.rgb * _BaseColor.rgb;
    outSurfaceData.alpha = Alpha(finalAlbedo.a, _BaseColor);
    outSurfaceData.fresnelMask = fresnel;

    outSurfaceData.normalTS = half3(0, 0, 1);
    
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
    half4 color = FragmentLitToon_LOD1(inputData, surfaceData, toonData);
    color.rgb = ApplyCombatSurfaceGlow(color.rgb, inputData, surfaceData, uv);
    color.rgb += surfaceData.emission;
    color.rgb *= GetShadowmapTintMultiplier(GetSceneShadowAttenuation(inputData));
    
    TOONPBR_DISSOLVE_APPLY(color, uv, input) // 溶解
    UBPA_APPLY_FOG(input, color) // 雾效

    half outAlpha = color.a * _DitherAlpha;
    outColor = half4(color.rgb, outAlpha);
}

#endif // TOONPBR_FORWARD_PASS_LOD1_INCLUDED
