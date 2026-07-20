using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    public class FullScreenTransitionRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingPostProcessing;

            public Camera TargetCamera;
            
            public Material Material;
            public float MaxFarDepth, MaxRadius, EdgeWidth;
            public Transform TransitionTransform;
            public Vector3 TransitionCenterPosition;
            public bool UseBlendTex = false;
            public RenderTexture BlendRT;

            public Material BackMaterial;
            public float BackMaxRadius;
            public Transform BackTransitionTF;
            public Vector3 BackTransitionCenterPosition;
            public bool BackUseBlendTex = false;
            public RenderTexture BackBlendRT;

            public void Reset()
            {
                MaxFarDepth = 0;
                MaxRadius = 0;
                EdgeWidth = 0;
                TransitionTransform = null;
                TransitionCenterPosition = Vector3.zero;
                UseBlendTex = false;
                BlendRT = null;

                BackMaxRadius = 0;
                BackTransitionTF = null;
                BackTransitionCenterPosition = Vector3.zero;
                BackUseBlendTex = false;
                BackBlendRT = null;
            }
        }

        public Settings Setting = new();
        
        private FullScreenTransitionPass m_pass;
        private FullScreenTransitionBackPass m_backPass;

        public override void Create()
        {
            m_pass = new FullScreenTransitionPass(Setting);
            m_backPass = new FullScreenTransitionBackPass(Setting);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            if (Setting.TargetCamera == null)
            {
                Setting.TargetCamera = Camera.main;
            }
            if (Setting.TargetCamera == null)
            {
                return;
            }

            if (Setting.TargetCamera.GetInstanceID() == renderingData.cameraData.camera.GetInstanceID())
            {
                if (m_pass.Setup(Setting))
                {
                    renderer.EnqueuePass(m_pass);
                }
                if (m_backPass.Setup(Setting))
                {
                    renderer.EnqueuePass(m_backPass);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Setting.Reset();
            m_pass?.Dispose();
            m_backPass?.Dispose();
        }

        public void DisableRendererFeature()
        {
            m_pass?.DisableRendererFeature();
            m_backPass?.DisableRendererFeature();
        }
    }
}