// Created By: WangYu  Date: 2025-04-02

using System;
using UnityEngine;
using XKT.TOD.DataStructure;
using XKT.TOD.Tag;

namespace XKT.TOD.Lightmap
{
    [AddComponentMenu("TOD/Tag/LightmapTag")]
    public class LightmapTag : AbsTodTag
    {
        private bool m_recorded;
        private int m_rawLightmapIndex;
        private Vector4 m_rawLightmapScaleOffset;
        
        private bool m_haveInit;
        private MeshRenderer m_mr;

        private void TryInit()
        {
            if(m_haveInit) return;
            m_haveInit = true;
            
            if (m_mr == null)
            {
                m_mr = gameObject.GetComponent<MeshRenderer>();
            }
        }

        private void RecordRawData()
        {
            if(m_recorded) return;
            m_recorded = true;

            TryInit();
            if (m_mr != null)
            {
                m_rawLightmapIndex = m_mr.lightmapIndex;
                m_rawLightmapScaleOffset = m_mr.lightmapScaleOffset;
            }
        }

        public void SetLightmapData(int rawLightmapCount, LightmapUniquenessData lud)
        {
            RecordRawData();

            TryInit();
            if (m_mr != null)
            {
                m_mr.lightmapIndex = rawLightmapCount + lud.lightmapIndex;
                m_mr.lightmapScaleOffset = lud.lightmapScaleOffset;
            }
        }

        public void ResetLightmapData()
        {
            if(!m_recorded) return;

            TryInit();
            if (m_mr != null)
            {
                m_mr.lightmapIndex = m_rawLightmapIndex;
                m_mr.lightmapScaleOffset = m_rawLightmapScaleOffset;
            }
        }
        
        #if UNITY_EDITOR
        
        private void OnValidate()
        {
            var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            if (meshRenderers == null)
            {
                return;
            }

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                //同一设置为0
                LightmapGroupEditorUtils.SetRendererBakeTag(meshRenderer, 0);
            }
        }
        
        #endif
        
    }
}