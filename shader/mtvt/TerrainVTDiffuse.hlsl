#ifndef __XKNIGHT_TERRAIN_VT_DIFFUSE__
#define __XKNIGHT_TERRAIN_VT_DIFFUSE__

#include ".././ShaderLibrary/Lighting.hlsl"

CBUFFER_START(TerrainBakeMaterial)
    float4 _Control_ST, _Control_TexelSize;
    
    half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;

    //烘焙时的偏移
    float4 _BakeScaleOffset;
CBUFFER_END

TEXTURE2D(_Control); SAMPLER(sampler_Control);
TEXTURE2D(_Splat0); SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);

struct Attributes
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float4 uv : TEXCOORD0;
    
    float4 uvSplat01 : TEXCOORD1; // xy: splat0, zw: splat1
    float4 uvSplat23 : TEXCOORD2; // xy: splat2, zw: splat3
};

//顶点阶段
Varyings vert(Attributes v)
{
    Varyings o = (Varyings)0;
    o.uv.zw = v.texcoord;
    v.texcoord = v.texcoord.xy * _BakeScaleOffset.xy + _BakeScaleOffset.zw;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
    o.vertex = vertexInput.positionCS;

    o.uv.xy = v.texcoord;
    o.uvSplat01.xy = TRANSFORM_TEX(v.texcoord, _Splat0);
    o.uvSplat01.zw = TRANSFORM_TEX(v.texcoord, _Splat1);
    o.uvSplat23.xy = TRANSFORM_TEX(v.texcoord, _Splat2);
    o.uvSplat23.zw = TRANSFORM_TEX(v.texcoord, _Splat3);

    return o;
}

half4 SplatmapMix(half4 splatControl, half4 uvSplat01, half4 uvSplat23)
{
    half4 lay0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplat01.xy);
    half4 lay1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvSplat01.zw);
    half4 lay2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvSplat23.xy);
    half4 lay3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvSplat23.zw);

    half4 mixSplat = lay0 * splatControl.r + lay1 * splatControl.g + lay2 * splatControl.b + lay3 * splatControl.a;
    half smoothness = 1.0 - mixSplat.a;
    return half4(mixSplat.rgb, smoothness);
}

//片元阶段
half4 frag(Varyings IN) : SV_Target
{
    float2 splatUV = (IN.uv.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);
    
    half4 mixedDiffuse = SplatmapMix(splatControl, IN.uvSplat01, IN.uvSplat23);
    return mixedDiffuse;
}

#endif //__XKNIGHT_TERRAIN_VT_DIFFUSE__