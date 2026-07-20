// Created By: WangYu  Date: 2024-05-07

using System.Collections.Generic;
using UnityEngine;

namespace SpecularProbe
{
    /// <summary>
    /// 镜面高光探针渲染器
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(ReflectionProbe))]
    public class SpecularProbeRenderer : MonoBehaviour
    {
        private void Start()
        {
#if UNITY_EDITOR
            GetProbe();
#else
            if (Application.isPlaying)
            {
                Destroy(this);
            }
#endif
        }
        
#if UNITY_EDITOR
        public float radius = 100;

        private ReflectionProbe m_probe;
        
        private void OnEnable()
        {
            UnityEditor.Lightmapping.bakeCompleted += OnBakeCompleted;
        }

        private void OnDisable()
        {
            UnityEditor.Lightmapping.bakeCompleted -= OnBakeCompleted;
        }

        private void OnBakeCompleted()
        {
            Debug.Log("Lightmap 烘焙结束，开始烘焙镜面高光");
            Bake();
        }
        
        private void OnDrawGizmosSelected()
        {
            if(!this.enabled) return;
            
            var lastColor = Gizmos.color;
            {
                Gizmos.color = new Color(1, 1, 1, 0.25f);
                Gizmos.DrawWireSphere(transform.position, radius);
            }
            Gizmos.color = lastColor;
        }
        
        private void GetProbe()
        {
            if (m_probe == null)
            {
                m_probe = GetComponent<ReflectionProbe>();
            }
        }
        
        public void Bake()
        {
            GetProbe();

            var closeLights = new List<ISpecularLight>();
            
            var allLights = FindObjectsOfType<AbsSpecularLight>();
            for (int i = 0; i < allLights.Length; i++)
            {
                var itemLight = allLights[i];
                if(!itemLight.enabled) continue;
                
                // 只处理范围内的高光灯
                if ((itemLight.transform.position - transform.position).sqrMagnitude < radius * radius)
                {
                    itemLight.ReadyToBake();
                    closeLights.Add(itemLight);
                }
            }
            
            // 加载已经烘焙过的反射球，并重烘1次
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(m_probe.bakedTexture);
            UnityEditor.Lightmapping.BakeReflectionProbe(m_probe, assetPath);
            
            // 回收参与的灯
            for (int i = 0; i < closeLights.Count; i++)
            {
                var itemLight = closeLights[i];
                itemLight.HideSpecular();
            }
        }
        
        public void BakeAll()
        {
            var renderers = FindObjectsOfType<SpecularProbeRenderer>();
            foreach (var item in renderers)
            {
                item.Bake();
            }
        }
#endif //UNITY_EDITOR
    }
}