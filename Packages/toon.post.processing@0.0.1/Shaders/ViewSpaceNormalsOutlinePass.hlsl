#ifndef __VIEW_SPACE_NORMALS_OUTLINE_PASS__
#define __VIEW_SPACE_NORMALS_OUTLINE_PASS__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/PostProcessing/XKnightForwardBuffers.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/MathFuncs.hlsl"

half4 FragCombine(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv_raw = UnityStereoTransformScreenSpaceTex(input.texcoord);

    half4 edgeCol = SAMPLE_TEXTURE2D_X(_OutlineTexture, sampler_OutlineTexture, uv_raw);
    //return edgeCol; // 临时用来检查描边结果的
    
    half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv_raw);
    
    half4 outlineColor = _OutlineColor;

    half4 finalCol = 1;
    finalCol.rgb = lerp(col.rgb, outlineColor.rgb, edgeCol.r);
    finalCol.a = outlineColor.a * edgeCol.a;

    return finalCol;
}

half4 FragDiffusion(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    float2 uv_raw = UnityStereoTransformScreenSpaceTex(input.texcoord);
    float2 uv_offset = float2(rcp(_ScreenParams.x), rcp(_ScreenParams.y));

    half4 blit_raw = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv_raw);
    half4 blit_n = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv_raw + uv_offset * float2(0, 1));
    half4 blit_e = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv_raw + uv_offset * float2(1, 0));
    half4 blit_s = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv_raw + uv_offset * float2(0, -1));
    half4 blit_w = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv_raw + uv_offset * float2(-1, 0));

    float avg_r = blit_raw.r + blit_n.r + blit_e.r + blit_s.r + blit_w.r;
    float col_r = smoothstep(0.0, 1.0f, avg_r / 4.0f);

    float avg_a = blit_raw.a + blit_n.a + blit_e.a + blit_s.a + blit_w.a;
    float col_a = smoothstep(0.0, 1.0f, avg_a / 4.0f);

    return half4(col_r, 0, 0, col_a);
}

struct VaryingsOutline
{
    float4 positionCS : SV_POSITION;
    float2 uv   : TEXCOORD0;
    
    float3 positionVS : TEXCOORD1;
    
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsOutline VertOutline(Attributes input)
{
    VaryingsOutline output = (VaryingsOutline)0;
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

    VertexPositionInputs vertexInput = GetVertexPositionInputs(pos);
    output.positionVS = vertexInput.positionVS;
    
    return output;
}

half4 FragOutline(VaryingsOutline input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv_raw = UnityStereoTransformScreenSpaceTex(input.uv);

    // 深度排除
    float cameraDepth01 = Sample_CameraDepth_01(uv_raw);
    #if !defined( _MRT_BUFFER )
    float maskDepth01 = Sample_NormalsVS_Depth_01(uv_raw);
    UNITY_BRANCH if (cameraDepth01 < maskDepth01 * DEPTH_COMPARE_FACTOR) return 0;
    #endif

    // 周边uv
    float outlineScale = OUTLINE_SCALE;
    float ou = rcp(_ScreenParams.x) * outlineScale;
    float ov = rcp(_ScreenParams.y) * outlineScale;
    float2 offsetUV = float2(ou, ov);

    float2 uv_0 = uv_raw + offsetUV * float2(1, 1);
    float2 uv_1 = uv_raw + offsetUV * float2(-1, -1);
    float2 uv_2 = uv_raw + offsetUV * float2(-1, 1);
    float2 uv_3 = uv_raw + offsetUV * float2(1, -1);

    // 视空间法线
    float4 normalsVS_raw = Sample_NormalsVS(uv_raw);
    float4 normalsVS_0 = Sample_NormalsVS(uv_0);
    float4 normalsVS_1 = Sample_NormalsVS(uv_1);
    float4 normalsVS_2 = Sample_NormalsVS(uv_2);
    float4 normalsVS_3 = Sample_NormalsVS(uv_3);

    // Roberts Cross 方法
    float3 dn_0 = normalsVS_0.rgb - normalsVS_1.rgb;
    float3 dn_1 = normalsVS_2.rgb - normalsVS_3.rgb;
    float3 normalsVSDiff = dot(dn_0, dn_0) + dot(dn_1, dn_1);
    normalsVSDiff = sqrt(normalsVSDiff);
    float normalThreshold = NORMAL_THRESHOLD;
    float edgeNormalsVS = step(normalThreshold, normalsVSDiff);
    
    // 深度
    float depth_raw = Sample_NormalsVS_Depth(uv_raw);
    float depth_0 = Sample_NormalsVS_Depth(uv_0);
    float depth_1 = Sample_NormalsVS_Depth(uv_1);
    float depth_2 = Sample_NormalsVS_Depth(uv_2);
    float depth_3 = Sample_NormalsVS_Depth(uv_3);

    // Roberts Cross 方法
    float dd_0 = depth_0 - depth_1;
    float dd_1 = depth_2 - depth_3;
    float depthDiff = (dd_0 * dd_0) + (dd_1 * dd_1);
    depthDiff = sqrt(depthDiff);
    float edgeDepth = depthDiff * DEPTH_DIFF_MULTIPLIER;

    // 附加条件
    float4 remapNormalsVS = 0;
    Remap(normalsVS_raw, float2(0, 1), float2(-1, 1), remapNormalsVS);
    float3 remapPositionVS = 0;
    Remap(input.positionVS, float2(0, 1), float2(-1, 1), remapPositionVS);

    // 菲涅尔
    float ndv = 1.0 - dot(remapNormalsVS.rgb, remapPositionVS.rgb);
    
    float depthNormalThreshold = smoothstep(STEEP_ANGLE_THRESHOLD, 2, ndv) * STEEP_ANGLE_MULTIPLIER + 1;
    float depthThreshold = depth_raw * DEPTH_THRESHOLD * depthNormalThreshold;
    edgeDepth = step(depthThreshold, edgeDepth);
    
    // 合并结果
    float edgeT = max(edgeDepth, edgeNormalsVS);

    // 深度衰减
    float depthFade = max(0, (OUTLINE_DISTANCE_FADE - cameraDepth01) / OUTLINE_DISTANCE_FADE);

    // 描边
    float outline = edgeT * depthFade;

    half4 outlineColor = 1;
    #ifdef _DIRECT_BLEND
    outlineColor = _OutlineColor;
    #endif
    
    half4 finalCol = outline * outlineColor;
    return finalCol;
}

#endif // __VIEW_SPACE_NORMALS_OUTLINE_PASS__
