#ifndef __GRASS_INPUT__
#define __GRASS_INPUT__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Extend/Depth/XKnightDepthMask_Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Extend/TransparentByDither.hlsl"

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
struct MeshInstanceTrs
{
    float4 positionScale;
    float4 rotation;
};

#if defined(_MESH_INSTANCE_TEX_FETCH_ON)
TEXTURE2D(_MeshInstanceTexA);
SAMPLER(sampler_MeshInstanceTexA);
TEXTURE2D(_MeshInstanceTexB);
SAMPLER(sampler_MeshInstanceTexB);
// TexC：RGBAFloat 格式，存每个实例所属 chunk 的中心世界坐标（xyz），float32 精度消除大坐标误差。
// 着色器用：worldPos = localPos(TexA.xyz) + chunkCenter(TexC.xyz)
TEXTURE2D(_MeshInstanceTexC);
SAMPLER(sampler_MeshInstanceTexC);
float _MeshInstanceTexWidth;
float _MeshInstanceTexInvWidth;
float _MeshInstanceTexHeight;
TEXTURE2D(_MeshInstanceVisibleIndexTex);
SAMPLER(sampler_MeshInstanceVisibleIndexTex);
float _MeshInstanceVisibleIndexTexInvWidth;
float _MeshInstanceVisibleIndexTexHeight;

// 从 visibleIndexTexture 读取 packed uint（bits[31:30]=LOD, bits[29:0]=trsIndex）
uint LoadTextureFetchVisibleInstanceIndex(uint visibleInstanceIndex)
{
    if (_MeshInstanceVisibleIndexTexHeight <= 0.0)
    {
        return visibleInstanceIndex;
    }

    float texHeight = max(_MeshInstanceVisibleIndexTexHeight, 1.0);
    uint texWidth = (uint)max(round(1.0 / max(_MeshInstanceVisibleIndexTexInvWidth, 1e-6)), 1.0);
    uint x = visibleInstanceIndex % texWidth;
    uint y = visibleInstanceIndex / texWidth;
    float2 uv = float2(((float)x + 0.5) * _MeshInstanceVisibleIndexTexInvWidth, ((float)y + 0.5) / texHeight);
    float4 encoded = SAMPLE_TEXTURE2D_LOD(_MeshInstanceVisibleIndexTex, sampler_MeshInstanceVisibleIndexTex, uv, 0);
    uint4 bytes = (uint4)round(saturate(encoded) * 255.0);
    // 重建完整 uint，高 2 位携带 LOD 等级，低 30 位为 trsIndex
    return bytes.x | (bytes.y << 8) | (bytes.z << 16) | (bytes.w << 24);
}
#endif

#if defined(_MESH_INSTANCE_TEX_FETCH_ON)
#elif defined(_MESH_INSTANCE_CULL_ON)
StructuredBuffer<MeshInstanceTrs> _MeshInstanceBuffer;
StructuredBuffer<uint> _MeshInstanceIndexBuffer;
#else
StructuredBuffer<MeshInstanceTrs> _MeshInstanceBuffer;
#endif

void ApplyInstanceMatrices(float3 pos, float4 q, float3 sc)
{
    q = normalize(q);
    float x = q.x, y = q.y, z = q.z, w = q.w;
    float xx = x * x, yy = y * y, zz = z * z;
    float xy = x * y, xz = x * z, yz = y * z;
    float wx = w * x, wy = w * y, wz = w * z;
    float3 ur0 = float3(1.0 - 2.0 * (yy + zz), 2.0 * (xy + wz), 2.0 * (xz - wy));
    float3 ur1 = float3(2.0 * (xy - wz), 1.0 - 2.0 * (xx + zz), 2.0 * (yz + wx));
    float3 ur2 = float3(2.0 * (xz + wy), 2.0 * (yz - wx), 1.0 - 2.0 * (xx + yy));
    float3 col0 = ur0 * sc.x;
    float3 col1 = ur1 * sc.y;
    float3 col2 = ur2 * sc.z;
    unity_ObjectToWorld._11_21_31_41 = float4(col0, 0.0);
    unity_ObjectToWorld._12_22_32_42 = float4(col1, 0.0);
    unity_ObjectToWorld._13_23_33_43 = float4(col2, 0.0);
    unity_ObjectToWorld._14_24_34_44 = float4(pos, 1.0);

    //没用到
    //float3 invS = float3(1.0 / sc.x, 1.0 / sc.y, 1.0 / sc.z);
    //float3 row0 = float3(ur0.x * invS.x, ur1.x * invS.y, ur2.x * invS.z);
    //float3 row1 = float3(ur0.y * invS.x, ur1.y * invS.y, ur2.y * invS.z);
    //float3 row2 = float3(ur0.z * invS.x, ur1.z * invS.y, ur2.z * invS.z);
    //float3 invPos = float3(
    //    -(row0.x * pos.x + row0.y * pos.y + row0.z * pos.z),
    //    -(row1.x * pos.x + row1.y * pos.y + row1.z * pos.z),
    //    -(row2.x * pos.x + row2.y * pos.y + row2.z * pos.z));
    //unity_WorldToObject._11_21_31_41 = float4(row0, 0.0);
    //unity_WorldToObject._12_22_32_42 = float4(row1, 0.0);
    //unity_WorldToObject._13_23_33_43 = float4(row2, 0.0);
    //unity_WorldToObject._14_24_34_44 = float4(invPos, 1.0);
}

// trsIndex 此处直接用于采样 TRS 数据
void LoadInstanceData(uint trsIndex, out float3 pos, out float4 q, out float3 sc)
{
#if defined(_MESH_INSTANCE_TEX_FETCH_ON)
    // trsIndex 来自 setup() 中 LoadTextureFetchVisibleInstanceIndex 返回值的低 30 位
    uint texWidth = (uint)max(_MeshInstanceTexWidth, 1.0);
    float texHeight = max(_MeshInstanceTexHeight, 1.0);
    uint x = trsIndex % texWidth;
    uint y = trsIndex / texWidth;
    float2 uv = float2(((float)x + 0.5) * _MeshInstanceTexInvWidth, ((float)y + 0.5) / texHeight);
    float4 texA = SAMPLE_TEXTURE2D_LOD(_MeshInstanceTexA, sampler_MeshInstanceTexA, uv, 0);
    float4 texB = SAMPLE_TEXTURE2D_LOD(_MeshInstanceTexB, sampler_MeshInstanceTexB, uv, 0);
    // TexC 存 chunk 中心世界坐标（RGBAFloat/float32），加回后精确还原世界位置，消除大坐标 f16 精度误差
    float4 texC = SAMPLE_TEXTURE2D_LOD(_MeshInstanceTexC, sampler_MeshInstanceTexC, uv, 0);
    pos = texA.xyz + texC.xyz;
    q = texB;
    float uniformScale = max(texA.w, 1e-6);
    sc = float3(uniformScale, uniformScale, uniformScale);
#else
    MeshInstanceTrs t = _MeshInstanceBuffer[trsIndex];
    pos = t.positionScale.xyz;
    q = float4(t.rotation.x, t.rotation.y, t.rotation.z, t.rotation.w);
    float uniformScale = max(t.positionScale.w, 1e-6);
    sc = float3(uniformScale, uniformScale, uniformScale);
#endif
}

#endif

void setup()
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
	    //packed uint：bits[31:30]=LOD, bits[29:0]=trsIndex
	    uint trsIndex = unity_InstanceID;
	    uint packedLod = 0u;

	#if defined(_MESH_INSTANCE_TEX_FETCH_ON)
	    // TEX 路径：可见索引纹理中存储 packed uint（LOD + trsIndex）
	    uint packedTex = LoadTextureFetchVisibleInstanceIndex(unity_InstanceID);
	    packedLod = packedTex >> 30u;
	    trsIndex   = packedTex & 0x3FFFFFFFu;
	#elif defined(_MESH_INSTANCE_CULL_ON)
	    // StructuredBuffer 路径：_MeshInstanceIndexBuffer 存储 packed uint
	    uint packedBuf = _MeshInstanceIndexBuffer[unity_InstanceID];
	    packedLod = packedBuf >> 30u;
	    trsIndex   = packedBuf & 0x3FFFFFFFu;
	#endif

	    float3 pos;
	    float4 q;
	    float3 sc;
	    LoadInstanceData(trsIndex, pos, q, sc);
	    ApplyInstanceMatrices(pos, q, sc);

	#if defined(_MESH_INSTANCE_CULL_ON) || defined(_MESH_INSTANCE_TEX_FETCH_ON)
	    // 将 LOD 等级存入矩阵第 4 行第 1 列，该位在标准 TRS 矩阵中恒为 0，
	    unity_ObjectToWorld._41 = (float)packedLod;
	#endif
#endif
}

// Properties
CBUFFER_START(UnityPerMaterial)
    half4   _SpecularColor;
    half4   _SpecularColor2;
    half    _AOStrength;
    half    _PersectiveCorrection;
    half    _GIIntensity;

    half    _WindVariation;
    half    _WindStrength;
    half    _TurbulenceStrength;

    half    _WindLineScale;
    half4   _WindLineColor;
    half    _WindLineLocalStrength;

    half    _VariationMaskScale;
    half3   _VariationColorA;
    half3   _VariationColorB;

    half    _BlendWithTerrainStrength;
    half    _BlendWithTerrainHeight;
    half    _BBlendWithTerrainStrength;
    half    _BBlendWithTerrainHeight;

    XKNIGHT_DEPTH_MASK_INPUT_1

    half    _DitherIntensity, _DitherSize, _DitherAlpha;
    half	_DitherWithMatrix;
    float4  _DitherTexture_TexelSize;
CBUFFER_END

XKNIGHT_DEPTH_MASK_INPUT_2

TEXTURE2D(_WindLineTexture); SAMPLER(sampler_WindLineTexture);
TEXTURE2D(_VariationMask);   SAMPLER(sampler_VariationMask);
TEXTURE2D(_DitherTexture);

#if defined( _EXCLUDE_CHARACTER_ON )
TEXTURE2D(_CameraCharacterDepthTexture); SAMPLER(sampler_CameraCharacterDepthTexture);
#endif

float4 _BakedGrassOcclusionStartPos;
float4 _BakedGrassOcclusionParams;
float _MaxDistance;
float _LOD0End;
float _LOD1End;

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    #define BAKED_GRASS_LOCAL_FADE_ON 1
#else
    #define BAKED_GRASS_LOCAL_FADE_ON 0
#endif

int GetGrassShaderLod(float3 positionWS)
{
#if !defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    return 0;
#endif

    float lod0End = max(_LOD0End, 0.0);
    float lod1End = max(_LOD1End, lod0End);
    float maxDistance = max(_MaxDistance, lod1End);
    float cameraDistance = distance(positionWS, GetCameraPositionWS());

    if (cameraDistance < lod0End)
    {
        return 0;
    }
    else if (cameraDistance < lod1End)
    {
        return 1;
    }
    else if (cameraDistance < maxDistance)
    {
        return 2;
    }
    else
    {
        return 2;
    }
}

half SampleBakedGrassOcclusionWeight(float3 positionWS)
{
#if BAKED_GRASS_LOCAL_FADE_ON
    if (_BakedGrassOcclusionParams.x <= 0.0h || _BakedGrassOcclusionParams.w <= 0.5h)
    {
        return 0.0h;
    }

    float fadeDistance = max(_BakedGrassOcclusionStartPos.w, 1e-4);
    float distanceToStart = distance(positionWS, _BakedGrassOcclusionStartPos.xyz);
    float localWeight = saturate(1.0 - distanceToStart / fadeDistance);
    return (half)(localWeight * _BakedGrassOcclusionParams.x);
#else
    return 0.0h;
#endif
}

half GetBakedGrassLocalDitherIntensity(float3 positionWS, half baseDitherIntensity)
{
#if BAKED_GRASS_LOCAL_FADE_ON
    if (_BakedGrassOcclusionParams.z <= 0.5h)
    {
        half occlusionWeight = SampleBakedGrassOcclusionWeight(positionWS);
        return lerp(baseDitherIntensity, (half)_BakedGrassOcclusionParams.y, occlusionWeight);
    }
#endif
    return baseDitherIntensity;
}

void ApplyGrassDitherClip(float4 positionSS, float3 positionWS)
{
#if defined(_DITHER_ON)
    half ditherIntensity = saturate(_DitherIntensity);
    ditherIntensity = saturate(GetBakedGrassLocalDitherIntensity(positionWS, ditherIntensity));
    UNITY_BRANCH
    if (ditherIntensity > 0.0h)
    {
        DitherWithTexture(positionSS, 1.0h - ditherIntensity, _DitherSize, _DitherWithMatrix,
            TEXTURE2D_ARGS(_DitherTexture, sampler_LinearRepeat), _DitherTexture_TexelSize);
    }
#endif
}

half ApplyBakedGrassLocalAlpha(float3 positionWS, half baseAlpha)
{
#if BAKED_GRASS_LOCAL_FADE_ON
    if (_BakedGrassOcclusionParams.z > 0.5h)
    {
        half occlusionWeight = SampleBakedGrassOcclusionWeight(positionWS);
        return lerp(baseAlpha, (half)_BakedGrassOcclusionParams.y, occlusionWeight);
    }
#endif
    return baseAlpha;
}

#endif // __GRASS_INPUT__
