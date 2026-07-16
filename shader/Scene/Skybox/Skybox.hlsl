#ifndef __XKT_SKYBOX__
#define __XKT_SKYBOX__

// -------------------------------------
// Pipeline keywords
#pragma multi_compile _ _HEIGHT_FOG
#pragma shader_feature _RECORDING_QUALITY

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_TexelSize;
    half4 _MainTex_HDR;
    half4 _Tint;
    half _Exposure;
    float _Rotation;
CBUFFER_END

inline float2 ToRadialCoords(float3 coords)
{
    float3 normalizedCoords = normalize(coords);
    float latitude = acos(normalizedCoords.y);
    float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
    float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / PI, 1.0 / PI);
    return float2(0.5, 1.0) - sphereCoords;
}

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

float4 frag(v2f i) : SV_Target
{
    float2 tc = ToRadialCoords(i.texcoord);
    float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tc);
    float3 c = DecodeHDREnvironment(tex, _MainTex_HDR);
    c = c * _Tint.rgb * _Exposure;

    UBPA_APPLY_FOG(i, c);

    return float4(c, 1);
}

#endif
