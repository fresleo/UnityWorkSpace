/*******************************************************************************
 * File: MeshInstanceBakeExporter.cs
 * Author: fan.shi
 * Date: 2026-03-16
 * Description: Mesh 实例烘焙导出
 ******************************************************************************/

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 通用 Mesh 实例烘焙导出:适用于草、树木等大量相同 Mesh 的 GameObject。
/// 1. 选择 Scene 里的根 GameObject
/// 2. 遍历其下所有 MeshRenderer,按 Mesh 分组,每组保存世界矩阵到单独 .bytes
/// 3. 在同级创建同名 _Bake 节点,挂 MeshInstanceRenderer,多 Mesh 时绑定多条 entries
/// </summary>
public class MeshInstanceBakeExporter : EditorWindow
{
    // 包内默认 Cull Compute 资源名。
    const string C_DEFAULT_CULL_COMPUTE_NAME = "MeshInstanceCull";
    // 包路径前缀,用于在 Packages 内筛选 Compute 资源。 
    const string C_PACKAGE_PATH_PREFIX = "Packages/xkt.mesh.instance.render";
    const string C_BAKE_ROOT_LAYER_NAME = "SceneModel";

    /// 仅当某 Mesh 实例数大于此值时纳入分组显示
    int _minInstanceCount = 200;

    /// <summary> 将烘焙根节点 Layer 设为 SceneModel(与场景静态模型一致)</summary>
    static void TrySetBakeRootLayer(GameObject bakeRoot)
    {
        if (bakeRoot == null)
        {
            return;
        }
        int layer = LayerMask.NameToLayer(C_BAKE_ROOT_LAYER_NAME);
        if (layer >= 0)
        {
            bakeRoot.layer = layer;
        }
    }

    /// <summary> 在工程内查找包自带的 MeshInstanceCull Compute 资源。 </summary>
    /// <returns> 找到则返回资源,否则为 null。 </returns>
    static UnityEngine.ComputeShader LoadDefaultCullCompute()
    {
        string path = "Packages/xkt.mesh.instance.render@0.01/Shaders/MeshInstanceCull.compute";
        var cull = AssetDatabase.LoadAssetAtPath<UnityEngine.ComputeShader>(path);
        if (cull != null)
        {
            return cull;
        }
        string[] guids = AssetDatabase.FindAssets(C_DEFAULT_CULL_COMPUTE_NAME + " t:ComputeShader", new[] { "Packages" });
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p != null && p.Contains(C_PACKAGE_PATH_PREFIX) && p.EndsWith(".compute"))
            {
                cull = AssetDatabase.LoadAssetAtPath<UnityEngine.ComputeShader>(p);
                if (cull != null)
                {
                    return cull;
                }
            }
        }
        return null;
    }

    /// <summary> 若找到默认 Cull Compute,则赋给组件并 SetDirty。 </summary>
    /// <param name="comp"> 目标 MeshInstanceRenderer;为 null 时直接返回。 </param>
    static void AssignDefaultCullCompute(MeshInstanceRenderer comp)
    {
        if (comp == null)
        {
            return;
        }
        var cull = LoadDefaultCullCompute();
        if (cull != null)
        {
            comp.cullCompute = cull;
            EditorUtility.SetDirty(comp);
        }
    }

    /// <summary> 默认保存基础名:根节点名_instancedata(带下划线)</summary>
    /// <returns> 用于生成 .bytes 文件名的基础字符串。 </returns>
    string GetDefaultSaveBaseName()
    {
        return _root != null ? (_root.name + "_instancedata").ToLowerInvariant() : "instancedata";
    }

    /// <summary> 默认导出目录:Assets/OutputRes/assetfiles/mesh_instance/&lt;根节点所在场景名&gt;/,不存在则创建。 </summary>
    /// <returns> 目录绝对路径。 </returns>
    string GetOrCreateDefaultExportDirectory()
    {
        string sceneName = _root != null && _root.scene.IsValid() ? _root.scene.name : "UnknownScene";
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = "UnknownScene";
        }
        string dir = Path.Combine(Application.dataPath, "OutputRes", "assetfiles", "mesh_instance", sceneName);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    /// <summary> 将完整路径转为相对项目的路径(Assets/...),用于 AssetDatabase。 </summary>
    /// <param name="fullPath"> 磁盘上的完整路径。 </param>
    /// <returns> 以 Assets/ 开头的相对路径,或无法转换时的规范化完整路径。 </returns>
    static string FullPathToProjectRelative(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            return fullPath;
        }
        string dataPathNorm = Application.dataPath.Replace('\\', '/');
        string fullPathNorm = fullPath.Replace('\\', '/');
        if (!fullPathNorm.StartsWith(dataPathNorm))
        {
            return fullPathNorm;
        }
        string relative = "Assets" + fullPathNorm.Substring(dataPathNorm.Length);
        return relative.Replace('\\', '/');
    }

    /// <summary> 将 Assets/OutputRes/ 下的工程路径转为 AssetSystem 使用的逻辑路径。 </summary>
    static string ToLogicalAssetPathForLoad(string projectRelativeUnityPath)
    {
        if (string.IsNullOrEmpty(projectRelativeUnityPath))
        {
            return projectRelativeUnityPath;
        }
        string p = projectRelativeUnityPath.Replace('\\', '/');
        const string prefix = "Assets/OutputRes/";
        if (p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return p.Substring(prefix.Length);
        }
        return p;
    }

    GameObject _root;
    List<MeshGroup> _collectedGroups = new List<MeshGroup>();
    // 保存时生成 基础名_0.bytes, 基础名_1.bytes … 留空则用「根节点名_instancedata」。
    string _saveBaseName = "";
    string _status = "";
    Vector2 _scrollGroups;

    [System.Serializable]
    class MeshGroup
    {
        public Mesh mesh;
        public Material material;
        public List<Matrix4x4> matrices = new List<Matrix4x4>();
        public List<GameObject> sourceObjects = new List<GameObject>();
        // 是否勾选导出:仅勾选的 Mesh 会写入 .bytes。
        public bool selected = true;
    }

    /// <summary> 通过菜单打开 Mesh 实例烘焙导出窗口(可用于草、树木等大量实例)。 </summary>
    [MenuItem("Tools/MeshInstanceRender/Exporter")]
    static void Open()
    {
        MeshInstanceBakeExporter w = GetWindow<MeshInstanceBakeExporter>(false, "Mesh 实例烘焙导出", true);
        w.minSize = new Vector2(380, 460);
    }

    /// <summary> 绘制窗口:根节点选择、收集、勾选分组、保存与创建 _Bake 节点。 </summary>
    void OnGUI()
    {
        GUILayout.Label("1. 选择场景中的根节点", EditorStyles.boldLabel);
        _root = (GameObject)EditorGUILayout.ObjectField("根节点 (Root)", _root, typeof(GameObject), true);

        EditorGUILayout.Space(6);
        GUILayout.Label("2. 按 Mesh 分组并收集世界矩阵", EditorStyles.boldLabel);
        _minInstanceCount = Mathf.Max(0, EditorGUILayout.IntField("最小实例数(仅显示超过此数量的 Mesh,0=全部)", _minInstanceCount));
        if (GUILayout.Button("遍历该节点下所有 MeshRenderer,按 Mesh 分组收集", GUILayout.Height(26)))
        {
            CollectMeshRenderersUnderRoot();
        }

        int totalInstances = 0;
        int selectedCount = 0;
        for (int i = 0; i < _collectedGroups.Count; i++)
        {
            totalInstances += _collectedGroups[i].matrices.Count;
            if (_collectedGroups[i].selected)
            {
                selectedCount++;
            }
        }
        EditorGUILayout.LabelField("已收集: " + _collectedGroups.Count + " 种 Mesh(> " + _minInstanceCount + " 实例),共 " + totalInstances + " 个实例");
        if (!string.IsNullOrEmpty(_status))
        {
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        if (_collectedGroups.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("勾选要导出的 Mesh(仅导出勾选项)", EditorStyles.boldLabel);
            if (GUILayout.Button("全选", GUILayout.Width(48)))
            {
                for (int i = 0; i < _collectedGroups.Count; i++)
                {
                    _collectedGroups[i].selected = true;
                }
            }
            if (GUILayout.Button("全不选", GUILayout.Width(56)))
            {
                for (int i = 0; i < _collectedGroups.Count; i++)
                {
                    _collectedGroups[i].selected = false;
                }
            }
            EditorGUILayout.EndHorizontal();
            _scrollGroups = EditorGUILayout.BeginScrollView(_scrollGroups, GUILayout.MaxHeight(160));
            for (int i = 0; i < _collectedGroups.Count; i++)
            {
                MeshGroup g = _collectedGroups[i];
                EditorGUILayout.BeginHorizontal();
                string label = (g.mesh != null ? g.mesh.name : "?") + "  (" + g.matrices.Count + " 实例)";
                g.selected = EditorGUILayout.ToggleLeft(label, g.selected, GUILayout.ExpandWidth(true));
                bool anyActive = false;
                for (int j = 0; j < g.sourceObjects.Count; j++)
                {
                    if (g.sourceObjects[j] != null && g.sourceObjects[j].activeSelf)
                    {
                        anyActive = true;
                        break;
                    }
                }
                if (GUILayout.Button(anyActive ? "隐藏" : "显示", GUILayout.Width(44)))
                {
                    SetGroupObjectsActive(g, !anyActive);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField("已勾选 " + selectedCount + " 项,将导出 " + selectedCount + " 个 .bytes");
        }

        EditorGUILayout.Space(6);
        GUILayout.Label("3. 保存为多个 .bytes 并创建/更新 _Bake 节点", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("保存基础名(将生成 基础名_0.bytes, 基础名_1.bytes …;留空则默认:根节点名_instancedata 小写)", EditorStyles.miniLabel);
        string defaultBaseName = GetDefaultSaveBaseName();
        bool useDefaultDisplay = string.IsNullOrEmpty(_saveBaseName) || (_root != null && _saveBaseName == "instancedata");
        string displayBaseName = useDefaultDisplay ? defaultBaseName : _saveBaseName;
        _saveBaseName = EditorGUILayout.TextField("基础名", displayBaseName);
        string effectiveBase = string.IsNullOrEmpty(_saveBaseName) || (_root != null && _saveBaseName == "instancedata") ? defaultBaseName : _saveBaseName;
        if (selectedCount > 0 && !string.IsNullOrEmpty(effectiveBase))
        {
            var preview = new System.Text.StringBuilder();
            int idx = 0;
            for (int i = 0; i < _collectedGroups.Count; i++)
            {
                if (!_collectedGroups[i].selected)
                {
                    continue;
                }
                if (idx > 0)
                {
                    preview.Append(", ");
                }
                preview.Append(effectiveBase).Append("_").Append(idx).Append(".bytes");
                idx++;
            }
            EditorGUILayout.HelpBox("将保存 " + selectedCount + " 个文件: " + preview.ToString(), MessageType.None);
        }
        GUI.enabled = selectedCount > 0 && _root != null;
        string bakeNodeSuffix = _root != null ? _root.name + "_Bake」节点" : "_Bake」节点";
        string saveButtonLabel = "选择保存目录并保存(生成 "
            + (selectedCount > 0 ? selectedCount + " 个 .bytes" : "")
            + ")+ 创建「" + bakeNodeSuffix;
        if (GUILayout.Button(saveButtonLabel, GUILayout.Height(28)))
        {
            SaveAndCreateBakeNode();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "适用于草、树木等大量实例:每种 Mesh 导出一个 .bytes,运行时 MeshInstanceRenderer 按条目分别绘制。",
            MessageType.None
        );
    }

    /// <summary>
    /// 从当前根节点下收集有效 MeshRenderer,按 Mesh 分组并过滤掉实例数过少的组。
    /// LOD 处理规则:若 MeshRenderer 的父级链中存在 LODGroup,则仅采用该 LODGroup
    /// LOD0 对应的 MeshRenderer;其余 LOD 层级的 MeshRenderer 全部跳过。
    /// 不在任何 LODGroup 下的 MeshRenderer 正常纳入收集。
    /// </summary>
    void CollectMeshRenderersUnderRoot()
    {
        _collectedGroups.Clear();
        if (_root == null)
        {
            _status = "请先指定根节点。";
            return;
        }

        // ---------- 第一步：处理 LODGroup ----------
        // 收集 LOD0 中的 MeshRenderer（有效），同时记录所有被 LODGroup 管辖的 MeshRenderer（排除用）
        var lod0Renderers = new HashSet<MeshRenderer>();
        var renderersUnderAnyLodGroup = new HashSet<MeshRenderer>();

        LODGroup[] lodGroups = _root.GetComponentsInChildren<LODGroup>(true);
        foreach (LODGroup lodGroup in lodGroups)
        {
            // 将该 LODGroup 子树下所有 MeshRenderer 标记为"已被 LOD 接管"
            MeshRenderer[] allUnderLod = lodGroup.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer mr in allUnderLod)
            {
                renderersUnderAnyLodGroup.Add(mr);
            }

            // 仅将 LOD0 的 Renderer 加入有效集合
            LOD[] lods = lodGroup.GetLODs();
            if (lods.Length > 0)
            {
                foreach (Renderer lodRenderer in lods[0].renderers)
                {
                    if (lodRenderer is MeshRenderer meshRenderer)
                    {
                        lod0Renderers.Add(meshRenderer);
                    }
                }
            }
        }

        // ---------- 第二步：汇总有效 MeshRenderer ----------
        // 有效 = (不在任何 LODGroup 下) 或 (属于某 LODGroup 的 LOD0)
        var validRenderers = new HashSet<MeshRenderer>();
        MeshRenderer[] allRenderers = _root.GetComponentsInChildren<MeshRenderer>(true);
        int lodSkipCount = 0;
        foreach (MeshRenderer r in allRenderers)
        {
            if (r == null)
            {
                continue;
            }
            if (renderersUnderAnyLodGroup.Contains(r))
            {
                if (lod0Renderers.Contains(r))
                {
                    validRenderers.Add(r);
                }
                else
                {
                    lodSkipCount++;
                }
            }
            else
            {
                validRenderers.Add(r);
            }
        }

        // ---------- 第三步：按 Mesh 分组 ----------
        var groupByMesh = new Dictionary<Mesh, MeshGroup>();
        foreach (MeshRenderer r in validRenderers)
        {
            MeshFilter filter = r.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
            {
                continue;
            }
            if (!groupByMesh.TryGetValue(mesh, out MeshGroup group))
            {
                group = new MeshGroup { mesh = mesh, material = r.sharedMaterial };
                groupByMesh[mesh] = group;
            }
            group.matrices.Add(r.transform.localToWorldMatrix);
            group.sourceObjects.Add(r.gameObject);
        }

        // ---------- 第四步：过滤实例数不足的组 ----------
        _collectedGroups.Clear();
        int ignored = 0;
        int ignoredInstances = 0;
        foreach (MeshGroup g in groupByMesh.Values)
        {
            if (g.matrices.Count > _minInstanceCount)
            {
                g.selected = true;
                _collectedGroups.Add(g);
            }
            else
            {
                ignored++;
                ignoredInstances += g.matrices.Count;
            }
        }
        int total = 0;
        for (int i = 0; i < _collectedGroups.Count; i++)
        {
            total += _collectedGroups[i].matrices.Count;
        }
        _status = "在「" + _root.name + "」下找到 " + _collectedGroups.Count + " 种 Mesh(> " + _minInstanceCount + " 实例),共 " + total + " 个实例。"
            + (lodGroups.Length > 0 ? " 检测到 " + lodGroups.Length + " 个 LODGroup,已跳过非 LOD0 的 " + lodSkipCount + " 个 MeshRenderer。" : "")
            + (ignored > 0 ? " 已忽略 " + ignored + " 种(≤" + _minInstanceCount + "),共 " + ignoredInstances + " 个实例。" : "");
    }

    /// <summary>
    /// 从 source 节点向上(含自身)在烘焙根范围内查找最近的 LODGroup 宿主 GameObject;
    /// 未找到则返回 source,即无 LODGroup 时仍操作 MeshRenderer 所在对象。
    /// </summary>
    static GameObject GetActiveToggleTargetForSource(GameObject source, Transform bakeRoot)
    {
        if (source == null)
        {
            return null;
        }
        if (bakeRoot == null)
        {
            return source;
        }
        for (Transform t = source.transform; t != null; t = t.parent)
        {
            bool underRoot = t == bakeRoot || t.IsChildOf(bakeRoot);
            if (!underRoot)
            {
                break;
            }
            if (t.GetComponent<LODGroup>() != null)
            {
                return t.gameObject;
            }
        }
        return source;
    }

    /// <summary>
    /// 对分组内源对象 SetActive:若存在 LODGroup 祖先(在根范围内),则切换该 LODGroup 节点;
    /// 否则切换 MeshRenderer 所在 GameObject。
    /// </summary>
    /// <param name="g"> 包含 sourceObjects 的分组。 </param>
    /// <param name="active"> true 为显示,false 为隐藏。 </param>
    void SetGroupObjectsActive(MeshGroup g, bool active)
    {
        if (g.sourceObjects == null || g.sourceObjects.Count == 0)
        {
            return;
        }
        var toSet = new HashSet<GameObject>();
        Transform bakeRoot = _root != null ? _root.transform : null;
        for (int i = 0; i < g.sourceObjects.Count; i++)
        {
            if (g.sourceObjects[i] == null)
            {
                continue;
            }
            GameObject target = GetActiveToggleTargetForSource(g.sourceObjects[i], bakeRoot);
            if (target != null)
            {
                toSet.Add(target);
            }
        }
        if (toSet.Count == 0)
        {
            return;
        }
        Undo.RecordObjects(new List<GameObject>(toSet).ToArray(), active ? "显示源 GameObject" : "隐藏源 GameObject");
        foreach (var go in toSet)
        {
            go.SetActive(active);
        }
    }

    /// <summary> 将勾选的分组导出为 .bytes,并在根节点同级创建或更新 _Bake 节点并绑定 MeshInstanceRenderer。 </summary>
    void SaveAndCreateBakeNode()
    {
        var toExport = new List<MeshGroup>();
        for (int i = 0; i < _collectedGroups.Count; i++)
        {
            if (_collectedGroups[i].selected)
            {
                toExport.Add(_collectedGroups[i]);
            }
        }
        if (toExport.Count == 0 || _root == null)
        {
            return;
        }

        string defaultName = string.IsNullOrEmpty(_saveBaseName) ? GetDefaultSaveBaseName() : _saveBaseName;
        string defaultDir = GetOrCreateDefaultExportDirectory();
        string firstPathFull = EditorUtility.SaveFilePanel(
            "选择保存位置(将在此目录生成 " + defaultName + "_0.bytes, _1.bytes … 共 " + toExport.Count + " 个文件)",
            defaultDir,
            defaultName,
            "bytes");
        if (string.IsNullOrEmpty(firstPathFull))
        {
            return;
        }

        string firstPathRelative = FullPathToProjectRelative(firstPathFull);
        string dir = Path.GetDirectoryName(firstPathRelative).Replace('\\', '/');
        string baseName = Path.GetFileNameWithoutExtension(firstPathRelative);

        var savedEntries = new List<MeshInstanceRenderer.MeshInstanceEntry>();
        for (int i = 0; i < toExport.Count; i++)
        {
            MeshGroup g = toExport[i];
            string relativePath = dir + "/" + baseName + "_" + i + ".bytes";
            Matrix4x4[] mats = g.matrices.ToArray();
            if (!MeshInstanceData.TryMatricesToTrsStructured(mats, out MeshInstanceTrsStructuredGpu[] trsStructured))
            {
                string meshLabel = g.mesh != null ? g.mesh.name : "?";
                EditorUtility.DisplayDialog(
                    "Mesh Instance 烘焙失败",
                    "网格「" + meshLabel + "」的实例矩阵无法分解为 TRS(含剪切或非正交),请检查变换后重试。",
                    "确定");
                return;
            }
            byte[] data = MeshInstanceData.ToBytesTrsStructured(trsStructured);
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            File.WriteAllBytes(fullPath, data);
            savedEntries.Add(new MeshInstanceRenderer.MeshInstanceEntry
            {
                mesh = g.mesh,
                material = g.material != null ? g.material : null,
                bakedDataAssetPath = ToLogicalAssetPathForLoad(relativePath),
                subMeshIndex = 0
            });
        }
        AssetDatabase.Refresh();

        Transform parent = _root.transform.parent;
        string bakeName = _root.name + "_Bake";
        Transform existing = null;
        if (parent != null)
        {
            existing = parent.Find(bakeName);
        }
        else
        {
            foreach (GameObject go in _root.scene.GetRootGameObjects())
            {
                if (go != null && go.name == bakeName)
                {
                    existing = go.transform;
                    break;
                }
            }
        }

        for (int i = 0; i < toExport.Count; i++)
        {
            SetGroupObjectsActive(toExport[i], false);
        }

        if (existing != null)
        {
            MeshInstanceRenderer comp = existing.GetComponent<MeshInstanceRenderer>();
            if (comp == null)
            {
                comp = existing.gameObject.AddComponent<MeshInstanceRenderer>();
            }
            comp.entries = new List<MeshInstanceRenderer.MeshInstanceEntry>(savedEntries);
            AssignDefaultCullCompute(comp);
            TrySetBakeRootLayer(existing.gameObject);
            EditorUtility.SetDirty(comp);
            EditorUtility.SetDirty(existing.gameObject);
            Selection.activeGameObject = existing.gameObject;
            _status = "已更新「" + bakeName + "」," + savedEntries.Count + " 条 Mesh 数据已绑定。";
            return;
        }

        GameObject bakeGo = new GameObject(bakeName);
        bakeGo.transform.SetParent(parent, false);
        bakeGo.transform.localPosition = Vector3.zero;
        bakeGo.transform.localRotation = Quaternion.identity;
        bakeGo.transform.localScale = Vector3.one;
        TrySetBakeRootLayer(bakeGo);

        MeshInstanceRenderer renderer = bakeGo.AddComponent<MeshInstanceRenderer>();
        renderer.entries = new List<MeshInstanceRenderer.MeshInstanceEntry>(savedEntries);
        AssignDefaultCullCompute(renderer);
        renderer.maxCullDistance = 60f;
        renderer.lod0EndDistance = 30f;
        renderer.lod1EndDistance = 50f;
        renderer.chunkSize = 10f;

        Undo.RegisterCreatedObjectUndo(bakeGo, "Create Instance Bake Node");
        Selection.activeGameObject = bakeGo;
        _status = "已创建「" + bakeName + "」并绑定 " + savedEntries.Count + " 条 Mesh 数据。可将原根节点下的源 GameObject 删除或禁用。";
    }
}
#endif
