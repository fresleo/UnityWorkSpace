// Created By: WangYu  Date: 2024-05-07

using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpecularProbe
{
    /// <summary>
    /// 球形高光假灯脚本
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class SphereSpecularLight : AbsSpecularLight
    {
        private GameObject m_instance;
        
        private void Start()
        {
#if UNITY_EDITOR
            GetLight();
#else
            if (Application.isPlaying)
            {
                HideSpecular(); // 确保不会出现意外没回收的情况
                Destroy(this);
            }
#endif
        }
        
        public override void HideSpecular()
        {
            // 直接把实体销毁了就行
            if (m_instance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_instance);
                }
                else
                {
                    DestroyImmediate(m_instance);
                }
                m_instance = null;
            }
        }
        
#if UNITY_EDITOR
        public float intensityMultiplier = 1;
        public float radius = 0.25f;

        private Light m_light;
        
        private void OnDrawGizmosSelected()
        {
            if(!this.enabled) return;
            
            GetLight();

            var lastColor = Gizmos.color;
            {
                Gizmos.color = m_light.color;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
            Gizmos.color = lastColor;
        }
        
        private void GetLight()
        {
            if (m_light == null)
            {
                m_light = GetComponent<Light>();
            }
        }
        
        public override void ReadyToBake()
        {
            GetLight();
            HideSpecular();
            
            m_instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var sphereCollider = m_instance.GetComponent<Collider>();
            if (sphereCollider != null) DestroyImmediate(sphereCollider);

            SetupSpecular(m_instance, m_light, intensityMultiplier, new Vector3(radius, radius, radius));
        }
        
        private void SetupSpecular(GameObject meshGo, Light bakeLight, float multiplier, Vector3 size)
        {
            meshGo.transform.position = bakeLight.transform.position;
            meshGo.transform.rotation = bakeLight.transform.rotation;
            meshGo.transform.localScale = size;
            
            GameObjectUtility.SetStaticEditorFlags(meshGo, StaticEditorFlags.ReflectionProbeStatic);
            
            var meshRenderer = meshGo.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = GetLightMaterial(bakeLight.color, bakeLight.intensity * multiplier);
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.allowOcclusionWhenDynamic = false;
                meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
        }
        
        private static Material GetLightMaterial(Color color, float intensity)
        {
            Material mat = null;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                mat = new Material(GraphicsSettings.currentRenderPipeline.defaultParticleMaterial.shader);
            }

            if (mat != null)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", color * intensity);
                }
            }
            
            return mat;
        }
#endif //UNITY_EDITOR
    }
}