/*******************************************************************************
 * File: SpiralFluidTransitionSettings.cs
 * Author: fan.shi
 * Date: 2026/05/20
 * Description: SpiralFluidTransition 运行时配置。
 *
 * Notice:
 *******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    /// <summary>
    /// SpiralFluidTransition 运行时配置。
    /// </summary>
    [Serializable]
    public class SpiralFluidTransitionSettings
    {
        /// <summary>
        /// Pass 插入点。默认在后处理之后、UI 之前执行，保证转场起点与相机最终画面亮度一致。
        /// </summary>
        [InspectorName("渲染时机")]
        public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingPostProcessing;

        /// <summary>
        /// 捕获当前画面的源相机。为空时运行时会回退到 Camera.main，仍找不到才会拦截 Pass 入队。
        /// </summary>
        [SerializeField, InspectorName("源摄像机")]
        public Camera sourceCamera;

        /// <summary>
        /// 旋涡流体转场 Shader。material 为空时可用于运行时创建材质。
        /// </summary>
        [HideInInspector]
        public Shader shader;

        /// <summary>
        /// 旋涡流体转场材质。
        /// </summary>
        [HideInInspector]
        public Material material;

        /// <summary>
        /// 当前场景截图 RT，通常为 World + PostProcess，不包含 UI。
        /// </summary>
        [HideInInspector]
        public RTHandle fromRT;

        /// <summary>
        /// 未手动传入 fromRT 时，是否自动捕获当前相机颜色作为 FromRT。
        /// 外部通过 SetRenderTextures 传入 fromRT 后会被自动设为 false。
        /// </summary>
        [HideInInspector]
        public bool captureFromCameraColor = true;

        /// <summary>
        /// 下一场景截图 RT，由 Additive 场景的 CaptureCamera 渲染得到。
        /// </summary>
        [HideInInspector]
        public RTHandle toRT;

        /// <summary>
        /// ToRT 是否来自 Camera.Render 外部捕获。为 true 时 Shader 会对 ToTex 做 sRGB 解码。
        /// </summary>
        [HideInInspector]
        public bool toRTFromCameraCapture;

        /// <summary>
        /// FromRT 是否来自 Camera.Render 外部捕获。为 true 时 Shader 会对 FromTex 做 sRGB 解码。
        /// 推荐 FromRT 走 URP 内部截屏并保持 false。
        /// </summary>
        [HideInInspector]
        public bool fromRTFromCameraCapture;

        [Header("效果参数")]
        /// <summary>
        /// 贴图驱动的 UV 扭曲噪声。
        /// </summary>
        [InspectorName("扭曲贴图")]
        public Texture distortionTex;

        /// <summary>
        /// 扭曲贴图 UV 平铺倍率。
        /// </summary>
        [InspectorName("扭曲贴图 Tiling")]
        public Vector2 distortionTiling = new Vector2(2f, 1f);

        /// <summary>
        /// 扭曲贴图 UV 流动方向和速度。
        /// </summary>
        [InspectorName("扭曲贴图流动")]
        public Vector2 distortionFlow = new Vector2(0f, -1f);

        /// <summary>
        /// 开场阶段贴图扭曲强度。
        /// </summary>
        [InspectorName("开场扭曲强度")]
        [Range(0f, 0.3f)]
        public float openingDistortionStrength = 0.12f;

        /// <summary>
        /// 扩散阶段贴图扭曲强度。
        /// </summary>
        [InspectorName("扩散扭曲强度")]
        [Range(0f, 0.12f)]
        public float expandingDistortionStrength = 0.025f;

        /// <summary>
        /// Warm bright display LUT used by the pre-reveal brighten phase.
        /// </summary>
        [InspectorName("Warm Bright LUT")]
        public Texture warmBrightLut;

        /// <summary>
        /// 是否启用转场 Pass。
        /// </summary>
        [InspectorName("启用转场")]
        public bool isActive;

        /// <summary>
        /// 初始变亮阶段时长（秒）。从画面开始提亮到旋涡出现前的持续时间。
        /// autoAdvanceProgress 为 true 时由 Feature 按 unscaledTime 自动驱动 progress。
        /// </summary>
        [InspectorName("变亮时长（秒）")]
        [Min(0.01f)]
        public float brightenDuration = 0.855f;

        /// <summary>
        /// 扭曲展开阶段时长（秒）。从 ToTex 传入后到转场结束的持续时间。
        /// </summary>
        [InspectorName("扭曲展开时长（秒）")]
        [Min(0.01f)]
        public float transitionDuration = 1.045f;

        /// <summary>
        /// 是否由 Feature 自动推进 progress。
        /// </summary>
        [HideInInspector]
        public bool autoAdvanceProgress = true;

        /// <summary>
        /// 转场进度，范围 0 到 1。
        /// </summary>
        [InspectorName("转场进度")]
        [Range(0f, 1f)]
        public float progress;

        /// <summary>
        /// 变亮阶段占总时长的比例，由 brightenDuration / (brightenDuration + transitionDuration) 计算得出。
        /// </summary>
        public float BrightenPhaseRatio
        {
            get
            {
                float total = brightenDuration + transitionDuration;
                return total < 0.001f ? 0.5f : Mathf.Clamp(brightenDuration / total, 0.05f, 0.95f);
            }
        }

        /// <summary>
        /// 总时长 = brightenDuration + transitionDuration。
        /// </summary>
        public float TotalDuration => brightenDuration + transitionDuration;

        /// <summary>
        /// 原图 alpha 淡出起始进度（0~1，相对 visualProgress）。
        /// </summary>
        [InspectorName("原图 Alpha 淡出开始")]
        [Range(0f, 1f)]
        public float alphaFadeStartRatio = 0.85f;

        /// <summary>
        /// 原图 末段 alpha 目标值。1→fromEndAlpha 淡出，目标图 alpha 全程不变。
        /// </summary>
        [InspectorName("原图 Alpha 结束值")]
        [Range(0f, 1f)]
        public float fromEndAlpha;

        /// <summary>
        /// 目标图 去模糊起点（visualProgress）。从该点开始逐渐变清晰。
        /// </summary>
        [InspectorName("目标图 变清晰起点")]
        [Range(0f, 0.99f)]
        public float toReachClarityRatio = 0.85f;

        /// <summary>
        /// 目标图 去模糊终点（visualProgress）。到该点时完全清晰。
        /// </summary>
        [InspectorName("目标图 变清晰终点")]
        [Range(0f, 1f)]
        public float toReachClarityEndRatio = 0.94f;

        /// <summary>
        /// 目标图 恢复正常亮度起点（visualProgress）。从该点开始逐渐回到正常亮度。
        /// </summary>
        [InspectorName("目标图 恢复正常亮度起点")]
        [Range(0f, 0.99f)]
        public float toReachNormalBrightRatio = 0.92f;

        /// <summary>
        /// 目标图 恢复正常亮度终点（visualProgress）。到该点时恢复正常亮度。
        /// </summary>
        [InspectorName("目标图 恢复正常亮度终点")]
        [Range(0f, 1f)]
        public float toReachNormalBrightEndRatio = 0.94f;

        /// <summary>
        /// 目标图 中段 LUT 提亮强度，与 原图 解耦。
        /// </summary>
        [InspectorName("目标图 中段提亮强度")]
        [Range(0f, 1f)]
        public float toBrightenIntensity = 0.45f;

        /// <summary>
        /// 目标图 最大模糊半径（UV 空间）。转场开始至 toReachClarityRatio 前保持最大模糊，
        /// 之后逐渐过渡到 0（清晰）。
        /// </summary>
        [InspectorName("目标图 最大模糊半径")]
        [Range(0f, 0.05f)]
        public float toMaxBlurRadius = 0.008f;

        /// <summary>
        /// 旋涡中心屏幕 UV，默认画面中心。
        /// </summary>
        [InspectorName("旋涡中心")]
        public Vector2 center = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// 起始半径，越小越像从中心突然撕开。
        /// </summary>
        [HideInInspector]
        public float startRadius = 0.02f;

        /// <summary>
        /// 结束半径，横屏下建议大于 1，确保推到屏幕外。
        /// </summary>
        [InspectorName("结束半径")]
        [Range(0.01f, 1.3f)]
        public float endRadius = 1.2f;

        /// <summary>
        /// 起始边缘宽度，越大越像水膜扩散。
        /// </summary>
        [HideInInspector]
        public float edgeWidthStart = 0.15f;

        /// <summary>
        /// 结束边缘宽度，越小收尾越干净。
        /// </summary>
        [HideInInspector]
        public float edgeWidthEnd = 0.035f;

        /// <summary>
        /// 中心旋转速度，单位为 Shader 内部角速度系数。
        /// </summary>
        [HideInInspector]
        public float spinSpeed = 72f;

        /// <summary>
        /// 中心扭曲强度。
        /// </summary>
        [HideInInspector]
        public float twistStrength = 10f;

        /// <summary>
        /// 基础 UV 扭曲强度。
        /// </summary>
        [HideInInspector]
        public float distortionStrength = 0.14f;

        /// <summary>
        /// 边缘局部 UV 扭曲强度。
        /// </summary>
        [HideInInspector]
        public float edgeDistortionStrength = 0.22f;

        /// <summary>
        /// Flow Noise 对边界半径的扰动强度。
        /// </summary>
        [HideInInspector]
        public float irregularStrength = 0.42f;

        /// <summary>
        /// 低频边界起伏强度，用于打破纯圆形展开区域。
        /// </summary>
        [HideInInspector]
        public float boundaryWaveStrength = 0.68f;

        /// <summary>
        /// Flow Noise 平铺倍率。
        /// </summary>
        [HideInInspector]
        public float noiseScale = 5.2f;

        /// <summary>
        /// Flow Noise 流动速度。
        /// </summary>
        [HideInInspector]
        public float flowSpeed = 0.28f;

        /// <summary>
        /// 边缘径向翻起强度。
        /// </summary>
        [HideInInspector]
        public float foldRadialStrength = 0.18f;

        /// <summary>
        /// 边缘切向翻卷强度。
        /// </summary>
        [HideInInspector]
        public float foldTangentStrength = 0.16f;

        /// <summary>
        /// 边缘流体浪形扰动强度。
        /// </summary>
        [HideInInspector]
        public float foldWaveStrength = 0.22f;

        /// <summary>
        /// FromRT 整体慢扩张强度。
        /// </summary>
        [HideInInspector]
        public float globalExpandStrength = 0.12f;

        /// <summary>
        /// FromRT 边缘外侧受展开形状影响的宽度。
        /// </summary>
        [HideInInspector]
        public float outerInfluenceWidth = 0.72f;

        /// <summary>
        /// FromRT 边缘外侧低强度扭曲强度。
        /// </summary>
        [HideInInspector]
        public float outerDistortStrength = 0.18f;

        /// <summary>
        /// FromRT 旧画面在强扭曲边缘膜上的轻量折射残影强度。
        /// </summary>
        [InspectorName("旧画面边缘残影强度")]
        [Range(0f, 0.25f)]
        public float fromRimOverlayStrength = 0.08f;

        /// <summary>
        /// FromRT 边缘残影向遮罩内侧取样的距离倍率。
        /// </summary>
        [InspectorName("旧画面边缘残影宽度")]
        [Range(0.1f, 2f)]
        public float fromRimOverlayWidth = 0.95f;

        /// <summary>
        /// FromRT 底图从中心轻微放大的强度。
        /// </summary>
        [InspectorName("旧画面缩放强度")]
        [Range(0f, 0.12f)]
        public float fromZoomStrength = 0.035f;

        /// <summary>
        /// 转场中段整体曝光提升强度。
        /// </summary>
        [InspectorName("整体曝光强度")]
        [Range(0f, 1f)]
        public float exposureIntensity = 0.28f;

        /// <summary>
        /// 是否使用低质量路径。低质量路径减少极坐标旋涡计算。
        /// </summary>
        [HideInInspector]
        public bool useLowQuality;

        /// <summary>
        /// 重置运行时状态。
        /// </summary>
        public void Reset()
        {
            sourceCamera = null;
            fromRT = null;
            captureFromCameraColor = true;
            fromRTFromCameraCapture = false;
            toRT = null;
            toRTFromCameraCapture = false;
            autoAdvanceProgress = true;
            progress = 0f;
        }
    }
}
