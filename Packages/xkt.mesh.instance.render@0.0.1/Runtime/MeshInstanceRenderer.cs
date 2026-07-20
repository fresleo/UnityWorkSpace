/*******************************************************************************
 * File: MeshInstanceRenderer.cs
 * Author: fan.shi
 * Date: 2026-03-16
 * Description: 使用烘焙数据，GPU Instancing 渲染，支持多种 Mesh（MeshInstanceRender）
 * Notice:
 * 1. 无剔除绘制 
 * 2. GPU Chunk 剔除 视锥剔除
 * 3. CPU Chunk 剔除 + GPU Compute 实例剔除
 * 4. CPU Chunk 剔除 视锥剔除
 ******************************************************************************/

using System;
using System.Collections.Generic;
using Common;
using GameModules;
using GameModules.Asset;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public partial class MeshInstanceRenderer : MonoBehaviour
{
    [Header("Baked 数据")]
    public List<MeshInstanceEntry> entries;

    [Header("Rendering")]
    public bool castShadows = false;

    public EMeshInstanceDataBackend instanceDataBackend = EMeshInstanceDataBackend.Auto;

    [Header("Culling")]
    // 剔除+渲染路径
    public EMeshInstanceCullMode cullMode = EMeshInstanceCullMode.PureCpu;
    public UnityEngine.ComputeShader cullCompute;
    [Tooltip("Chunk 在 XZ 平面上的边长")]
    public float chunkSize = 10f;
    [Tooltip("超过此距离的实例不绘制")]
    public float maxCullDistance = 60f;
    [Tooltip("用于剔除的相机，空则使用 Camera.main")]
    public Camera cullCamera;
    [Tooltip("视锥向上下左右外扩展的世界距离，可减少屏幕边缘误剔除的瞬时闪烁")]
    [Min(0f)]
    public float frustumExpandMargin = 1f;


    [Header("LOD 距离与密度")]
    [Tooltip("近距终点：小于此距离全密度绘制")]
    public float lod0EndDistance = 30;
    [Tooltip("中距终点：近距到此距离按 LOD1 密度绘制")]
    public float lod1EndDistance = 50f;
    [Tooltip("中距目标密度 (0~1)：lod0End~lod1End 间平滑过渡，并按实例哈希抖动，避免块状剔除")]
    [Range(0.1f, 1f)]
    public float lod1Density = 0.5f;
    [Tooltip("远距目标密度 (0~1)：lod1End~maxCullDistance 间平滑过渡 + 抖动")]
    [Range(0.05f, 1f)]
    public float lod2Density = 0.25f;

    List<EntryState> _entryStates = new List<EntryState>();
    Plane[] _frustumPlanes = new Plane[6];
    Vector4[] _frustumPlanesVec4 = new Vector4[6];
    NativeArray<float4> _planesNative;
    int _kernelCull = -1;
    int _kernelChunkAndInstanceCull = -1;
    readonly uint[] _counterAndArgsScratch = new uint[6];
    // LOD 参数标记
    bool _lodParamsDirty = true;


    static uint FloatToUInt32Bits(float f)
    {
        var u = new FloatUIntUnion { FloatValue = f };
        return u.UIntValue;
    }

    static EMeshInstanceDataBackend ResolveInstanceDataBackend(EMeshInstanceDataBackend backend)
    {
        if (backend != EMeshInstanceDataBackend.Auto)
        {
            return backend;
        }
        return SupportsStructuredBufferPath()
            ? EMeshInstanceDataBackend.StructuredBuffer
            : EMeshInstanceDataBackend.Texture;
    }

    static EMeshInstanceCullMode ResolveEffectiveCullMode(EMeshInstanceCullMode requestedCullMode, EMeshInstanceDataBackend backend)
    {
        if (requestedCullMode == EMeshInstanceCullMode.NoCull)
        {
            return EMeshInstanceCullMode.NoCull;
        }
        if (backend == EMeshInstanceDataBackend.Texture)
        {
            return EMeshInstanceCullMode.PureCpu;
        }
        return requestedCullMode;
    }

    /// <summary> 从源材质克隆并固定 keyword,避免多 entry 共享 Material 时运行时 Enable/Disable 互相覆盖。 </summary>
    static Material CreateMaterialWithKeywords(Material source, bool meshInstanceCullOn, bool meshInstanceTexFetchOn)
    {
        var m = new Material(source);
        if (meshInstanceCullOn)
        {
            m.EnableKeyword(C_KeywordMeshInstanceCull);
        }
        else
        {
            m.DisableKeyword(C_KeywordMeshInstanceCull);
        }
        if (meshInstanceTexFetchOn)
        {
            m.EnableKeyword(C_KeywordMeshInstanceTexFetch);
        }
        else
        {
            m.DisableKeyword(C_KeywordMeshInstanceTexFetch);
        }
        return m;
    }

    static void DestroyDrawMaterials(EntryState state)
    {
        if (state.materialStructuredCull != null)
        {
            DestroyMaterial(state.materialStructuredCull);
            state.materialStructuredCull = null;
        }
        if (state.materialStructuredFull != null)
        {
            DestroyMaterial(state.materialStructuredFull);
            state.materialStructuredFull = null;
        }
        if (state.materialTexFetch != null)
        {
            DestroyMaterial(state.materialTexFetch);
            state.materialTexFetch = null;
        }
    }

    static void DestroyMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(material);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    static bool SupportsStructuredBufferPath()
    {
        GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
        if (deviceType == GraphicsDeviceType.Vulkan 
            || deviceType == GraphicsDeviceType.Metal 
            || deviceType == GraphicsDeviceType.Direct3D11
            || deviceType == GraphicsDeviceType.Direct3D12
            )
        {
            return true;
        }

        if (deviceType != GraphicsDeviceType.OpenGLES3)
        {
            return false;
        }

        return SystemInfo.graphicsShaderLevel >= 50
            && TryGetOpenGlesVersion(out int major, out int minor)
            && (major > 3 || (major == 3 && minor >= 2))
            && !IsMaliGpu();
    }

    static bool IsMaliGpu()
    {
        return ContainsIgnoreCase(SystemInfo.graphicsDeviceName, "mali")
            || ContainsIgnoreCase(SystemInfo.graphicsDeviceVendor, "mali");
    }

    static bool ContainsIgnoreCase(string source, string value)
    {
        return !string.IsNullOrEmpty(source)
            && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool TryGetOpenGlesVersion(out int major, out int minor)
    {
        major = 0;
        minor = 0;
        string version = SystemInfo.graphicsDeviceVersion;
        if (string.IsNullOrEmpty(version))
        {
            return false;
        }

        string[] tokens = version.Split(' ', '/', '(', ')', '-', '_');
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (string.IsNullOrEmpty(token) || !char.IsDigit(token[0]))
            {
                continue;
            }

            string[] versionParts = token.Split('.');
            if (versionParts.Length == 0 || !int.TryParse(versionParts[0], out major))
            {
                continue;
            }

            if (versionParts.Length > 1)
            {
                int.TryParse(versionParts[1], out minor);
            }
            return true;
        }

        return false;
    }

    /// <summary> 打包 Chunk bounds（每个 chunk 为 2×float4，按位写成 8×uint）、starts（chunkCount+1 个）与扁平 indices，供 _ChunkPackedMetaAndIndices。</summary>
    static uint[] BuildChunkPackedMetaAndIndices(List<Bounds> chunkBounds, List<int> chunkInstanceStarts, List<int> chunkInstanceIndices)
    {
        int chunkCount = chunkBounds.Count;
        int boundsUInts = chunkCount * 8;
        int startsUInts = chunkCount + 1;
        int indicesUInts = chunkInstanceIndices.Count;
        var packed = new uint[boundsUInts + startsUInts + indicesUInts];

        int w = 0;
        for (int c = 0; c < chunkCount; c++)
        {
            var b = chunkBounds[c];
            var c0 = new Vector4(b.center.x, b.center.y, b.center.z, 0f);
            var c1 = new Vector4(b.extents.x, b.extents.y, b.extents.z, 0f);
            packed[w++] = FloatToUInt32Bits(c0.x);
            packed[w++] = FloatToUInt32Bits(c0.y);
            packed[w++] = FloatToUInt32Bits(c0.z);
            packed[w++] = FloatToUInt32Bits(c0.w);
            packed[w++] = FloatToUInt32Bits(c1.x);
            packed[w++] = FloatToUInt32Bits(c1.y);
            packed[w++] = FloatToUInt32Bits(c1.z);
            packed[w++] = FloatToUInt32Bits(c1.w);
        }
        for (int c = 0; c <= chunkCount; c++)
        {
            packed[w++] = (uint)chunkInstanceStarts[c];
        }
        for (int k = 0; k < chunkInstanceIndices.Count; k++)
        {
            packed[w++] = (uint)chunkInstanceIndices[k];
        }
        return packed;
    }

    void OnEnable()
    {
        BeginRebuildBuffersAsync();
    }

    void OnDisable()
    {
        ReleaseBuffers();
    }

    void OnValidate()
    {
        NormalizeShaderLodDistances(ref lod0EndDistance, ref lod1EndDistance, ref maxCullDistance);
        _lodParamsDirty = true;
    }

    /// <summary> 通过 AssetSystem 异步加载各 entry 烘焙二进制，全部完成后再重建 GPU/Native 缓冲。</summary>
    void BeginRebuildBuffersAsync()
    {
        ReleaseBuffers();

        if (entries == null || entries.Count == 0)
        {
            return;
        }

        LoadAllBakedAndRebuild();
    }

    void LoadAllBakedAndRebuild()
    {
        if (GmCullModeOverride.HasValue)
        {
            cullMode = GmCullModeOverride.Value;
        }

        if (GmInstanceDataBackendOverride.HasValue)
        {
            instanceDataBackend = GmInstanceDataBackendOverride.Value;
        }

        var effectiveEntries = new List<MeshInstanceEntry>();
        foreach (var e in entries)
        {
            if (e != null && e.mesh != null && e.material != null && !string.IsNullOrWhiteSpace(e.bakedDataAssetPath))
            {
                effectiveEntries.Add(e);
            }
        }
        if (effectiveEntries.Count == 0)
        {
            return;
        }

        var bakedBytes = new byte[effectiveEntries.Count][];
        int remaining = effectiveEntries.Count;

        for (int i = 0; i < effectiveEntries.Count; i++)
        {
            int idx = i;
            var ent = effectiveEntries[idx];
            string binPath = ent.bakedDataAssetPath.Trim().Replace('\\', '/');

            if (!Application.isPlaying)
            {
                bakedBytes[idx] = LoadEditorBakedBytes(binPath);
                remaining--;
                if (remaining == 0)
                {
                    RebuildBuffersFromResolvedBytes(effectiveEntries, bakedBytes);
                }
                continue;
            }

            AssetSystem.Instance.LoadAsset<TextAsset>(binPath, ta =>
            {
                try
                {
                    if (ta != null)
                    {
                        bakedBytes[idx] = ta.bytes;
                    }
                }
                finally
                {
                    if (ta != null)
                    {
                        AssetSystem.Instance.Release(binPath);
                    }
                }

                remaining--;
                if (remaining == 0)
                {
                    RebuildBuffersFromResolvedBytes(effectiveEntries, bakedBytes);
                }
            }, ELoadType.Async);
        }
    }

    static byte[] LoadEditorBakedBytes(string logicalPath)
    {
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(logicalPath))
        {
            return null;
        }

        string projectPath = logicalPath.Replace('\\', '/');
        if (!projectPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            projectPath = "Assets/OutputRes/" + projectPath;
        }

        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(projectPath);
        return textAsset != null ? textAsset.bytes : null;
#else
        return null;
#endif
    }

    /// <summary>
    /// 根据已解析的烘焙字节重建所有 Entry 的 GPU/Native 缓冲与 Chunk 数据；无剔除时全量，否则按 cullMode 创建对应资源。
    /// </summary>
    void RebuildBuffersFromResolvedBytes(List<MeshInstanceEntry> effectiveEntries, byte[][] bakedBytesPerEntry)
    {
        ReleaseBuffers();
        if (effectiveEntries == null || effectiveEntries.Count == 0)
        {
            return;
        }

        EMeshInstanceDataBackend resolvedBackend = ResolveInstanceDataBackend(instanceDataBackend);
        bool supportsTextureFetch = SupportsTextureBackendRgbahalf();
        bool needsPlanes = false;
        bool needsGpuChunkKernel = false;
        bool needsComputeCullKernel = false;

        for (int idx = 0; idx < effectiveEntries.Count; idx++)
        {
            var entry = effectiveEntries[idx];
            var raw = bakedBytesPerEntry != null && idx < bakedBytesPerEntry.Length ? bakedBytesPerEntry[idx] : null;
            if (raw == null || !MeshInstanceData.TryLoad(raw, out MeshInstanceLoadResult load) || load.Count <= 0)
            {
                string pathLabel = !string.IsNullOrWhiteSpace(entry.bakedDataAssetPath) ? entry.bakedDataAssetPath.Trim() : "null";
                string meshName = entry.mesh != null ? entry.mesh.name : "null";
                string goName = gameObject != null ? gameObject.name : "null";
                D.Error(
                    "[MeshInstanceRenderer] 烘焙实例数据无效（需 TRS .bytes：TryLoad 失败或 Count<=0），已跳过该 entry。Mesh={0}, subMeshIndex={1}, bakedDataAssetPath={2}, gameObject={3}",
                    meshName,
                    entry.subMeshIndex,
                    pathLabel,
                    goName);
                continue;
            }

            MeshInstanceTrsStructuredGpu[] trsStructured = load.TrsStructured;
            bool useTextureFetch = resolvedBackend == EMeshInstanceDataBackend.Texture;
            if (useTextureFetch && !supportsTextureFetch)
            {
                D.Error(
                    "[MeshInstanceRenderer] Texture 后端需要 RGBAHalf 纹理支持，当前平台不支持，entry 已跳过。Mesh={0}, gameObject={1}",
                    entry.mesh != null ? entry.mesh.name : "null",
                    gameObject != null ? gameObject.name : "null");
                continue;
            }

            EMeshInstanceDataBackend entryBackend = useTextureFetch
                ? EMeshInstanceDataBackend.Texture
                : EMeshInstanceDataBackend.StructuredBuffer;
            EMeshInstanceCullMode entryCullMode = ResolveEffectiveCullMode(cullMode, entryBackend);
            var worldPos = new Vector3[load.Count];
            for (int i = 0; i < load.Count; i++)
            {
                var p = trsStructured[i].PositionScale;
                worldPos[i] = new Vector3(p.x, p.y, p.z);
            }

            if (entry.material == null)
            {
                D.Error(
                    "[MeshInstanceRenderer] entry.material 为空，已跳过。Mesh={0}, bakedDataAssetPath={1}, gameObject={2}",
                    entry.mesh != null ? entry.mesh.name : "null",
                    !string.IsNullOrWhiteSpace(entry.bakedDataAssetPath) ? entry.bakedDataAssetPath.Trim() : "null",
                    gameObject != null ? gameObject.name : "null");
                continue;
            }

            var state = new EntryState
            {
                mesh = entry.mesh,
                subMeshIndex = entry.subMeshIndex,
                instanceCount = load.Count,
                dataBackend = entryBackend,
                effectiveCullMode = entryCullMode,
                instanceWorldPositions = worldPos,
                instanceTrsData = entryBackend == EMeshInstanceDataBackend.Texture ? trsStructured : null,
                propertyBlock = new MaterialPropertyBlock()
            };
            if (entryBackend == EMeshInstanceDataBackend.StructuredBuffer)
            {
                state.materialStructuredCull = CreateMaterialWithKeywords(entry.material, true, false);
                state.materialStructuredFull = CreateMaterialWithKeywords(entry.material, false, false);
            }
            else
            {
                state.materialTexFetch = CreateMaterialWithKeywords(entry.material, false, true);
            }
            ApplyOcclusionToEntryMaterials(state);
            ComputeBoundsFromInstancePositions(state);

            if (entryBackend == EMeshInstanceDataBackend.StructuredBuffer)
            {
                state.instanceBuffer = new ComputeBuffer(load.Count, C_InstanceBufferStrideTrs);
                state.instanceBuffer.SetData(trsStructured);
            }
            state.args[0] = state.mesh.GetIndexCount(state.subMeshIndex);
            state.args[1] = (uint)load.Count;
            state.args[2] = state.mesh.GetIndexStart(state.subMeshIndex);
            state.args[3] = state.mesh.GetBaseVertex(state.subMeshIndex);
            state.args[4] = 0;
            state.counterAndArgsBuffer = new ComputeBuffer(6, sizeof(uint), ComputeBufferType.IndirectArguments);
            UploadCounterAndArgs(state);

            if (state.effectiveCullMode != EMeshInstanceCullMode.NoCull)
            {
                state.chunkBounds = Common.ListPool<Bounds>.Get();
                state.chunkInstanceStarts = Common.ListPool<int>.Get();
                state.chunkInstanceIndices = Common.ListPool<int>.Get();
                BuildChunks(state);

                // Texture 后端：BuildChunks 完成后预计算每个实例的 chunk 中心
                if (entryBackend == EMeshInstanceDataBackend.Texture)
                {
                    state.instanceChunkCentersForTex = BuildInstanceChunkCenters(state);
                }

                if (state.chunkBounds.Count > 0)
                {
                    int maxCandidates = state.chunkInstanceIndices.Count;
                    int chunkCount = state.chunkBounds.Count;

                    if (entryBackend == EMeshInstanceDataBackend.StructuredBuffer)
                    {
                        state.visibleIndexBuffer = new ComputeBuffer(Mathf.Max(maxCandidates, 1), sizeof(uint));
                    }

                    if (state.effectiveCullMode == EMeshInstanceCullMode.PureGpu && entryBackend == EMeshInstanceDataBackend.StructuredBuffer && cullCompute != null && SystemInfo.supportsComputeShaders)
                    {
                        uint[] packed = BuildChunkPackedMetaAndIndices(state.chunkBounds, state.chunkInstanceStarts, state.chunkInstanceIndices);
                        state.chunkPackedMetaAndIndicesBuffer = new ComputeBuffer(packed.Length, sizeof(uint));
                        state.chunkPackedMetaAndIndicesBuffer.SetData(packed);
                        needsGpuChunkKernel = true;
                    }
                    else if (state.effectiveCullMode == EMeshInstanceCullMode.CpuChunkComputeCull)
                    {
                        state.chunkBoundsNative = new NativeArray<Bounds>(chunkCount, Allocator.Persistent);
                        state.visibleFlagsNative = new NativeArray<int>(chunkCount, Allocator.Persistent);
                        state.candidateIndices = Common.ListPool<int>.Get();
                        state.candidateIndices.Capacity = Mathf.Max(state.candidateIndices.Capacity, maxCandidates);
                        for (int c = 0; c < chunkCount; c++)
                        {
                            state.chunkBoundsNative[c] = state.chunkBounds[c];
                        }

                        if (cullCompute != null && SystemInfo.supportsComputeShaders)
                        {
                            state.candidateIndicesBuffer = new ComputeBuffer(Mathf.Max(maxCandidates, 1), sizeof(uint));
                            needsComputeCullKernel = true;
                        }
                        needsPlanes = true;
                    }
                    else if (state.effectiveCullMode == EMeshInstanceCullMode.PureCpu)
                    {
                        AllocatePureCpuCullingBuffers(state, chunkCount, maxCandidates);
                        needsPlanes = true;
                    }
                }
            }
            // NoCull + Texture 路径不会经过 BuildChunks，退化为使用整体包围盒中心作为 chunk 中心
            if (entryBackend == EMeshInstanceDataBackend.Texture && state.instanceChunkCentersForTex == null)
            {
                state.instanceChunkCentersForTex = BuildInstanceChunkCenters(state);
            }

            InitEntryPropertyBlockStatics(state);
            _entryStates.Add(state);
        }
        // 重建完成后标记 LOD 脏，确保下一帧 Update 刷新所有新 entry 的 PropertyBlock。
        _lodParamsDirty = true;
        if (needsPlanes || needsGpuChunkKernel || needsComputeCullKernel)
        {
            if (!_planesNative.IsCreated)
            {
                _planesNative = new NativeArray<float4>(6, Allocator.Persistent);
            }
            if (cullCompute != null)
            {
                if (needsGpuChunkKernel)
                {
                    _kernelChunkAndInstanceCull = cullCompute.FindKernel("ChunkAndInstanceCull");
                }
                if (needsComputeCullKernel)
                {
                    _kernelCull = cullCompute.FindKernel("Cull");
                }
            }
        }
    }

    /// <summary>
    /// 在 entry 创建完成后一次性写入 PropertyBlock 中不会逐帧变化的静态内容
    /// </summary>
    void InitEntryPropertyBlockStatics(EntryState state)
    {
        if (state.propertyBlock == null)
        {
            return;
        }
        if (state.dataBackend == EMeshInstanceDataBackend.StructuredBuffer)
        {
            ClearMeshInstanceTexturesOnPropertyBlock(state.propertyBlock);
            state.mpbTexturesCleared = true;
        }
        // 立即写入当前 LOD 值
        ApplyShaderLodParameters(state.propertyBlock, lod0EndDistance, lod1EndDistance, maxCullDistance);

    }

    /// <summary> 将 state.args 写入 counterAndArgsBuffer，布局 [0]=counter，[1..5]=args。</summary>
    /// <param name="state">当前条目的状态，需已创建 counterAndArgsBuffer。</param>
    void UploadCounterAndArgs(EntryState state)
    {
        _counterAndArgsScratch[0] = 0;
        _counterAndArgsScratch[1] = state.args[0];
        _counterAndArgsScratch[2] = state.args[1];
        _counterAndArgsScratch[3] = state.args[2];
        _counterAndArgsScratch[4] = state.args[3];
        _counterAndArgsScratch[5] = state.args[4];
        state.counterAndArgsBuffer.SetData(_counterAndArgsScratch);
    }

    void SetupArgsForDraw(EntryState state, Mesh mesh, int subMeshIndex, int instanceCount)
    {
        state.args[0] = mesh.GetIndexCount(subMeshIndex);
        state.args[1] = (uint)instanceCount;
        state.args[2] = mesh.GetIndexStart(subMeshIndex);
        state.args[3] = mesh.GetBaseVertex(subMeshIndex);
        state.args[4] = 0;
    }

    static int NormalizeSubMeshIndex(Mesh mesh, int subMeshIndex)
    {
        if (mesh == null || mesh.subMeshCount <= 0)
        {
            return 0;
        }
        return Mathf.Clamp(subMeshIndex, 0, mesh.subMeshCount - 1);
    }

    /// <summary> 根据 <see cref="EntryState.instanceWorldPositions"/> 计算整体 AABB 并写入 state.bounds。</summary>
    void ComputeBoundsFromInstancePositions(EntryState state)
    {
        var pos = state.instanceWorldPositions;
        if (pos == null || pos.Length == 0)
        {
            return;
        }
        Vector3 min = pos[0];
        Vector3 max = min;
        for (int i = 1; i < pos.Length; i++)
        {
            Vector3 p = pos[i];
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        Vector3 size = max - min;
        size.x = Mathf.Max(size.x, 1f);
        size.y = Mathf.Max(size.y, 1f);
        size.z = Mathf.Max(size.z, 1f);
        state.bounds = new Bounds((min + max) * 0.5f, size);
    }

    /// <summary>
    /// 根据 BuildChunks 结果，为每个实例计算其所属 chunk 的中心坐标（世界空间）。减小大坐标精度误差。
    /// 若 chunkBounds 为空（NoCull 路径），退化为用整体包围盒中心
    /// </summary>
    static Vector3[] BuildInstanceChunkCenters(EntryState state)
    {
        int count = state.instanceCount;
        var centers = new Vector3[count];

        if (state.chunkBounds == null || state.chunkBounds.Count == 0
            || state.chunkInstanceStarts == null || state.chunkInstanceIndices == null)
        {
            Vector3 fallback = state.bounds.center;
            for (int i = 0; i < count; i++)
            {
                centers[i] = fallback;
            }
            return centers;
        }

        int chunkCount = state.chunkBounds.Count;
        for (int c = 0; c < chunkCount; c++)
        {
            int start = state.chunkInstanceStarts[c];
            int end = state.chunkInstanceStarts[c + 1];
            Vector3 center = state.chunkBounds[c].center;
            for (int j = start; j < end; j++)
            {
                int instanceIdx = state.chunkInstanceIndices[j];
                if ((uint)instanceIdx < (uint)count)
                {
                    centers[instanceIdx] = center;
                }
            }
        }
        return centers;
    }

    /// <summary> 按 chunkSize 将实例划分到 Chunk，填充 chunkBounds、chunkInstanceStarts、chunkInstanceIndices。</summary>
    void BuildChunks(EntryState state)
    {
        state.chunkBounds.Clear();
        state.chunkInstanceStarts.Clear();
        state.chunkInstanceIndices.Clear();
        var pos = state.instanceWorldPositions;
        if (pos == null || pos.Length == 0)
        {
            return;
        }

        var chunkDict = new Dictionary<Vector2Int, List<int>>();
        float invChunk = 1f / Mathf.Max(0.01f, chunkSize);
        for (int i = 0; i < pos.Length; i++)
        {
            Vector3 p = pos[i];
            int cx = Mathf.FloorToInt(p.x * invChunk);
            int cz = Mathf.FloorToInt(p.z * invChunk);
            var key = new Vector2Int(cx, cz);
            if (!chunkDict.TryGetValue(key, out var list))
            {
                list = Common.ListPool<int>.Get();
                chunkDict[key] = list;
            }
            list.Add(i);
        }

        int offset = 0;
        foreach (var kv in chunkDict)
        {
            var indices = kv.Value;
            Vector3 min = pos[indices[0]];
            Vector3 max = min;
            for (int j = 1; j < indices.Count; j++)
            {
                Vector3 p = pos[indices[j]];
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Vector3 size = max - min;
            size.x = Mathf.Max(size.x, 0.1f);
            size.y = Mathf.Max(size.y, 0.1f);
            size.z = Mathf.Max(size.z, 0.1f);
            state.chunkBounds.Add(new Bounds((min + max) * 0.5f, size));
            state.chunkInstanceStarts.Add(offset);
            for (int j = 0; j < indices.Count; j++)
            {
                state.chunkInstanceIndices.Add(indices[j]);
            }
            offset += indices.Count;
        }
        state.chunkInstanceStarts.Add(offset);
        foreach (var list in chunkDict.Values)
        {
            Common.ListPool<int>.Release(list);
        }
    }

    /// <summary> 每帧更新视锥/距离剔除并绘制：根据 cullMode 走全量绘制或 UpdateCullingAndDraw。</summary>
    void Update()
    {
        if (_entryStates == null || _entryStates.Count == 0)
        {
            return;
        }

        Camera cam = cullCamera != null ? cullCamera : Camera.main;
        bool hasCullEntries = false;
        for (int i = 0; i < _entryStates.Count; i++)
        {
            if (_entryStates[i].effectiveCullMode != EMeshInstanceCullMode.NoCull)
            {
                hasCullEntries = true;
                break;
            }
        }
        bool doCulling = hasCullEntries && cam != null;

        if (doCulling)
        {
            // 与 MeshInstanceCull.compute 同一套平面约定：normal.xyz + w，点在内部时 dot(normal, point) + w > 0
            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);
            float margin = frustumExpandMargin > 0f ? frustumExpandMargin : 0f;
            for (int i = 0; i < 6; i++)
            {
                var p = _frustumPlanes[i];
                float w = p.distance + (i < 5 ? margin : 0f); // 0..4: Left,Right,Bottom,Top,Near 扩展
                _frustumPlanesVec4[i] = new Vector4(p.normal.x, p.normal.y, p.normal.z, w);
            }
            if (_planesNative.IsCreated)
            {
                for (int i = 0; i < 6; i++)
                {
                    var v = _frustumPlanesVec4[i];
                    _planesNative[i] = new float4(v.x, v.y, v.z, v.w);
                }
            }
        }

        // LOD 参数变化时统一刷新所有 entry PropertyBlock。
        if (_lodParamsDirty)
        {
            GetNormalizedShaderLodValues(lod0EndDistance, lod1EndDistance, maxCullDistance,
                out float lodN0, out float lodN1, out float lodNMax);
            for (int li = 0; li < _entryStates.Count; li++)
            {
                var ls = _entryStates[li];
                if (ls?.propertyBlock != null)
                {
                    ls.propertyBlock.SetFloat(s_LOD0End, lodN0);
                    ls.propertyBlock.SetFloat(s_LOD1End, lodN1);
                    ls.propertyBlock.SetFloat(s_MaxDistance, lodNMax);
                }
            }
            _lodParamsDirty = false;
        }

        for (int i = 0; i < _entryStates.Count; i++)
        {
            var state = _entryStates[i];
            if (state.counterAndArgsBuffer == null || state.mesh == null)
            {
                continue;
            }
            if (state.dataBackend == EMeshInstanceDataBackend.Texture)
            {
                if (state.materialTexFetch == null)
                {
                    continue;
                }
            }
            else if (state.materialStructuredFull == null)
            {
                continue;
            }

            if (doCulling && state.effectiveCullMode != EMeshInstanceCullMode.NoCull && state.chunkBounds != null && state.chunkBounds.Count > 0)
            {
                UpdateCullingAndDraw(state, cam);
            }
            else
            {
                DrawFull(state);
            }
        }
    }

    /// <summary> 根据 cullMode 执行 Chunk/实例剔除并绘制（PureGpu / PureCpu / CpuChunkComputeCull）。</summary>
    /// <param name="state">当前条目的状态。</param>
    /// <param name="cam">用于视锥与距离的相机。</param>
    void UpdateCullingAndDraw(EntryState state, Camera cam)
    {
        int chunkCount = state.chunkBounds.Count;
        EMeshInstanceCullMode effectiveCullMode = state.effectiveCullMode;

        if (effectiveCullMode == EMeshInstanceCullMode.PureGpu && state.chunkPackedMetaAndIndicesBuffer != null && SystemInfo.supportsComputeShaders && _kernelChunkAndInstanceCull >= 0)
        {
            UpdateCullingAndDrawGpuChunk(state, cam, chunkCount);
            return;
        }

        if (effectiveCullMode == EMeshInstanceCullMode.PureCpu)
        {
            UpdateCullingAndDrawPureCpu(state, cam, chunkCount);
            return;
        }

        if (effectiveCullMode == EMeshInstanceCullMode.CpuChunkComputeCull)
        {
            if (!state.chunkBoundsNative.IsCreated || !state.visibleFlagsNative.IsCreated || !_planesNative.IsCreated)
            {
                DrawFull(state);
                return;
            }
            Vector3 camPosV = cam.transform.position;
            float3 camPosJob = new float3(camPosV.x, camPosV.y, camPosV.z);
            var chunkVisJob = new ChunkVisibilityJob
            {
                chunkBounds = state.chunkBoundsNative,
                planes = _planesNative,
                cameraPos = camPosJob,
                maxDistance = maxCullDistance,
                visibleFlags = state.visibleFlagsNative
            };
            chunkVisJob.Schedule(chunkCount, 32).Complete();
        }

        state.candidateIndices.Clear();
        for (int c = 0; c < chunkCount; c++)
        {
            if (state.visibleFlagsNative[c] == 0)
            {
                continue;
            }
            int start = state.chunkInstanceStarts[c];
            int end = state.chunkInstanceStarts[c + 1];
            for (int k = start; k < end; k++)
            {
                state.candidateIndices.Add(state.chunkInstanceIndices[k]);
            }
        }

        int numCandidates = state.candidateIndices.Count;
        if (numCandidates == 0)
        {
            return;
        }

        if (effectiveCullMode == EMeshInstanceCullMode.CpuChunkComputeCull
            && (cullCompute == null || !SystemInfo.supportsComputeShaders || _kernelCull < 0 || state.candidateIndicesBuffer == null || state.visibleIndexBuffer == null))
        {
            DrawFull(state);
            return;
        }

        if (effectiveCullMode == EMeshInstanceCullMode.CpuChunkComputeCull && cullCompute != null && SystemInfo.supportsComputeShaders && _kernelCull >= 0)
        {
            state.candidateIndicesBuffer.SetData(state.candidateIndices);
            state.args[1] = 0;
            UploadCounterAndArgs(state);
            DispatchCull(state, cam);
        }

        state.propertyBlock.SetBuffer(s_MeshInstanceBuffer, state.instanceBuffer);
        state.propertyBlock.SetBuffer(s_MeshInstanceIndexBuffer, (ComputeBuffer)state.visibleIndexBuffer);
        if (!state.mpbTexturesCleared) { ClearMeshInstanceTexturesOnPropertyBlock(state.propertyBlock); state.mpbTexturesCleared = true; }
        Graphics.DrawMeshInstancedIndirect(
            state.mesh, state.subMeshIndex, state.materialStructuredCull, state.bounds, state.counterAndArgsBuffer,
            C_ArgsOffsetBytes, state.propertyBlock,
            castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off,
            true, gameObject.layer
        );
    }

    /// <summary> PureGpu 路径：Dispatch ChunkAndInstanceCull 后间接绘制。</summary>
    /// <param name="state">当前条目状态。</param>
    /// <param name="cam">剔除用相机。</param>
    /// <param name="chunkCount">Chunk 数量。</param>
    void UpdateCullingAndDrawGpuChunk(EntryState state, Camera cam, int chunkCount)
    {
        state.args[1] = 0;
        UploadCounterAndArgs(state);

        int k = _kernelChunkAndInstanceCull;
        cullCompute.SetBuffer(k, s_ChunkPackedMetaAndIndices, state.chunkPackedMetaAndIndicesBuffer);
        cullCompute.SetBuffer(k, s_MeshInstanceTrsBuffer, state.instanceBuffer);
        cullCompute.SetBuffer(k, s_VisibleIndexBuffer, state.visibleIndexBuffer);
        cullCompute.SetBuffer(k, s_CounterAndArgsBuffer, state.counterAndArgsBuffer);
        cullCompute.SetVectorArray(s_FrustumPlanes, _frustumPlanesVec4);
        cullCompute.SetVector(s_CameraPos, cam.transform.position);
        GetNormalizedShaderLodValues(
            lod0EndDistance,
            lod1EndDistance,
            maxCullDistance,
            out float normalizedLod0EndDistance,
            out float normalizedLod1EndDistance,
            out float normalizedMaxDistance);
        cullCompute.SetFloat(s_MaxDistance, normalizedMaxDistance);
        cullCompute.SetFloat(s_MaxDistanceSq, normalizedMaxDistance * normalizedMaxDistance); // [Issue11]
        cullCompute.SetFloat(s_LOD0End, normalizedLod0EndDistance);
        cullCompute.SetFloat(s_LOD1End, normalizedLod1EndDistance);
        cullCompute.SetFloat(s_LOD1Density, Mathf.Clamp01(lod1Density));
        cullCompute.SetFloat(s_LOD2Density, Mathf.Clamp01(lod2Density));
        cullCompute.SetInt(s_ChunkCount, chunkCount);

        cullCompute.Dispatch(k, (chunkCount + 255) / 256, 1, 1);

        state.propertyBlock.SetBuffer(s_MeshInstanceBuffer, state.instanceBuffer);
        state.propertyBlock.SetBuffer(s_MeshInstanceIndexBuffer, (ComputeBuffer)state.visibleIndexBuffer);
        if (!state.mpbTexturesCleared) { ClearMeshInstanceTexturesOnPropertyBlock(state.propertyBlock); state.mpbTexturesCleared = true; }
        Graphics.DrawMeshInstancedIndirect(
            state.mesh, state.subMeshIndex, state.materialStructuredCull, state.bounds, state.counterAndArgsBuffer,
            C_ArgsOffsetBytes, state.propertyBlock,
            castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off,
            true, gameObject.layer
        );
    }

    /// <summary> 全量绘制：关闭剔除 keyword，用全实例数写入 args 并 DrawMeshInstancedIndirect。</summary>
    /// <param name="state">当前条目状态。</param>
    void DrawStructuredVisible(EntryState state, Mesh mesh, Material material, int subMeshIndex, NativeArray<uint> visibleList, int instanceCount)
    {
        if (material == null || state.visibleIndexBuffer == null || state.counterAndArgsBuffer == null || state.propertyBlock == null)
        {
            return;
        }
        subMeshIndex = NormalizeSubMeshIndex(mesh, subMeshIndex);

        int copyCount = Mathf.Min(Mathf.Min(instanceCount, visibleList.Length), state.visibleIndexBuffer.count);
        if (copyCount <= 0)
        {
            return;
        }

        var slice = visibleList.GetSubArray(0, copyCount);
        state.visibleIndexBuffer.SetData(slice);
        SetupArgsForDraw(state, mesh, subMeshIndex, copyCount);
        UploadCounterAndArgs(state);
        state.propertyBlock.SetBuffer(s_MeshInstanceBuffer, state.instanceBuffer);
        state.propertyBlock.SetBuffer(s_MeshInstanceIndexBuffer, (ComputeBuffer)state.visibleIndexBuffer);
        if (!state.mpbTexturesCleared) { ClearMeshInstanceTexturesOnPropertyBlock(state.propertyBlock); state.mpbTexturesCleared = true; }
        Graphics.DrawMeshInstancedIndirect(
            mesh, subMeshIndex, material, state.bounds, state.counterAndArgsBuffer,
            C_ArgsOffsetBytes, state.propertyBlock,
            castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off,
            true, gameObject.layer
        );
    }

    void DrawFull(EntryState state)
    {
        Mesh mesh = state.mesh;
        int subMeshIndex = state.subMeshIndex;
        if (state.dataBackend == EMeshInstanceDataBackend.Texture)
        {
            if (state.materialTexFetch == null)
            {
                return;
            }
            DrawTextureFetch(state, mesh, state.materialTexFetch, subMeshIndex, state.instanceCount, default, false);
            return;
        }
        if (state.materialStructuredFull == null)
        {
            return;
        }
        subMeshIndex = NormalizeSubMeshIndex(mesh, subMeshIndex);

        state.propertyBlock.SetBuffer(s_MeshInstanceBuffer, state.instanceBuffer);
        state.propertyBlock.SetBuffer(s_MeshInstanceIndexBuffer, (ComputeBuffer)null);
        if (!state.mpbTexturesCleared) { ClearMeshInstanceTexturesOnPropertyBlock(state.propertyBlock); state.mpbTexturesCleared = true; }
        SetupArgsForDraw(state, mesh, subMeshIndex, state.instanceCount);
        UploadCounterAndArgs(state);
        var mode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, state.materialStructuredFull, state.bounds, state.counterAndArgsBuffer, C_ArgsOffsetBytes, state.propertyBlock, mode, true, gameObject.layer);
    }

    // ---------- PureCpu----------
    void AllocatePureCpuCullingBuffers(EntryState state, int chunkCount, int maxCandidates)
    {
        int n = state.instanceCount;
        state.chunkBoundsNative = new NativeArray<Bounds>(chunkCount, Allocator.Persistent);
        for (int c = 0; c < chunkCount; c++)
        {
            state.chunkBoundsNative[c] = state.chunkBounds[c];
        }

        state.pureCpuInstancePositions = new NativeArray<float3>(n, Allocator.Persistent);
        for (int i = 0; i < n; i++)
        {
            Vector3 wp = state.instanceWorldPositions[i];
            state.pureCpuInstancePositions[i] = new float3(wp.x, wp.y, wp.z);
        }
        state.pureCpuChunkStarts = new NativeArray<int>(chunkCount + 1, Allocator.Persistent);
        for (int c = 0; c <= chunkCount; c++)
        {
            state.pureCpuChunkStarts[c] = state.chunkInstanceStarts[c];
        }
        state.pureCpuFlatInstanceIndices = new NativeArray<int>(maxCandidates, Allocator.Persistent);
        for (int k = 0; k < maxCandidates; k++)
        {
            state.pureCpuFlatInstanceIndices[k] = state.chunkInstanceIndices[k];
        }
        state.pureCpuVisibleScratch = new NativeArray<uint>(maxCandidates, Allocator.Persistent);
        if (state.pureCpuCullCounter.IsCreated)
        {
            state.pureCpuCullCounter.Dispose();
        }
        state.pureCpuCullCounter = new NativeReference<int>(0, Allocator.Persistent);
    }

    void DisposePureCpuBuffers(EntryState state)
    {
        if (state.pureCpuCullCounter.IsCreated)
        {
            state.pureCpuCullCounter.Dispose();
        }
        if (state.pureCpuInstancePositions.IsCreated)
        {
            state.pureCpuInstancePositions.Dispose();
        }
        if (state.pureCpuChunkStarts.IsCreated)
        {
            state.pureCpuChunkStarts.Dispose();
        }
        if (state.pureCpuFlatInstanceIndices.IsCreated)
        {
            state.pureCpuFlatInstanceIndices.Dispose();
        }
        if (state.pureCpuVisibleScratch.IsCreated)
        {
            state.pureCpuVisibleScratch.Dispose();
        }
    }

    static bool IsPureCpuBuffersReady(EntryState state)
    {
        return state.chunkBoundsNative.IsCreated
            && state.pureCpuInstancePositions.IsCreated
            && state.pureCpuChunkStarts.IsCreated
            && state.pureCpuFlatInstanceIndices.IsCreated
            && state.pureCpuVisibleScratch.IsCreated
            && state.pureCpuCullCounter.IsCreated;
    }

    void UpdateCullingAndDrawPureCpu(EntryState state, Camera cam, int chunkCount)
    {
        if (!IsPureCpuBuffersReady(state))
        {
            DrawFull(state);
            return;
        }
        int totalVisible = RunPureCpuBurstCull(state, cam, chunkCount);
        if (totalVisible <= 0)
        {
            return;
        }
        if (state.dataBackend == EMeshInstanceDataBackend.Texture)
        {
            DrawTextureFetch(state, state.mesh, state.materialTexFetch, state.subMeshIndex, totalVisible, state.pureCpuVisibleScratch, true);
            return;
        }
        DrawStructuredVisible(state, state.mesh, state.materialStructuredCull, state.subMeshIndex, state.pureCpuVisibleScratch, totalVisible);
    }

    int RunPureCpuBurstCull(EntryState state, Camera cam, int chunkCount)
    {
        if (chunkCount <= 0
            || state.pureCpuChunkStarts.Length < chunkCount + 1
            || !_planesNative.IsCreated
            || state.pureCpuVisibleScratch.Length == 0)
        {
            return 0;
        }

        int scratchLen = state.pureCpuVisibleScratch.Length;
        int visibleCap = scratchLen;
        if (state.visibleIndexBuffer != null)
        {
            visibleCap = Mathf.Min(visibleCap, state.visibleIndexBuffer.count);
        }
        if (visibleCap <= 0)
        {
            return 0;
        }

        Vector3 camPosV = cam.transform.position;
        float3 camPos = new float3(camPosV.x, camPosV.y, camPosV.z);
        float maxDistSq = maxCullDistance * maxCullDistance;

        // 计算归一化 LOD 阈值并取平方，供 Burst Job 无 sqrt 确定 LOD 等级
        GetNormalizedShaderLodValues(
            lod0EndDistance, lod1EndDistance, maxCullDistance,
            out float nLod0, out float nLod1, out float _);
        float lod0Sq = nLod0 * nLod0;
        float lod1Sq = nLod1 * nLod1;

        state.pureCpuCullCounter.Value = 0;
        var cullJob = new MeshInstancePureCpuCullJob
        {
            chunkCount = chunkCount,
            chunkBounds = state.chunkBoundsNative,
            chunkStarts = state.pureCpuChunkStarts,
            flatInstanceIndices = state.pureCpuFlatInstanceIndices,
            instancePositions = state.pureCpuInstancePositions,
            planes = _planesNative,
            cameraPos = camPos,
            maxDistance = maxCullDistance,
            maxDistSq = maxDistSq,
            lod0EndSq = lod0Sq,
            lod1EndSq = lod1Sq,
            counter = state.pureCpuCullCounter,
            visibleOut = state.pureCpuVisibleScratch,
            visibleOutCapacity = visibleCap
        };
        cullJob.Schedule(chunkCount, 32).Complete();
        int totalVisible = state.pureCpuCullCounter.Value;
        if (totalVisible > visibleCap)
        {
            totalVisible = visibleCap;
        }
        return totalVisible;
    }

    /// <summary> CpuChunkComputeCull 路径：设置 Compute 缓冲与参数并 Dispatch 实例剔除 Kernel。</summary>
    /// <param name="state">当前条目状态。</param>
    /// <param name="cam">剔除用相机。</param>
    void DispatchCull(EntryState state, Camera cam)
    {
        uint numCandidates = (uint)state.candidateIndices.Count;
        int k = _kernelCull;
        cullCompute.SetBuffer(k, s_MeshInstanceTrsBuffer, state.instanceBuffer);
        cullCompute.SetBuffer(k, s_CandidateIndices, state.candidateIndicesBuffer);
        cullCompute.SetBuffer(k, s_VisibleIndexBuffer, state.visibleIndexBuffer);
        cullCompute.SetBuffer(k, s_CounterAndArgsBuffer, state.counterAndArgsBuffer);
        cullCompute.SetVectorArray(s_FrustumPlanes, _frustumPlanesVec4);
        cullCompute.SetVector(s_CameraPos, cam.transform.position);
        GetNormalizedShaderLodValues(
            lod0EndDistance,
            lod1EndDistance,
            maxCullDistance,
            out float normalizedLod0EndDistance,
            out float normalizedLod1EndDistance,
            out float normalizedMaxDistance);
        cullCompute.SetFloat(s_MaxDistance, normalizedMaxDistance);
        cullCompute.SetFloat(s_MaxDistanceSq, normalizedMaxDistance * normalizedMaxDistance); // [Issue11]
        cullCompute.SetFloat(s_LOD0End, normalizedLod0EndDistance);
        cullCompute.SetFloat(s_LOD1End, normalizedLod1EndDistance);
        cullCompute.SetFloat(s_LOD1Density, Mathf.Clamp01(lod1Density));
        cullCompute.SetFloat(s_LOD2Density, Mathf.Clamp01(lod2Density));
        cullCompute.Dispatch(k, (int)(numCandidates + 255) / 256, 1, 1);
    }

    /// <summary> 释放所有 Entry 的 GPU/Native 缓冲并清空 _entryStates；OnDisable 时调用。</summary>
    void ReleaseBuffers()
    {
        if (_entryStates != null)
        {
            foreach (var state in _entryStates)
            {
                DestroyDrawMaterials(state);
                if (state.instanceBuffer != null) { state.instanceBuffer.Release(); state.instanceBuffer = null; }
                if (state.counterAndArgsBuffer != null) { state.counterAndArgsBuffer.Release(); state.counterAndArgsBuffer = null; }
                if (state.candidateIndicesBuffer != null) { state.candidateIndicesBuffer.Release(); state.candidateIndicesBuffer = null; }
                if (state.visibleIndexBuffer != null) { state.visibleIndexBuffer.Release(); state.visibleIndexBuffer = null; }
                if (state.chunkBoundsNative.IsCreated) { state.chunkBoundsNative.Dispose(); }
                if (state.visibleFlagsNative.IsCreated) { state.visibleFlagsNative.Dispose(); }
                if (state.chunkPackedMetaAndIndicesBuffer != null) { state.chunkPackedMetaAndIndicesBuffer.Release(); state.chunkPackedMetaAndIndicesBuffer = null; }
                if (state.instanceTextureA != null) { DestroyTexture(state.instanceTextureA); state.instanceTextureA = null; }
                if (state.instanceTextureB != null) { DestroyTexture(state.instanceTextureB); state.instanceTextureB = null; }
                if (state.instanceTextureC != null) { DestroyTexture(state.instanceTextureC); state.instanceTextureC = null; }
                if (state.instanceVisibleIndexTexture != null) { DestroyTexture(state.instanceVisibleIndexTexture); state.instanceVisibleIndexTexture = null; }
                if (state.chunkBounds != null) { Common.ListPool<Bounds>.Release(state.chunkBounds); state.chunkBounds = null; }
                if (state.chunkInstanceStarts != null) { Common.ListPool<int>.Release(state.chunkInstanceStarts); state.chunkInstanceStarts = null; }
                if (state.chunkInstanceIndices != null) { Common.ListPool<int>.Release(state.chunkInstanceIndices); state.chunkInstanceIndices = null; }
                if (state.candidateIndices != null) { Common.ListPool<int>.Release(state.candidateIndices); state.candidateIndices = null; }
                DisposePureCpuBuffers(state);
            }
            _entryStates.Clear();
        }
        if (_planesNative.IsCreated) { _planesNative.Dispose(); }
        _materialOcclusionStates.Clear();
        _kernelCull = -1;
        _kernelChunkAndInstanceCull = -1;
    }
}
