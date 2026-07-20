/*******************************************************************************
 * File: SpiralFluidTransitionRendererFeature.cs
 * Author: fan.shi
 * Date: 2026/05/20
 * Description: 旋涡流体转场效果。
 *
 * Notice:
 *******************************************************************************/

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    /// <summary>
    /// 旋涡流体转场效果。
    /// </summary>
    public class SpiralFluidTransitionRendererFeature : ScriptableRendererFeature
    {
        private const int CDEFAULT_DEPTH_BITS = 0;
        private const int CNEXT_CAMERA_DEPTH_BITS = 24;
        private const int CMIN_TEXTURE_SIZE = 1;
        private const int CMSAA_SAMPLES = 1;
        private const string CNEXT_CAMERA_RT_NAME = "_SpiralFluidTransitionNextCameraRT";

        /// <summary>
        /// 冻结时 progress 距 brightenPhaseRatio 的保护余量，确保 rawProgress 严格小于
        /// Shader 侧的 brightenPhase，避免 Shader 在边界值处提前触发旋涡渲染。
        /// </summary>
        private const float CFROZEN_PROGRESS_GUARD = 0.001f;

        /// <summary>
        /// 配置。
        /// </summary>
        [InspectorName("转场设置")]
        public SpiralFluidTransitionSettings setting = new SpiralFluidTransitionSettings();

        [SerializeField]
        [HideInInspector]
        [Reload("Shaders/SpiralFluidTransition.shader")]
        private Shader _spiralFluidShader;

        private SpiralFluidTransitionPass _pass;
        private Material _runtimeMaterial;
        private RTHandle _capturedFromRT;
        private Camera _nextCamera;
        private RenderTexture _nextCameraRT;
        private RTHandle _nextCameraRTHandle;
        private float _transitionStartTime = -1f;
        private float _frozenStartTime = -1f;
        private bool _runtimeActive;
        private GraphicsFormat _currentCameraColorFormat = GraphicsFormat.None;

        /// <summary>
        /// 创建转场 Pass。
        /// </summary>
        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, PackageConst.c_packagePath);
#endif

            EnsurePass();
        }

        /// <summary>
        /// 判断当前相机是否需要执行转场，并把 Pass 加入渲染队列。
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CanRunThisFrame(in renderingData))
            {
                return;
            }

            AutoStartTransition();
            AdvanceTransitionProgress();
            PreparePassForFrame(renderer, in renderingData);
            renderer.EnqueuePass(_pass);
        }

        /// <summary>
        /// 相机 RT 已分配后同步 Pass 输入与内部捕获 RT。
        /// </summary>
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (!CanRunThisFrame(in renderingData))
            {
                return;
            }

            PreparePassForFrame(renderer, in renderingData);
        }

        /// <summary>
        /// 设置转场需要的两张场景截图 RT。
        /// fromRT 非空时自动关闭内部截屏；为空且 captureFromCameraColor 为 true 时首帧内部 Blit。
        /// </summary>
        public void SetRenderTextures(RTHandle fromRT, RTHandle toRT)
        {
            if (setting == null)
            {
                return;
            }

            ReleaseNextCameraCapture();

            setting.fromRT = fromRT;
            setting.toRT = toRT;
            setting.captureFromCameraColor = fromRT == null;
            setting.fromRTFromCameraCapture = false;
            setting.toRTFromCameraCapture = false;

            _pass?.ResetCaptureState();
        }

        /// <summary>
        /// 设置用于捕获 ToTex 的下一场景相机。临时 RT 和相机 targetTexture 由 Feature 统一管理。
        /// </summary>
        public void SetNextCamera(Camera camera)
        {
            if (_nextCamera == camera)
            {
                return;
            }

            ReleaseNextCameraCapture();
            _nextCamera = camera;
            if (_runtimeActive)
            {
                CaptureNextCameraToRT();
            }
        }

        /// <summary>
        /// 设置转场启用状态。
        /// </summary>
        public new void SetActive(bool active)
        {
            _runtimeActive = active;
            if (setting == null)
            {
                if (active)
                {
                    base.SetActive(true);
                }
                else
                {
                    ForceDisableRuntimeState();
                }

                return;
            }

            if (active)
            {
                base.SetActive(true);
                EnsurePass();
                EnsureMaterial();
                CaptureNextCameraToRT();
                _transitionStartTime = Time.unscaledTime;
                _frozenStartTime = -1f;
                setting.progress = 0f;
                _pass?.ResetCaptureState();
                return;
            }

            ForceDisableRuntimeState();
        }

        /// <summary>
        /// 关闭 Feature 时清理 Pass 运行时状态。
        /// </summary>
        public void DisableRendererFeature()
        {
            ForceDisableRuntimeState();
        }

        /// <summary>
        /// 释放转场 Pass、运行时材质和内部捕获 RT。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            ForceDisableRuntimeState();

            if (_pass != null)
            {
                _pass.Dispose();
                _pass = null;
            }

            if (_runtimeMaterial != null)
            {
                CoreUtils.Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }

            ReleaseCapturedRT(ref _capturedFromRT);
            ReleaseNextCameraCapture();
        }

        private bool CanRunThisFrame(in RenderingData renderingData)
        {
            if (setting == null || !_runtimeActive || !renderingData.cameraData.postProcessEnabled)
            {
                return false;
            }

            Camera renderingCamera = renderingData.cameraData.camera;
            if (renderingCamera == null)
            {
                return false;
            }

            if (setting.sourceCamera != null && setting.sourceCamera.GetInstanceID() != renderingCamera.GetInstanceID())
            {
                return false;
            }

            EnsurePass();
            EnsureMaterial();

            return _pass.CanSetup(setting, renderingCamera);
        }

        private void PreparePassForFrame(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            _pass.SetResolvedWarmBrightLut(ResolveWarmBrightLut(renderer));
            SyncCurrentCameraColorFormat(in renderingData);
            EnsureNextCameraCaptureMatchesCurrentFormat();
            TryPrepareInternalCapture(in renderingData);
        }

        private void AutoStartTransition()
        {
            if (_transitionStartTime >= 0f || !_runtimeActive)
            {
                return;
            }

            _transitionStartTime = Time.unscaledTime;
            setting.progress = 0f;
            _pass?.ResetCaptureState();
        }

        private bool HasValidToRT()
        {
            return setting != null && setting.toRT != null;
        }

        private void AdvanceTransitionProgress()
        {
            if (!_runtimeActive || _transitionStartTime < 0f || !setting.autoAdvanceProgress)
            {
                return;
            }

            float duration = Mathf.Max(0.01f, setting.TotalDuration);
            float brightenMax = setting.BrightenPhaseRatio;

            if (_frozenStartTime >= 0f)
            {
                if (HasValidToRT())
                {
                    _transitionStartTime += Time.unscaledTime - _frozenStartTime;
                    _frozenStartTime = -1f;
                }
                else
                {
                    setting.progress = Mathf.Max(0f, brightenMax - CFROZEN_PROGRESS_GUARD);
                    return;
                }
            }

            float rawProgress = Mathf.Clamp01((Time.unscaledTime - _transitionStartTime) / duration);

            if (!HasValidToRT() && rawProgress >= brightenMax)
            {
                _frozenStartTime = Time.unscaledTime;
                setting.progress = Mathf.Max(0f, brightenMax - CFROZEN_PROGRESS_GUARD);
                return;
            }

            setting.progress = rawProgress;
        }

        private void EnsurePass()
        {
            if (_pass == null)
            {
                _pass = new SpiralFluidTransitionPass(setting);
            }
        }

        private void TryPrepareInternalCapture(in RenderingData renderingData)
        {
            bool fromUsesInternalCapture = setting.fromRT == null && setting.captureFromCameraColor;

            if (!fromUsesInternalCapture)
            {
                _pass.SetCapturedFromRT(null, false);
                return;
            }

            RenderTextureDescriptor baseDesc = renderingData.cameraData.cameraTargetDescriptor;
            baseDesc.depthBufferBits = CDEFAULT_DEPTH_BITS;
            baseDesc.msaaSamples = CMSAA_SAMPLES;
            baseDesc.useMipMap = false;
            baseDesc.autoGenerateMips = false;

            XKnightRenderingUtils.ReAllocateIfNeeded(ref _capturedFromRT, baseDesc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_SpiralFluidTransitionFromRT");

            _pass.SetCapturedFromRT(_capturedFromRT, _pass.NeedsFromCapture);
        }

        private void SyncCurrentCameraColorFormat(in RenderingData renderingData)
        {
            GraphicsFormat colorFormat = renderingData.cameraData.cameraTargetDescriptor.graphicsFormat;
            if (colorFormat != GraphicsFormat.None && SystemInfo.IsFormatSupported(colorFormat, FormatUsage.Render))
            {
                _currentCameraColorFormat = colorFormat;
            }
        }

        private void EnsureNextCameraCaptureMatchesCurrentFormat()
        {
            if (_nextCamera == null || setting == null)
            {
                return;
            }

            if (setting.toRT != null && setting.toRT != _nextCameraRTHandle)
            {
                return;
            }

            GraphicsFormat desiredFormat = GetNextCameraColorFormat(_nextCamera);
            if (_nextCameraRT != null && _nextCameraRT.graphicsFormat == desiredFormat && setting.toRT == _nextCameraRTHandle)
            {
                return;
            }

            CaptureNextCameraToRT();
        }

        private void ResetRuntimeState()
        {
            _pass?.SetCapturedFromRT(null, false);
            _pass?.SetResolvedWarmBrightLut(null);
            _pass?.ResetCaptureState();

            ReleaseCapturedRT(ref _capturedFromRT);
            ReleaseNextCameraCapture();

            Material material = setting != null ? setting.material : null;
            if (material == null)
            {
                return;
            }

            material.SetTexture(SpiralFluidTransitionShaderProperties.FromTex, null);
            material.SetTexture(SpiralFluidTransitionShaderProperties.ToTex, null);
            material.SetTexture(SpiralFluidTransitionShaderProperties.WarmBrightLut, null);
            material.SetVector(SpiralFluidTransitionShaderProperties.WarmBrightLutParams, Vector4.zero);
            material.SetVector(SpiralFluidTransitionShaderProperties.VisualParams, Vector4.zero);
            material.SetVector(SpiralFluidTransitionShaderProperties.ToFinishParams, Vector4.zero);
        }

        private void ForceDisableRuntimeState()
        {
            _runtimeActive = false;
            base.SetActive(false);
            _transitionStartTime = -1f;
            _frozenStartTime = -1f;
            ResetRuntimeState();
            setting?.Reset();
        }

        private static void ReleaseCapturedRT(ref RTHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            handle.Release();
            handle = null;
        }

        private void CaptureNextCameraToRT()
        {
            if (_nextCamera == null || setting == null)
            {
                return;
            }

            EnsureNextCameraRT(_nextCamera);
            if (_nextCameraRT == null || _nextCameraRTHandle == null)
            {
                return;
            }

            _nextCamera.enabled = true;
            _nextCamera.targetTexture = _nextCameraRT;
            _nextCamera.Render();
            _nextCamera.enabled = false;

            setting.toRT = _nextCameraRTHandle;
            setting.toRTFromCameraCapture = false;
        }

        private void EnsureNextCameraRT(Camera camera)
        {
            int width = Mathf.Max(CMIN_TEXTURE_SIZE, camera.pixelWidth);
            int height = Mathf.Max(CMIN_TEXTURE_SIZE, camera.pixelHeight);
            GraphicsFormat colorFormat = GetNextCameraColorFormat(camera);

            if (_nextCameraRT != null
                && _nextCameraRT.width == width
                && _nextCameraRT.height == height
                && _nextCameraRT.graphicsFormat == colorFormat)
            {
                return;
            }

            if (_nextCameraRTHandle != null)
            {
                _nextCameraRTHandle.Release();
                _nextCameraRTHandle = null;
            }
            if (_nextCameraRT != null)
            {
                _nextCameraRT.Release();
                CoreUtils.Destroy(_nextCameraRT);
                _nextCameraRT = null;
            }

            RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = colorFormat,
                depthBufferBits = CNEXT_CAMERA_DEPTH_BITS,
                msaaSamples = CMSAA_SAMPLES,
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = false,
            };

            _nextCameraRT = new RenderTexture(desc)
            {
                name = CNEXT_CAMERA_RT_NAME,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _nextCameraRT.Create();
            _nextCameraRTHandle = RTHandles.Alloc(_nextCameraRT);
        }

        private GraphicsFormat GetNextCameraColorFormat(Camera camera)
        {
            if (_currentCameraColorFormat != GraphicsFormat.None
                && SystemInfo.IsFormatSupported(_currentCameraColorFormat, FormatUsage.Render))
            {
                return _currentCameraColorFormat;
            }

            return GetCameraColorFormat(camera.allowHDR);
        }

        private static GraphicsFormat GetCameraColorFormat(bool allowHDR)
        {
            GraphicsFormat format = SystemInfo.GetGraphicsFormat(allowHDR ? DefaultFormat.HDR : DefaultFormat.LDR);
            if (format != GraphicsFormat.None && SystemInfo.IsFormatSupported(format, FormatUsage.Render))
            {
                return format;
            }

            if (allowHDR && SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render))
            {
                return GraphicsFormat.R16G16B16A16_SFloat;
            }

            return GraphicsFormat.R8G8B8A8_UNorm;
        }

        private void ReleaseNextCameraCapture()
        {
            if (_nextCamera != null)
            {
                _nextCamera.enabled = false;
                _nextCamera.targetTexture = null;
            }

            if (setting != null && setting.toRT == _nextCameraRTHandle)
            {
                setting.toRT = null;
                setting.toRTFromCameraCapture = false;
            }
            
            if (_nextCameraRTHandle != null)
            {
                _nextCameraRTHandle.Release();
                _nextCameraRTHandle = null;
            }

            if (_nextCameraRT != null)
            {
                _nextCameraRT.Release();
                CoreUtils.Destroy(_nextCameraRT);
                _nextCameraRT = null;
            }
        }

        private Texture ResolveWarmBrightLut(ScriptableRenderer renderer)
        {
            XKnightRenderer xKnightRenderer = renderer as XKnightRenderer;
            Texture rendererLut = xKnightRenderer != null
                && xKnightRenderer.RendererData != null
                && xKnightRenderer.RendererData.postProcessData != null
                && xKnightRenderer.RendererData.postProcessData.textures != null
                ? xKnightRenderer.RendererData.postProcessData.textures.spiralWarmBrightLut
                : null;
            if (rendererLut != null)
            {
                return rendererLut;
            }

            return setting != null ? setting.warmBrightLut : null;
        }

        private void EnsureMaterial()
        {
            if (setting.material != null)
            {
                return;
            }

            setting.shader ??= _spiralFluidShader;

            if (setting.shader == null)
            {
                return;
            }

            if (_runtimeMaterial == null)
            {
                _runtimeMaterial = CoreUtils.CreateEngineMaterial(setting.shader);
            }

            setting.material = _runtimeMaterial;
        }
    }
}
