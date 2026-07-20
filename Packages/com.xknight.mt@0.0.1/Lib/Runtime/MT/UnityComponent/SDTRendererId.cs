// Created By: WangYu  Date: 2024-05-30

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using com.xknight.mt.Lib.Runtime.MT.OcclusionCulling;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif //UNITY_EDITOR

namespace com.xknight.mt.Lib.Runtime.MT.UnityComponent
{
    public class SDTRendererId : MonoBehaviour
    {
        public enum EEventType
        {
            Show = 100000,
            Hide = 200000,
        }
        
        /// <summary>
        /// 被进行空间数据标记的渲染器 id
        /// </summary>
        public int id;

        private LODGroup m_lodGroup;
        private MeshRenderer m_meshRenderer;

        private bool m_isVisible;

        /// <summary>
        /// 延迟隐藏时间
        /// </summary>
        public static float s_delayHideTime = 0.5f;

        private DynamicOcclusionCuller m_culler;
        private float m_hideTimer = -1; // -1 也是停止的标志
        private float m_randomDelayHideTime; // 随机延迟隐藏时间


        private void OnDestroy()
        {
            int key = (int)EEventType.Show + this.id;
            EventManager.GetInstance.StopListening(key, ShowEvent);
            key = (int)EEventType.Hide + this.id;
            EventManager.GetInstance.StopListening(key, HideEvent);
            
            m_culler?.RemoveUpdateAction(RegisteredUpdateAction);
            m_culler = null;
            m_hideTimer = -1;
        }

        private void Start()
        {
            EnsureRenderComponents();
            SetVisibility(true);
            
            int key = (int)EEventType.Show + this.id;
            EventManager.GetInstance.StartListening(key, ShowEvent);
            key = (int)EEventType.Hide + this.id;
            EventManager.GetInstance.StartListening(key, HideEvent);
            
            RandomDelayHideTime();
            m_culler = FindObjectOfType<DynamicOcclusionCuller>();
            m_culler?.AddUpdateAction(RegisteredUpdateAction);
        }
        
        
        private void EnsureRenderComponents()
        {
            if (m_lodGroup == null)
            {
                m_lodGroup = GetComponent<LODGroup>();
            }
            if (m_meshRenderer == null)
            {
                m_meshRenderer = GetComponent<MeshRenderer>();
            }
        }
        
        private void ShowEvent()
        {
            m_hideTimer = -1;
            
            SetVisibility(true);
        }

        private void HideEvent()
        {
            if (m_isVisible)
            {
                m_isVisible = false;

                m_hideTimer = 0;
            }
        }
        
        private void RandomDelayHideTime()
        {
            m_randomDelayHideTime = Random.Range(s_delayHideTime, s_delayHideTime + 0.5f);
        }
        
        // 注册的 Update 动作
        private void RegisteredUpdateAction()
        {
            if(m_hideTimer < 0) return;
            
            if (m_hideTimer >= m_randomDelayHideTime)
            {
                m_hideTimer = -1;
                SetVisibility(false, false);
            }
            else
            {
                m_hideTimer += Time.deltaTime;
            }
        }
        
        /// <summary>
        /// 设置可见性
        /// </summary>
        public void SetVisibility(bool isVisible, bool checkState = true)
        {
            if (checkState)
            {
                if (m_isVisible == isVisible)
                {
                    return;
                }
            }
            
            m_isVisible = isVisible;
            
            if (m_lodGroup != null)
            {
                var lods = m_lodGroup.GetLODs();
                foreach (var itemLod in lods)
                {
                    foreach (var itemRenderer in itemLod.renderers)
                    {
                        if (itemRenderer.enabled != isVisible)
                        {
                            itemRenderer.enabled = isVisible;
                        }
                    }
                }
            }
            else if (m_meshRenderer != null)
            {
                if (m_meshRenderer.enabled != isVisible)
                {
                    m_meshRenderer.enabled = isVisible;
                }
            }
        }
        
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!IsValid())
            {
                // 验证不过的，把自己移除掉
                UnityEditor.EditorApplication.delayCall += () => { DestroyImmediate(this); };
            }
            
            RandomDelayHideTime();
        }
        
        private bool IsValid()
        {
            var lodGroup = GetComponent<LODGroup>();
            var meshRenderer = GetComponent<MeshRenderer>();
            
            bool result = lodGroup != null || meshRenderer != null;
            return result;
        }
        
        /// <summary>
        /// 整体的包围盒
        /// </summary>
        public Bounds WholeBounds
        {
            get
            {
                EnsureRenderComponents();
                
                if (m_lodGroup != null)
                {
                    var lods = m_lodGroup.GetLODs();
                    if (lods.Length > 0)
                    {
                        var lod0 = lods[0];
                        Bounds bnd = MTRuntimeUtils.GetWholeBounds(lod0.renderers);
                        return bnd;
                    }
                }

                if (m_meshRenderer != null)
                {
                    return m_meshRenderer.bounds;
                }

                return default;
            }
        }

        
        [MenuItem("Tools/巨大地形/清除当前场景的 SDTRendererId 组件")]
        private static void ClearSDTRendererIdComponents()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            ClearSceneIdComponents(activeScene);
        }
        
        [MenuItem("Tools/巨大地形/给当前场景挂 SDTRendererId 组件")]
        private static void GenerateSDTRendererIdComponents()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GenerateSceneIdComponents(activeScene);
        }
        
        /// <summary>
        /// 清除场景的 id 组件
        /// </summary>
        public static void ClearSceneIdComponents(Scene scene)
        {
            var rootGameObjects = scene.GetRootGameObjects();
            ClearIdComponents(rootGameObjects);
        }
        
        /// <summary>
        /// 生成场景的 id 组件
        /// </summary>
        public static void GenerateSceneIdComponents(Scene scene)
        {
            bool hasChanges = false;
            var hashSetIndex = new HashSet<int>();
            void CreateRendererId(GameObject go, ref IEnumerator<int> enumerator)
            {
                var hidden = go.GetComponent<SDTRendererId>();
                if (hidden != null) return;

                hidden = go.AddComponent<SDTRendererId>();
                hidden.id = enumerator.Current;

                if (!hashSetIndex.Add(hidden.id))
                {
                    Debug.LogError($"不应该出现这样的问题: {hidden.id}");
                }
                //Debug.Log($"{enumerator.Current} {hidden.gameObject.name} {hidden.gameObject.scene.name}");

                hasChanges = true;
                enumerator.MoveNext();
            }

            var rootGameObjects = scene.GetRootGameObjects();
            ValidateIdComponents(rootGameObjects);

            var enumerateSequence = EnumerateSequenceNumber(rootGameObjects).GetEnumerator();
            enumerateSequence.MoveNext();
            
            foreach (var rootGo in rootGameObjects)
            {
                var localLODGroups = rootGo.GetComponentsInChildren<LODGroup>(true);
                var localMeshRenderers = rootGo.GetComponentsInChildren<MeshRenderer>(true);
                
                // 给 LODGroup 挂组件
                foreach (var itemGroup in localLODGroups)
                {
                    // 处于激活状态的
                    if (itemGroup.gameObject.activeInHierarchy && itemGroup.enabled)
                    {
                        CreateRendererId(itemGroup.gameObject, ref enumerateSequence);
                    }
                }

                // 收集 LODGroup 管理的所有渲染器
                List<Renderer> groupsRenderers = new ();
                foreach (var itemGroup in localLODGroups)
                {
                    // 处于激活状态的
                    if (itemGroup.gameObject.activeInHierarchy && itemGroup.enabled)
                    {
                        foreach (var lod in itemGroup.GetLODs())
                        {
                            foreach (var renderer in lod.renderers)
                            {
                                groupsRenderers.Add(renderer);
                            }
                        }
                    }
                }

                foreach (var itemRenderer in localMeshRenderers)
                {
                    // 非 LODGroup 下的渲染器
                    if (!groupsRenderers.Contains(itemRenderer))
                    {
                        // 处于激活状态的
                        if (itemRenderer.gameObject.activeInHierarchy && itemRenderer.enabled)
                        {
                            // 不能是静态合批的
                            if (!itemRenderer.gameObject.isStatic && !itemRenderer.isPartOfStaticBatch)
                            {
                                CreateRendererId(itemRenderer.gameObject, ref enumerateSequence);
                            }
                        }
                    }
                }
            }

            enumerateSequence.Dispose();

            if (hasChanges)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        
        /// <summary>
        /// 清除 id 组件
        /// </summary>
        private static void ClearIdComponents(in GameObject[] objects)
        {
            foreach (var obj in objects)
            {
                var rendererIds = obj.GetComponentsInChildren<SDTRendererId>(true); // 未激活的也需要处理
                foreach (var rendererId in rendererIds)
                {
                    UnityEngine.Object.DestroyImmediate(rendererId);
                }
            }
        }
        
        /// <summary>
        /// 验证 id 组件是否存在重复！如果有，则删除所有以再生。
        /// </summary>
        private static void ValidateIdComponents(in GameObject[] gameObjects)
        {
            var validationHashSet = new HashSet<int>();

            SDTRendererId isError = null;
            foreach (var gameObject in gameObjects)
            {
                var rendererIds = gameObject.GetComponentsInChildren<SDTRendererId>(true); // 未激活的也需要处理
                foreach (var rendererId in rendererIds)
                {
                    // 重复的 id 或者不止一个组件
                    if (!validationHashSet.Add(rendererId.id) || rendererId.GetComponents<SDTRendererId>().Length != 1)
                    {
                        isError = rendererId;
                        break;
                    }
                }
            }

            if (isError == null) return;

            Debug.LogError($"{nameof(SDTRendererId)} 组件验证出错，开始删除已有的所有组件！");

            ClearIdComponents(gameObjects);
        }
        
        /// <summary>
        /// 枚举出可选用的序列号
        /// </summary>
        /// <param name="objects">对象数组，用于筛选出已经分配了 id 的对象</param>
        private static IEnumerable<int> EnumerateSequenceNumber(GameObject[] objects)
        {
            int[] rendererIds = objects.SelectMany(obj => obj.GetComponentsInChildren<SDTRendererId>(true))
                .Select(id => id.id)
                .ToArray();

            int sequentialId = 0;
            if (rendererIds.Length != 0)
            {
                sequentialId = rendererIds.Max();
                var missing = Enumerable.Range(0, sequentialId).Except(rendererIds);
                foreach (var i in missing)
                {
                    yield return i;
                }
            }

            while (true)
            {
                yield return ++sequentialId;
            }
            // ReSharper disable once IteratorNeverReturns
        }
#endif //UNITY_EDITOR
    }
}