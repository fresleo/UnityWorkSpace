// Created By: WangYu  Date: 2025-03-20

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace XKT.TOD.Utils
{
    public static class TODUtils
    {
        public static readonly int sp_Tint = Shader.PropertyToID("_Tint");
        public static readonly int sp_Exposure = Shader.PropertyToID("_Exposure");
        public static readonly int sp_Rotation = Shader.PropertyToID("_Rotation");
        public static readonly int sp_Tex = Shader.PropertyToID("_Tex");

        private static StringBuilder s_sb = new();

        public static string GetHierarchyPath(Transform tf)
        {
            if (tf == null) return string.Empty;

            string path = tf.name;
            Transform parent = tf.parent;

            while (parent != null)
            {
                s_sb.Clear();
                s_sb.Append(parent.name).Append("/").Append(path);

                path = s_sb.ToString();
                parent = parent.parent;
            }

            return path;
        }

        private static Transform FindRoot(string name)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (var root in activeScene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root.transform;
                }
            }

            return null;
        }

        public static Transform FindHierarchyPath(string path, int forwardIndex = 0)
        {
            if (string.IsNullOrEmpty(path)) return null;

            string[] parts = path.Split('/');

            // 查找根对象
            Transform current = FindRoot(parts[0]);
            if (current == null) return null;

            // 遍历层级结构
            int len = parts.Length - forwardIndex;
            if (len > 1)
            {
                for (int i = 1; i < len; i++)
                {
                    Transform child = current.Find(parts[i]);
                    if (child == null) return null;

                    current = child;
                }
            }

            return current;
        }

        public static string GetHierarchyGameObjectName(string hierarchyPath)
        {
            string[] parts = hierarchyPath.Split('/');
            if (parts.Length > 1)
            {
                return parts[^1];
            }
            else
            {
                return parts[0];
            }
        }

        public static readonly int sp_EmissionTod = Shader.PropertyToID("_EmissionTOD");

        public static bool CheckEmissionMeshRenderer(MeshRenderer mr)
        {
            if (mr == null) return false;

            // 没勾选贡献 GI，不参与 Lightmap 烘焙
            // if (!GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.ContributeGI)) return false;

            // 没有材质，这样的渲染都会有问题
#if UNITY_EDITOR
            if (mr.sharedMaterials == null || mr.sharedMaterials.Length == 0) return false;
#else
            if (mr.materials == null || mr.materials.Length == 0) return false;
#endif

            return true;
        }

        public static bool CheckEmissionMaterial(Material mat)
        {
            if (mat == null) return false;

            // 材质没有设置任何自发光特性
            if ((mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) == 0) return false;

            // 检查材质是否勾了自发光参与 TOD 的开关
            if (!mat.HasProperty(sp_EmissionTod) || mat.GetFloat(sp_EmissionTod) < 1) return false;

            return true;
        }

        public static void DestroyUnityObject(UnityObject obj)
        {
            if (obj == null) return;

#if UNITY_EDITOR
            if (Application.isPlaying && !UnityEditor.EditorApplication.isPaused)
                UnityObject.Destroy(obj);
            else
                UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
        }
        
        /// <summary>
        /// 在场景中根据类型查找所有目标对象
        /// </summary>
        /// <param name="includeInactive">false: 只找激活了的组件</param>
        /// <typeparam name="TComponent">组件泛型类型</typeparam>
        /// <returns></returns>
        public static List<TComponent> FindObjectsOfTypeInActiveScene<TComponent>(bool includeInactive = false) where TComponent : Component
        {
            Scene activeScene = SceneManager.GetActiveScene();
            List<TComponent> result = FindObjectsOfTypeInTargetScene<TComponent>(activeScene, includeInactive);
            return result;
        }
        
        public static List<TComponent> FindObjectsOfTypeInTargetScene<TComponent>(Scene targetScene, bool includeInactive = false) where TComponent : Component
        {
            var result = new List<TComponent>();

            // 限定为已加载的场景，防止跨场景搜索
            if (!targetScene.isLoaded)
            {
                return result;
            }

            GameObject[] rootObjects = targetScene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                var monos = rootObj.GetComponentsInChildren<TComponent>(includeInactive);
                result.AddRange(monos);
            }

            return result;
        }
        
        public static TComponent FindObjectOfTypeInTargetScene<TComponent>(Scene targetScene, bool includeInactive = false) where TComponent : Component
        {
            if (!targetScene.isLoaded)
            {
                return null;
            }

            GameObject[] rootObjects = targetScene.GetRootGameObjects();
            foreach (GameObject rootObj in rootObjects)
            {
                var mono = rootObj.GetComponentInChildren<TComponent>(includeInactive);
                if (mono != null)
                {
                    return mono;
                }
            }

            return null;
        }
        
    }
}