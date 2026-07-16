#ifndef TOONPBR_DISSOLVE
#define TOONPBR_DISSOLVE

// 方向溶解
#if _DIRECTION_DISSOLVE_ON
#define TOONPBR_DISSOLVE_FACTOR(ID) \
    float directionFactor : TEXCOORD##ID;

#define TOONPBR_DISSOLVE_TRANSFER_FACTOR(v2f, positionWS) \
    v2f.directionFactor = CalculatePosOffset(positionWS.xyz);

#define TOONPBR_DISSOLVE_APPLY(finalColor, uv, v2f) \
    CalculateFinalColor(finalColor, uv, v2f.directionFactor);

// 随机溶解 / 遮罩溶解
#elif _RANDOM_DISSOLVE_ON || _MASK_DISSOLVE_ON
#define TOONPBR_DISSOLVE_FACTOR(ID)
#define TOONPBR_DISSOLVE_TRANSFER_FACTOR(v2f, positionWS)

#define TOONPBR_DISSOLVE_APPLY(finalColor, uv, v2f) \
    CalculateFinalColor(finalColor, uv, 0);

#else
#define TOONPBR_DISSOLVE_FACTOR(ID)
#define TOONPBR_DISSOLVE_TRANSFER_FACTOR(v2f, positionWS)
#define TOONPBR_DISSOLVE_APPLY(finalColor, uv, v2f)

#endif

float CalculatePosOffset(float3 positionWS)
{
    float3 objectOriginWS = float3(
        unity_ObjectToWorld._m03,
        unity_ObjectToWorld._m13,
        unity_ObjectToWorld._m23
    );
    float3 vertexOffset = positionWS - objectOriginWS;
    float posOffset = dot(normalize(_DissolveDir.xyz), vertexOffset);
    return posOffset;
}

void CalculateFinalColor(inout float4 finalColor, float2 uv, float directionFactor)
{
    half cutoff = _DissolveCutoff * _DissolveCutoffMultiplier;

    // 溶解边缘遮罩（噪声纹理）
    half4 dissolveEdgeCol = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, uv * _DissolveTex_ST.xy + _DissolveTex_ST.zw);
    half dissolveEdge = dot(dissolveEdgeCol, _DissolveTex_Channel);

    half dissolve;

    // 方向溶解
    #if defined(_DIRECTION_DISSOLVE_ON)
    dissolve = dissolveEdge - cutoff + directionFactor;

    // 遮罩溶解
    #elif defined(_MASK_DISSOLVE_ON)
    half4 dissolveMaskCol = SAMPLE_TEXTURE2D(_DissolveMaskTex, sampler_DissolveMaskTex, uv);
    half dissolveMask = dot(dissolveMaskCol, _DissolveMaskTex_Channel);
    dissolveMask = abs(_DissolveMaskReverse - dissolveMask);
    dissolve = dissolveMask + dissolveEdge - cutoff;

    // 随机溶解
    #else
    dissolve = dissolveEdge - cutoff;
    
    #endif
    
    dissolve = smoothstep(_DissolveFadingMin, _DissolveFadingMax, dissolve);
    clip(dissolve - 0.01);

    // 当前不做判断，永远都有溶解边缘
    // if(_DissolveEdgeOn > 0)
    {
        float edgeColorBlend = smoothstep(max(0, _EdgeWidth - 0.1), _EdgeWidth + 0.1, dissolve);
        float4 edgeColor = lerp(_EdgeColor1, _EdgeColor2, edgeColorBlend);
        
        float edgeMask = smoothstep(0, 0.6, 1 - dissolve);
        finalColor.xyz = lerp(finalColor.xyz, (finalColor.xyz * 0.5 + edgeColor.xyz) * 1.5, edgeMask);
    }
}

#endif // TOONPBR_DISSOLVE
