// Created By: WangYu  Date: 2024-07-16

using System;
using ToonPostProcessing.Volumes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    public class ToonPostProcessingRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            [Reload("WaterColorGroupRendererData.asset")]
            public WaterColorGroupRendererData waterColorGroupRendererData;
        }
        
        public Settings settings;

        private ToonPostProcessingRenderPass m_renderPass;


        protected override void Dispose(bool disposing)
        {
            m_renderPass?.Dispose();
        }

        public override void Create()
        {
            m_renderPass = new ToonPostProcessingRenderPass();
        }
        
#if UNITY_EDITOR
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            // 碰到 SceneView 的摄像机时，就把 SceneView 的渲染目标传递进去
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                m_renderPass.SetEditorCameraColorTarget(renderer.cameraColorTargetHandle);
            }
        }
#endif //UNITY_EDITOR

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 没开后处理
            if (!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }
            
            // 后处理里有这个 Volume 配置
            var volumeStack = VolumeManager.instance.stack;
            var wcV = volumeStack.GetComponent<WaterColor>();
            var soV = volumeStack.GetComponent<SobelOutline>();
            var poioV = volumeStack.GetComponent<PreObjectIdOutline>();
            var vsnoV = volumeStack.GetComponent<ViewSpaceNormalsOutline>();

            if (m_renderPass.ExecuteSetup(this.settings, wcV, soV, poioV, vsnoV))
            {
                renderer.EnqueuePass(m_renderPass);
            }
        }
        
    }
}