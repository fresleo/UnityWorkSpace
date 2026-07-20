
using System;

namespace UnityEngine.Rendering.Universal
{
    [DisallowMultipleRendererFeature("Sky")]
    public class SkyRendererFeature : ScriptableRendererFeature
    {
        private SkyPass m_Pass;

        public override void Create()
        {
            if (m_Pass == null)
            {
                m_Pass = new SkyPass();
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.renderType == CameraRenderType.Overlay 
                || renderingData.cameraData.camera.clearFlags != CameraClearFlags.Skybox)
            {
                return;
            }
            
            if (m_Pass != null && m_Pass.Setup())
            {
                renderer.EnqueuePass(m_Pass);
            }
        }

        private class SkyPass : ScriptableRenderPass
        {
            private ProfilingSampler m_ProfilingSampler = new(nameof(SkyPass));

            private SkyVolume m_SkyVolume;
            private SkyControl m_SkyControl;

            private const string k_HEIGHT_FOG = "_HEIGHT_FOG";

            internal SkyPass()
            {
                base.renderPassEvent = RenderPassEvent.BeforeRenderingSkybox + 1;

                m_SkyControl = new SkyControl();
            }

            public void Dispose()
            {
                m_SkyControl.Dispose();
            }

            internal bool Setup()
            {
                base.renderPassEvent = RenderPassEvent.BeforeRenderingSkybox + 1;

                return true;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                // Sky
                m_SkyVolume = VolumeManager.instance.stack.GetComponent<SkyVolume>();
                AtmosphericScatteringProfile rootProfile = m_SkyVolume != null ? m_SkyVolume.SkySetting.value : null;
                m_SkyControl.Update(rootProfile, ref renderingData.lightData);

                // Fog
                var fog = VolumeManager.instance.stack.GetComponent<ExponentialHeightFog>();
                if (fog != null && fog.IsActive())
                {
                    // 开关
                    CoreUtils.SetKeyword(cmd, k_HEIGHT_FOG, true);

                    // 设置控制参数
                    fog.PushFogParams(cmd, ref renderingData);
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {                   
                    if (m_SkyVolume != null && m_SkyVolume.RenderClouds.value)
                    {
                        m_SkyControl.DrawClouds(renderingData.cameraData.camera, cmd);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// <inheritdoc/>
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                CoreUtils.SetKeyword(cmd, k_HEIGHT_FOG, false);
            }
            
        }
    }
}
