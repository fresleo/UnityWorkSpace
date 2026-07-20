using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA
{
    /// <summary>
    /// 将屏幕空间的阴影RT应用到画面上
    /// </summary>
    public class GPerObjectShadowApplyPass : ScriptableRenderPass
    {
        private GPerObjectShadowSettings settings;
        private GPerObjectShadowApplyPassSettings passSettings;
        //private RTHandle m_CameraColorTarget;

        private Material applyMaterial;

        public GPerObjectShadowApplyPass(GPerObjectShadowSettings settings)
        {
            this.settings = settings;
            this.passSettings = settings.applyPassSettings;

            this.profilingSampler = new ProfilingSampler(nameof(GPerObjectShadowApplyPass));

            this.renderPassEvent = passSettings.Event;

            if (passSettings.applyShader == null)
            {
                passSettings.applyShader = Shader.Find(GPerObjectShadowPropertyID.ApplyShaderName);
            }

            if (passSettings.applyShader != null)
                applyMaterial = new Material(passSettings.applyShader);
        }

        //public void SetTarget(RTHandle colorHandle)
        //{
        //    m_CameraColorTarget = colorHandle;
        //}

        //public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        //{
        //    ConfigureTarget(m_CameraColorTarget);
        //}

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ResetTarget();
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (applyMaterial == null)
                return;

            CommandBuffer cmd = renderingData.commandBuffer; 
            using (new ProfilingScope(cmd, profilingSampler))
            {
                applyMaterial.SetColor(GPerObjectShadowPropertyID.PID_Color, settings.shadowColor);
                applyMaterial.SetFloat(GPerObjectShadowPropertyID.PID_SrcBlend, (float)passSettings.srcBlend);
                applyMaterial.SetFloat(GPerObjectShadowPropertyID.PID_DstBlend, (float)passSettings.dstBlend);

                Blitter.BlitTexture(cmd, new Vector4(1, 1, 0, 0), applyMaterial, 0);
                //Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, applyMaterial, 0);

                ResetMainLightShadowSettings(cmd, ref renderingData);
            }
        }

        void ResetMainLightShadowSettings(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ShadowData shadowData = renderingData.shadowData;
            int cascadesCount = shadowData.mainLightShadowCascadesCount;
            bool mainLightShadows = renderingData.shadowData.supportsMainLightShadows;
            bool receiveShadowsNoCascade = mainLightShadows && cascadesCount == 1;
            bool receiveShadowsCascades = mainLightShadows && cascadesCount > 1;

            // Before transparent object pass, force to disable screen space shadow of main light
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, false);

            // then enable main light shadows with or without cascades
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, receiveShadowsNoCascade);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, receiveShadowsCascades);
        }
    }
}