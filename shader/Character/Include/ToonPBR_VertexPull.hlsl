#ifndef TOONPBR_VERTEXPULL
#define TOONPBR_VERTEXPULL

// 特效需求：顶点拉扯效果
// 模型空间
half3 VertexOffset(half3 normalOS, half3 direction, half intensity, half2 uv, TEXTURE2D_PARAM(noiseTexture, samplerNoise), half4 noiseST)
{
#ifndef _VERTEX_PULL_ON
    return half3(0,0,0);
#endif    
    
    uv = uv * noiseST.xy + noiseST.zw * _Time.xx;
    half noise = SAMPLE_TEXTURE2D_LOD(noiseTexture, samplerNoise, uv, 0).r;
    half NdotD = saturate(dot(normalOS, direction));

    return direction * NdotD * intensity * noise;
}

#endif