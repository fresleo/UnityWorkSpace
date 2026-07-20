/*******************************************************************************
 * File: MeshInstanceRenderer.Types.cs
 * Author: fan.shi
 * Date: 2026-03-27
 * Description: MeshInstanceRenderer 使用的枚举与嵌套数据结构
 ******************************************************************************/

using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

/// <summary> 剔除+渲染流程 </summary>
public enum EMeshInstanceCullMode
{
    NoCull,                // 不剔除
    PureGpu,               // 纯 GPU：Chunk + 实例剔除（ChunkAndInstanceCull）
    CpuChunkComputeCull,   // CPU Chunk 可见性 + Compute 实例剔除
    PureCpu                // 纯 CPU：Chunk 可见性 + CPU 实例剔除（移动端/无 Compute）
}

/// <summary> 数据传递方式 </summary>
public enum EMeshInstanceDataBackend
{
    Auto,
    StructuredBuffer,
    /// <summary> 实例 TRS 经纹理采样读取 </summary>
    Texture
}

public partial class MeshInstanceRenderer
{
    static readonly int s_MeshInstanceBuffer = Shader.PropertyToID("_MeshInstanceBuffer");
    static readonly int s_MeshInstanceTrsBuffer = Shader.PropertyToID("_MeshInstanceTrsBuffer");
    static readonly int s_MeshInstanceIndexBuffer = Shader.PropertyToID("_MeshInstanceIndexBuffer");
    static readonly int s_CandidateIndices = Shader.PropertyToID("_CandidateIndices");
    static readonly int s_VisibleIndexBuffer = Shader.PropertyToID("_VisibleIndexBuffer");
    static readonly int s_CounterAndArgsBuffer = Shader.PropertyToID("_CounterAndArgsBuffer");
    static readonly int s_FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
    static readonly int s_CameraPos = Shader.PropertyToID("_CameraPos");
    static readonly int s_LOD1Density = Shader.PropertyToID("_LOD1Density");
    static readonly int s_LOD2Density = Shader.PropertyToID("_LOD2Density");
    static readonly int s_ChunkPackedMetaAndIndices = Shader.PropertyToID("_ChunkPackedMetaAndIndices");
    static readonly int s_ChunkCount = Shader.PropertyToID("_ChunkCount");
    // 预计算最大距离平方
    static readonly int s_MaxDistanceSq = Shader.PropertyToID("_MaxDistanceSq");

    const string C_KeywordMeshInstanceCull = "_MESH_INSTANCE_CULL_ON";

    const int C_ArgsOffsetBytes = 4;
    static readonly int C_InstanceBufferStrideTrs = MeshInstanceData.BytesPerTrsStructured;

    [StructLayout(LayoutKind.Explicit)]
    struct FloatUIntUnion
    {
        [FieldOffset(0)] public float FloatValue;
        [FieldOffset(0)] public uint UIntValue;
    }

    [System.Serializable]
    public class MeshInstanceEntry
    {
        public Mesh mesh;
        public Material material;
        [Tooltip("AssetSystem 完整逻辑路径，如 assetfiles/mesh_instance/场景名_xxx_0.bytes")]
        public string bakedDataAssetPath;
        public int subMeshIndex;
    }

    partial class EntryState
    {
        // ---------- 通用（与后端 / 剔除模式无关）----------
        public Mesh mesh;
        /// <summary> 该 entry 使用的 Mesh 子网格索引（传给 DrawMeshInstancedIndirect）。 </summary>
        public int subMeshIndex;
        /// <summary> 实例总数 </summary>
        public int instanceCount;
        /// <summary> 实例数据后端：StructuredBuffer 或 Texture。 </summary>
        public EMeshInstanceDataBackend dataBackend;
        /// <summary> 本 entry 实际生效的剔除模式。 </summary>
        public EMeshInstanceCullMode effectiveCullMode;
        /// <summary> 每个实例世界位置，用于整体 Bounds、空间 Chunk 划分与 PureCpu Burst 剔除。 </summary>
        public Vector3[] instanceWorldPositions;
        /// <summary> 所有实例的世界空间轴对齐包围盒。 </summary>
        public Bounds bounds;
        /// <summary> 当前 entry 专用材质属性块。 </summary>
        public MaterialPropertyBlock propertyBlock;
        public bool mpbTexturesCleared;

        // ---------- StructuredBuffer 后端 ----------
        /// <summary> StructuredBuffer 路径:剔除绘制用(含 _MESH_INSTANCE_CULL_ON)。 </summary>
        public Material materialStructuredCull;
        /// <summary> StructuredBuffer 路径:全量绘制用(无 cull/tex-fetch keyword)。 </summary>
        public Material materialStructuredFull;
        /// <summary> 32B TRS/实例 GPU 缓冲；顶点着色器绑 _MeshInstanceBuffer，Compute 剔除绑 _MeshInstanceTrsBuffer。 </summary>
        public ComputeBuffer instanceBuffer;
        /// <summary> 剔除后可见实例在原始实例列表中的索引缓冲，对应着色器 _MeshInstanceIndexBuffer。 </summary>
        public ComputeBuffer visibleIndexBuffer;

        // ---------- TextureFetch 后端（_MESH_INSTANCE_TEX_FETCH_ON）----------
        /// <summary> Texture 后端路径绘制用。 </summary>
        public Material materialTexFetch;
        /// <summary> TextureFetch 路径下 CPU 侧 32B TRS 源数据，用于写入 instanceTextureA/B。 </summary>
        public MeshInstanceTrsStructuredGpu[] instanceTrsData;
        /// <summary> TextureFetch：RGBA16F 纹理 A，存实例 localPosition.xyz（相对 chunk 中心）+ uniform scale（w）。 </summary>
        public Texture2D instanceTextureA;
        /// <summary> TextureFetch：RGBA16F 纹理 B，存实例旋转四元数 xyzw。 </summary>
        public Texture2D instanceTextureB;
        /// <summary> TextureFetch：RGBAFloat 纹理 C，存每个实例所属 chunk 的中心世界坐标（xyz），float32 以消除大坐标精度误差。 </summary>
        public Texture2D instanceTextureC;
        /// <summary> 待上传到 instanceTextureA 的 RGBAHalf 像素数据（每像素 4×f16，位置已转为本地坐标）。 </summary>
        public ushort[] instanceTextureAData;
        /// <summary> 待上传到 instanceTextureB 的 RGBAHalf 像素数据（每像素 4×f16）。 </summary>
        public ushort[] instanceTextureBData;
        /// <summary> 待上传到 instanceTextureC 的 RGBAFloat 像素数据（每像素 4×f32，存 chunkCenter.xyz + 0）。 </summary>
        public float[] instanceTextureCData;
        /// <summary> TextureFetch：每个实例对应的 chunk 中心世界坐标，用于写入 TexC（Build 阶段预计算，上传后可释放）。 </summary>
        public Vector3[] instanceChunkCentersForTex;
        /// <summary> 实例纹理当前逻辑宽度（实例按行排列时的列数）。 </summary>
        public int instanceTextureWidth;
        /// <summary> 实例纹理当前逻辑高度（行数）。 </summary>
        public int instanceTextureHeight;
        /// <summary> 实例纹理可容纳的最大实例数（宽×高）。 </summary>
        public int instanceTextureCapacity;
        /// <summary> 是否已将 instanceTextureA/B 的 CPU 数据完整 SetPixels/Apply 到 GPU。 </summary>
        public bool instanceTextureFullDataUploaded;
        /// <summary> TextureFetch+剔除时：RGBA8 纹理，每像素 4 字节打包 uint 可见实例原始索引。</summary>
        public Texture2D instanceVisibleIndexTexture;
        /// <summary> 可见索引纹理宽度。 </summary>
        public int instanceVisibleIndexTextureWidth;
        /// <summary> 可见索引纹理高度。 </summary>
        public int instanceVisibleIndexTextureHeight;
        /// <summary> 可见索引纹理可容纳的最大条目数（宽×高）。 </summary>
        public int instanceVisibleIndexTextureCapacity;

        // ---------- Indirect 绘制（Structured / Texture 共用）----------
        /// <summary> GPU 缓冲：布局与 Compute 中 _CounterAndArgsBuffer 一致；[0] 为可见计数，[1..5] 为 DrawMeshInstancedIndirect 的 args。 </summary>
        public ComputeBuffer counterAndArgsBuffer;
        /// <summary> CPU 侧缓存的 DrawIndirect 五元组（indexCount, instanceCount, startIndex, baseVertex, startInstance）。 </summary>
        public uint[] args = new uint[5];

        // ---------- Chunk 划分与 CPU Chunk 可见性（凡启用剔除时；多剔除模式共享）----------
        /// <summary> 按 chunkSize 划分的每个 Chunk 世界空间 AABB 列表。 </summary>
        public List<Bounds> chunkBounds;
        /// <summary> 每个 Chunk 在 chunkInstanceIndices 扁平表中的起始下标；长度为 chunkCount+1，末元素为总索引数。 </summary>
        public List<int> chunkInstanceStarts;
        /// <summary> 按 Chunk 顺序拼接的实例索引列表（指向原始实例下标）。 </summary>
        public List<int> chunkInstanceIndices;
        /// <summary> chunkBounds 的 Native 副本，供 Burst/Job 只读访问。 </summary>
        public NativeArray<Bounds> chunkBoundsNative;
        /// <summary> ChunkVisibilityJob 输出：每个 Chunk 是否视锥/距离内可见（1/0）。 </summary>
        public NativeArray<int> visibleFlagsNative;

        // ---------- PureGpu----------
        /// <summary> 合并 bounds + starts + 扁平 indices 为单个 buffer，对应 compute 中 _ChunkPackedMetaAndIndices（减少 SSBO 绑定数）。 </summary>
        public ComputeBuffer chunkPackedMetaAndIndicesBuffer;

        // ---------- CpuChunkComputeCull（CPU Chunk + Compute 实例剔除）----------
        /// <summary> 当前帧写入 GPU _CandidateIndices 的候选实例索引列表（CPU 侧）。 </summary>
        public List<int> candidateIndices;
        /// <summary> 候选实例索引的 GPU 缓冲，对应 _CandidateIndices。 </summary>
        public ComputeBuffer candidateIndicesBuffer;

        // ---------- PureCpu----------
        public NativeArray<float3> pureCpuInstancePositions;
        public NativeArray<int> pureCpuChunkStarts;
        public NativeArray<int> pureCpuFlatInstanceIndices;
        public NativeArray<uint> pureCpuVisibleScratch;
        /// <summary> <see cref="MeshInstancePureCpuCullJob"/> 原子计数输出。Persistent 复用，避免每帧 TempJob 分配。</summary>
        public NativeReference<int> pureCpuCullCounter;
    }
}
