#if !defined(VFX_DATA_HLSL)
#define VFX_DATA_HLSL

#include "TangentLib.hlsl"
#include "Packages/com.xknight.sky/Shaders/ShaderLibrary/ExponentialHeightFog.hlsl"

/*
 *   严格按照以下顶点流信息，才能适配上特效模板
 *   
 *   uv1.w 和 uv2.xy 是预留出来以后扩展模板用的，建议保留
 *   
 */

struct appdata
{
    float4 vertex  : POSITION;
    float3 normal  : NORMAL;
    float4 tangent : TANGENT;
    float4 color   : COLOR;
    float4 uv      : TEXCOORD0;        // xy:main uv, zw : particle's customData(mainTex scroll)
    float4 uv1     : TEXCOORD1;        // particle's customData(x:dissolve, y:dissolveEdgeWidth, z : _VertexWaveAttenMask_UseCustomeData2_X), w : null
    float4 uv2     : TEXCOORD2;        // x null, y : frenel alpha | 方向衰减的w, zw : center.xy
    float4 uv3     : TEXCOORD3;        // x  : center.w   yzw: size

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex              : SV_POSITION;
    float4 uv                  : TEXCOORD0;
    float4 fresnel_customDataZ : TEXCOORD1; // x:fresnel,y:customData.x,z:_VertexWaveAttenMask_UseCustomeData2_X, w:frenel alpha
    float4 grabPos             : TEXCOORD2;
#ifdef _LIGHTON_ON
    TANGENT_SPACE_DECLARE(3,4,5);
#endif
#ifdef _DECALEFFECTON_ON
    float3 center              : TEXCOORD6;
    float3 size                : TEXCOORD7;
#endif
    float4 color               : TEXCOORD9;

    float3 positionWS          : TEXCOORD10;
    UBPA_FOG_COORDS(11)
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif //VFX_DATA_HLSL