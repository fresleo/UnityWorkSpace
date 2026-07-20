#ifndef __PRE_OBJECT_ID_OUTLINE_PASS__
#define __PRE_OBJECT_ID_OUTLINE_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/PostProcessing/XKnightForwardBuffers.hlsl"

inline float Sample_ObjectId(float2 uv)
{
    return Sample_SceneSpaceOutlineMask(uv);
}

half4 FragOutline(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 UV = UnityStereoTransformScreenSpaceTex(input.texcoord);

    // 深度排除
    float cameraDepth = Sample_CameraDepth_01(UV);
    #if !defined( _MRT_BUFFER )
    float maskDepth = Sample_SceneSpaceOutlineMask_Depth_01(UV);
    UNITY_BRANCH if (cameraDepth < maskDepth * DEPTH_COMPARE_FACTOR) return 0;
    #endif

    float outlineWidth = OUTLINE_WIDTH; // 通过偏移 UV 来达到增加宽度的目的，但当数值太大时，会出现断线的情况
    float3 uvInc = float3(_BlitTexture_TexelSize.x * outlineWidth, _BlitTexture_TexelSize.y * outlineWidth, 0);
    float outline = 0;

    // 采样对象id
    float objS = Sample_ObjectId(UV - uvInc.zy);
    float objN = Sample_ObjectId(UV + uvInc.zy);
    float objW = Sample_ObjectId(UV - uvInc.xz);
    float objE = Sample_ObjectId(UV + uvInc.xz);

    float maxObj = max(max(objS, objN), max(objW, objE));
    float minObj = min(min(objS, objN), min(objW, objE));

    /*
    // 增加更多的采样点
    float objS = Sample_ObjectId(UV - uvInc.zy);
    float objN = Sample_ObjectId(UV + uvInc.zy);
    float objW = Sample_ObjectId(UV - uvInc.xz);
    float objE = Sample_ObjectId(UV + uvInc.xz);
    float objSW = Sample_ObjectId(UV - uvInc.xy);
    float objSE = Sample_ObjectId(UV + uvInc.xy);
    float objNW = Sample_ObjectId(UV - uvInc.yx);
    float objNE = Sample_ObjectId(UV + uvInc.yx);

    float maxObj = max(max(max(objS, objN), max(objW, objE)), max(max(objSW, objSE), max(objNW, objNE)));
    float minObj = min(min(min(objS, objN), min(objW, objE)), min(min(objSW, objSE), min(objNW, objNE)));
    */

    float objDiff = maxObj - minObj;
    outline = objDiff > 0.01;

    // 检查对象是否足够大，并增强描边的效果
    #if OUTLINE_MIN_SEPARATION_ON
    uvInc *= OUTLINE_MIN_SEPARATION;
    float objS2 = Sample_ObjectId(UV - uvInc.zy);
    float objN2 = Sample_ObjectId(UV + uvInc.zy);
    if (abs(objN2 - objN) > 0.01 && abs(objS2 - objS) > 0.01) outline *= 0.25 / OUTLINE_INTENSITY_MULTIPLIER;
    float objW2 = Sample_ObjectId(UV - uvInc.xz);
    float objE2 = Sample_ObjectId(UV + uvInc.xz);
    if (abs(objE2 - objE) > 0.01 && abs(objW2 - objW) > 0.01) outline *= 0.25 / OUTLINE_INTENSITY_MULTIPLIER;
    #endif // OUTLINE_MIN_SEPARATION

    float depthFade = max(0, (OUTLINE_DISTANCE_FADE - cameraDepth) / OUTLINE_DISTANCE_FADE);
    outline *= depthFade;

    return outline;
}

// 决定是横向滤波，还是纵向滤波
#if defined( OUTLINE_BLUR_HORIZ )
    #define OUTLINE_VERTEX_OUTPUT_GAUSSIAN_UV(o) \
        float2 inc = float2(_BlitTexture_TexelSize.x * 1.3846153846 * _BlurScale, 0); \
        o.uv1 = o.uv - inc; \
        o.uv2 = o.uv + inc; \
        float2 inc2 = float2(_BlitTexture_TexelSize.x * 3.2307692308 * _BlurScale, 0); \
        o.uv3 = o.uv - inc2; \
        o.uv4 = o.uv + inc2;
#else
    #define OUTLINE_VERTEX_OUTPUT_GAUSSIAN_UV(o) \
        float2 inc = float2(0, _BlitTexture_TexelSize.y * 1.3846153846 * _BlurScale); \
        o.uv1 = o.uv - inc; \
        o.uv2 = o.uv + inc; \
        float2 inc2 = float2(0, _BlitTexture_TexelSize.y * 3.2307692308 * _BlurScale); \
        o.uv3 = o.uv - inc2; \
        o.uv4 = o.uv + inc2;
#endif // BLUR_HORIZ

struct VaryingsBlur
{
    float4 positionCS : SV_POSITION;
    float2 uv: TEXCOORD0;

    float2 uv1 : TEXCOORD1;
    float2 uv2: TEXCOORD2;
    float2 uv3: TEXCOORD3;
    float2 uv4: TEXCOORD4;

    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsBlur VertBlur(Attributes input)
{
    VaryingsBlur output = (VaryingsBlur)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if SHADER_API_GLES
    float4 pos = input.positionOS;
    float2 uv  = input.uv;
    #else
    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);
    #endif

    output.positionCS = pos;
    output.uv = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;

    OUTLINE_VERTEX_OUTPUT_GAUSSIAN_UV(output);

    return output;
}

half4 FragBlur(VaryingsBlur input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    input.uv = UnityStereoTransformScreenSpaceTex(input.uv);

    half4 pixel =
        SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv) * 0.2270270270
        + (SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv1) + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv2)) * 0.3162162162
        + (SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv3) + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv4)) * 0.0702702703;
    return pixel;
}

half4 FragCopy(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 UV = UnityStereoTransformScreenSpaceTex(input.texcoord);

    half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV);
    half outline = col.r;

    half3 outlineFlagColor = half3(1, 0, 0);

    half4 color = half4(outlineFlagColor, outline);
    color.rgb *= OUTLINE_INTENSITY_MULTIPLIER;
    color.a = saturate(color.a);

    return color;
}

half4 FragCombine(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 UV = UnityStereoTransformScreenSpaceTex(input.texcoord);

    half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV);
    half4 outlineCol = SAMPLE_TEXTURE2D_X(_OutlineTexture, sampler_OutlineTexture, UV);

    half4 outlineColor = _OutlineColor;

    half4 finalCol = 1;
    finalCol.rgb = lerp(col.rgb, outlineColor.rgb, outlineCol.r);
    finalCol.a = outlineColor.a;

    return finalCol;
}

half4 FragDiffusion(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    float2 UV = UnityStereoTransformScreenSpaceTex(input.texcoord);

    float br = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV).r;
    
    float n = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV + _BlitTexture_TexelSize * float2(0, 1)).r;
    float e = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV + _BlitTexture_TexelSize * float2(1, 0)).r;
    float s = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV + _BlitTexture_TexelSize * float2(0, -1)).r;
    float w = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV + _BlitTexture_TexelSize * float2(-1, 0)).r;

    float avg = br + n + e + s + w;

    return smoothstep(0.0, 1.0f, avg / 4.0f);
}

#endif // __PRE_OBJECT_ID_OUTLINE_PASS__
