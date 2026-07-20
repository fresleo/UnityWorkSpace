/*******************************************************************************
 * File: MeshInstanceRenderer.Occlusion.cs
 * Author: fan.shi
 * Date: 2026-03-27
 * Description:  Occlusion半透相关逻辑 Alpha/Dither
 * Notice:
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class MeshInstanceRenderer
{
    /// <summary>
    /// 遮挡时的半透明表现模式：屏幕空间抖动或 Alpha 混合。
    /// </summary>
    public enum ETranslucencyMode
    {
        None,
        Dither,
        Alpha
    }

    readonly Dictionary<Material, MaterialOcclusionState> _materialOcclusionStates = new Dictionary<Material, MaterialOcclusionState>();
    float _occlusionDitherIntensity;
    float _occlusionDitherSize = 1f;
    float _occlusionDitherAlpha = 1f;
    int _occlusionDitherWithMatrix;
    ETranslucencyMode _translucencyMode = ETranslucencyMode.None;

    static readonly int s_DitherIntensity = Shader.PropertyToID("_DitherIntensity");
    static readonly int s_DitherSize = Shader.PropertyToID("_DitherSize");
    static readonly int s_DitherWithMatrix = Shader.PropertyToID("_DitherWithMatrix");
    static readonly int s_DitherAlpha = Shader.PropertyToID("_DitherAlpha");
    static readonly int s_SrcBlend = Shader.PropertyToID("_SrcBlend");
    static readonly int s_DstBlend = Shader.PropertyToID("_DstBlend");
    static readonly int s_SrcBlendAlpha = Shader.PropertyToID("_SrcBlendAlpha");
    static readonly int s_DstBlendAlpha = Shader.PropertyToID("_DstBlendAlpha");

    const string C_KeywordDitherOn = "_DITHER_ON";
    const int C_AlphaRenderQueue = 2501;
    const int C_CullOff = 0;

    /// <summary>
    /// 记录材质在应用 Occlusion 前的原始状态，用于恢复。
    /// </summary>
    sealed class MaterialOcclusionState
    {
        public int renderQueue;
        public bool hasSrcBlend;
        public float srcBlend;
        public bool hasDstBlend;
        public float dstBlend;
        public bool hasSrcBlendAlpha;
        public float srcBlendAlpha;
        public bool hasDstBlendAlpha;
        public float dstBlendAlpha;
        public bool hasDitherSize;
        public float ditherSize;
        public bool hasDitherWithMatrix;
        public float ditherWithMatrix;
    }

    /// <summary>
    /// 按遮挡需求写入混合与剔除：可选 Alpha 混合预设或恢复缓存值。
    /// </summary>
    /// <param name="material">目标材质。</param>
    /// <param name="o">原始状态缓存。</param>
    /// <param name="ditherAlphaBlendPreset">为 true 时写入 SrcAlpha/OneMinusSrcAlpha 等预设。</param>
    void ApplyOcclusionBlendCull(Material material, MaterialOcclusionState o)
    {
        if (_translucencyMode == ETranslucencyMode.Alpha)
        {
            if (o.hasSrcBlend)
            {
                material.SetFloat(s_SrcBlend, (int)BlendMode.SrcAlpha);
            }
            if (o.hasDstBlend)
            {
                material.SetFloat(s_DstBlend, (int)BlendMode.OneMinusSrcAlpha);
            }
            if (o.hasSrcBlendAlpha)
            {
                material.SetFloat(s_SrcBlendAlpha, (int)BlendMode.One);
            }
            if (o.hasDstBlendAlpha)
            {
                material.SetFloat(s_DstBlendAlpha, (int)BlendMode.OneMinusSrcAlpha);
            }
        }
        else
        {
            if (o.hasSrcBlend)
            {
                material.SetFloat(s_SrcBlend, o.srcBlend);
            }
            if (o.hasDstBlend)
            {
                material.SetFloat(s_DstBlend, o.dstBlend);
            }
            if (o.hasSrcBlendAlpha)
            {
                material.SetFloat(s_SrcBlendAlpha, o.srcBlendAlpha);
            }
            if (o.hasDstBlendAlpha)
            {
                material.SetFloat(s_DstBlendAlpha, o.dstBlendAlpha);
            }
        }
    }

    /// <summary>
    /// 写入 Dither 相关浮点参数，可按运行时值或缓存的原始值。
    /// </summary>
    /// <param name="material">目标材质。</param>
    /// <param name="o">原始状态缓存。</param>
    /// <param name="useRuntimeValues">为 true 时使用传入的运行时参数。</param>
    /// <param name="intensity">抖动强度。</param>
    /// <param name="ditherSize">抖动尺度。</param>
    /// <param name="ditherWithMatrix">是否与矩阵关联（Shader 约定）。</param>
    /// <param name="ditherAlpha">抖动 Alpha。</param>
    static void ApplyOcclusionDitherFloats(
        Material material,
        MaterialOcclusionState o,
        bool useRuntimeValues,
        float ditherSize,
        int ditherWithMatrix)
    {
        if (o.hasDitherSize)
        {
            material.SetFloat(s_DitherSize, useRuntimeValues ? ditherSize : o.ditherSize);
        }
        if (o.hasDitherWithMatrix)
        {
            material.SetFloat(s_DitherWithMatrix, useRuntimeValues ? ditherWithMatrix : o.ditherWithMatrix);
        }
    }

    /// <summary>
    /// 应用材质的参数
    /// </summary>
    /// <param name="material">目标材质。</param>
    /// <param name="originalState">该材质的原始状态；不可为 null。</param>
    /// <param name="ditherSize">抖动尺度。</param>
    /// <param name="ditherWithMatrix">是否与矩阵关联。</param>
    void ApplyOcclusionToMaterial(Material material, MaterialOcclusionState originalState, float ditherSize, int ditherWithMatrix)
    {
        if (material == null || originalState == null)
        {
            return;
        }

        if (_translucencyMode == ETranslucencyMode.Dither)
        {
            material.EnableKeyword(C_KeywordDitherOn);
            material.renderQueue = originalState.renderQueue;
            ApplyOcclusionBlendCull(material, originalState);
            ApplyOcclusionDitherFloats(material, originalState, useRuntimeValues: true, ditherSize, ditherWithMatrix);
            return;
        }
        else if (_translucencyMode == ETranslucencyMode.Alpha)
        {
            material.DisableKeyword(C_KeywordDitherOn);
            material.renderQueue = C_AlphaRenderQueue;
            ApplyOcclusionBlendCull(material, originalState);
            ApplyOcclusionDitherFloats(material, originalState, useRuntimeValues: false, ditherSize, ditherWithMatrix);
        }
    }

    /// <summary>
    /// 获取或创建并缓存材质的 Occlusion 前快照。
    /// </summary>
    /// <param name="material">目标材质。</param>
    /// <returns>缓存的状态；<paramref name="material"/> 为 null 时返回 null。</returns>
    MaterialOcclusionState GetOrCreateMaterialOcclusionState(Material material)
    {
        if (material == null)
        {
            return null;
        }
        if (_materialOcclusionStates.TryGetValue(material, out MaterialOcclusionState cached))
        {
            return cached;
        }

        var state = new MaterialOcclusionState
        {
            renderQueue = material.renderQueue,
            hasSrcBlend = material.HasProperty(s_SrcBlend),
            hasDstBlend = material.HasProperty(s_DstBlend),
            hasSrcBlendAlpha = material.HasProperty(s_SrcBlendAlpha),
            hasDstBlendAlpha = material.HasProperty(s_DstBlendAlpha),
            hasDitherSize = material.HasProperty(s_DitherSize),
            hasDitherWithMatrix = material.HasProperty(s_DitherWithMatrix),
        };

        if (state.hasSrcBlend)
        {
            state.srcBlend = material.GetFloat(s_SrcBlend);
        }
        if (state.hasDstBlend)
        {
            state.dstBlend = material.GetFloat(s_DstBlend);
        }
        if (state.hasSrcBlendAlpha)
        {
            state.srcBlendAlpha = material.GetFloat(s_SrcBlendAlpha);
        }
        if (state.hasDstBlendAlpha)
        {
            state.dstBlendAlpha = material.GetFloat(s_DstBlendAlpha);
        }
        if (state.hasDitherSize)
        {
            state.ditherSize = material.GetFloat(s_DitherSize);
        }
        if (state.hasDitherWithMatrix)
        {
            state.ditherWithMatrix = material.GetFloat(s_DitherWithMatrix);
        }
        _materialOcclusionStates.Add(material, state);
        return state;
    }

    /// <summary>
    /// 对所有 Entry 关联材质重新应用当前 Occlusion 状态。
    /// </summary>
    void ApplyOcclusionToAllEntryMaterials()
    {
        if (_entryStates == null)
        {
            return;
        }

        for (int i = 0; i < _entryStates.Count; i++)
        {
            ApplyOcclusionToEntryMaterials(_entryStates[i]);
        }
    }

    /// <summary>
    /// 对指定 Entry 的三套路径材质应用当前 Occlusion 设置。
    /// </summary>
    /// <param name="state">Entry 状态；为 null 时直接返回。</param>
    void ApplyOcclusionToEntryMaterials(EntryState state)
    {
        if (state == null)
        {
            return;
        }

        ApplyOcclusionToMaterial(state.materialStructuredCull, GetOrCreateMaterialOcclusionState(state.materialStructuredCull), _occlusionDitherSize, _occlusionDitherWithMatrix);
        ApplyOcclusionToMaterial(state.materialStructuredFull, GetOrCreateMaterialOcclusionState(state.materialStructuredFull), _occlusionDitherSize, _occlusionDitherWithMatrix);
        ApplyOcclusionToMaterial(state.materialTexFetch, GetOrCreateMaterialOcclusionState(state.materialTexFetch), _occlusionDitherSize, _occlusionDitherWithMatrix);
    }

    /// <summary>
    /// 启用 Dither 遮挡半透明，并设置抖动相关参数。
    /// </summary>
    /// <param name="intensity">抖动强度（小于 0 时按 0 处理）。</param>
    /// <param name="ditherSize">抖动尺度。</param>
    /// <param name="ditherWithMatrix">是否与矩阵关联。</param>
    /// <param name="ditherAlpha">抖动 Alpha。</param>
    public void SetOcclusionDither(float intensity, float ditherSize, int ditherWithMatrix, float ditherAlpha)
    {
        _translucencyMode = ETranslucencyMode.Dither;
        _occlusionDitherIntensity = Mathf.Max(0f, intensity);
        _occlusionDitherSize = ditherSize;
        _occlusionDitherWithMatrix = ditherWithMatrix;
        _occlusionDitherAlpha = ditherAlpha;
        ApplyOcclusionToAllEntryMaterials();
    }

    /// <summary>
    /// 启用或关闭基于 Alpha 混合的本地 Occlusion，并更新 Dither 相关参数（用于 Shader 侧一致读取）。
    /// </summary>
    /// <param name="enabled">为 true 时使用 Alpha 模式；为 false 时回到 Dither 默认语义。</param>
    /// <param name="ditherSize">抖动尺度。</param>
    /// <param name="ditherWithMatrix">是否与矩阵关联。</param>
    /// <param name="ditherAlpha">抖动 Alpha。</param>
    public void SetOcclusionBlend(float ditherSize, int ditherWithMatrix, float ditherAlpha)
    {
        _translucencyMode = ETranslucencyMode.Alpha;
        _occlusionDitherIntensity = 0f;
        _occlusionDitherSize = ditherSize;
        _occlusionDitherWithMatrix = ditherWithMatrix;
        _occlusionDitherAlpha = ditherAlpha;
        ApplyOcclusionToAllEntryMaterials();
    }

    /// <summary>
    /// 关闭本地 Alpha Occlusion，保留当前 dither 参数尺寸。
    /// </summary>
    public void ClearOcclusionBlend()
    {
        ClearOcclusionState();
    }

    /// <summary>
    /// 清除 Dither Occlusion，恢复为默认遮挡状态。
    /// </summary>
    public void ClearOcclusionDither()
    {
        ClearOcclusionState();
    }

    /// <summary>
    /// 重置内部 Occlusion 模式与强度，并刷新所有材质。
    /// </summary>
    public void ClearOcclusionState()
    {
        _translucencyMode = ETranslucencyMode.None;
        _occlusionDitherIntensity = 0f;
        if (_entryStates == null)
        {
            return;
        }

        for (int i = 0; i < _entryStates.Count; i++)
        {
            ResetEntryMaterials(_entryStates[i]);
        }
    }

    void ResetEntryMaterials(EntryState state)
    {
        if (state == null)
        {
            return;
        }

        ResetMaterial(state.materialStructuredCull, GetOrCreateMaterialOcclusionState(state.materialStructuredCull));
        ResetMaterial(state.materialStructuredFull, GetOrCreateMaterialOcclusionState(state.materialStructuredFull));
        ResetMaterial(state.materialTexFetch, GetOrCreateMaterialOcclusionState(state.materialTexFetch));
    }


    void ResetMaterial(Material material, MaterialOcclusionState originalState)
    {
        if (material == null || originalState == null)
        {
            return;
        }

        material.DisableKeyword(C_KeywordDitherOn);
        material.renderQueue = originalState.renderQueue;
        material.SetFloat(s_DitherSize, originalState.ditherSize);
        material.SetFloat(s_DitherWithMatrix, originalState.ditherWithMatrix);
    }
}
