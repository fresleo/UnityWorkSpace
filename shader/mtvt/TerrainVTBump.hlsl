#ifndef __XKNIGHT_TERRAIN_VT_BUMP__
#define __XKNIGHT_TERRAIN_VT_BUMP__

#include ".././ShaderLibrary/Lighting.hlsl"

CBUFFER_START(TerrainBakeMaterial)
    float4 _Control_ST, _Control_TexelSize;
    
    half4 _Normal0_ST, _Normal1_ST, _Normal2_ST, _Normal3_ST;
    half _NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3;
    
    //烘焙时的偏移
    float4 _BakeScaleOffset;
CBUFFER_END

TEXTURE2D(_Control); SAMPLER(sampler_Control);
TEXTURE2D(_Normal0); SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);

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
    v.texcoord = v.texcoord * _BakeScaleOffset.xy + _BakeScaleOffset.zw;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
    o.vertex = vertexInput.positionCS;

    o.uv.xy = v.texcoord;
    o.uvSplat01.xy = TRANSFORM_TEX(v.texcoord, _Normal0);
    o.uvSplat01.zw = TRANSFORM_TEX(v.texcoord, _Normal1);
    o.uvSplat23.xy = TRANSFORM_TEX(v.texcoord, _Normal2);
    o.uvSplat23.zw = TRANSFORM_TEX(v.texcoord, _Normal3);

    return o;
}

half4 SampleNormals(half4 splatControl, half4 uvSplat01, half4 uvSplat23)
{
    half4 normal0 = SAMPLE_TEXTURE2D(_Normal0, sampler_Normal0, uvSplat01.xy);
    half4 normal1 = SAMPLE_TEXTURE2D(_Normal1, sampler_Normal0, uvSplat01.zw);
    half4 normal2 = SAMPLE_TEXTURE2D(_Normal2, sampler_Normal0, uvSplat23.xy);
    half4 normal3 = SAMPLE_TEXTURE2D(_Normal3, sampler_Normal0, uvSplat23.zw);

    half3 realNormal0 = normal0.rgb * _NormalScale0;
    half3 realNormal1 = normal1.rgb * _NormalScale1;
    half3 realNormal2 = normal2.rgb * _NormalScale2;
    half3 realNormal3 = normal3.rgb * _NormalScale3;
    half3 mixNormal = realNormal0 * splatControl.r + realNormal1 * splatControl.g + realNormal2 * splatControl.b + realNormal3 * splatControl.a;
    
    half occlusion = normal0.b * splatControl.r + normal1.b * splatControl.g + normal2.b * splatControl.b + normal3.b * splatControl.a;
    half metallic = normal0.a * splatControl.r + normal1.a * splatControl.g + normal2.a * splatControl.b + normal3.a * splatControl.a;
    
    return half4(mixNormal.rg, occlusion, metallic);
}

//片元阶段
half4 frag(Varyings IN) : SV_Target
{
    float2 splatUV = (IN.uv.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);
    
    half4 mixedNormal = SampleNormals(splatControl, IN.uvSplat01, IN.uvSplat23);
    return mixedNormal;
}

#endif //__XKNIGHT_TERRAIN_VT_BUMP__