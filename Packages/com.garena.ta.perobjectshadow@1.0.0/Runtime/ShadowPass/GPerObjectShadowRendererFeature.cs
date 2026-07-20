
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA
{
    /// <summary>
    /// 逐物体阴影 RendererFeature
    /// </summary>
    public class GPerObjectShadowRendererFeature : ScriptableRendererFeature
    {
        /// <summary>SRP Batcher 合批用Shader (默认引用Package的Shadow)</summary>
        public const string shadowOnlyShaderName = "GarenaTA/GPerObjectShadow/ShadowOnly";

        public GPerObjectShadowSettings settings = new GPerObjectShadowSettings();

        private GPerObjectShadowMapPass shadowPass;

        private GPerObjectShadowResolvePass resolvePass;

        private GPerObjectShadowApplyPass applyPass;

        public static Material shadowPassMaterial;

        public override void Create()
        {
            if (settings.shadowPassSettings.shadowOnlyShader == null)
            {
                settings.shadowPassSettings.shadowOnlyShader = Shader.Find(shadowOnlyShaderName);
            }

            shadowPassMaterial = new Material(settings.shadowPassSettings.shadowOnlyShader)
            {
                enableInstancing = false,
                hideFlags = HideFlags.DontSave,
                doubleSidedGI = false,
                renderQueue = 2000
            };
            
            shadowPass = new GPerObjectShadowMapPass(settings);

            resolvePass = new GPerObjectShadowResolvePass(settings);

            applyPass = new GPerObjectShadowApplyPass(settings);

            Shader.SetGlobalFloat(GPerObjectShadowPropertyID.PID_GPerObjectShadowEnable, IsShadowEnable() ? 1 : 0);
            Shader.SetGlobalFloat(GPerObjectShadowPropertyID.PID_GPO_CharacterShadowEnable, IsSelfShadowEnable() ? 1 : 0);

            if (!IsEnable())
            {
                GPerObjectShadowManager.Instance.DisableAll();
                shadowPass.Disable();
            }
        }

        protected override void Dispose(bool disposing)
        {
            shadowPass.Dispose();
            base.Dispose(disposing);
            GPerObjectShadowManager.Instance.DisableAll();
            Shader.SetGlobalFloat(GPerObjectShadowPropertyID.PID_GPerObjectShadowEnable, 0);
            Shader.SetGlobalFloat(GPerObjectShadowPropertyID.PID_GPO_CharacterShadowEnable, 0);
        }

        bool IsEnable()
        {
            return isActive && (settings.shadowPassSettings.enable || settings.selfShadowPassSetting.enable);
        }

        bool IsShadowEnable()
        {
            return isActive && settings.shadowPassSettings.enable;
        }

        bool IsSelfShadowEnable()
        {
            return isActive && settings.selfShadowPassSetting.enable;
        }

        public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            if (cameraData.cameraType == CameraType.Preview
                || UniversalRenderer.IsOffscreenDepthTexture(in cameraData))
                return;

            //开启SRP Batcher会在常规渲染前更新数据，这里是不开启SRP Batcher的路径
            if (!settings.shadowPassSettings.srpBatcher)
            {
                UpdateDataList(cameraData.camera);
            }
        }

        public void UpdateDataList(Camera camera)
        {
            if (camera.cameraType == CameraType.Preview)
                return;

            //相机剔除前更新逐阴影目标状态，避免切换回正常级联阴影方案时闪烁
            if (IsEnable())
            {
                GPerObjectShadowManager.Instance.UpdateFinalTargetDataList(settings.shadowPassSettings, camera);
                GPerObjectShadowManager.Instance.CascadeShadow(!IsShadowEnable());
            }
            else
            {
                GPerObjectShadowManager.Instance.DisableAll();
                shadowPass.Disable();
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || UniversalRenderer.IsOffscreenDepthTexture(in renderingData.cameraData))
                return;

            Shader.SetGlobalFloat(GPerObjectShadowPropertyID.PID_GPerObjectShadowEnable, IsShadowEnable() ? 1 : 0);
            Shader.SetGlobalFloat(GPerObjectShadowPropertyID.PID_GPO_CharacterShadowEnable, IsSelfShadowEnable() ? 1 : 0);

            if (IsEnable())
            {
                if (!settings.shadowPassSettings.srpBatcher)
                    renderer.EnqueuePass(shadowPass);

                if (settings.resolvePassSettings.enable)
                {
                    renderer.EnqueuePass(resolvePass);

                    if (settings.applyPassSettings.enable)
                        renderer.EnqueuePass(applyPass);
                }
            }
        }

        public void RenderShadowMap(ScriptableRenderContext context, Camera camera, CommandBuffer cmd, Light light)
        {
            if (camera.cameraType == CameraType.Preview)
                return;

            if (IsEnable())
                shadowPass?.ExecuteBeforeNormalRendering(context, camera, cmd, light);
        }
    }
}
