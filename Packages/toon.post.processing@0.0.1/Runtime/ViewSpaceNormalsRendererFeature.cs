// Created By: WangYu  Date: 2024-08-05

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    public class ViewSpaceNormalsRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            public int renderPassEventOffset = 1;
            
            public LayerMask drawLayerMask = -1;
        }
        
        [SerializeField]
        private Settings m_settings = new();
        
        private ViewSpaceNormalsRenderPass m_renderPass;

        protected override void Dispose(bool disposing)
        {
            m_renderPass?.Dispose();
        }

        public override void Create()
        {
            m_renderPass = new ViewSpaceNormalsRenderPass();
            m_renderPass.CreateAssets();
            m_renderPass.Setup(m_settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_renderPass);
        }
    }
}