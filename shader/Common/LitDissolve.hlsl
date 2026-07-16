#ifndef LIT_DISSOLVE
#define LIT_DISSOLVE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

TEMPLATE_2_HALF(TransformTangentToWorld_Half, dirTS, tangentToWorld, return mul(dirTS, tangentToWorld))

// local space direction dissolve
float DissolvePosOffset(float3 position, float3 dissolveDir)
{
    float posOffset = dot(normalize(dissolveDir), position);
    return posOffset;
}

// world space direction dissolve
float DissolveWorldPosOffset(float3 positionWS, float3 dissolveDir)
{
    float3 rootPos = float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
    float3 pos = positionWS - rootPos;
    float posOffset = dot(normalize(dissolveDir), pos);
    return posOffset;
}

void DissolveFinalColor(inout float4 finalColor, float2 uv, float3 worldFactor)
{
    float2 dissolveUV = uv * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
    half4 dissolveTex = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV.xy);
    half refDissolve = dot(dissolveTex, _DissolveTexChannel);

    half cutoff = lerp(-0.15, 1.01, _DissolveCutoff);

    half dissolve = 0;
    #if _RANDOM_DISSOLVE_ON
    dissolve = refDissolve - cutoff;
    #elif _DIRECTION_DISSOLVE_ON
    dissolve = refDissolve - cutoff + worldFactor;
    #endif

    dissolve = saturate(smoothstep(_DissolveFadingMin, _DissolveFadingMax, dissolve));

    clip(dissolve - 0.01);

    // _DissolveEdgeOn temp all open it.
    // if(1)
    {
        float edge = saturate(smoothstep(_EdgeWidth - 0.1, _EdgeWidth + 0.1, dissolve));
        float4 edgeColor = lerp(_EdgeColor1, _EdgeColor2, edge);
        edge = saturate(smoothstep(0, .6, 1 - dissolve));
        finalColor.xyz = lerp(finalColor.xyz, (finalColor.xyz * 0.5 + edgeColor.xyz) * 1.5, edge);
    }
}

#if defined( _DIRECTION_DISSOLVE_ON ) || defined( _RANDOM_DISSOLVE_ON )
#define DISSOLVE_FACTOR(ID)  float directionFactor : TEXCOORD##ID;
#define DISSOLVE_TRANSFER_FACTOR(v2f, position, dissolveDir)  v2f.directionFactor = DissolvePosOffset(position, dissolveDir);
#define DISSOLVE_APPLY(finalColor, uv, directionFactor)  DissolveFinalColor(finalColor, uv, directionFactor);
#else
#define DISSOLVE_FACTOR(ID)
#define DISSOLVE_TRANSFER_FACTOR(v2f, position, dissolveDir)
#define DISSOLVE_APPLY(finalColor, uv, directionFactor)
#endif

#endif // LIT_DISSOLVE
