// Created By: WangYu  Date: 2025-04-26

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.Utils
{
    public static class UnpackUtil
    {
        public static bool UnpackPrefabs(GameObject targetObject)
        {
            // 获取相关的最外层预设根节点
            GameObject outerRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(targetObject);
            if (outerRoot == null)
            {
                return true;
            }

            // 收集所有嵌套预设根节点
            List<GameObject> prefabRoots = new List<GameObject>();
            CollectAllPrefabRoots(outerRoot, prefabRoots);
            
            prefabRoots.Sort((left, right) =>
            {
                int leftDepth = GetHierarchyDepth(left);
                int rightDepth = GetHierarchyDepth(right);
                int result = leftDepth.CompareTo(rightDepth);
                return result;
            });

            // 从内到外解包
            bool result = true;
            foreach (var prefabRoot in prefabRoots)
            {
                if (prefabRoot == null)
                {
                    continue;
                }
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(prefabRoot))
                {
                    continue;
                }
                
                try
                {
                    PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"解包失败 {prefabRoot.name}: {e.Message}");
                    result = false;
                }
            }

            return result;
        }

        private static void CollectAllPrefabRoots(GameObject root, List<GameObject> results)
        {
            // 如果是预设根节点，添加到列表
            if (PrefabUtility.IsAnyPrefabInstanceRoot(root))
            {
                results.Add(root);
            }

            // 递归所有子对象
            foreach (Transform child in root.transform)
            {
                CollectAllPrefabRoots(child.gameObject, results);
            }
        }

        private static int GetHierarchyDepth(GameObject obj)
        {
            int depth = 0;
            Transform current = obj.transform;
            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }
    }
}