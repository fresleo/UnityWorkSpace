#ifndef SSR_BLENDS
#define SSR_BLENDS

TEXTURE2D_X(_MainTex);

TEXTURE2D_X_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

float4 _SSRSettings;
#define THICKNESS _SSRSettings.x

float _MinimumBlur;
float _MinimumThickness;
#define MINIMUM_THICKNESS _MinimumThickness

float4 _MainTex_TexelSize;


struct AttributesFS
{
    float4 positionHCS : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsSSR
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsSSR VertSSR(AttributesFS input)
{
    VaryingsSSR output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS = float4(input.positionHCS.xyz, 1.0);

    #if UNITY_UV_STARTS_AT_TOP
    output.positionCS.y *= -1;
    #endif

    output.uv = input.uv;
    return output;
}

half4 FragCopyDepth(VaryingsSSR i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    i.uv = UnityStereoTransformScreenSpaceTex(i.uv);
    float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, i.uv.xy).r;
    depth = LinearEyeDepth(depth, _ZBufferParams);
    #if SSR_BACK_FACES
    float backDepth = SAMPLE_TEXTURE2D_X(_DownscaledBackDepthRT, sampler_PointClamp, i.uv.xy).r;
    backDepth = LinearEyeDepth(backDepth, _ZBufferParams);
    backDepth = clamp(backDepth, depth + MINIMUM_THICKNESS, depth + THICKNESS);
    return half4(depth, backDepth, 0, 1.0);
    #else
    return half4(depth.xxx, 1.0);
    #endif
}

//Point Copy限制最小值为0，阻止NaN扩散
half4 FragCopyExact(VaryingsSSR i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half4 pixel = SAMPLE_TEXTURE2D_X(_MainTex, sampler_PointClamp, i.uv);
    pixel = max(pixel, 0.0);
    return pixel;
}

TEXTURE2D(_RayCastRT);

half4 FragRayCast(VaryingsSSR i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half4 pixel = SAMPLE_TEXTURE2D_X(_RayCastRT, sampler_PointClamp, i.uv);
    pixel = max(pixel, 0.0);
    return pixel;
}

#endif
