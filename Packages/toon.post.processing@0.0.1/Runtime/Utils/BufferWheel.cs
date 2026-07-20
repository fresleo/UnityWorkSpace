// Created By: WangYu  Date: 2024-08-05

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    public class BufferWheel
    {
        private bool m_swapBuffer;
        private RTHandle m_leftRT, m_rightRT;

        private string m_baseName;
        private string m_leftName, m_rightName;
        
        public void Dispose()
        {
            m_leftRT?.Release();
            m_leftRT = null;
            
            m_rightRT?.Release();
            m_rightRT = null;
        }

        public void ReAllocateIfNeeded(RenderTextureDescriptor desc, FilterMode fm, TextureWrapMode twm, string baseName)
        {
            if (m_baseName != baseName)
            {
                m_baseName = baseName;
                
                m_leftName = $"{m_baseName}_Left";
                m_rightName = $"{m_baseName}_Right";
            }
            
            XKnightRenderingUtils.ReAllocateIfNeeded(ref m_leftRT, desc, fm, twm, false, 1, 0, m_leftName);
            XKnightRenderingUtils.ReAllocateIfNeeded(ref m_rightRT, desc, fm, twm, false, 1, 0, m_rightName);
        }
        
        public RTHandle GetLeftBuffer()
        {
            if (m_swapBuffer)
            {
                return m_rightRT;
            }
            return m_leftRT;
        }
        
        public RTHandle GetRightBuffer()
        {
            if (m_swapBuffer)
            {
                return m_leftRT;
            }
            return m_rightRT;
        }

        public void SwapBuffer()
        {
            m_swapBuffer = !m_swapBuffer;
        }
        
    }
}