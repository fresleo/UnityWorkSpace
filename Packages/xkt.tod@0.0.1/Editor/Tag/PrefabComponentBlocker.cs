// Created By: WangYu  Date: 2025-04-27

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace XKT.TOD.Tag
{
    /// <summary>
    /// 预设组件拦截器
    /// </summary>
    [InitializeOnLoad]
    public class PrefabComponentBlocker
    {
        static PrefabComponentBlocker()
        {
            // 在预设菜单中，触发预设保存前的事件
            PrefabStage.prefabSaving -= OnPrefabSaving;
            PrefabStage.prefabSaving += OnPrefabSaving;

            // 在 Inspector 界面中点击 Apply 按钮触发预设覆盖事件
            PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
        }

        private static void OnPrefabSaving(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            AbsTodTag[] tags = prefab.GetComponentsInChildren<AbsTodTag>(true);
            if (tags.Length > 0)
            {
                foreach (var tag in tags)
                {
                    UnityObject.DestroyImmediate(tag, true);
                }
            }
        }

        private static void OnPrefabInstanceUpdated(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            // 根据实例找到资源
            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (prefabAsset == null)
            {
                return;
            }

            AbsTodTag[] tags;
            bool modified = false;

            tags = instance.GetComponentsInChildren<AbsTodTag>(true);
            if (tags.Length > 0)
            {
                foreach (var tag in tags)
                {
                    UnityObject.DestroyImmediate(tag, true);
                }

                modified = true;
            }

            tags = prefabAsset.GetComponentsInChildren<AbsTodTag>(true);
            if (tags.Length > 0)
            {
                foreach (var tag in tags)
                {
                    UnityObject.DestroyImmediate(tag, true);
                }

                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SavePrefabAsset(prefabAsset);
                AssetDatabase.SaveAssets();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); // 刷新编辑器视图
            }
        }
        
    }
}
