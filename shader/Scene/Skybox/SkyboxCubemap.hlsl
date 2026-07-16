#ifndef __XKT_SKYBOX_CUBEMAP__
#define __XKT_SKYBOX_CUBEMAP__

// -------------------------------------
// Pipeline keywords
#pragma multi_compile _ _HEIGHT_FOG
#pragma shader_feature _RECORDING_QUALITY

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

TEXTURECUBE(_Tex);
SAMPLER(sampler_Tex);

CBUFFER_START(UnityPerMaterial)
    float4 _Tex_TexelSize;
    half4 _Tex_HDR;
    half4 _Tint;
    half _Exposure;
    float _Rotation;
CBUFFER_END

float3 RotateAroundYInDegrees(float3 vertex, float degrees)
{
    float alpha = degrees * PI / 180.0;
    float sina, cosa;
    sincos(alpha, sina, cosa);
    float2x2 m = float2x2(cosa, -sina, sina, cosa);
    return float3(mul(m, vertex.xz), vertex.y).xzy;
}

struct appdata_t
{
    float4 vertex : POSITION;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float3 texcoord : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    UBPA_FOG_COORDS(2)
};

v2f vert(appdata_t v)
{
    v2f o = (v2f)0;

    float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(rotated);
    
    o.vertex = vertexInput.positionCS;
    #if defined(SHADER_API_D3D11)
    o.vertex.y = -o.vertex.y;
    #endif
    
    o.texcoord = v.vertex.xyz;
    o.positionWS = vertexInput.positionWS;

    UBPA_TRANSFER_FOG(o, vertexInput.positionWS);

    return o;
}

float4 BlendTwoCubeTextures(float3 reflection, half mip)
{
    float4 textureFrom;
    float4 textureTo;
    float blendFactor;

    textureFrom = _MLS_Sky_Cubemap_Blend_From.SampleLevel(sampler_Tex, reflection, mip);
    textureTo = _MLS_Sky_Cubemap_Blend_To.SampleLevel(sampler_Tex, reflection, mip);
    blendFactor = _MLS_Sky_Cubemap_Blend_Factor;

    return lerp(textureFrom, textureTo, blendFactor);
}

float4 frag(v2f i) : SV_Target
{
    half4 tex;
    half4 resultTint;
    half resultExposure;

    if (_MLS_ENABLE_SKY_CUBEMAPS_BLENDING)
    {
        tex = BlendTwoCubeTextures(i.texcoord, 0);
        resultTint = BlendSkyTint();
        resultExposure = BlendSkyExposure();
    }
    else
    {
        tex = SAMPLE_TEXTURECUBE(_Tex, sampler_Tex, i.texcoord);
        resultTint = _Tint;
        resultExposure = _Exposure;
    }

    float3 c = DecodeHDREnvironment(tex, _Tex_HDR);
    c = c * resultTint * resultExposure;

    UBPA_APPLY_FOG(i, c);

    return float4(c, 1);
}

#endif
