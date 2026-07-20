// Created By: WangYu  Date: 2025-06-05

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using XKT.Editor.Inspectors;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

using XKT.TOD.Utils;

namespace XKT.TOD.Lightmap
{
    /// <summary>
    /// 在 bake 时，自动切换阴影投射和 GI 设置
    /// </summary>
    [ExecuteAlways]
    public class AutoSwitchOfShadowOnlyAndGI : MonoBehaviour
    {
        public MeshRenderer[] lightmapObjects;
        public MeshRenderer[] litAlphaTestObjects;
        public GameObject[] shadowOnlyObjects;

#if UNITY_EDITOR
        // lightmap 正在烘焙中
        private bool _lightmapIsBaking;
#endif // UNITY_EDITOR

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.Lightmapping.bakeStarted -= BakeStarted;
            UnityEditor.Lightmapping.bakeCompleted -= BakeCompleted;
            UnityEditor.EditorApplication.delayCall -= OnBakeCompleted;

            if (_lightmapIsBaking)
            {
                OnBakeCompleted();
            }
#endif // UNITY_EDITOR
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.Lightmapping.bakeStarted -= BakeStarted;
            UnityEditor.Lightmapping.bakeStarted += BakeStarted;

            UnityEditor.Lightmapping.bakeCompleted -= BakeCompleted;
            UnityEditor.Lightmapping.bakeCompleted += BakeCompleted;
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            RemoveDuplicateAndOverlappedObjects();

            if (shadowOnlyObjects != null)
            {
                foreach (var item in shadowOnlyObjects)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    
                    SetLightmapFlags(item, true);
                }
            }

            if (lightmapObjects != null)
            {
                foreach (var item in lightmapObjects)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    
                    SetLightmapFlags(item.gameObject);
                }
            }

            if (litAlphaTestObjects != null)
            {
                foreach (var item in litAlphaTestObjects)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    SetLightmapFlags(item.gameObject, true);
                }
            }
        }
        
        public void BakeStarted()
        {
            _lightmapIsBaking = true;
            SwitchLightmapObjects(true);
            SwitchShadowOnlyObjects(true);
        }

        public void BakeCompleted()
        {
            // 因为可能有其它地方也在监听 lightmap 的结束，所以需要延迟调用，确保不干扰其它地方的执行
            UnityEditor.EditorApplication.delayCall -= OnBakeCompleted;
            UnityEditor.EditorApplication.delayCall += OnBakeCompleted;
        }

        private void OnBakeCompleted()
        {
            _lightmapIsBaking = false;
            ApplyRuntimeSettings();
        }

        
        // 队列去重，队列里都是唯一的对象
        private void RemoveDuplicateAndOverlappedObjects()
        {
            HashSet<GameObject> usedObjects = new HashSet<GameObject>();

            litAlphaTestObjects = FilterMeshRendererObjects(litAlphaTestObjects, usedObjects);
            lightmapObjects = FilterMeshRendererObjects(lightmapObjects, usedObjects);
            shadowOnlyObjects = FilterGameObjects(shadowOnlyObjects, usedObjects);
        }

        private MeshRenderer[] FilterMeshRendererObjects(MeshRenderer[] sourceObjects, HashSet<GameObject> usedObjects)
        {
            if (sourceObjects == null || sourceObjects.Length == 0)
            {
                return sourceObjects;
            }

            List<MeshRenderer> result = new List<MeshRenderer>();
            foreach (var item in sourceObjects)
            {
                // 保留空槽位
                if (item == null || item.gameObject == null)
                {
                    result.Add(item);
                    continue;
                }

                if (!usedObjects.Add(item.gameObject))
                {
                    continue;
                }

                result.Add(item);
            }

            return result.ToArray();
        }

        private GameObject[] FilterGameObjects(GameObject[] sourceObjects, HashSet<GameObject> usedObjects)
        {
            if (sourceObjects == null || sourceObjects.Length == 0)
            {
                return sourceObjects;
            }

            List<GameObject> result = new List<GameObject>();
            foreach (var item in sourceObjects)
            {
                if (item == null)
                {
                    result.Add(item);
                    continue;
                }

                if (!usedObjects.Add(item))
                {
                    continue;
                }

                result.Add(item);
            }

            return result.ToArray();
        }
        
        public void SetLightmapFlags(GameObject obj, bool isRemove = false)
        {
            if (!obj)
            {
                return;
            }

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(obj);

            if (isRemove)
            {
                // 移除 flags
                if ((flags & StaticEditorFlags.ContributeGI) != 0)
                {
                    flags &= ~StaticEditorFlags.ContributeGI;
                }

                if ((flags & StaticEditorFlags.ReflectionProbeStatic) != 0)
                {
                    flags &= ~StaticEditorFlags.ReflectionProbeStatic;
                }
            }
            else
            {
                // 添加 flags
                if ((flags & StaticEditorFlags.ContributeGI) == 0)
                {
                    flags |= StaticEditorFlags.ContributeGI;
                }

                if ((flags & StaticEditorFlags.ReflectionProbeStatic) == 0)
                {
                    flags |= StaticEditorFlags.ReflectionProbeStatic;
                }
            }

            GameObjectUtility.SetStaticEditorFlags(obj, flags);
        }
        
        
        /// <summary>
        /// 重新收集子层级中的 ShadowOnly 节点并应用静态标记。
        /// Lightmap / LitAlphaTest 对象需手动拖入列表。
        /// </summary>
        public void AutoConfigureShadowOnlyObjects()
        {
            List<GameObject> shadowOnlyObjectList = new List<GameObject>();
            CollectShadowOnlyObjects(transform, shadowOnlyObjectList);
            shadowOnlyObjects = shadowOnlyObjectList.ToArray();
            
            OnValidate(); // 去重 + 设置 lightmap 标志
            ApplyRuntimeSettings(); // 应用运行时设置
        }
        
        private void CollectShadowOnlyObjects(Transform root, List<GameObject> shadowOnlyObjectList)
        {
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponent<Renderer>() != null
                    && child.name.EndsWith(LODGroupRuleConstant.C_SHADOW_ONLY_SUFFIX, StringComparison.Ordinal))
                {
                    shadowOnlyObjectList.Add(child.gameObject);
                }

                CollectShadowOnlyObjects(child, shadowOnlyObjectList);
            }
        }
        
        /// <summary>
        /// 对当前组件所在场景内全部本组件应用运行时阴影设置，不重新收集子节点列表。
        /// </summary>
        public void RefreshAllInScene()
        {
            List<AutoSwitchOfShadowOnlyAndGI> switchers = 
                TODUtils.FindObjectsOfTypeInTargetScene<AutoSwitchOfShadowOnlyAndGI>(gameObject.scene, true);
            for (int i = 0; i < switchers.Count; i++)
            {
                AutoSwitchOfShadowOnlyAndGI switcher = switchers[i];
                if (switcher == null)
                {
                    continue;
                }

                switcher.ApplyRuntimeSettings();
            }
        }
#endif // UNITY_EDITOR
        

        /// <summary>
        /// 应用运行时设置
        /// </summary>
        public void ApplyRuntimeSettings()
        {
#if UNITY_EDITOR
            if (_lightmapIsBaking)
            {
                return;
            }
#endif // UNITY_EDITOR

            SwitchLightmapObjects(false);
            SwitchShadowOnlyObjects(false);
            
            TurnOffLODShadowCasting(this.transform);
        }
        
        // 切换烘焙 Lightmap 对象的投阴影开关。
        // 运行时保持关闭，换面数更低的 ShadowOnly 对象投射实时阴影，以节省性能。
        private void SwitchLightmapObjects(bool baking)
        {
            if (lightmapObjects != null)
            {
                foreach (var item in lightmapObjects)
                {
                    if (!item)
                    {
                        continue;
                    }

                    item.shadowCastingMode = baking ? ShadowCastingMode.On : ShadowCastingMode.Off;
                }
            }
        }
        
        // 切换 ShadowOnly 对象的显示/隐藏（烘焙时隐藏）
        private void SwitchShadowOnlyObjects(bool baking)
        {
            if (shadowOnlyObjects != null)
            {
                foreach (var item in shadowOnlyObjects)
                {
                    if (!item)
                    {
                        continue;
                    }

                    item.SetActive(!baking);
                }
            }
        }
        
        // 关闭 LOD 的阴影投射
        private void TurnOffLODShadowCasting(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                if (meshRenderer != null && IsLODSuffixName(child.name))
                {
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                TurnOffLODShadowCasting(child);
            }
        }
        
        // 是 LOD 的后缀名
        private static bool IsLODSuffixName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            for (int i = 0; i < LODGroupRuleConstant.S_LOD_SUFFIXES.Length; i++)
            {
                string suffix = LODGroupRuleConstant.S_LOD_SUFFIXES[i];
                if (!string.IsNullOrEmpty(suffix) && objectName.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
        
    }
}
