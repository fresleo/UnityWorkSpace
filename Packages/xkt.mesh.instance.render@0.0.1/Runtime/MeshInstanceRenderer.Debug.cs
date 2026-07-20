/*******************************************************************************
 * File: MeshInstanceRenderer.Debug.cs
 * Author: fan.shi
 * Date: 2026-03-21
 * Description: GM 调试相关的
 ******************************************************************************/

using System.Collections.Generic;
using Common;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class MeshInstanceRenderer
{
    /// <summary>
    /// GM 强制剔除模式:非 null 时在加载/重建缓冲时覆盖 <see cref="MeshInstanceRenderer.cullMode"/>；
    /// null 表示使用Inspector值。
    /// </summary>
    public static EMeshInstanceCullMode? GmCullModeOverride;

    /// <summary>
    /// GM 强制实例数据后端:非 null 时在加载/重建缓冲时覆盖 <see cref="MeshInstanceRenderer.instanceDataBackend"/>；
    /// null 表示使用Inspector值。
    /// </summary>
    public static EMeshInstanceDataBackend? GmInstanceDataBackendOverride;

    /// <summary> GM:在四种 <see cref="EMeshInstanceCullMode"/> 间循环</summary>
    public static void GmCycleCullMode()
    {
        if (!GmCullModeOverride.HasValue)
        {
            GmCullModeOverride = EMeshInstanceCullMode.NoCull;
        }
        else
        {
            int next = ((int)GmCullModeOverride.Value + 1) % 4;
            GmCullModeOverride = (EMeshInstanceCullMode)next;
        }

        var m = GmCullModeOverride.Value;
        D.Warn("[GM] MeshInstanceRenderer cullMode={0} (int={1})", m, (int)m);

        var all = Object.FindObjectsOfType<MeshInstanceRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
            {
                all[i].RequestRebuildBuffers();
            }
        }
    }

    /// <summary> 重新异步加载配置与烘焙数据并重建缓冲（供 GM 等外部调用）。</summary>
    public void RequestRebuildBuffers()
    {
        BeginRebuildBuffersAsync();
    }

    const int C_InstanceDataBackendEnumCount = 3;

    /// <summary> GM:在 <see cref="EMeshInstanceDataBackend"/>（Auto / StructuredBuffer / Texture）间循环。</summary>
    public static void GmCycleInstanceDataBackend()
    {
        if (!GmInstanceDataBackendOverride.HasValue)
        {
            GmInstanceDataBackendOverride = EMeshInstanceDataBackend.Auto;
        }
        else
        {
            int next = ((int)GmInstanceDataBackendOverride.Value + 1) % C_InstanceDataBackendEnumCount;
            GmInstanceDataBackendOverride = (EMeshInstanceDataBackend)next;
        }

        var b = GmInstanceDataBackendOverride.Value;
        D.Warn("[GM] MeshInstanceRenderer instanceDataBackend={0} (int={1})", b, (int)b);

        var all = Object.FindObjectsOfType<MeshInstanceRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null)
            {
                all[i].RequestRebuildBuffers();
            }
        }
    }

    // true = 显示 Grass_Bake、关闭 Grass 下(仅子级)MeshRenderer;
    // false = 相反。
    static bool s_GmGrassShowBake;

    /// <summary>
    /// GM:在活动场景中切换「烘焙草 / 原始草」。
    /// </summary>
    public static void GmToggleGrassBakeOrOriginal()
    {
        s_GmGrassShowBake = !s_GmGrassShowBake;
        int applied = ApplyGrassBakeDisplayToActiveScene(s_GmGrassShowBake);

        if (s_GmGrassShowBake)
        {
            D.Warn("[GM] Grass display: Bake Grass (applied MeshBrush={0})", applied);
        }
        else
        {
            D.Warn("[GM] Grass display: Original Grass (applied MeshBrush={0})", applied);
        }
    }

    static int ApplyGrassBakeDisplayToActiveScene(bool showBake)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            D.Warn("[GM] Grass toggle: active scene invalid or not loaded.");
            return 0;
        }

        var meshBrushes = new List<Transform>(8);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null)
            {
                CollectTransformsNamedMeshBrush(root.transform, meshBrushes);
            }
        }

        if (meshBrushes.Count == 0)
        {
            D.Warn("[GM] Grass toggle: no GameObject named \"MeshBrush\" in active scene.");
            return 0;
        }

        int applied = 0;
        for (int i = 0; i < meshBrushes.Count; i++)
        {
            Transform brush = meshBrushes[i];
            Transform grass = brush.Find("Grass");
            Transform bake = brush.Find("Grass_Bake");
            if (grass == null)
            {
                D.Warn("[GM] Grass toggle: MeshBrush \"{0}\" has no child \"Grass\".", brush.name);
                continue;
            }
            if (bake == null)
            {
                D.Warn("[GM] Grass toggle: MeshBrush \"{0}\" has no child \"Grass_Bake\".", brush.name);
                continue;
            }

            if (showBake)
            {
                bake.gameObject.SetActive(true);
                SetGrassChildMeshRenderersEnabled(grass, false);
            }
            else
            {
                bake.gameObject.SetActive(false);
                SetGrassChildMeshRenderersEnabled(grass, true);
            }
            applied++;
        }

        return applied;
    }

    static void CollectTransformsNamedMeshBrush(Transform t, List<Transform> outList)
    {
        if (t.name == "MeshBrush")
        {
            outList.Add(t);
        }
        int n = t.childCount;
        for (int i = 0; i < n; i++)
        {
            CollectTransformsNamedMeshBrush(t.GetChild(i), outList);
        }
    }

    /// <summary>
    /// 从 MeshRenderer 节点向上(含自身),仅在 Grass 根子树内查找最近的 LODGroup 宿主;
    /// 未找到则返回该 MeshRenderer 所在 GameObject。
    /// </summary>
    static GameObject GetGrassDisplayToggleTarget(MeshRenderer mr, Transform grassRoot)
    {
        if (mr == null)
        {
            return null;
        }
        if (grassRoot == null)
        {
            return mr.gameObject;
        }
        for (Transform t = mr.transform; t != null; t = t.parent)
        {
            bool underGrass = t == grassRoot || t.IsChildOf(grassRoot);
            if (!underGrass)
            {
                break;
            }
            if (t.GetComponent<LODGroup>() != null)
            {
                return t.gameObject;
            }
        }
        return mr.gameObject;
    }

    /// <summary>
    /// 仅 Grass 子层级带 MeshRenderer 的物体:用 GameObject.SetActive;排除 Grass 根自身。
    /// 若有 LODGroup 祖先(在 Grass 子树内),则切换该 LODGroup 节点;同一 LODGroup 多实例只切换一次。
    /// </summary>
    static void SetGrassChildMeshRenderersEnabled(Transform grassRoot, bool enabled)
    {
        var mrs = grassRoot.GetComponentsInChildren<MeshRenderer>(true);
        var targets = new HashSet<GameObject>();
        for (int i = 0; i < mrs.Length; i++)
        {
            MeshRenderer mr = mrs[i];
            if (mr == null)
            {
                continue;
            }
            if (mr.transform == grassRoot)
            {
                continue;
            }
            GameObject target = GetGrassDisplayToggleTarget(mr, grassRoot);
            if (target != null)
            {
                targets.Add(target);
            }
        }
        foreach (GameObject go in targets)
        {
            go.SetActive(enabled);
        }
    }
}
