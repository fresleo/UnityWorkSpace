#ifndef ___XKTVFXICEINPUT___
#define ___XKTVFXICEINPUT___

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "../../ShaderLibrary/Lighting.hlsl"


CBUFFER_START(UnityPerMaterial)


half4 _Color;
float _ParallaxDepth;

// float _Normal;
half _NormalIntensity;

// float _RenderMode;
half _DiffuseOffset;
half _DiffuseFeather;
half _DiffuseIntensity;
half _Metallic;
half _Smoothness;
float _MatcapBlend;
half4 _MatcapColor;

// float _Emission;
half4 _EmissionColor;

// float _OpaqueAlphaBlend;
half _OpaqueBlendDistance;

// float _CubeMapReflection;
half4 _CubeColor;
half _CubeTexMipLevel;

// float _Fresnel;
half4 _FresnelColor;
half _FresnelFeather;
half _FresnelOffset;
half _FresnelPower;
half _FresnelScale;
float _FresnelReverse;
half _FresnelDiffuseScale;

// float _Streamer;
half4 _StreamerColor;
float _StreamerMaskChannal;
// float _StreamerSpeedX;
// float _StreamerSpeedY;

float4 _MainTex_ST;
float4 _NormalTex_ST;
float4 _MetallicRoughnessMap_ST;
float4 _MatcapTex_ST;
float4 _EmissionTex_ST;
float4 _CubeTex_ST;
float4 _StreamerTex_ST;
float4 _StreamerMask_ST;

float4 _CubeTex_HDR;

CBUFFER_END


TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

#ifdef _NORMAL_ON
TEXTURE2D(_NormalTex);
SAMPLER(sampler_NormalTex);
#endif
#ifdef  _RENDERMODE_PBR
TEXTURE2D(_MetallicRoughnessMap);
SAMPLER(sampler_MetallicRoughnessMap);
#endif
#ifdef _RENDERMODE_MATCAP
TEXTURE2D(_MatcapTex);
SAMPLER(sampler_MatcapTex);
#endif
#ifdef _EMISSION_ON
TEXTURE2D(_EmissionTex);
SAMPLER(sampler_EmissionTex);
#endif
#ifdef _CUBEMAPREFLECTION_ON
TEXTURECUBE(_CubeTex);
SAMPLER(sampler_CubeTex);
#endif
#ifdef _STREAMER_ON
TEXTURE2D(_StreamerTex);
SAMPLER(sampler_StreamerTex);
TEXTURE2D(_StreamerMask);
SAMPLER(sampler_StreamerMask);
#endif




TEXTURE2D_X_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);






struct appdata
{
    float4 vertex : POSITION;
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float4 color : COLOR;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float4 uv : TEXCOORD0;
    float4 uv1 : TEXCOORD1;

    float3 normalWS : TEXCOORD2;
    float4 tangentWS : TEXCOORD3;
    float3 bitangentWS : TEXCOORD4;

    float3 vertexOS : TEXCOORD5;
    float3 vertexWS : TEXCOORD6;
    float4 vertexSS : TEXCOORD7;
    float3 viewDirTS : TEXCOORD8;

    float4 color : TEXCOORD9;
};

v2f vert (appdata v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.uv = v.uv0;
    o.uv1 = v.uv1;

    o.normalWS = TransformObjectToWorldNormal(v.normal);
    o.tangentWS = float4(TransformObjectToWorldNormal(v.tangent.xyz), v.tangent.w);
    o.bitangentWS = normalize(cross(o.normalWS, o.tangentWS.xyz) * o.tangentWS.w);

    o.vertexOS = v.vertex.xyz;
    o.vertexWS = TransformObjectToWorld(v.vertex.xyz);
    o.vertexSS = ComputeScreenPos(o.vertex);

    float3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos.xyz - o.vertexWS);
    o.viewDirTS = float3(dot(viewDirWS, o.tangentWS.xyz), dot(viewDirWS, o.bitangentWS), dot(viewDirWS, o.normalWS));

    o.color = v.color;

    return o;
}

// 获取法线
float3 GetNormal(v2f i)
{
    float3 normal = i.normalWS;

    #ifdef _NORMAL_ON
    {
        float3x3 tbnWS = float3x3(i.tangentWS.xyz, i.bitangentWS, i.normalWS);
        
        float4 normalTex = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.uv.xy * _NormalTex_ST.xy + _NormalTex_ST.zw);
        float3 normalTS = UnpackNormal(normalTex);
        normalTS.xy *= _NormalIntensity;
        normalTS = normalize(normalTS);
        normal = normalize(TransformTangentToWorld(normalTS, tbnWS));
    }
    #endif
    
    return normal;
}


BRDFData InitializeBRDFData(half4 color, half metallic, half smoothness)
{
    BRDFData brdf = (BRDFData)0;
    InitializeBRDFData(color.rgb, metallic, 0, smoothness, color.a, brdf);
    return brdf;
}


float3 GlobalIllumination_UE(BRDFData brdfData, float3 vertexWS, float3 normal, float3 viewDirWS, float2 uvScreen)
{
    half3 reflectVector = reflect(-viewDirWS, normal);

    half3 indirectDiffuse = SampleSH(normal);
    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, vertexWS, 
        brdfData.perceptualRoughness, 1.0h, uvScreen);
    
    half3 color = EnvironmentBRDF_UE(brdfData, indirectDiffuse, indirectSpecular, normal, viewDirWS);
    return color;
}

float3 LightingPhysicallyBased_XK(BRDFData brdfData, Light mainLight, float diffuse)
{
    half lightAttenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
    half3 radiance = mainLight.color * (lightAttenuation * diffuse);
    
    half3 brdf = brdfData.diffuse;

    return brdf * radiance;
}


#endif