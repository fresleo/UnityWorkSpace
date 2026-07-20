using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace XKT.TOD
{
    [RequireComponent(typeof(LODGroup))]
    [ExecuteAlways]
    public class LightmappedLOD : MonoBehaviour
    {
        private LODGroup m_lodGroup;

        public bool lastIsBillboard;
        
        void Awake()
        {
            m_lodGroup = GetComponent<LODGroup>();
            
            RendererInfoTransfer();
        }
        
#if UNITY_EDITOR
        private static readonly HashSet<string> s_dialogScenePaths = new();

        static LightmappedLOD()
        {
            EditorSceneManager.sceneClosed += OnEditorSceneClosed;
        }

        private static void OnEditorSceneClosed(Scene scene)
        {
            string scenePath = scene.path;
            if (!string.IsNullOrEmpty(scenePath))
            {
                s_dialogScenePaths.Remove(scenePath);
            }
        }
        
        void Update()
        {
            if (!Application.isPlaying)
            {
                RendererInfoTransfer();
            }
        }
#endif // UNITY_EDITOR

        /// <summary>
        /// 渲染器信息传输
        /// </summary>
        public void RendererInfoTransfer()
        {
            if (!m_lodGroup || m_lodGroup.lodCount < 2)
            {
                return;
            }

            LOD[] lods = m_lodGroup.GetLODs();
            int maxLodID = lods.Length;
            
            var lod0Rens = lods[0].renderers;
            for (int i = 0, imax = lod0Rens.Length; i < imax; ++i)
            {
                for (int lodID = 1; lodID < maxLodID; ++lodID)
                {
                    var curLodRens = lods[lodID].renderers;
                    if (i >= curLodRens.Length) 
                    {
                        continue;
                    }

                    var curRender = curLodRens[i];
                    
                    // 最后1级是公告板
                    if (lodID == maxLodID - 1 && this.lastIsBillboard)
                    {
                        curRender.lightmapIndex = -1;
                        continue;
                    }

                    // 逐级传递 Lightmap 信息
                    var lod0Render = lod0Rens[i];
                    if (curRender != null && lod0Render != null)
                    {
                        curRender.lightProbeUsage = lod0Render.lightProbeUsage;
                        curRender.lightmapIndex = lod0Render.lightmapIndex;
                        curRender.lightmapScaleOffset = lod0Render.lightmapScaleOffset;
                    }
                    else
                    {
                        Scene scene = this.gameObject.scene;
                        
                        string ownerPath = GetHierarchyPath(this.transform);
                        string errorTitle = $"[{nameof(LightmappedLOD)}] 配置异常";
                        string errorLog = $"{errorTitle}，场景 [{scene.name}] 中的 LODGroup 组件上有空配置，对象路径: {ownerPath}";
                        Debug.LogError(errorLog, this);
                        
#if UNITY_EDITOR
                        string scenePath = scene.path; // 新场景会变化
                        if (CanShowConfigurationDialog(scenePath) && s_dialogScenePaths.Add(scenePath))
                        {
                            string dialogMsg = $"场景 [{scene.name}] 中的 LODGroup 组件上有空配置。" + "\n已输出可点击日志，请在 Console 中定位并修复。";
                            EditorUtility.DisplayDialog(errorTitle, dialogMsg, "知道了");
                        }
#endif // UNITY_EDITOR
                    }
                }
            }
        }
        
        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            string path = target.name;
            Transform parent = target.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

#if UNITY_EDITOR
        private bool CanShowConfigurationDialog(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return false;
            }

            if (EditorUtility.IsPersistent(this))
            {
                return false;
            }

            GameObject owner = this.gameObject;
            if (owner == null)
            {
                return false;
            }

            if (EditorSceneManager.IsPreviewSceneObject(owner))
            {
                return false;
            }

            return true;
        }
#endif // UNITY_EDITOR
        
    }
}


