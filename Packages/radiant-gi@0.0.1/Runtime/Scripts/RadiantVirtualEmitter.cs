using UnityEngine;
using System.Collections.Generic;

namespace RadiantGI.Universal
{
    /// <summary>
    /// 辐照度虚拟发射器
    /// </summary>
    [ExecuteInEditMode]
    public class RadiantVirtualEmitter : MonoBehaviour
    {
        [ColorUsage(showAlpha: false, hdr: true)]
        public Color color = new Color(1, 1, 1);
        
        public bool addMaterialEmission;
        
        public Renderer targetRenderer;
        
        public Material material;

        public string emissionPropertyName = "_EmissionColor";
        
        public int materialIndex;

        public float intensity = 1f;
        public float range = 10f;

        public Vector3 boxCenter;
        public Vector3 boxSize = new Vector3(25, 25, 25);
        public bool boundsInLocalSpace = true;

        private int m_emissionNameId;
        private Renderer m_thisRenderer;

        private static List<Material> s_sharedMaterials = new List<Material>();

        private void OnValidate()
        {
            intensity = Mathf.Max(0, intensity);
            range = Mathf.Max(0, range);
        }

        private void OnEnable()
        {
            m_emissionNameId = Shader.PropertyToID(emissionPropertyName);
            m_thisRenderer = GetComponentInChildren<Renderer>();
            RadiantRenderFeature.RegisterVirtualEmitter(this);
        }

        private void OnDisable()
        {
            RadiantRenderFeature.UnregisterVirtualEmitter(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1f, 0, 0.75F);
            Gizmos.DrawWireSphere(transform.position, range);
        }


        public Color GetGIColor()
        {
            Color sum = color;
            if (addMaterialEmission)
            {
                Material mat = material;
                if (mat == null)
                {
                    Renderer r = targetRenderer != null ? targetRenderer : m_thisRenderer;
                    if (r != null)
                    {
                        if (materialIndex == 0)
                        {
                            mat = r.sharedMaterial;
                        }
                        else
                        {
                            r.GetSharedMaterials(s_sharedMaterials);
                            if (materialIndex < s_sharedMaterials.Count)
                            {
                                mat = s_sharedMaterials[materialIndex];
                            }
                        }
                    }
                }

                if (mat != null && mat.HasProperty(m_emissionNameId))
                {
                    sum += mat.GetColor(m_emissionNameId);
                }
            }

            return sum * intensity;
        }


        public Vector4 GetGIColorAndRange()
        {
            Color giColor = GetGIColor();
            return new Vector4(giColor.r, giColor.g, giColor.b, range);
        }

        /// <summary>
        /// Returns emitter area of influence in world space
        /// </summary>
        /// <returns></returns>
        public Bounds GetBounds()
        {
            Bounds bounds = new Bounds(boxCenter, boxSize);
            if (boundsInLocalSpace)
            {
                bounds.center += transform.position;
            }

            return bounds;
        }

        /// <summary>
        /// Sets emitter area of influence in world space
        /// </summary>
        /// <param name="bounds"></param>
        public void SetBounds(Bounds bounds)
        {
            if (boundsInLocalSpace)
            {
                bounds.center -= transform.position;
            }

            boxCenter = bounds.center;
            boxSize = bounds.size;
        }
    }
}