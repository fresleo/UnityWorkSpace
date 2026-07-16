#ifndef __INTERACTIVE_SNOW_META_PASS__
#define __INTERACTIVE_SNOW_META_PASS__

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
    float4 uv						: TEXCOORD0; // xy 是底图，遮罩图，自发光图的 uv， zw 是法线图的 uv
    float3 positionWS				: TEXCOORD1;

    float3 normalWS					: TEXCOORD2;
    float4 tangentWS				: TEXCOORD3;
    
    float2 maskUV					: TEXCOORD4;
    
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord				: TEXCOORD6;
    #endif

    #if defined(_OPAQUE_BLEND)
    float4 positionSS				: TEXCOORD8;
    #endif

    float4 vertexColor				: TEXCOORD9;

    float4 positionCS				: SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

Varyings Vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 vertexOffset = 0;
    #if defined(_IVD_ON)
    output.maskUV = TRANSFORM_TEX(input.texcoord, _IVD_Mask);
    half4 ivd_maskT2d = SAMPLE_TEXTURE2D_LOD(_IVD_Mask, sampler_IVD_Mask, output.maskUV, 0);
    
    float2 noiseUV = TRANSFORM_TEX(input.texcoord, _IVD_Noise);
    half4 ivd_noiseT2d = SAMPLE_TEXTURE2D_LOD(_IVD_Noise, sampler_IVD_Noise, noiseUV, 0);
    
    // 有遮罩的地方按遮罩来，没有的直接靠噪声扰1下
    half4 ivd_maskValue = ( ivd_maskT2d.r * _IVD_MaskIntensity ).xxxx;
    vertexOffset = ( (ivd_noiseT2d - ivd_maskValue) * float4(normalize(input.normalOS), 0) * _IVD_VertexNoiseIntensity * 0.01 ).rgb;
    #endif
    
    // 抬高修正顶点位置
    float3 vertex = input.positionOS.xyz + vertexOffset;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(vertex);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.uv.zw = TRANSFORM_TEX(input.texcoord, _BumpMap);

    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

    #if defined(_OPAQUE_BLEND)
    output.positionSS = ComputeScreenPos(vertexInput.positionCS);
    #endif

    output.positionWS = vertexInput.positionWS;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);
    #endif

    output.positionCS = vertexInput.positionCS;

    output.vertexColor = input.color;
    output.vertexColor.w = input.staticLightmapUV.x; // 强制使用uv1的trick

    return output;
}

void FragMeta(Varyings input
    , out half4 outColor : SV_Target0
    )
{
    UNITY_SETUP_INSTANCE_ID(input);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, input.maskUV, surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.Emission = surfaceData.emission;
    outColor = UnityMetaFragment(metaInput);
}

#endif // __INTERACTIVE_SNOW_META_PASS__
