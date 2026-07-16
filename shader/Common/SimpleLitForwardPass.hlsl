#ifndef __SIMPLE_LIT_FORWARD_PASS__
#define __SIMPLE_LIT_FORWARD_PASS__

#if defined( LOD_FADE_CROSSFADE )
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/ExtraBlend.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    
    UBPA_FOG_COORDS(3)
    
    #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
    float4 shadowCoord : TEXCOORD4;
    #endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);

    #ifdef _DITHER_ON
    float4 positionSS : TEXCOORD6;
    #endif
    
    float objectId : TEXCOORD7;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    half3 viewDirWS;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    #if defined( REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR )
    output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
    #endif

    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;

    UBPA_TRANSFER_FOG(output, vertexInput.positionWS);

    #ifdef _DITHER_ON
    output.positionSS = ComputeScreenPos(vertexInput.positionCS);
    #endif
    
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
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined( LOD_FADE_CROSSFADE )
    LODFadeCrossFade(input.positionCS);
    #endif

    // 抖动
    #ifdef _DITHER_ON
    DitherWithTexture(input.positionSS, 1.0 - _DitherIntensity, _DitherSize, _DitherWithMatrix, 
        TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    #endif
    
    half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
    half3 bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, input.normalWS);

    float4 shadowCoord = float4(0, 0, 0, 0);
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #endif
    
    half4 shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    
    half3 color = FragmentDiffuse(shadowMask, shadowCoord, input.positionWS, input.normalWS, bakedGI, albedo);
    ExtraBlend(color, input.positionWS);
    UBPA_APPLY_FOG(input, color);

    half outAlpha = _BaseColor.a * _DitherAlpha;
    outColor = half4(color, outAlpha);
    
    #ifdef _MRT_BUFFER
    MRTBufferPass(input, input.objectId, outForwardBuffer0, outForwardBuffer1, outForwardBuffer2);
    #endif // _MRT_BUFFER
}

#endif // __SIMPLE_LIT_FORWARD_PASS__
