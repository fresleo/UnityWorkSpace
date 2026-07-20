using UnityEngine;

namespace RadiantGI.Universal
{
    /// <summary>
    /// 辐照度探针
    /// </summary>
    [ExecuteInEditMode]
    public class RadiantProbe : MonoBehaviour
    {
        private ReflectionProbe m_probe;

        private void OnEnable()
        {
            m_probe = GetComponent<ReflectionProbe>();
            RadiantRenderFeature.RegisterReflectionProbe(m_probe);
        }

        private void OnDisable()
        {
            RadiantRenderFeature.UnregisterReflectionProbe(m_probe);
        }
    }
}