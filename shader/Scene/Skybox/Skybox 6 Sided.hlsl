#ifndef __XKT_SKYBOX_6_SIDED__
#define __XKT_SKYBOX_6_SIDED__

// -------------------------------------
// Pipeline keywords
#pragma multi_compile _ _HEIGHT_FOG
#pragma shader_feature _RECORDING_QUALITY

#include "UnityCG.cginc"

// 用来替代 urp 中的同名方法，以兼容内置 cg 语言的写法
float3 GetWorldSpaceViewDir(float3 positionWS)
{
    return UnityWorldSpaceViewDir(positionWS);
}

#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

half4 _Tint;
half _Exposure;
float _Rotation;

float3 RotateAroundYInDegrees(float3 vertex, float degrees)
{
    float alpha = degrees * UNITY_PI / 180.0;
    float sina, cosa;
    sincos(alpha, sina, cosa);
    float2x2 m = float2x2(cosa, -sina, sina, cosa);
    return float3(mul(m, vertex.xz), vertex.y).xzy;
}

struct appdata_t
{
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;

    float3 positionWS : TEXCOORD1;
    UBPA_FOG_COORDS(2)

    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata_t v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
    
    o.vertex = UnityObjectToClipPos(rotated);
    #if defined(SHADER_API_D3D11)
    o.vertex.y = -o.vertex.y;
    #endif
    
    o.texcoord = v.texcoord;
    o.positionWS = mul(unity_ObjectToWorld, rotated).xyz;

    UBPA_TRANSFER_FOG(o, o.positionWS);

    return o;
}

half4 skybox_frag(v2f i, sampler2D smp, half4 smpDecode)
{
    half4 tex = tex2D(smp, i.texcoord);
    half3 c = DecodeHDR(tex, smpDecode);
    c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
    c *= _Exposure;

    UBPA_APPLY_FOG(i, c);

    return half4(c, 1);
}

#endif
