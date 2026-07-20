/*******************************************************************************
 * File: MeshInstanceData.cs
 * Author: fan.shi
 * Date: 2026-03-16
 * Description: Mesh 实例数据: 4 字节 int32 count + MeshInstanceTrsStructuredGpu(32B);
 ******************************************************************************/

using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

// TRS 布局
[StructLayout(LayoutKind.Sequential)]
public struct MeshInstanceTrsStructuredGpu
{
    public float4 PositionScale;
    public float4 Rotation;
}

public struct MeshInstanceLoadResult
{
    public int Count;
    public MeshInstanceTrsStructuredGpu[] TrsStructured;
}

/// <summary>
/// Mesh 实例纯二进制序列化。
/// 格式: 前 4 字节 int32 count, 后跟 MeshInstanceTrsStructuredGpu[count](各 32 字节)
/// </summary>
public static class MeshInstanceData
{
    public const int BytesPerTrsStructured = 32;

    const float C_DECOMPOSE_TOLERANCE = 1e-3f;
    const float C_UNIFORM_SCALE_EPSILON = 1e-3f;

    /// <summary> TRS 载荷。空数组写 count=0 的 4 字节前缀。 </summary>
    public static byte[] ToBytesTrsStructured(MeshInstanceTrsStructuredGpu[] trs)
    {
        int count = trs != null ? trs.Length : 0;
        byte[] buffer = new byte[sizeof(int) + count * BytesPerTrsStructured];
        Buffer.BlockCopy(BitConverter.GetBytes(count), 0, buffer, 0, sizeof(int));
        if (count == 0)
        {
            return buffer;
        }
        GCHandle h = GCHandle.Alloc(trs, GCHandleType.Pinned);
        try
        {
            Marshal.Copy(h.AddrOfPinnedObject(), buffer, sizeof(int), count * BytesPerTrsStructured);
        }
        finally
        {
            h.Free();
        }
        return buffer;
    }

    /// <summary> 解析 int32 count + 32B/实例 TRS。 </summary>
    public static bool TryLoad(byte[] buffer, out MeshInstanceLoadResult result)
    {
        result = default;
        if (buffer == null || buffer.Length < sizeof(int))
        {
            return false;
        }

        int count = BitConverter.ToInt32(buffer, 0);
        if (count < 0)
        {
            return false;
        }
        if (count == 0)
        {
            result.Count = 0;
            result.TrsStructured = Array.Empty<MeshInstanceTrsStructuredGpu>();
            return true;
        }
        int payloadBytes = buffer.Length - sizeof(int);
        if (payloadBytes != count * BytesPerTrsStructured)
        {
            return false;
        }
        var trs = new MeshInstanceTrsStructuredGpu[count];
        GCHandle h = GCHandle.Alloc(trs, GCHandleType.Pinned);
        try
        {
            Marshal.Copy(buffer, sizeof(int), h.AddrOfPinnedObject(), count * BytesPerTrsStructured);
        }
        finally
        {
            h.Free();
        }
        result.Count = count;
        result.TrsStructured = trs;
        return true;
    }

    /// <summary> 将世界矩阵分解为 TRS;若剪切/非正交过强则返回 false。 </summary>
    public static bool TryDecomposeToTrs(Matrix4x4 m, out float3 pos, out quaternion q, out float3 scale)
    {
        var col3 = m.GetColumn(3);
        pos = new float3(col3.x, col3.y, col3.z);
        var v0 = m.GetColumn(0);
        var v1 = m.GetColumn(1);
        var v2 = m.GetColumn(2);
        float3 c0 = new float3(v0.x, v0.y, v0.z);
        float3 c1 = new float3(v1.x, v1.y, v1.z);
        float3 c2 = new float3(v2.x, v2.y, v2.z);
        float sx = math.length(c0);
        float sy = math.length(c1);
        float sz = math.length(c2);
        const float eps = 1e-5f;
        if (sx < eps || sy < eps || sz < eps)
        {
            q = quaternion.identity;
            scale = float3.zero;
            return false;
        }
        float3 r0 = c0 / sx;
        float3 r1 = c1 / sy;
        float3 r2 = c2 / sz;
        if (math.abs(math.dot(r0, r1)) > 0.01f
            || math.abs(math.dot(r1, r2)) > 0.01f
            || math.abs(math.dot(r0, r2)) > 0.01f)
        {
            q = quaternion.identity;
            scale = float3.zero;
            return false;
        }
        scale = new float3(sx, sy, sz);
        float3x3 rm = new float3x3(r0, r1, r2);
        q = new quaternion(rm);
        var uq = new Quaternion(q.value.x, q.value.y, q.value.z, q.value.w);
        var rebuilt = Matrix4x4.TRS(new Vector3(pos.x, pos.y, pos.z), uq, new Vector3(scale.x, scale.y, scale.z));
        float maxErr = 0f;
        for (int col = 0; col < 4; col++)
        {
            var a = m.GetColumn(col);
            var b = rebuilt.GetColumn(col);
            maxErr = math.max(maxErr, math.abs(a.x - b.x));
            maxErr = math.max(maxErr, math.abs(a.y - b.y));
            maxErr = math.max(maxErr, math.abs(a.z - b.z));
            maxErr = math.max(maxErr, math.abs(a.w - b.w));
        }
        if (maxErr > C_DECOMPOSE_TOLERANCE)
        {
            return false;
        }
        return true;
    }

    public static MeshInstanceTrsStructuredGpu ToStructuredGpu(float3 pos, quaternion q, float3 scale)
    {
        return new MeshInstanceTrsStructuredGpu
        {
            PositionScale = new float4(pos, scale.x),
            Rotation = new float4(q.value.x, q.value.y, q.value.z, q.value.w)
        };
    }

    /// <summary> 整组矩阵尝试转 TRS GPU 数组;任一条失败返回 false。 </summary>
    public static bool TryMatricesToTrsStructured(Matrix4x4[] matrices, out MeshInstanceTrsStructuredGpu[] structured)
    {
        structured = null;
        if (matrices == null || matrices.Length == 0)
        {
            return false;
        }
        var arr = new MeshInstanceTrsStructuredGpu[matrices.Length];
        for (int i = 0; i < matrices.Length; i++)
        {
            if (!TryDecomposeToTrs(matrices[i], out float3 pos, out quaternion q, out float3 scale))
            {
                return false;
            }
            if (math.max(math.abs(scale.x - scale.y), math.abs(scale.x - scale.z)) > C_UNIFORM_SCALE_EPSILON)
            {
                return false;
            }
            arr[i] = ToStructuredGpu(pos, q, scale);
        }
        structured = arr;
        return true;
    }
}
