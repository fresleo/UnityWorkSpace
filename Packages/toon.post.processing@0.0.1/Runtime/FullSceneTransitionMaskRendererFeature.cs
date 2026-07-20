/*******************************************************************************
 * File: FullSceneTransitionMaskRendererFeature.cs
 * Author: WangYu
 * Date: 2026-01-23
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    public class FullSceneTransitionMaskRendererFeature : ScriptableRendererFeature
    {
        public FullSceneTransitionMaskSettings settings;
        
        private FullSceneTransitionMaskRenderPass _renderPass;
        private FullSceneTransitionMaskRenderBackPass _renderBackPass;

        public override void Create()
        {
            _renderPass = new FullSceneTransitionMaskRenderPass();
            _renderBackPass = new FullSceneTransitionMaskRenderBackPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }
            if (settings.TargetCamera == null)
            {
                return;
            }

            if (settings.TargetCamera.GetInstanceID() == renderingData.cameraData.camera.GetInstanceID())
            {
                if (_renderPass.ExecuteSetup(this.settings))
                {
                    renderer.EnqueuePass(_renderPass);
                }
                
                if (_renderBackPass.ExecuteSetup(this.settings))
                {
                    renderer.EnqueuePass(_renderBackPass);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.settings?.Reset();
            _renderPass?.Dispose();
            _renderBackPass?.Dispose();
        }
        
        public void DisableRendererFeature()
        {
            _renderPass?.OnDisableRendererFeature();
            // _renderBackPass?.DisableRendererFeature();
        }
    }
}