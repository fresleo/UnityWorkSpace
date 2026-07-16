#ifndef __KNIFE_MARK__FORWARD_PASS__
#define __KNIFE_MARK__FORWARD_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
// #include "Assets/OutputRes/shader/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

// GLES2 的插值器数量有限
#if defined(_PARALLAXMAP) && !defined(SHADER_API_GLES)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if ( defined(_NORMALMAP) || ( defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR) ) )
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

struct Attributes
{
    float4 positionOS			: POSITION;
    float3 normalOS				: NORMAL;
    float4 tangentOS			: TANGENT;
    
    float2 texcoord				: TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS			: SV_POSITION;
    
    float3 normalWS				: TEXCOORD0;
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    float4 tangentWS			: TEXCOORD1;
    #endif
    
    float3 vertexSH             : TEXCOORD2;

    float3 positionWS			: TEXCOORD3;
    UBPA_FOG_COORDS(4)
    
    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS             : TEXCOORD5;
    #endif

    float2 uv					: TEXCOORD6; // 原始 UV
    float2 projUV               : TEXCOORD7; // 投影 UV
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = vertexInput.positionCS;
    
    output.normalWS = normalInput.normalWS;
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    #endif
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
    #endif
    
    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
    #endif

    output.positionWS = vertexInput.positionWS;

    UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

    // 原始的 UV
    float2 rawUV = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.uv = rawUV;

    // 投影UV
    float4 projPos = mul(_PM, float4(vertexInput.positionWS, 1.0));
    output.projUV = projPos.xyz / max(projPos.w, 1e-5); // 防止除0
    
    return output;
}

void InitializeStandardLitSurfaceData(Varyings input,
    out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    float2 uv = input.uv;
    
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half baseAlpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    outSurfaceData.alpha = baseAlpha * _AlphaControl;
    
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);
    
    outSurfaceData.metallic = _Metallic;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);

    outSurfaceData.smoothness = _Smoothness;
    outSurfaceData.occlusion = 1;
    
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    // 合并两种发光效果
    half3 highTempEmission = half3(0, 0, 0);
    SampleHighTempEmission(uv, TEXTURE2D_ARGS(_HighTempMap, sampler_HighTempMap), highTempEmission);
    outSurfaceData.emission = highTempEmission;
    
    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
}

void InitializeInputData(Varyings input, half3 normalTS,
    out InputData outInputData)
{
    outInputData = (InputData)0;

    outInputData.positionWS = input.positionWS;

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    #if defined(_NORMALMAP)
    float sgn = input.tangentWS.w; // 应该为 +1 或 -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    
    outInputData.tangentToWorld = tangentToWorld;
    
    outInputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    #else
    outInputData.normalWS = input.normalWS;
    #endif
    
    outInputData.normalWS = NormalizeNormalPerPixel(outInputData.normalWS);
    outInputData.viewDirectionWS = viewDirWS;

    outInputData.shadowCoord = float4(0, 0, 0, 0);
    outInputData.fogCoord = 0;
    
    outInputData.bakedGI = SampleSHPixel(input.vertexSH, outInputData.normalWS);
    // outInputData.bakedGI = SampleSHPixel(input.vertexSH, outInputData.normalWS) * _GIIndirectDiffuseBoost;
    
    outInputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    outInputData.shadowMask = half4(1, 1, 1, 1);
}

void LitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // 矫正完的 UV
    float3 projDir = normalize(mul((float3x3)_PM, float3(0, 0, 1)));
    float normalDotProj = abs(dot(normalize(input.normalWS), projDir));
    float2 correctionUV = lerp(input.projUV, input.uv, (1 - normalDotProj) * _CurvatureCorrection);

    // 选择 UV 矫正的强度
    float2 finalUV = lerp(input.uv, correctionUV, _UVCorrection);

    // UV缩放和偏移
    float2 scaledUV = finalUV;
    scaledUV.x = _UVTilingOffset.x < 0 ? 
        (1.0 - finalUV.x) * abs(_UVTilingOffset.x) + _UVTilingOffset.z :
        finalUV.x * _UVTilingOffset.x + _UVTilingOffset.z;
    scaledUV.y = _UVTilingOffset.y < 0 ? 
        (1.0 - finalUV.y) * abs(_UVTilingOffset.y) + _UVTilingOffset.w :
        finalUV.y * _UVTilingOffset.y + _UVTilingOffset.w;

    // 当我们用 _UVTilingOffset 缩放过 UV 后，会出现多个图像，要靠限位来只保留第一个
    #ifdef _SINGLEIMAGE_ON
    input.uv = clamp(scaledUV, 0, 1);
    #endif

    #if defined(_PARALLAXMAP)
    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
    #else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
    #endif
    input.uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap2D, sampler_ParallaxMap2D), viewDirTS, _Parallax, input.uv);
    #endif
    
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    
    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    // ExtendData extendData = (ExtendData)0;
    // extendData.specularScaleBRDF = _SpecularScaleBRDF;
    //
    // half4 color = FragmentPBR(inputData, surfaceData, extendData);
    
    UBPA_APPLY_FOG(input, color);
    outColor = color;
}

#endif // __KNIFE_MARK__FORWARD_PASS__
