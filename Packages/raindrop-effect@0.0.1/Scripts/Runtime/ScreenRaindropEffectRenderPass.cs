// Created By: WangYu  Date: 2024-11-18

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RaindropEffect
{
    public class ScreenRaindropEffectRenderPass : ScriptableRenderPass
    {
        private readonly string _profilerTag, _cmdTag;
        
        /// <summary>
        /// 帧时间增量
        /// </summary>
        public float deltaTime;

        private RaindropSimulator m_simulator;
        private RaindropRenderer m_renderer;
        
        public ScreenRaindropEffectRenderPass()
        {
            _profilerTag = nameof(ScreenRaindropEffectRenderPass);
            _cmdTag = _profilerTag + "_cmd";
            
            base.profilingSampler = new ProfilingSampler(_profilerTag);
        }

        public void Setup(RaindropSimulator simulator, RaindropRenderer renderer)
        {
            m_simulator = simulator;
            m_renderer = renderer;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_cmdTag);
            using (new ProfilingScope(cmd, this.profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                RTHandle sourceRenderTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                RTHandle destinationRenderTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                
                m_simulator.Update(this.deltaTime);

                cmd.Blit(sourceRenderTarget, m_renderer.backgroundTex, 0, 0);

                m_renderer.RenderRaindrops(m_simulator.raindropList, this.deltaTime);
                m_renderer.BlendRaindropEffect();
                
                RenderTexture raindropEffectTexture = m_renderer.GetRaindropEffectTexture();
                cmd.Blit(raindropEffectTexture, destinationRenderTarget);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        
    }
}