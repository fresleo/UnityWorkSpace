/*******************************************************************************
 * File: MeshInstanceRenderer.Texture.cs
 * Author: fan.shi
 * Date: 2026-03-27
 * Description: Texture 后端（RGBAHalf 实例纹理、可见索引纹理、MPB 绑定与上传）。
 ******************************************************************************/

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public partial class MeshInstanceRenderer
{
    static readonly int s_MeshInstanceTexA = Shader.PropertyToID("_MeshInstanceTexA");
    static readonly int s_MeshInstanceTexB = Shader.PropertyToID("_MeshInstanceTexB");
    static readonly int s_MeshInstanceTexC = Shader.PropertyToID("_MeshInstanceTexC");
    static readonly int s_MeshInstanceTexWidth = Shader.PropertyToID("_MeshInstanceTexWidth");
    static readonly int s_MeshInstanceTexInvWidth = Shader.PropertyToID("_MeshInstanceTexInvWidth");
    static readonly int s_MeshInstanceTexHeight = Shader.PropertyToID("_MeshInstanceTexHeight");
    static readonly int s_MeshInstanceVisibleIndexTex = Shader.PropertyToID("_MeshInstanceVisibleIndexTex");
    static readonly int s_MeshInstanceVisibleIndexTexInvWidth = Shader.PropertyToID("_MeshInstanceVisibleIndexTexInvWidth");
    static readonly int s_MeshInstanceVisibleIndexTexHeight = Shader.PropertyToID("_MeshInstanceVisibleIndexTexHeight");

    const int C_TextureFetchWidth = 1024;
    const string C_KeywordMeshInstanceTexFetch = "_MESH_INSTANCE_TEX_FETCH_ON";

    // 非纹理路径用内置占位绑定 
    static Texture MpbTextureNonNullPlaceholder => Texture2D.blackTexture;

    void ClearMeshInstanceTexturesOnPropertyBlock(MaterialPropertyBlock mpb)
    {
        Texture ph = MpbTextureNonNullPlaceholder;
        mpb.SetTexture(s_MeshInstanceTexA, ph);
        mpb.SetTexture(s_MeshInstanceTexB, ph);
        mpb.SetTexture(s_MeshInstanceTexC, ph);
        mpb.SetTexture(s_MeshInstanceVisibleIndexTex, ph);
        mpb.SetFloat(s_MeshInstanceVisibleIndexTexInvWidth, 0f);
        mpb.SetFloat(s_MeshInstanceVisibleIndexTexHeight, 0f);
    }

    static float GetUniformScale(MeshInstanceTrsStructuredGpu trs)
    {
        return Mathf.Max(trs.PositionScale.w, 1e-6f);
    }

    /// <summary> Texture 后端依赖 RGBAHalf 纹理格式是否受当前图形设备支持。 </summary>
    static bool SupportsTextureBackendRgbahalf()
    {
        return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf);
    }

    static void WriteHalfPixel(ushort[] dst, int pixelIndex, float x, float y, float z, float w)
    {
        int baseIndex = pixelIndex * 4;
        dst[baseIndex + 0] = (ushort)math.f32tof16(x);
        dst[baseIndex + 1] = (ushort)math.f32tof16(y);
        dst[baseIndex + 2] = (ushort)math.f32tof16(z);
        dst[baseIndex + 3] = (ushort)math.f32tof16(w);
    }

    static void WriteFloatPixel(float[] dst, int pixelIndex, float x, float y, float z, float w)
    {
        int baseIndex = pixelIndex * 4;
        dst[baseIndex + 0] = x;
        dst[baseIndex + 1] = y;
        dst[baseIndex + 2] = z;
        dst[baseIndex + 3] = w;
    }

    static void DestroyTexture(Texture2D tex)
    {
        if (tex == null)
        {
            return;
        }
        if (Application.isPlaying)
        {
            Object.Destroy(tex);
        }
        else
        {
            Object.DestroyImmediate(tex);
        }
    }

    void DrawTextureFetch(EntryState state, Mesh mesh, Material material, int subMeshIndex, int instanceCount, NativeArray<uint> visibleList, bool useVisibleList)
    {
        if (material == null)
        {
            return;
        }
        if (!UploadTextureFetchTrsData(state))
        {
            return;
        }
        if (useVisibleList && !UploadTextureFetchVisibleIndices(state, visibleList, instanceCount))
        {
            return;
        }
        subMeshIndex = NormalizeSubMeshIndex(mesh, subMeshIndex);

        state.propertyBlock.SetBuffer(s_MeshInstanceBuffer, (ComputeBuffer)null);
        state.propertyBlock.SetBuffer(s_MeshInstanceIndexBuffer, (ComputeBuffer)null);
        state.propertyBlock.SetTexture(s_MeshInstanceTexA, state.instanceTextureA);
        state.propertyBlock.SetTexture(s_MeshInstanceTexB, state.instanceTextureB);
        state.propertyBlock.SetTexture(s_MeshInstanceTexC, state.instanceTextureC != null ? (Texture)state.instanceTextureC : MpbTextureNonNullPlaceholder);
        state.propertyBlock.SetFloat(s_MeshInstanceTexWidth, state.instanceTextureWidth);
        state.propertyBlock.SetFloat(s_MeshInstanceTexInvWidth, 1f / Mathf.Max(state.instanceTextureWidth, 1));
        state.propertyBlock.SetFloat(s_MeshInstanceTexHeight, state.instanceTextureHeight);
        Texture visibleIdxTex = (useVisibleList && state.instanceVisibleIndexTexture != null) ? state.instanceVisibleIndexTexture : MpbTextureNonNullPlaceholder;
        state.propertyBlock.SetTexture(s_MeshInstanceVisibleIndexTex, visibleIdxTex);
        state.propertyBlock.SetFloat(s_MeshInstanceVisibleIndexTexInvWidth, useVisibleList ? 1f / Mathf.Max(state.instanceVisibleIndexTextureWidth, 1) : 0f);
        state.propertyBlock.SetFloat(s_MeshInstanceVisibleIndexTexHeight, useVisibleList ? state.instanceVisibleIndexTextureHeight : 0f);
        SetupArgsForDraw(state, mesh, subMeshIndex, instanceCount);
        UploadCounterAndArgs(state);
        var mode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, material, state.bounds, state.counterAndArgsBuffer, C_ArgsOffsetBytes, state.propertyBlock, mode, true, gameObject.layer);
    }

    void EnsureTextureFetchTextures(EntryState state, int requiredCount)
    {
        if (requiredCount <= 0)
        {
            return;
        }
        if (state.instanceTextureA != null && state.instanceTextureB != null && state.instanceTextureC != null && state.instanceTextureCapacity >= requiredCount)
        {
            return;
        }

        if (state.instanceTextureA != null)
        {
            DestroyTexture(state.instanceTextureA);
            state.instanceTextureA = null;
        }
        if (state.instanceTextureB != null)
        {
            DestroyTexture(state.instanceTextureB);
            state.instanceTextureB = null;
        }
        if (state.instanceTextureC != null)
        {
            DestroyTexture(state.instanceTextureC);
            state.instanceTextureC = null;
        }

        state.instanceTextureWidth = C_TextureFetchWidth;
        state.instanceTextureHeight = Mathf.Max(1, (requiredCount + C_TextureFetchWidth - 1) / C_TextureFetchWidth);
        state.instanceTextureCapacity = state.instanceTextureWidth * state.instanceTextureHeight;
        state.instanceTextureAData = new ushort[state.instanceTextureCapacity * 4];
        state.instanceTextureBData = new ushort[state.instanceTextureCapacity * 4];
        // TexC 使用 float32 存储 chunk 中心坐标，避免大世界坐标下 float16 的精度误差
        state.instanceTextureCData = new float[state.instanceTextureCapacity * 4];

        // TexA 存本地位置（相对 chunk 中心），精度 ~0.004 单位（±5m 范围内）。
        state.instanceTextureA = new Texture2D(state.instanceTextureWidth, state.instanceTextureHeight, TextureFormat.RGBAHalf, false, true);
        state.instanceTextureA.name = state.mesh != null ? state.mesh.name + "_MeshInstanceTexA" : "MeshInstanceTexA";
        state.instanceTextureA.wrapMode = TextureWrapMode.Clamp;
        state.instanceTextureA.filterMode = FilterMode.Point;
        state.instanceTextureA.anisoLevel = 0;

        state.instanceTextureB = new Texture2D(state.instanceTextureWidth, state.instanceTextureHeight, TextureFormat.RGBAHalf, false, true);
        state.instanceTextureB.name = state.mesh != null ? state.mesh.name + "_MeshInstanceTexB" : "MeshInstanceTexB";
        state.instanceTextureB.wrapMode = TextureWrapMode.Clamp;
        state.instanceTextureB.filterMode = FilterMode.Point;
        state.instanceTextureB.anisoLevel = 0;

        // TexC：存 chunk 中心世界坐标。
        state.instanceTextureC = new Texture2D(state.instanceTextureWidth, state.instanceTextureHeight, TextureFormat.RGBAFloat, false, true);
        state.instanceTextureC.name = state.mesh != null ? state.mesh.name + "_MeshInstanceTexC" : "MeshInstanceTexC";
        state.instanceTextureC.wrapMode = TextureWrapMode.Clamp;
        state.instanceTextureC.filterMode = FilterMode.Point;
        state.instanceTextureC.anisoLevel = 0;

        state.instanceTextureFullDataUploaded = false;
    }

    void EnsureTextureFetchVisibleIndexTexture(EntryState state, int requiredCount)
    {
        if (requiredCount <= 0)
        {
            return;
        }
        if (state.instanceVisibleIndexTexture != null && state.instanceVisibleIndexTextureCapacity >= requiredCount)
        {
            return;
        }

        if (state.instanceVisibleIndexTexture != null)
        {
            DestroyTexture(state.instanceVisibleIndexTexture);
            state.instanceVisibleIndexTexture = null;
        }

        state.instanceVisibleIndexTextureWidth = C_TextureFetchWidth;
        state.instanceVisibleIndexTextureHeight = Mathf.Max(1, (requiredCount + C_TextureFetchWidth - 1) / C_TextureFetchWidth);
        state.instanceVisibleIndexTextureCapacity = state.instanceVisibleIndexTextureWidth * state.instanceVisibleIndexTextureHeight;
        state.instanceVisibleIndexTexture = new Texture2D(state.instanceVisibleIndexTextureWidth, state.instanceVisibleIndexTextureHeight, TextureFormat.RGBA32, false, true);
        state.instanceVisibleIndexTexture.name = state.mesh != null ? state.mesh.name + "_MeshInstanceVisibleIndexTex" : "MeshInstanceVisibleIndexTex";
        state.instanceVisibleIndexTexture.wrapMode = TextureWrapMode.Clamp;
        state.instanceVisibleIndexTexture.filterMode = FilterMode.Point;
        state.instanceVisibleIndexTexture.anisoLevel = 0;
    }

    bool UploadTextureFetchTrsData(EntryState state)
    {
        if (state.instanceTrsData == null || state.instanceCount <= 0)
        {
            return false;
        }

        EnsureTextureFetchTextures(state, state.instanceCount);
        if (state.instanceTextureA == null || state.instanceTextureB == null || state.instanceTextureC == null)
        {
            return false;
        }

        if (state.instanceTextureFullDataUploaded)
        {
            return true;
        }

        bool hasChunkCenters = state.instanceChunkCentersForTex != null
            && state.instanceChunkCentersForTex.Length >= state.instanceCount;

        for (int i = 0; i < state.instanceCount; i++)
        {
            if ((uint)i >= (uint)state.instanceTrsData.Length)
            {
                return false;
            }

            MeshInstanceTrsStructuredGpu trs = state.instanceTrsData[i];

            Vector3 chunkCenter = hasChunkCenters ? state.instanceChunkCentersForTex[i] : Vector3.zero;

            // TexA：存本地位置（worldPos - chunkCenter），精度由 ±chunkSize/2 决定（通常 ±5m，f16 误差 ~0.004）
            float localX = trs.PositionScale.x - chunkCenter.x;
            float localY = trs.PositionScale.y - chunkCenter.y;
            float localZ = trs.PositionScale.z - chunkCenter.z;
            WriteHalfPixel(state.instanceTextureAData, i, localX, localY, localZ, GetUniformScale(trs));

            // TexB：旋转四元数，分量在 [-1,1]，f16 精度 ~0.001
            WriteHalfPixel(state.instanceTextureBData, i, trs.Rotation.x, trs.Rotation.y, trs.Rotation.z, trs.Rotation.w);

            // TexC：chunk 中心世界坐标
            WriteFloatPixel(state.instanceTextureCData, i, chunkCenter.x, chunkCenter.y, chunkCenter.z, 0f);
        }

        state.instanceTextureA.SetPixelData(state.instanceTextureAData, 0);
        state.instanceTextureB.SetPixelData(state.instanceTextureBData, 0);
        state.instanceTextureC.SetPixelData(state.instanceTextureCData, 0);
        state.instanceTextureA.Apply(false, false);
        state.instanceTextureB.Apply(false, false);
        state.instanceTextureC.Apply(false, false);
        state.instanceTextureFullDataUploaded = true;
        return true;
    }

    bool UploadTextureFetchVisibleIndices(EntryState state, NativeArray<uint> visibleList, int instanceCount)
    {
        if (instanceCount <= 0 || !visibleList.IsCreated)
        {
            return false;
        }

        EnsureTextureFetchVisibleIndexTexture(state, instanceCount);
        if (state.instanceVisibleIndexTexture == null)
        {
            return false;
        }

        var rawData = state.instanceVisibleIndexTexture.GetRawTextureData<uint>();
        if (rawData.Length < instanceCount)
        {
            return false;
        }
        NativeArray<uint>.Copy(visibleList, 0, rawData, 0, instanceCount);
        state.instanceVisibleIndexTexture.Apply(false, false);

        return true;
    }
}
