// Created By: WangYu  Date: 2025-04-01

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.Utils;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace XKT.TOD.Lightmap
{
    [ExecuteAlways]
    [AddComponentMenu("TOD/LightmapVolume")]
    public class LightmapVolume : MonoBehaviour, ILightmapVolume
    {
        private Collider m_volumeCollider;

        public Collider VolumeCollider => m_volumeCollider;

        public Vector3 Position => transform.position;


        // 编辑器代码
#if UNITY_EDITOR

        /// <summary>
        /// 被包含的渲染器
        /// </summary>
        public List<MeshRenderer> containedRenderers = new();

        private void Update()
        {
            UpdateCollider();
        }

        public const string c_iconPath = "Packages/com.unity.render-pipelines.core/Editor/Volume/Icons/Volume.png";
        
        private void OnDrawGizmos()
        {
            string iconName = Path.GetFileName(c_iconPath);
            Gizmos.DrawIcon(transform.position, iconName);
        }

        private void UpdateCollider()
        {
            var colls = GetComponents<Collider>();
            if (colls.Length > 1)
            {
                Debug.LogError($"{nameof(LightmapVolume)} 当前只支持单 Collider");
            }

            if (colls.Length > 0)
            {
                m_volumeCollider = colls[0];
            }
        }

        /// <summary>
        /// 刷新包含的渲染器
        /// </summary>
        public void RefreshContainedRenderers()
        {
            UpdateCollider();

            containedRenderers.Clear();
            m_volumeCollider?.FindRenderersInsideColliderWithGI(containedRenderers);
        }

        // 移除列表
        private static List<LightmapTag> s_removeLTL = new();

        /// <summary>
        /// 刷新标记脚本
        /// </summary>
        public static void UpdateTagScripts()
        {
            // 使用编辑器安全的方式查找当前场景中的 LightmapVolume 组件
            var lvs = TODUtils.FindObjectsOfTypeInActiveScene<LightmapVolume>();

            // 特殊情况，场景中1个 Volume 都没有的话，它可能是运行时场景，LightmapTag 已经挂好了，再改就得自己手动做了
            if (lvs.Count == 0)
            {
                return;
            }

            // 场景上现有的，其实都是潜在可能要移除的
            var lts = TODUtils.FindObjectsOfTypeInActiveScene<LightmapTag>();
            s_removeLTL.Clear();
            s_removeLTL.AddRange(lts);

            // 检查 Volume 的包含情况
            foreach (var lv in lvs)
            {
                lv.RefreshContainedRenderers();

                foreach (var cr in lv.containedRenderers)
                {
                    LightmapTag lt = cr.GetComponent<LightmapTag>();
                    if (lt != null)
                    {
                        // 再次命中，就可以从列表中移除了
                        if (s_removeLTL.Contains(lt))
                        {
                            s_removeLTL.Remove(lt);
                        }
                    }
                    else
                    {
                        // 新增组件
                        lt = cr.gameObject.AddComponent<LightmapTag>();
                    }
                    lt.UpdateHierarchyPath();
                }
            }

            // 移除已经过时的
            bool hasRemove = s_removeLTL.Count > 0;
            
            for (int i = s_removeLTL.Count - 1; i >= 0; i--)
            {
                LightmapTag lt = s_removeLTL[i];
                TODUtils.DestroyUnityObject(lt);
            }
            s_removeLTL.Clear();

            if (hasRemove)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

#endif // UNITY_EDITOR
    }
}