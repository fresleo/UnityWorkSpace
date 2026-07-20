// Created by: WangYu   Date: 2025-12-15

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    public class OutlineDistortRendererFeature : ScriptableRendererFeature
    {
        public OutlineDistortSettings settings;
        
        private OutlineDistortRenderPass m_renderPass;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            m_renderPass?.Dispose();
        }

        public override void Create()
        {
            m_renderPass = new OutlineDistortRenderPass();
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            // base.SetupRenderPasses(renderer, in renderingData);

            var realRenderer = renderer as XKnightRenderer;
            RTHandle characterMask = realRenderer?.CharacterDepthTexture;
            
#if UNITY_EDITOR
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                if (characterMask == null)
                {
                    // Debug.LogError("获取不到角色的深度遮罩");
                }
            }
#endif // UNITY_EDITOR
            
            m_renderPass.SetBlitTarget(characterMask);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 没开后处理
            if (!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            if (m_renderPass.ExecuteSetup(this.settings))
            {
                renderer.EnqueuePass(m_renderPass);
            }
        }
        
        public void ApplyAlpha(float alpha)
        {
            m_renderPass?.ApplyAlpha(alpha);
        }
    }
}