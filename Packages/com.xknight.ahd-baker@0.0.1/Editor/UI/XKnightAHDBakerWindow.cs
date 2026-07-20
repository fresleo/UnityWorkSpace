/*******************************************************************************
 * File: XKnightAHDBakerWindow.cs
 * Author: WangYu
 * Date: 2026-05-13
 * Description: X-Knight AHD Baker 编辑器窗口。
 * Notice: 仅用于 Unity Editor 烘焙流程。
 *******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace XKnight.AHDBaker.Editor
{
    /// <summary>
    /// X-Knight AHD Baker 编辑器窗口。
    /// </summary>
    public sealed class XKnightAHDBakerWindow : EditorWindow
    {
        private const string c_WindowTitle = "AHD 烘焙器";
        private const string c_Version = "V2";
        
        [MenuItem("Window/TA工具集/" + c_WindowTitle + "/" + c_Version)]
        public static void Open()
        {
            var window = GetWindow<XKnightAHDBakerWindow>(c_WindowTitle + "-" + c_Version);
            window.minSize = new Vector2(680, 760);
            window.Show();
        }
        
        
        private const float C_LABEL_WIDTH = 190;
        
        private const int C_TRANSITION_SMALL_BLUR_RADIUS = 3;
        private const int C_MAX_TRANSITION_DETECT_RADIUS = 64;
        private const int C_MAX_TRANSITION_FEATHER_RADIUS = 64;
        private const int C_MAX_TRANSITION_FEATHER_ITERATIONS = 4;
        
        private readonly AHDBakeSettings _settings = new();
        private readonly IAHDBakerBackend _cpuBackend = new CpuBvhAHDBakerBackend();
        
        private Vector2 _scrollPosition;
        
        private string _lastSummary = string.Empty;
        private AHDSceneSummary _sceneSummary;
        private double _nextSummaryRefreshTime;
        private int _sectionIndex; // 分步索引
        
        private void OnGUI()
        {
            using (new AHDBakerGUIUtils.AHDBakerEditorFontScope())
            {
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = C_LABEL_WIDTH;
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                DrawHeader();

                EditorGUILayout.Space();
                DrawSettings();

                EditorGUILayout.Space();
                DrawSceneSummary();

                EditorGUILayout.Space();
                DrawActions();

                EditorGUILayout.Space();
                DrawSummary();

                EditorGUILayout.EndScrollView();
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
        }

        private void DrawHeader()
        {
            AHDBakerGUIUtils.DrawTitleLabel("X-Knight AHD 烘焙器");
            
            AHDBakerGUIUtils.DrawHelpBox(
                "输出的主贴图会作为 AHD baked specular direction map 使用：RGB 存世界空间方向，Alpha 存强度。" 
                + "\n运行时通过场景上的 Binder 绑定到 LightmapSettings 上采样。", 
                MessageType.None);
        }

        private void DrawSettings()
        {
            bool settingsChanged = false;
            _sectionIndex = 0;
            
            AHDBakerGUIUtils.DrawTitleLabel("烘焙设置");
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("后端", _cpuBackend.DisplayName);
            DrawDescription(
                "当前只有 CPU BVH 后端。它用 C# Job/BVH 做遮挡测试，在 Editor 中离线生成方向贴图。" 
                + "\n写入 Binder 时只登记方向贴图引用，不直接改 LightmapSettings。");
            
            EditorGUILayout.Space();
            bool hasOutputFolder = AHDBakeRuntimeBridge.TryGetDefaultOutputFolder(out string outputFolder);
            EditorGUILayout.LabelField("输出目录", hasOutputFolder ? outputFolder : "未找到已保存的场景路径");
            DrawDescription($"贴图会写到当前场景同名目录下，文件名格式为 {CpuBvhAHDBakerBackend.c_TextureNamePrefix}-<lightmapIndex>_*.png。");
            if (!hasOutputFolder)
            {
                AHDBakerGUIUtils.DrawHelpBox("请先打开并保存一个场景，再生成 AHD direction map。", MessageType.Error);
            }
            
            EditorGUILayout.Space();
            using (new AHDBakerGUIUtils.SettingsSectionScope(_sectionIndex++))
            {
                AHDBakerGUIUtils.DrawTitleLabel("步骤 0：全局预设");
                _settings.UpdateQualityPresetFromParameters();
                int qualityPresetIndex = Mathf.Clamp(
                    (int)_settings.qualityPreset,
                    0, AHDBakeSettings.s_QualityPresetNames.Length - 1);

                EditorGUI.BeginChangeCheck();
                qualityPresetIndex = EditorGUILayout.Popup(
                    new GUIContent("质量预设", "切换预设会立即把下方相关参数改成预存值。"),
                    qualityPresetIndex, AHDBakeSettings.s_QualityPresetNames);
                if (EditorGUI.EndChangeCheck())
                {
                    EAHDBakeQualityPreset selectedPreset = (EAHDBakeQualityPreset)qualityPresetIndex;
                    _settings.ApplyQualityPreset(selectedPreset);
                    settingsChanged = true;
                }

                DrawDescription(
                    "切换预设，可以快速切换参数组合。"
                    + "\n手动改参数后，若组合不匹配任何预设，这里会显示为自定义质量；" + "如果又改回某个预设组合，则自动显示对应质量。"
                    + "\n快速：采样 1 / Chart 扩展 1 / 降噪 0；"
                    + "\n平衡：采样 8 / Chart 扩展 4 / 降噪 1；"
                    + "\n高质量：采样 16 / Chart 扩展 8 / 降噪 2。");
            }
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            using (new AHDBakerGUIUtils.SettingsSectionScope(_sectionIndex++))
            {
                AHDBakerGUIUtils.DrawTitleLabel("步骤 1：收集场景输入");
                
                _settings.onlyActiveAndEnabledLights = EditorGUILayout.Toggle(
                    new GUIContent("仅激活灯光", "只收集当前激活并启用的灯光。"),
                    _settings.onlyActiveAndEnabledLights);
                DrawDescription("影响 AHDSceneCollector.CollectLights：开启后会忽略未激活 GameObject 或 disabled 的 Light。");
                
                EditorGUILayout.Space();
                _settings.onlyBakedOrMixedLights = EditorGUILayout.Toggle(
                    new GUIContent("仅 Baked/Mixed 灯", "只收集 Lightmap Bake Type 为 Baked 或 Mixed 的灯。"),
                    _settings.onlyBakedOrMixedLights);
                DrawDescription("影响灯光收集：开启后只处理参与烘焙语义的灯光，Realtime 灯不会进入 AHD 方向计算。");
                
                EditorGUILayout.Space();
                _settings.filterIgnoredLightTag = EditorGUILayout.Toggle(
                    new GUIContent(
                        "排除 NoAHDBakedSpecular",
                        "排除 GameObject Tag 为 NoAHDBakedSpecular 的灯光。"),
                    _settings.filterIgnoredLightTag);
                DrawDescription("影响灯光收集：Tag 为 NoAHDBakedSpecular 的 Light 不参与统计和烘焙。");
                
                EditorGUILayout.Space();
                _settings.onlyOccluderStaticBlockers = EditorGUILayout.Toggle(
                    new GUIContent("仅 Occluder Static 遮挡", "只把勾选 Occluder Static 的物体加入 BVH 遮挡体。"),
                    _settings.onlyOccluderStaticBlockers);
                DrawDescription("影响遮挡体收集：开启后只让 Occluder Static 物体进入 BVH；关闭后会收集更多 Mesh。");
            }
            
            EditorGUILayout.Space();
            using (new AHDBakerGUIUtils.SettingsSectionScope(_sectionIndex++))
            {
                AHDBakerGUIUtils.DrawTitleLabel("步骤 2：烘焙采样与射线");
                
                _settings.samplesPerLight = EditorGUILayout.IntSlider(
                    new GUIContent("每灯采样数", "每个有效 texel 对每盏灯发射的可见性采样数量。"),
                    _settings.samplesPerLight,
                    1, 64);
                DrawDescription(
                    "数值越高，软阴影和 Area Light 面采样越稳定，但烘焙时间会近似线性增加。"
                    + "\nArea Light 会按自身矩形或圆盘尺寸采样，不使用下面的点光/聚光半径。");
                
                EditorGUILayout.Space();
                _settings.rayBias = EditorGUILayout.FloatField(
                    new GUIContent("射线偏移", "射线起点沿法线方向的偏移距离，用于避免自遮挡。"),
                    _settings.rayBias);
                DrawDescription("从接收面沿法线抬起射线起点，过小容易自遮挡，过大可能漏掉贴近表面的遮挡。");
                
                EditorGUILayout.Space();
                _settings.lightSourceRadiusRatio = EditorGUILayout.Slider(
                    new GUIContent("光源端 Jitter 比例",
                        "点光/聚光的光源端球面 jitter 半径相对 light.range 的比例。V1 默认 0.02。"),
                    _settings.lightSourceRadiusRatio,
                    0,
                    0.2f);
                DrawDescription(
                    "控制点光/聚光在光源端的 jitter 范围。"
                    + "\n0 表示硬可见性；越大阴影边越软，但更容易漏到背面。"
                    + "\nArea Light 使用 Light 的 Shape 和 Area Size 作为真实采样范围，不使用这个比例。");

                EditorGUILayout.Space();
                _settings.softOcclusionRadius = EditorGUILayout.Slider(
                    new GUIContent("切平面 Jitter 半径",
                        "接收点在切平面上的 jitter 半径，单位米。V1 默认 0.03。"),
                    _settings.softOcclusionRadius,
                    0,
                    0.2f);
                DrawDescription(
                    "控制接收点端的 jitter 范围。"
                    + "\n配合光源端 jitter，能在阴影边得到平滑的半影过渡。"
                    + "\n过大会让薄结构产生穿透。");
            }
            
            EditorGUILayout.Space();
            using (new AHDBakerGUIUtils.SettingsSectionScope(_sectionIndex++))
            {
                AHDBakerGUIUtils.DrawTitleLabel("步骤 3：烘焙输入遮罩");
                
                _settings.useLightmapLuminanceMask = EditorGUILayout.Toggle(
                    new GUIContent("Lightmap 亮度遮罩", "用已有 lightmap 亮度压低暗区的 AHD 强度。"),
                    _settings.useLightmapLuminanceMask);
                DrawDescription(
                    "在 BuildLightmapWorkset 阶段读取已有 lightmap 亮度，先生成 texel.luminanceMask，"
                    + "后续烘焙 strength 会被它调制。");

                using (new EditorGUI.IndentLevelScope(1))
                {
                    using (new EditorGUI.DisabledScope(!_settings.useLightmapLuminanceMask))
                    {
                        _settings.lightmapLuminanceMaskCutoff = EditorGUILayout.Slider(
                            new GUIContent("亮度遮罩阈值", "低于该归一化亮度的区域会被压低 AHD 强度。"),
                            _settings.lightmapLuminanceMaskCutoff, 0, 1);
                        DrawDescription("归一化亮度低于该值时，AHD 强度趋近 0。适合排除几乎无直接光贡献的暗区。");

                        EditorGUILayout.Space();
                        _settings.lightmapLuminanceMaskSoftness = EditorGUILayout.Slider(
                            new GUIContent("亮度遮罩过渡", "从阈值到完全保留强度的过渡宽度。"),
                            _settings.lightmapLuminanceMaskSoftness, 0.001f, 1);
                        DrawDescription("控制阈值附近的渐变宽度。数值越大，亮度遮罩过渡越柔和。");
                    }
                }
                
            }
            
            EditorGUILayout.Space();
            using (new AHDBakerGUIUtils.SettingsSectionScope(_sectionIndex++))
            {
                AHDBakerGUIUtils.DrawTitleLabel("步骤 4：贴图后处理");
                DrawDescription("AHDTexturePostProcessor.Apply 的实际执行顺序：先降噪，再做过渡带羽化，最后 Chart 扩展。");

                _settings.denoiseRadius = EditorGUILayout.IntSlider(
                    new GUIContent("1，降噪半径", "在同一 chart 内做简单方向和强度平滑。"),
                    _settings.denoiseRadius, 0, 4);
                DrawDescription("在同一 chart 内平滑方向、可见性和强度。过大可能抹掉小范围阴影方向变化。");

                EditorGUILayout.Space();
                _settings.useTransitionFeather = EditorGUILayout.Toggle(
                    new GUIContent(
                        "2，AHD 过渡带羽化",
                        "识别 strength、occlusion、direction 的明显过渡带，并在扩边前局部羽化。"),
                    _settings.useTransitionFeather);
                DrawDescription(
                    "识别低频 strength、occlusion 和 direction 过渡带，"
                    + "在写图前局部平滑 AHD 强度和方向。"
                    + "\n阈值决定多明显才处理；检测半径抓宽色带；"
                    + "羽化半径、强度和迭代决定软化范围。"
                    + "\n压高不抬低更适合正式 bake；双向平滑会抬亮暗侧，主要用于诊断对比。");

                using (new EditorGUI.IndentLevelScope(1))
                {
                    using (new EditorGUI.DisabledScope(!_settings.useTransitionFeather))
                    {
                        _settings.transitionFeatherThreshold = EditorGUILayout.Slider(
                            new GUIContent(
                                "过渡检测阈值",
                                "检测半径内 strength/occlusion/direction 差异超过该阈值时，"
                                + "会被视为需要羽化的过渡带。"),
                            _settings.transitionFeatherThreshold, 0.001f, 1);
                        DrawDescription("数值越低，越容易命中过渡带；过低会把普通低频变化也当成需要羽化。");

                        _settings.transitionDetectRadius = EditorGUILayout.IntSlider(
                            new GUIContent(
                                "过渡检测半径",
                                "用多尺度邻域识别低频过渡带，单位为贴图像素。"),
                            _settings.transitionDetectRadius, C_TRANSITION_SMALL_BLUR_RADIUS + 1, C_MAX_TRANSITION_DETECT_RADIUS);
                        DrawDescription("半径越大越容易抓到宽色带，但后处理更慢，也更可能跨过细小结构。");

                        _settings.transitionFeatherRadius = EditorGUILayout.IntSlider(
                            new GUIContent(
                                "过渡羽化半径",
                                "围绕过渡带做局部平滑的半径，单位为贴图像素。"),
                            _settings.transitionFeatherRadius, 1, C_MAX_TRANSITION_FEATHER_RADIUS);
                        DrawDescription("决定被软化的贴图空间范围。半径过大可能抹宽高光方向变化。");

                        _settings.transitionFeatherStrength = EditorGUILayout.Slider(
                            new GUIContent(
                                "过渡羽化强度",
                                "按过渡检测权重混合平滑结果。"),
                            _settings.transitionFeatherStrength, 0, 1);
                        DrawDescription("0 表示只检测不改结果；1 表示完全按检测权重应用羽化结果。");

                        _settings.transitionFeatherIterations = EditorGUILayout.IntSlider(
                            new GUIContent(
                                "过渡羽化迭代",
                                "重复羽化次数。"),
                            _settings.transitionFeatherIterations, 1, C_MAX_TRANSITION_FEATHER_ITERATIONS);
                        DrawDescription("迭代越多过渡越软，但也越容易损失小范围方向变化。");

                        _settings.transitionDirectionWeight = EditorGUILayout.Slider(
                            new GUIContent(
                                "过渡方向差权重",
                                "把方向差异加入 transition score。0 表示只看强度和遮挡。"),
                            _settings.transitionDirectionWeight, 0, 2);
                        DrawDescription("多灯交界处强度可能接近，但方向会突然翻转；这时方向差权重能帮助命中过渡带。");

                        int featherMode = EditorGUILayout.Popup(
                            new GUIContent(
                                "过渡羽化模式",
                                "压高不抬低用于正式 bake，双向平滑用于诊断对比。"),
                            (int)_settings.transitionFeatherMode, AHDBakeSettings.s_TransitionFeatherModeNames);
                        _settings.transitionFeatherMode = (EAHDTransitionFeatherMode)featherMode;
                        DrawDescription("压高不抬低只削弱高强度一侧，避免把暗区抬亮；双向平滑会让两侧都靠近局部平均。");
                    }
                }

                EditorGUILayout.Space();
                _settings.chartDilateRadius = EditorGUILayout.IntSlider(
                    new GUIContent("3，Chart 扩展半径", "把有效 texel 向 chart 边缘外扩，降低双线性采样漏色。"),
                    _settings.chartDilateRadius, 0, 32);
                DrawDescription("把有效结果向无效 texel 外扩，主要用于降低 UV chart 边缘双线性采样漏色。");
            }
            
            EditorGUILayout.Space();
            using (new AHDBakerGUIUtils.SettingsSectionScope(_sectionIndex++))
            {
                AHDBakerGUIUtils.DrawTitleLabel("步骤 5：写入与调试输出");

                _settings.assignToSceneBinder = EditorGUILayout.Toggle(
                    new GUIContent("写入 Binder", "生成后把方向贴图引用写入当前场景 Binder；不直接改 LightmapSettings。"),
                    _settings.assignToSceneBinder);
                DrawDescription("生成贴图保存完成后执行。开启后会查找或创建场景 Binder，并把主方向贴图数组写入 Binder。");

                EditorGUILayout.Space();
                _settings.writeDebugMaps = EditorGUILayout.Toggle(
                    new GUIContent("输出调试贴图", "额外输出调试用 PNG，不写入 Binder，也不供运行时 shader 直接采样。"),
                    _settings.writeDebugMaps);
                DrawDescription(
                    "生成主贴图后执行。开启后每张 lightmap 额外输出 debug 贴图，"
                    + "用于检查强度、可见性、一致性、亮度遮罩、主导灯和羽化命中区域。");
                if (_settings.writeDebugMaps)
                {
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        string textureName = CpuBvhAHDBakerBackend.c_TextureNamePrefix
                            + "-" + "<index>" + CpuBvhAHDBakerBackend.c_TextureNameSuffix;
                        
                        AHDBakerGUIUtils.DrawHelpBox(
                            $"正式输出：{textureName}。RGB 是压缩后的世界空间方向，"
                            + "Alpha 是 AHD 强度；写入 Binder 后由运行时 AHD shader 采样。"
                            + "\n\n"
                            + "调试输出不会写入 Binder，只用于人工检查："
                            + "\n- debug_strength：AHD 强度，越白代表 baked specular 方向贡献越强。"
                            + "\n- debug_visibility：灯光可见比例，越白代表从 texel 到有效灯光越少被遮挡。"
                            + "\n- debug_confidence：方向一致性，越白代表有效灯方向越集中；偏黑表示方向分散或不可靠。"
                            + "\n- debug_luminance_mask：lightmap 亮度遮罩结果，越黑代表该区域被压低。"
                            + "\n- debug_dominant：主导灯 ID 的伪彩色图，用于确认不同区域主要受哪盏灯影响。"
                            + "\n- debug_transition_score：过渡带检测总分，混合强度、遮挡和方向差。"
                            + "\n- debug_direction_diff：方向差贡献，越白表示邻域方向变化越大。"
                            + "\n- debug_feather_weight：最终羽化权重，越白表示羽化影响越强。"
                            + "\n- debug_feather_mask：羽化命中遮罩，白色代表该 texel 被羽化处理。"
                            + "\n- debug_feather_delta：羽化前后强度变化量，用于检查是否削弱过度。",
                            MessageType.Info);
                    }
                }
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                _settings.rayBias = Mathf.Max(0.0001f, _settings.rayBias);
                _settings.lightSourceRadiusRatio = Mathf.Clamp(_settings.lightSourceRadiusRatio, 0, 1);
                _settings.softOcclusionRadius = Mathf.Max(0, _settings.softOcclusionRadius);
                _settings.transitionFeatherThreshold = Mathf.Clamp01(_settings.transitionFeatherThreshold);
                _settings.transitionDetectRadius = Mathf.Clamp(_settings.transitionDetectRadius,
                    C_TRANSITION_SMALL_BLUR_RADIUS + 1, C_MAX_TRANSITION_DETECT_RADIUS);
                _settings.transitionFeatherRadius = Mathf.Clamp(_settings.transitionFeatherRadius,
                    1, C_MAX_TRANSITION_FEATHER_RADIUS);
                _settings.transitionFeatherStrength = Mathf.Clamp01(_settings.transitionFeatherStrength);
                _settings.transitionFeatherIterations = Mathf.Clamp(_settings.transitionFeatherIterations,
                    1, C_MAX_TRANSITION_FEATHER_ITERATIONS);
                _settings.transitionDirectionWeight = Mathf.Max(0, _settings.transitionDirectionWeight);
                
                _settings.UpdateQualityPresetFromParameters();
                
                settingsChanged = true;
            }

            if (settingsChanged)
            {
                RefreshSceneSummary(true);
            }
        }

        private static void DrawDescription(string text)
        {
            using (new EditorGUI.IndentLevelScope(1))
            {
                AHDBakerGUIUtils.DrawHelpBox(text, MessageType.None);
            }
        }

        private void DrawSceneSummary()
        {
            RefreshSceneSummary(false);

            using (new AHDBakerGUIUtils.SettingsSectionScope(_sectionIndex++))
            {
                AHDBakerGUIUtils.DrawTitleLabel("场景统计");
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.LabelField("Lightmap 数", _sceneSummary.lightmapCount.ToString());
                    EditorGUILayout.LabelField("收集到的灯光数", _sceneSummary.lightCount.ToString());
                    EditorGUILayout.LabelField(AHDSceneCollector.c_IgnoredLightTag + " 排除数", _sceneSummary.tagExcludedLightCount.ToString());
                    EditorGUILayout.LabelField("接收 Renderer 数", _sceneSummary.receiverRendererCount.ToString());
                    EditorGUILayout.LabelField("接收三角形数", _sceneSummary.receiverTriangleCount.ToString());
                    EditorGUILayout.LabelField("遮挡 Renderer 数", _sceneSummary.occluderRendererCount.ToString());
                    EditorGUILayout.LabelField("遮挡三角形数", _sceneSummary.occluderTriangleCount.ToString());
                }
            }
        }

        // 刷新场景摘要
        private void RefreshSceneSummary(bool force)
        {
            double now = EditorApplication.timeSinceStartup;
            if (!force && now < _nextSummaryRefreshTime)
            {
                return;
            }

            _sceneSummary = AHDSceneCollector.CollectSummary(_settings);
            _nextSummaryRefreshTime = now + 1;
        }

        private void DrawActions()
        {
            bool hasOutputFolder = AHDBakeRuntimeBridge.TryGetDefaultOutputFolder(out _);
            if (!hasOutputFolder)
            {
                AHDBakerGUIUtils.DrawHelpBox("当前场景没有有效保存路径，不能生成 AHD direction map。", MessageType.Error);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!hasOutputFolder))
                {
                    if (GUILayout.Button("烘焙全部", GUILayout.Height(40f)))
                    {
                        RefreshSceneSummary(true);
                        Bake();
                    }
                }
            }
        }

        private void DrawSummary()
        {
            if (string.IsNullOrEmpty(_lastSummary))
            {
                return;
            }
            
            AHDBakerGUIUtils.DrawTitleLabel("上次结果");
            AHDBakerGUIUtils.DrawHelpBox(_lastSummary, MessageType.None);
        }

        private void Bake()
        {
            try
            {
                AHDBakeContext context = new AHDBakeContext((title, progress) =>
                {
                    bool cancelled = EditorUtility.DisplayCancelableProgressBar("AHD 烘焙器", title, progress);
                    return !cancelled;
                });
                AHDBakeResult result = _cpuBackend.Bake(context, _settings);
                if (result.cancelled)
                {
                    _lastSummary = "烘焙已取消。";
                }
                else if (result.succeeded)
                {
                    _lastSummary = result.summary;
                }
                else
                {
                    _lastSummary = "烘焙结束，但没有生成方向贴图。" + result.summary;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
    }
}
