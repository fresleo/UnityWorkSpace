/*******************************************************************************
 * File: MeshInstanceRenderer.ShaderLod.cs
 * Author: fan.shi
 * Date: 2026-03-27
 * Description: shader LOD 参数
 ******************************************************************************/

using UnityEngine;

public partial class MeshInstanceRenderer
{
    static readonly int s_MaxDistance = Shader.PropertyToID("_MaxDistance");
    static readonly int s_LOD0End = Shader.PropertyToID("_LOD0End");
    static readonly int s_LOD1End = Shader.PropertyToID("_LOD1End");

    /// <summary>
    /// 将 LOD 距离归一化为单调递增顺序。
    /// </summary>
    /// <param name="lod0EndDistance">LOD0 结束距离。</param>
    /// <param name="lod1EndDistance">LOD1 结束距离。</param>
    /// <param name="maxDistance">最大距离。</param>
    public static void NormalizeShaderLodDistances(
        ref float lod0EndDistance,
        ref float lod1EndDistance,
        ref float maxDistance)
    {
        lod0EndDistance = Mathf.Max(0f, lod0EndDistance);
        lod1EndDistance = Mathf.Max(lod0EndDistance, lod1EndDistance);
        maxDistance = Mathf.Max(lod1EndDistance, maxDistance);
    }


    /// <summary>
    /// 在不修改源输入的前提下，获取经 <see cref="NormalizeShaderLodDistances"/> 归一化后的 Shader LOD 距离。
    /// </summary>
    /// <param name="lod0EndDistance">LOD0 结束距离（输入）。</param>
    /// <param name="lod1EndDistance">LOD1 结束距离（输入）。</param>
    /// <param name="maxDistance">最大距离（输入）。</param>
    /// <param name="normalizedLod0EndDistance">归一化后的 LOD0 结束距离。</param>
    /// <param name="normalizedLod1EndDistance">归一化后的 LOD1 结束距离。</param>
    /// <param name="normalizedMaxDistance">归一化后的最大距离。</param>
    public static void GetNormalizedShaderLodValues(
        float lod0EndDistance,
        float lod1EndDistance,
        float maxDistance,
        out float normalizedLod0EndDistance,
        out float normalizedLod1EndDistance,
        out float normalizedMaxDistance)
    {
        normalizedLod0EndDistance = lod0EndDistance;
        normalizedLod1EndDistance = lod1EndDistance;
        normalizedMaxDistance = maxDistance;
        NormalizeShaderLodDistances(
            ref normalizedLod0EndDistance,
            ref normalizedLod1EndDistance,
            ref normalizedMaxDistance);
    }

    /// <summary>
    /// 仅写入运行时批准的 shader LOD 参数。
    /// </summary>
    /// <param name="propertyBlock">目标属性块。</param>
    /// <param name="lod0EndDistance">LOD0 结束距离。</param>
    /// <param name="lod1EndDistance">LOD1 结束距离。</param>
    /// <param name="maxDistance">最大距离。</param>
    public static void ApplyShaderLodParameters(
        MaterialPropertyBlock propertyBlock,
        float lod0EndDistance,
        float lod1EndDistance,
        float maxDistance)
    {
        if (propertyBlock == null)
        {
            return;
        }

        GetNormalizedShaderLodValues(
            lod0EndDistance,
            lod1EndDistance,
            maxDistance,
            out float normalizedLod0EndDistance,
            out float normalizedLod1EndDistance,
            out float normalizedMaxDistance);
        propertyBlock.SetFloat(s_LOD0End, normalizedLod0EndDistance);
        propertyBlock.SetFloat(s_LOD1End, normalizedLod1EndDistance);
        propertyBlock.SetFloat(s_MaxDistance, normalizedMaxDistance);
    }
}
