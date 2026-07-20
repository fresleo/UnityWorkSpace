/*******************************************************************************
 * File: SpiralFluidTransitionPass.cs
 * Author: fan.shi
 * Date: 2026/05/20
 * Description: 旋涡流体转场全屏绘制 Pass。
 *
 * Notice:
 *******************************************************************************/

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    /// <summary>
    /// 旋涡流体转场全屏绘制 Pass。
    /// </summary>
    public class SpiralFluidTransitionPass : ScriptableRenderPass
    {
        private const int CMATERIAL_PASS_INDEX = 0;
        private const int CFULLSCREEN_TRIANGLE_VERTEX_COUNT = 3;
        private const float CMIN_CAMERA_SIZE = 1f;
        private const float CBRIGHTEN_PHASE_MIN = 0.05f;
        private const float CBRIGHTEN_PHASE_MAX = 0.95f;

        private SpiralFluidTransitionSettings _settings;
        private readonly string _cmdTag;
        private readonly string _captureCmdTag;
        private RTHandle _capturedFromRT;
        private Texture _resolvedWarmBrightLut;
        private bool _needCaptureFromRT;

        /// <summary>
        /// 内部 FromRT 是否仍待捕获。
        /// </summary>
        public bool NeedsFromCapture => _needCaptureFromRT;

        /// <summary>
        /// 创建旋涡流体转场全屏绘制 Pass。
        /// </summary>
        public SpiralFluidTransitionPass(SpiralFluidTransitionSettings setting)
        {
            _settings = setting;
            if (setting != null)
            {
                renderPassEvent = setting.renderEvent;
            }

            const string profilerTag = nameof(SpiralFluidTransitionPass);
            _cmdTag = profilerTag + "_cmd";
            _captureCmdTag = profilerTag + "_capture_cmd";
            profilingSampler = new ProfilingSampler(profilerTag);
        }

        /// <summary>
        /// 设置内部捕获 FromRT。
        /// </summary>
        public void SetCapturedFromRT(RTHandle capturedFromRT, bool needCapture)
        {
            _capturedFromRT = capturedFromRT;
            _needCaptureFromRT = needCapture;
        }

        /// <summary>
        /// 设置本帧解析后的暖亮 LUT。
        /// </summary>
        public void SetResolvedWarmBrightLut(Texture warmBrightLut)
        {
            _resolvedWarmBrightLut = warmBrightLut;
        }

        /// <summary>
        /// 重置内部捕获标记，供新一轮转场重新 Blit。
        /// </summary>
        public void ResetCaptureState()
        {
            if (_settings == null)
            {
                return;
            }

            if (_settings.fromRT == null && _settings.captureFromCameraColor)
            {
                _needCaptureFromRT = true;
            }
        }

        /// <summary>
        /// 检查本帧绘制参数是否可用，并同步 Pass 配置。
        /// </summary>
        public bool CanSetup(SpiralFluidTransitionSettings setting, Camera camera)
        {
            _settings = setting;
            if (_settings == null || camera == null || _settings.material == null)
            {
                return false;
            }

            renderPassEvent = _settings.renderEvent;
            return true;
        }

        /// <summary>
        /// 执行转场全屏绘制。
        /// </summary>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_settings == null || _settings.material == null || renderingData.cameraData.renderer == null)
            {
                return;
            }

            RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            if (cameraColorTarget == null)
            {
                return;
            }

            if (NeedsCapturePass())
            {
                CommandBuffer captureCmd = CommandBufferPool.Get(_captureCmdTag);
                CaptureInternalRTs(captureCmd, cameraColorTarget);
                context.ExecuteCommandBuffer(captureCmd);
                captureCmd.Clear();
                CommandBufferPool.Release(captureCmd);
            }

            RTHandle fromRT = GetCurrentFromRT();
            if (fromRT == null)
            {
                return;
            }

            float transitionProgress = Mathf.Clamp01(_settings.progress);
            float brightenPhase = Mathf.Clamp(_settings.BrightenPhaseRatio, CBRIGHTEN_PHASE_MIN, CBRIGHTEN_PHASE_MAX);
            float brightenOnlyProgress = transitionProgress <= 0f
                ? 0f
                : Mathf.Clamp01(Mathf.InverseLerp(0f, brightenPhase, transitionProgress));
            float visualProgress = transitionProgress <= brightenPhase
                ? 0f
                : Mathf.Clamp01(Mathf.InverseLerp(brightenPhase, 1f, transitionProgress));

            CommandBuffer cmd = CommandBufferPool.Get(_cmdTag);
            using (new ProfilingScope(cmd, profilingSampler))
            {
                DrawTransitionFrame(cmd, cameraColorTarget, renderingData.cameraData.camera, transitionProgress,
                    visualProgress, brightenOnlyProgress);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private bool NeedsCapturePass()
        {
            return _needCaptureFromRT && _capturedFromRT != null;
        }

        /// <summary>
        /// 释放 Pass 引用。
        /// </summary>
        public void Dispose()
        {
            _settings = null;
            _capturedFromRT = null;
            _resolvedWarmBrightLut = null;
        }

        private void CaptureInternalRTs(CommandBuffer cmd, RTHandle cameraColorTarget)
        {
            if (_needCaptureFromRT && _capturedFromRT != null)
            {
                Blitter.BlitCameraTexture(cmd, cameraColorTarget, _capturedFromRT);
                _needCaptureFromRT = false;
            }
        }

        private void DrawTransitionFrame(CommandBuffer cmd, RTHandle cameraColorTarget, Camera camera,
            float rawProgress, float visualProgress, float brightenOnlyProgress)
        {
            if (camera == null || cameraColorTarget == null)
            {
                return;
            }

            ApplyMaterialProperties(camera, rawProgress, visualProgress, brightenOnlyProgress);

            CoreUtils.SetRenderTarget(cmd, cameraColorTarget, RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
            cmd.DrawProcedural(Matrix4x4.identity, _settings.material, CMATERIAL_PASS_INDEX,
                MeshTopology.Triangles, CFULLSCREEN_TRIANGLE_VERTEX_COUNT);
        }

        private RTHandle GetCurrentFromRT()
        {
            return _settings == null ? null : _settings.fromRT != null ? _settings.fromRT : _capturedFromRT;
        }

        private RTHandle GetCurrentToRT()
        {
            return _settings?.toRT;
        }

        private void ApplyMaterialProperties(Camera camera, float rawProgress, float visualProgress,
            float brightenOnlyProgress)
        {
            Material material = _settings.material;
            float aspect = Mathf.Max(CMIN_CAMERA_SIZE, camera.pixelWidth) / Mathf.Max(CMIN_CAMERA_SIZE, camera.pixelHeight);

            RTHandle fromRT = GetCurrentFromRT();
            RTHandle toRT = GetCurrentToRT();
            SetRTHandleTexture(material, SpiralFluidTransitionShaderProperties.FromTex, fromRT);
            SetRTHandleTexture(material, SpiralFluidTransitionShaderProperties.ToTex, toRT);

            if (_settings.distortionTex != null)
            {
                material.SetTexture(SpiralFluidTransitionShaderProperties.DistortionTex, _settings.distortionTex);
            }

            material.SetVector(SpiralFluidTransitionShaderProperties.DistortionTilingFlow,
                new Vector4(_settings.distortionTiling.x, _settings.distortionTiling.y,
                    _settings.distortionFlow.x, _settings.distortionFlow.y));
            material.SetVector(SpiralFluidTransitionShaderProperties.TextureDistortionParams,
                new Vector4(_settings.openingDistortionStrength, _settings.expandingDistortionStrength, 0f, 0f));

            ApplyWarmBrightLut(material);

            SetKeyword(material, SpiralFluidTransitionShaderProperties.CLOW_QUALITY, _settings.useLowQuality);
            SetGammaKeyword(material, _settings.renderEvent);
            SetKeyword(material, SpiralFluidTransitionShaderProperties.CFROM_TEX_DISPLAY_SRGB,
                _settings.fromRT != null && _settings.fromRTFromCameraCapture);
            SetKeyword(material, SpiralFluidTransitionShaderProperties.CTO_TEX_DISPLAY_SRGB,
                _settings.toRT != null && _settings.toRTFromCameraCapture);

            material.SetVector(SpiralFluidTransitionShaderProperties.Center, _settings.center);
            material.SetVector(SpiralFluidTransitionShaderProperties.TransitionParams,
                new Vector4(rawProgress, aspect, Time.unscaledTime,
                    Mathf.Clamp(_settings.BrightenPhaseRatio, CBRIGHTEN_PHASE_MIN, CBRIGHTEN_PHASE_MAX)));
            material.SetVector(SpiralFluidTransitionShaderProperties.VisualParams,
                new Vector4(visualProgress, brightenOnlyProgress,
                    Mathf.Clamp01(_settings.alphaFadeStartRatio), Mathf.Clamp01(_settings.fromEndAlpha)));
            material.SetVector(SpiralFluidTransitionShaderProperties.ToFinishParams,
                new Vector4(Mathf.Clamp01(_settings.toReachClarityRatio),
                    Mathf.Clamp01(_settings.toReachNormalBrightRatio),
                    Mathf.Clamp01(_settings.toBrightenIntensity),
                    Mathf.Max(0f, _settings.toMaxBlurRadius)));
            material.SetVector(SpiralFluidTransitionShaderProperties.RadiusParams,
                new Vector4(_settings.startRadius, _settings.endRadius, _settings.edgeWidthStart, _settings.edgeWidthEnd));
            material.SetVector(SpiralFluidTransitionShaderProperties.SwirlParams,
                new Vector4(_settings.spinSpeed, _settings.twistStrength, _settings.distortionStrength,
                    _settings.edgeDistortionStrength));
            material.SetVector(SpiralFluidTransitionShaderProperties.NoiseParams,
                new Vector4(_settings.irregularStrength, _settings.noiseScale, _settings.flowSpeed,
                    _settings.boundaryWaveStrength));
            material.SetVector(SpiralFluidTransitionShaderProperties.EdgeParams,
                new Vector4(0f, _settings.fromRimOverlayStrength, _settings.fromRimOverlayWidth, _settings.fromZoomStrength));
            material.SetVector(SpiralFluidTransitionShaderProperties.FoldParams,
                new Vector4(_settings.foldRadialStrength, _settings.foldTangentStrength, _settings.foldWaveStrength, 0f));
            material.SetVector(SpiralFluidTransitionShaderProperties.ExposureParams,
                new Vector4(_settings.exposureIntensity,
                    Mathf.Clamp01(_settings.toReachClarityEndRatio),
                    Mathf.Clamp01(_settings.toReachNormalBrightEndRatio), 0f));
            material.SetVector(SpiralFluidTransitionShaderProperties.LayerParams,
                new Vector4(_settings.globalExpandStrength, _settings.outerInfluenceWidth,
                    _settings.outerDistortStrength, 0f));
        }

        private void ApplyWarmBrightLut(Material material)
        {
            Texture warmBrightLut = _resolvedWarmBrightLut != null
                ? _resolvedWarmBrightLut
                : (_settings != null ? _settings.warmBrightLut : null);

            material.SetTexture(SpiralFluidTransitionShaderProperties.WarmBrightLut, warmBrightLut);
            if (warmBrightLut == null)
            {
                material.SetVector(SpiralFluidTransitionShaderProperties.WarmBrightLutParams, Vector4.zero);
                return;
            }

            float lutSize = Mathf.Max(2f, warmBrightLut.height);
            material.SetVector(SpiralFluidTransitionShaderProperties.WarmBrightLutParams,
                new Vector4(lutSize, Mathf.Max(1f, warmBrightLut.width), lutSize, 0f));
        }

        private static void SetRTHandleTexture(Material material, int propertyId, RTHandle handle)
        {
            if (handle != null && handle.rt != null)
            {
                material.SetTexture(propertyId, handle.rt);
                return;
            }

            material.SetTexture(propertyId, null);
        }

        private static void SetKeyword(Material material, string keyword, bool isEnabled)
        {
            if (isEnabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        private static void SetGammaKeyword(Material material, RenderPassEvent renderEvent)
        {
            bool needLinearToSRGB = !(renderEvent <= RenderPassEvent.AfterRenderingPostProcessing);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                needLinearToSRGB = false;
            }
#endif
            SetKeyword(material, SpiralFluidTransitionShaderProperties.CNEED_LINEAR_TO_SRGB, needLinearToSRGB);
        }
    }
}
