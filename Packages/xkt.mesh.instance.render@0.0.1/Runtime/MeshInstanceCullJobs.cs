/*******************************************************************************
 * File: MeshInstanceCullJobs.cs
 * Author: fan.shi
 * Date: 2026-03-18
 * Description: MeshInstance 剔除用 Burst Job  仅Chunk剔除和Chunk+视锥剔除)
 ******************************************************************************/
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

/// <summary> Chunk 视锥+距离可见性,输出 visibleFlags。与 MeshInstanceCull.compute 平面顺序一致。 </summary>
[BurstCompile]
public struct ChunkVisibilityJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Bounds> chunkBounds;
    [ReadOnly] public NativeArray<float4> planes;
    public float3 cameraPos;
    public float maxDistance;
    [WriteOnly] public NativeArray<int> visibleFlags;

    static readonly int[] s_PlaneOrder = { 4, 5, 0, 1, 2, 3 }; // Near, Far, Left, Right, Bottom, Top

    /// <summary> 计算单个 Chunk 是否在视锥内且在最大距离内,写入 visibleFlags。 </summary>
    /// <param name="index"> Chunk 下标。 </param>
    public void Execute(int index)
    {
        Bounds b = chunkBounds[index];
        float3 center = new float3(b.center.x, b.center.y, b.center.z);
        float3 extents = new float3(b.extents.x, b.extents.y, b.extents.z);
        float dist = math.distance(cameraPos, center);
        if (dist > maxDistance + math.length(extents))
        {
            visibleFlags[index] = 0;
            return;
        }
        float3 min = center - extents;
        float3 max = center + extents;
        for (int i = 0; i < 6; i++)
        {
            float4 p = planes[s_PlaneOrder[i]];
            float nx = p.x, ny = p.y, nz = p.z, d = p.w;
            float vx = nx >= 0 ? max.x : min.x;
            float vy = ny >= 0 ? max.y : min.y;
            float vz = nz >= 0 ? max.z : min.z;
            if (nx * vx + ny * vy + nz * vz + d < 0f)
            {
                visibleFlags[index] = 0;
                return;
            }
        }
        visibleFlags[index] = 1;
    }
}

/// <summary> PureCpu 单 Job 并行:按 Chunk 并行,内联 Chunk 可见性 + 实例视锥/距离剔除,原子计数器写入 visibleOut。 </summary>
[BurstCompile]
public struct MeshInstancePureCpuCullJob : IJobParallelFor
{
    public int chunkCount;
    [ReadOnly] public NativeArray<Bounds> chunkBounds;
    [ReadOnly] public NativeArray<int> chunkStarts;
    [ReadOnly] public NativeArray<int> flatInstanceIndices;
    [ReadOnly] public NativeArray<float3> instancePositions;
    [ReadOnly] public NativeArray<float4> planes;
    public float3 cameraPos;
    public float maxDistance;
    public float maxDistSq;
    // LOD 阈值平方
    public float lod0EndSq;
    public float lod1EndSq;
    [NativeDisableParallelForRestriction]
    [NativeDisableUnsafePtrRestriction]
    public NativeReference<int> counter;
    [NativeDisableParallelForRestriction]
    [WriteOnly] public NativeArray<uint> visibleOut;
    /// <summary> 与 <see cref="visibleOut"/> 一致的可写入槽位数;同时应 ≤ GPU VisibleIndexBuffer 元素数,避免主线程上传越界。 </summary>
    public int visibleOutCapacity;

    static readonly int[] s_PlaneOrder = { 4, 5, 0, 1, 2, 3 };

    /// <summary> 对单个 Chunk 做视锥与距离粗剔,再对其中实例做点视锥与距离剔除,可见下标写入 visibleOut。 </summary>
    /// <param name="c"> Chunk 下标。 </param>
    public void Execute(int c)
    {
        Bounds b = chunkBounds[c];
        float3 center = new float3(b.center.x, b.center.y, b.center.z);
        float3 extents = new float3(b.extents.x, b.extents.y, b.extents.z);
        float dist = math.distance(cameraPos, center);
        if (dist > maxDistance + math.length(extents))
        {
            return;
        }
        float3 min = center - extents;
        float3 max = center + extents;
        for (int i = 0; i < 6; i++)
        {
            float4 p = planes[s_PlaneOrder[i]];
            float nx = p.x, ny = p.y, nz = p.z, d = p.w;
            float vx = nx >= 0 ? max.x : min.x;
            float vy = ny >= 0 ? max.y : min.y;
            float vz = nz >= 0 ? max.z : min.z;
            if (nx * vx + ny * vy + nz * vz + d < 0f)
            {
                return;
            }
        }

        int start = chunkStarts[c];
        int end = chunkStarts[c + 1];
        unsafe
        {
            var atomicCounter = new UnsafeAtomicCounter32(counter.GetUnsafePtr());
            int cap = visibleOutCapacity;
            if (cap <= 0)
            {
                return;
            }
            for (int k = start; k < end; k++)
            {
                int idx = flatInstanceIndices[k];
                float3 pos = instancePositions[idx];
                float dx = pos.x - cameraPos.x, dy = pos.y - cameraPos.y, dz = pos.z - cameraPos.z;
                float distSq = dx * dx + dy * dy + dz * dz;
                if (distSq > maxDistSq)
                {
                    continue;
                }
                bool inFrustum = true;
                for (int i = 0; i < 6; i++)
                {
                    // 与 Chunk 视锥检测、compute 平面顺序一致(Near, Far, Left, Right, Bottom, Top),利于 early-out。
                    float4 pl = planes[s_PlaneOrder[i]];
                    if (pl.x * pos.x + pl.y * pos.y + pl.z * pos.z + pl.w < 0f)
                    {
                        inFrustum = false;
                        break;
                    }
                }
                if (!inFrustum)
                {
                    continue;
                }
                // AddSat:计数不超过 cap,返回加之前的旧值作为槽位;已满时返回 >= cap,禁止写入避免 scratch 越界。
                int slot = atomicCounter.AddSat(1, cap);
                if (slot >= cap)
                {
                    continue;
                }
                // 计算 LOD 等级（平方距离比较，无需 sqrt）并打包进高 2 位，与 compute shader 编码一致
                uint lod;
                if (distSq < lod0EndSq) lod = 0u;
                else if (distSq < lod1EndSq) lod = 1u;
                else lod = 2u;
                visibleOut[slot] = (lod << 30) | (uint)idx;
            }
        }
    }
}
