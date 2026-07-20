/*******************************************************************************
 * File: TimeOfDaySceneDuplicateNameUtility.cs
 * Author: Codex
 * Date: 2026/04/24
 * Description: TOD 导出前的场景层级重名修复工具。
 *******************************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.Utils;

namespace XKT.TOD
{
    /// <summary>
    /// 修复当前场景中会导致 HierarchyPath 重复的 GameObject 名称。
    /// </summary>
    internal static class TimeOfDaySceneDuplicateNameUtility
    {
        private const int MaxRenameCount = 10000;

        /// <summary>
        /// 检查当前场景中重复的 HierarchyPath，并重命名冲突路径中最靠上的同名 GameObject。
        /// </summary>
        public static int RenameDuplicatedHierarchyPathObjects()
        {
            int renameCount = 0;
            Scene activeScene = SceneManager.GetActiveScene();

            while (renameCount < MaxRenameCount)
            {
                List<GameObject> duplicatedObjects = FindDuplicatedPathObjects(activeScene);
                if (duplicatedObjects.Count == 0)
                {
                    break;
                }

                GameObject target = FindFirstDuplicatedLevelObject(duplicatedObjects[0].transform);
                RenameObject(target);
                renameCount++;
            }

            if (renameCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            return renameCount;
        }

        private static List<GameObject> FindDuplicatedPathObjects(Scene scene)
        {
            Dictionary<string, GameObject> firstObjects = new Dictionary<string, GameObject>();
            List<GameObject> duplicatedObjects = new List<GameObject>();

            foreach (GameObject gameObject in GetAllSceneGameObjects(scene))
            {
                string hierarchyPath = TODUtils.GetHierarchyPath(gameObject.transform);
                if (firstObjects.ContainsKey(hierarchyPath))
                {
                    duplicatedObjects.Add(gameObject);
                    continue;
                }

                firstObjects.Add(hierarchyPath, gameObject);
            }

            return duplicatedObjects;
        }

        private static IEnumerable<GameObject> GetAllSceneGameObjects(Scene scene)
        {
            foreach (GameObject rootGameObject in scene.GetRootGameObjects())
            {
                yield return rootGameObject;

                Transform[] children = rootGameObject.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.gameObject == rootGameObject)
                    {
                        continue;
                    }

                    yield return child.gameObject;
                }
            }
        }

        private static GameObject FindFirstDuplicatedLevelObject(Transform duplicatedTransform)
        {
            List<Transform> chain = GetTransformChain(duplicatedTransform);
            for (int i = 0; i < chain.Count; i++)
            {
                Transform current = chain[i];
                if (HasSameNameSiblingOrRoot(current))
                {
                    return current.gameObject;
                }
            }

            return duplicatedTransform.gameObject;
        }

        private static List<Transform> GetTransformChain(Transform transform)
        {
            List<Transform> chain = new List<Transform>();
            Transform current = transform;
            while (current != null)
            {
                chain.Add(current);
                current = current.parent;
            }

            chain.Reverse();
            return chain;
        }

        private static bool HasSameNameSiblingOrRoot(Transform transform)
        {
            if (transform.parent == null)
            {
                return transform.gameObject.scene.GetRootGameObjects().Count(root => root.name == transform.name) > 1;
            }

            int sameNameCount = 0;
            Transform parent = transform.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == transform.name)
                {
                    sameNameCount++;
                }
            }

            return sameNameCount > 1;
        }

        private static void RenameObject(GameObject target)
        {
            string baseName = target.name;
            string newName = GetAvailableName(target.transform, baseName);
            Undo.RecordObject(target, "Rename duplicated TOD hierarchy object");
            target.name = newName;
            EditorUtility.SetDirty(target);
        }

        private static string GetAvailableName(Transform target, string baseName)
        {
            HashSet<string> usedNames = new HashSet<string>(GetSiblingOrRootNames(target));
            for (int i = 1; i < MaxRenameCount; i++)
            {
                string candidate = $"{baseName}_{i}";
                if (!usedNames.Contains(candidate))
                {
                    return candidate;
                }
            }

            return $"{baseName}_{MaxRenameCount}";
        }

        private static IEnumerable<string> GetSiblingOrRootNames(Transform target)
        {
            if (target.parent == null)
            {
                foreach (GameObject root in target.gameObject.scene.GetRootGameObjects())
                {
                    if (root != target.gameObject)
                    {
                        yield return root.name;
                    }
                }

                yield break;
            }

            Transform parent = target.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != target)
                {
                    yield return child.name;
                }
            }
        }
    }
}
