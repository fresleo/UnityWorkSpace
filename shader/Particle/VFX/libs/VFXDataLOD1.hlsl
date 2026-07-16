#if !defined(VFX_DATA_LOD1_HLSL)
#define VFX_DATA_LOD1_HLSL

#include "TangentLib.hlsl"

/*
 *  低配版的顶点流和高配产生了差异，所以复制出来一份
 */


/*
 *  custom data 1  —— xy: 用于主纹理offset z: 用于溶解，即Clip w: 用于主纹理强度或者控制使用data2.xy来改溶解图offset
 *  custom data 2  —— xy: 用于主纹理遮罩的offset z:null w:用于Fresnel透明度
 */

struct appdata
{
    float4 vertex  : POSITION;
    float3 normal  : NORMAL;
    float4 tangent : TANGENT;
    float4 color   : COLOR;
    float4 uv      : TEXCOORD0;          // mainuv, data1.xy
    half4 uv1      : TEXCOORD1;          // data1.zw, data2.xy
    half4 uv2      : TEXCOORD2;          // data2.zw

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex              : SV_POSITION;
    float4 uv_fresnel          : TEXCOORD0;
    float4 customData1         : TEXCOORD1;
    float4 customData2         : TEXCOORD2;
    float4 grabPos             : TEXCOORD3;
    float4 color               : TEXCOORD4;
    float4 worldPos            : TEXCOORD5;
#ifndef _MAINTEXUSESCREENUV_ON
    float4 mainUV              : TEXCOORD6;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif //VFX_DATA_LOD1_HLSL