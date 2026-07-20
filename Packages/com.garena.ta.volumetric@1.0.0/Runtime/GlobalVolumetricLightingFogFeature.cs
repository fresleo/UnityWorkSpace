using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA.VolumetricLightingFog
{
    public enum DownsampleFactor : byte
    {
        One = 1,
        Half = 2,
        Quarter = 4,
        Eighth = 8
    }

    [Serializable]
    public class GlobalVolumetricLightingFogSetting
    {
        public Shader DownsampleDepthShader;
        public Shader VolumetricLightingShader;
        public DownsampleFactor Downsample = DownsampleFactor.Half;
    }

    public class GlobalVolumetricLightingFogFeature : ScriptableRendererFeature
    {
        public GlobalVolumetricLightingFogSetting Setting = new GlobalVolumetricLightingFogSetting();
        GlobalVolumetricLightingFogPass m_pass;
        public override void Create()
        {
            if (Setting.DownsampleDepthShader == null)
                Setting.DownsampleDepthShader = Shader.Find("Unlit/VolumetricLightingFogDownsampleDepth");
            if (Setting.VolumetricLightingShader == null)
                Setting.VolumetricLightingShader = Shader.Find("Unlit/VolumetricLightingFog");

            m_pass = new GlobalVolumetricLightingFogPass(Setting);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //renderer.EnqueuePass(m_pass);
            var volumeStack = VolumeManager.instance.stack;
            var volumetricFogSettings = volumeStack.GetComponent<GlobalVolumetricLightingFogSettings>();

            if (volumetricFogSettings != null)
            {
                m_pass.Setup(volumetricFogSettings);
                renderer.EnqueuePass(m_pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_pass?.Dispose();
        }
    }
}