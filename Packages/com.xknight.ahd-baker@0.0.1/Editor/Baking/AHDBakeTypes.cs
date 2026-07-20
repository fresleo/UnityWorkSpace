/*******************************************************************************
 * File: AHDBakeTypes.cs
 * Author: WangYu
 * Date: 2026-05-13
 * Description: AHD 烘焙器公共数据类型。
 * Notice: 仅用于 Unity Editor 烘焙流程。
 *******************************************************************************/

using System;
using UnityEngine;

namespace XKnight.AHDBaker.Editor
{
    /// <summary>
    /// AHD 烘焙质量预设
    /// </summary>
    public enum EAHDBakeQualityPreset
    {
        /// <summary>
        /// 快速预览
        /// 采样数低
        /// </summary>
        Preview,

        /// <summary>
        /// 平衡质量
        /// 适合日常验证
        /// </summary>
        Balanced,

        /// <summary>
        /// 高质量
        /// 适合最终检查
        /// </summary>
        High,

        /// <summary>
        /// 自定义质量
        /// 当前参数未匹配任何内置预设
        /// </summary>
        Custom
    }

    /// <summary>
    /// AHD 过渡带羽化模式
    /// </summary>
    public enum EAHDTransitionFeatherMode
    {
        /// <summary>
        /// 只压低高强度一侧，不抬亮低强度一侧。
        /// </summary>
        ReduceHighOnly = 0,

        /// <summary>
        /// 高低两侧都向局部平滑结果靠拢。
        /// </summary>
        SmoothBoth = 1
    }
    
    /// <summary>
    /// AHD 烘焙设置。
    /// </summary>
    [Serializable]
    public sealed class AHDBakeSettings
    {
        private const int 
            C_PREVIEW_SAMPLES_PER_LIGHT = 1, 
            C_PREVIEW_CHART_DILATE_RADIUS = 1, 
            C_PREVIEW_DENOISE_RADIUS = 0;
        
        private const int 
            C_BALANCED_SAMPLES_PER_LIGHT = 8, 
            C_BALANCED_CHART_DILATE_RADIUS = 4,
            C_BALANCED_DENOISE_RADIUS = 1;
        
        private const int 
            C_HIGH_SAMPLES_PER_LIGHT = 16,
            C_HIGH_CHART_DILATE_RADIUS = 8,
            C_HIGH_DENOISE_RADIUS = 2;
        
        /// <summary>
        /// 质量预设，下拉菜单显示名称。
        /// </summary>
        internal static readonly string[] s_QualityPresetNames =
        {
            "快速预览",
            "平衡质量",
            "高质量",
            "自定义质量"
        };
        
        /// <summary>
        /// 过渡羽化模式，下拉菜单显示名称。
        /// </summary>
        internal static readonly string[] s_TransitionFeatherModeNames =
        {
            "压高不抬低",
            "双向平滑"
        };
        
        /// <summary>
        /// 质量预设
        /// </summary>
        public EAHDBakeQualityPreset qualityPreset = EAHDBakeQualityPreset.Balanced;

        /// <summary>
        /// 每盏灯的可见性采样数
        /// </summary>
        public int samplesPerLight = 8;

        /// <summary>
        /// 射线起点沿法线偏移距离
        /// </summary>
        public float rayBias = 0.01f;

        /// <summary>
        /// 方向光遮挡射线最大距离
        /// </summary>
        public float directionalRayDistance = 1000;

        /// <summary>
        /// 点光/聚光 的光源端 jitter 半径相对 light.range 的比例。
        /// 0 表示硬可见性判断；V1 默认 0.02。
        /// </summary>
        public float lightSourceRadiusRatio = 0.02f;

        /// <summary>
        /// 接收点切平面 jitter 半径，单位米。用于软化阴影边的硬转折。
        /// V1 默认 0.03。
        /// </summary>
        public float softOcclusionRadius = 0.03f;

        /// <summary>
        /// 是否只收集当前激活并启用的灯光
        /// </summary>
        public bool onlyActiveAndEnabledLights = true;

        /// <summary>
        /// 是否只收集 Baked 和 Mixed 灯光
        /// </summary>
        public bool onlyBakedOrMixedLights = true;

        /// <summary>
        /// 是否排除带 NoAHDBakedSpecular Tag 的灯光
        /// </summary>
        public bool filterIgnoredLightTag = true;

        /// <summary>
        /// 是否只使用 Occluder Static 物体作为遮挡体
        /// </summary>
        public bool onlyOccluderStaticBlockers = true;

        /// <summary>
        /// 是否把生成的方向贴图写入当前场景 Binder
        /// </summary>
        public bool assignToSceneBinder = true;

        /// <summary>
        /// 是否启用 lightmap 亮度遮罩
        /// </summary>
        public bool useLightmapLuminanceMask = true;

        /// <summary>
        /// 亮度遮罩阈值
        /// </summary>
        public float lightmapLuminanceMaskCutoff = 0.06f;

        /// <summary>
        /// 亮度遮罩过渡宽度
        /// </summary>
        public float lightmapLuminanceMaskSoftness = 0.18f;

        /// <summary>
        /// 是否启用 AHD 过渡带羽化
        /// </summary>
        public bool useTransitionFeather = true;

        /// <summary>
        /// 过渡带检测阈值
        /// </summary>
        public float transitionFeatherThreshold = 0.035f;

        /// <summary>
        /// 过渡带检测半径，单位为贴图像素。
        /// </summary>
        public int transitionDetectRadius = 32;

        /// <summary>
        /// 过渡带羽化半径，单位为贴图像素。
        /// </summary>
        public int transitionFeatherRadius = 24;

        /// <summary>
        /// 过渡带羽化强度
        /// </summary>
        public float transitionFeatherStrength = 0.75f;

        /// <summary>
        /// 过渡带羽化迭代次数
        /// </summary>
        public int transitionFeatherIterations = 1;

        /// <summary>
        /// 方向差异参与过渡带检测的权重
        /// </summary>
        public float transitionDirectionWeight = 1;

        /// <summary>
        /// 过渡带羽化模式
        /// </summary>
        public EAHDTransitionFeatherMode transitionFeatherMode = EAHDTransitionFeatherMode.ReduceHighOnly;

        /// <summary>
        /// 有效 chart 边缘扩展半径
        /// </summary>
        public int chartDilateRadius = 4;

        /// <summary>
        /// 简单边缘保留降噪半径
        /// </summary>
        public int denoiseRadius = 1;

        /// <summary>
        /// 是否输出 debug 贴图
        /// </summary>
        public bool writeDebugMaps = false;

        /// <summary>
        /// 创建当前显式设置的副本
        /// </summary>
        public AHDBakeSettings Clone()
        {
            return (AHDBakeSettings)MemberwiseClone();
        }

        /// <summary>
        /// 应用指定质量预设到可见参数
        /// </summary>
        public void ApplyQualityPreset(EAHDBakeQualityPreset preset)
        {
            qualityPreset = preset;
            
            switch (preset)
            {
                case EAHDBakeQualityPreset.Preview:
                {
                    samplesPerLight = C_PREVIEW_SAMPLES_PER_LIGHT;
                    chartDilateRadius = C_PREVIEW_CHART_DILATE_RADIUS;
                    denoiseRadius = C_PREVIEW_DENOISE_RADIUS;
                }
                    break;
                
                case EAHDBakeQualityPreset.High:
                {
                    samplesPerLight = C_HIGH_SAMPLES_PER_LIGHT;
                    chartDilateRadius = C_HIGH_CHART_DILATE_RADIUS;
                    denoiseRadius = C_HIGH_DENOISE_RADIUS;
                }
                    break;

                case EAHDBakeQualityPreset.Balanced:
                {
                    samplesPerLight = C_BALANCED_SAMPLES_PER_LIGHT;
                    chartDilateRadius = C_BALANCED_CHART_DILATE_RADIUS;
                    denoiseRadius = C_BALANCED_DENOISE_RADIUS;
                }
                    break;

                default:
                {
                    qualityPreset = EAHDBakeQualityPreset.Custom;
                }
                    break;
            }
        }

        /// <summary>
        /// 根据当前参数刷新质量预设显示
        /// </summary>
        public void UpdateQualityPresetFromParameters()
        {
            if (MatchesPreset(
                    C_PREVIEW_SAMPLES_PER_LIGHT,
                    C_PREVIEW_CHART_DILATE_RADIUS,
                    C_PREVIEW_DENOISE_RADIUS))
            {
                qualityPreset = EAHDBakeQualityPreset.Preview;
                return;
            }

            if (MatchesPreset(
                    C_BALANCED_SAMPLES_PER_LIGHT,
                    C_BALANCED_CHART_DILATE_RADIUS,
                    C_BALANCED_DENOISE_RADIUS))
            {
                qualityPreset = EAHDBakeQualityPreset.Balanced;
                return;
            }

            if (MatchesPreset(
                    C_HIGH_SAMPLES_PER_LIGHT,
                    C_HIGH_CHART_DILATE_RADIUS,
                    C_HIGH_DENOISE_RADIUS))
            {
                qualityPreset = EAHDBakeQualityPreset.High;
                return;
            }

            qualityPreset = EAHDBakeQualityPreset.Custom;
        }

        private bool MatchesPreset(
            int presetSamplesPerLight,
            int presetChartDilateRadius,
            int presetDenoiseRadius)
        {
            return samplesPerLight == presetSamplesPerLight
                && chartDilateRadius == presetChartDilateRadius
                && denoiseRadius == presetDenoiseRadius;
        }
    }

    /// <summary>
    /// AHD 烘焙后端接口
    /// </summary>
    public interface IAHDBakerBackend
    {
        /// <summary>
        /// 后端显示名称。
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 执行烘焙。
        /// </summary>
        /// <param name="context">烘焙上下文。</param>
        /// <param name="settings">烘焙设置。</param>
        /// <returns>烘焙结果。</returns>
        AHDBakeResult Bake(AHDBakeContext context, AHDBakeSettings settings);
    }

    /// <summary>
    /// AHD 烘焙上下文。
    /// </summary>
    public sealed class AHDBakeContext
    {
        /// <summary>
        /// 进度回调，返回 false 表示取消。
        /// </summary>
        public readonly Func<string, float, bool> progress;

        /// <summary>
        /// 创建上下文。
        /// </summary>
        /// <param name="progress">进度回调。</param>
        public AHDBakeContext(Func<string, float, bool> progress)
        {
            this.progress = progress;
        }
    }

    /// <summary>
    /// AHD 烘焙结果。
    /// </summary>
    public sealed class AHDBakeResult
    {
        /// <summary>
        /// 是否完成且未取消。
        /// </summary>
        public bool succeeded;

        /// <summary>
        /// 是否被用户取消。
        /// </summary>
        public bool cancelled;

        /// <summary>
        /// 写入的方向贴图。
        /// </summary>
        public Texture2D[] directionMaps = Array.Empty<Texture2D>();

        /// <summary>
        /// 可读统计信息。
        /// </summary>
        public string summary = string.Empty;
    }
}
