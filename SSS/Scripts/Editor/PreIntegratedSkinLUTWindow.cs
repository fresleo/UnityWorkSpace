/// <summary>
/// author:calvin
/// date: 2026-05-26
/// description: 预积分LUT，Diffusion Profile生成器
/// </summary>


using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;


namespace Garena.TA.SSS
{
    public class PreIntegratedSkinLUTWindow : EditorWindow
    {
        //分辨率
        [SerializeField] private int _resolution = 256;

        //预积分采样数量
        [SerializeField] private int _integrationSamples = 360;
        //是否映射到UV空间
        [SerializeField] private bool _remapNdotL = true; // [-1,1] → UV[0,1]
        [SerializeField] private float _indexOfRefraction = 1.4f;
        //输出格式
        [SerializeField] private OutputFormat _outputFormat = OutputFormat.PNG;
        //输出路径
        [SerializeField] private string _outputPath = "Assets/SSS_LUT.png";
        SSS_PreIntegrated PreIntegrated = new SSS_PreIntegrated();


        //GUI面板参数
        private bool _needsLutRegen = true;
        private bool _needsPreviewRegen = true;
        private bool _autoUpdateLUT = false;

        private int _selectedPaperPreset = 0; // 默认 Apple

        private string _multiLutBasePath = "Assets/SSS/SSS_Output";
        [SerializeField] private float _WorldScale = 0f;
        [SerializeField] private int _discImportanceCdfResolution = 1024;
        [SerializeField] private string _resolveParamsAssetPath = "Assets/SSS/SSSResolveProfileParams.asset";
        [FormerlySerializedAs("_resolveParamsAsset")] [SerializeField] private DiffusionProfileParam resolveParam;

        // ===== Preview =====

        [SerializeField] private int _discPreviewSize = 256;

        [SerializeField] private float _previewExposure = 6.0f;


        // ---------- Runtime ----------
        private Texture2D _lutTexture;
        private RenderTexture _discPreviewTexture;

        private Texture2D _discKernelTex; // GenerateDiscKernel 产出的 N×1 数据
        private Texture2D _discKernelPreviewTex; // 可视化预览图
        private int _discKernelPreviewSize = 256;

        private int _discSampleCount = 32; // disc 采样点数

        // private Texture2D _curvePreviewTexture;
        //  [SerializeField] private bool _previewLogScale = false;
        private Vector2 _scroll;

        // Menu
        [MenuItem("Tools/SSS/预积分贴图生成器")]
        public static void ShowWindow()
        {
            var window = GetWindow<PreIntegratedSkinLUTWindow>("SSS");
            window.minSize = new Vector2(400, 640);
            window.Show();
        }

        private void OnEnable()
        {
            _selectedPaperPreset = FindMaterialPresetIndex("Apple");
            PreIntegrated.LoadPaperPreset(SSS_MaterialLibrary.Get(_selectedPaperPreset));
            this.minSize = new Vector2(500, 600);
            this.maxSize = new Vector2(800, 1000);
            MarkAllDirty();
        }

        private void OnDisable()
        {
            if (_lutTexture != null) DestroyImmediate(_lutTexture);
            if (_discPreviewTexture != null) DestroyImmediate(_discPreviewTexture);
            if (_discKernelPreviewTex != null) DestroyImmediate(_discKernelPreviewTex);
            // if (_curvePreviewTexture != null) DestroyImmediate(_curvePreviewTexture);
        }

        private void MarkAllDirty()
        {
            _needsLutRegen = true;
            _needsPreviewRegen = true;
        }

        //gui绘制
        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();

            DrawPaperPresets();
            DrawLUTSettings();
            DrawBurleyParams();

            //绘制Diffusion Profile预览
            DrawProfilePreview();
            DrawDiscKernelPreview();
            DrawLUTGenerationSection();

            DrawMultiLUTExport();
            DrawResolveProfileParamsExport();
            // DrawPreview();
            DrawOutput();

            EditorGUILayout.EndScrollView();
            if (Event.current.type == EventType.Repaint)
            {
                if (_needsPreviewRegen)
                {
                    RegeneratePreviews();
                    _needsPreviewRegen = false;
                }
                else if (_needsLutRegen && _autoUpdateLUT)
                {
                    PreIntegrated.RegenerateLUT(ref _lutTexture, _resolution, _remapNdotL, this);
                    _needsLutRegen = false;
                }
            }
        }

        private void RegeneratePreviews()
        {
            // SSS_PreIntegrated currently generates a Texture2D preview. Generate it then blit into a RenderTexture for runtime use.
            Texture2D tempPreview = null;
            PreIntegrated.RegenerateDiscPreview(ref tempPreview, _discPreviewSize, _previewExposure);

            if (tempPreview != null)
            {
                if (_discPreviewTexture == null || _discPreviewTexture.width != tempPreview.width || _discPreviewTexture.height != tempPreview.height)
                {
                    if (_discPreviewTexture != null) Object.DestroyImmediate(_discPreviewTexture);
                    _discPreviewTexture = new RenderTexture(tempPreview.width, tempPreview.height, 0, RenderTextureFormat.ARGBFloat)
                    {
                        name = "DiscPreview_RT",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        useMipMap = false,
                        autoGenerateMips = false
                    };
                }

                var prev = RenderTexture.active;
                Graphics.Blit(tempPreview, _discPreviewTexture);
                RenderTexture.active = prev;

                Object.DestroyImmediate(tempPreview);
            }

            // PreIntegrated.RegenerateCurvePreview(ref _curvePreviewTexture, _discPreviewSize, _previewExposure, _previewLogScale);
            Repaint();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField("预积分贴图生成器", titleStyle);
            EditorGUILayout.Space(10);
        }

        //LUT需要的参数
        private void DrawLUTSettings()
        {
            EditorGUILayout.LabelField("预积分图片设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            EditorGUI.BeginChangeCheck();

            _resolution = EditorGUILayout.IntPopup("图片分辨率",
                _resolution,
                new[] { "64", "128", "256", "512", "1024" },
                new[] { 64, 128, 256, 512, 1024 });

            _integrationSamples = EditorGUILayout.IntSlider(
                new GUIContent("采样次数", "积分数量，越高越精确但越慢"),
                _integrationSamples, 64, 2048);


            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    new GUIContent("采样N·L到0-1空间(影响shader中的计算)",
                        "On:开启表示值对0-180度的角度  Off: 关闭表示-180到180度,采样背面"),
                    GUILayout.Width(220)); // 可调整宽度
                _remapNdotL = EditorGUILayout.Toggle(_remapNdotL);
            }

            _indexOfRefraction = EditorGUILayout.Slider(
                new GUIContent("边缘散射度(折射率)",
                    "皮肤 ≈ 1.38. 大理石 ≈ 1.5"),
                _indexOfRefraction, 1.0f, 2.0f);

            EditorGUILayout.Space();
        }


        //绘制输出按钮
        private void DrawOutput()
        {
            EditorGUILayout.LabelField("输出图片", EditorStyles.boldLabel);
            _outputFormat = (OutputFormat)EditorGUILayout.EnumPopup(
                new GUIContent("格式",
                    "PNG: 8-bit sRGB,EXR: 16-bit HDR linear. Asset: Texture2D RGBAFloat"),
                _outputFormat);
            _outputPath = EditorGUILayout.TextField("输出 路径 (相对路径)", _outputPath);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存衰减图到项目文件", GUILayout.Height(28)))
                {
                    // Save RenderTexture: read back to Texture2D then call SaveToAsset
                    bool result = false;
                    if (_discPreviewTexture != null)
                    {
                        var prev = RenderTexture.active;
                        RenderTexture.active = _discPreviewTexture;
                        var temp = new Texture2D(_discPreviewTexture.width, _discPreviewTexture.height, TextureFormat.RGBAFloat, false, true);
                        temp.ReadPixels(new Rect(0, 0, _discPreviewTexture.width, _discPreviewTexture.height), 0, 0, false);
                        temp.Apply(false, false);
                        RenderTexture.active = prev;

                        result = SSS_ImageExportTools.SaveToAsset(_outputFormat, ref temp, _outputPath);
                        if (!result)
                        {
                            PreIntegrated.RegenerateDiscPreview(ref temp, _discPreviewSize, _previewExposure);
                            // update display RT from regenerated temp
                            if (_discPreviewTexture == null) _discPreviewTexture = new RenderTexture(temp.width, temp.height, 0, RenderTextureFormat.ARGBFloat);
                            Graphics.Blit(temp, _discPreviewTexture);
                        }
                        Object.DestroyImmediate(temp);
                    }
                }

                if (GUILayout.Button("保存预积分图到项目文件", GUILayout.Height(28)))
                {
                    bool result = SSS_ImageExportTools.SaveToAsset(_outputFormat, ref _lutTexture, _outputPath);
                    if (!result) PreIntegrated.RegenerateLUT(ref _lutTexture, _resolution, _remapNdotL, this);
                }
            }
        }


        private void DrawProfilePreview()
        {
            EditorGUILayout.LabelField("衰减图预览", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _previewExposure = EditorGUILayout.Slider(
                new GUIContent("曝光值", "Brightens dim profiles so the falloff is visible. Display-only."),
                _previewExposure, 0.1f, 50f);
            // _previewLogScale = EditorGUILayout.Toggle(
            //     new GUIContent("Curve: Log Y-Scale", "Log Y-axis on the 1D curve to see the long tail."),
            //     _previewLogScale);
            if (EditorGUI.EndChangeCheck()) _needsPreviewRegen = true;

            using (new EditorGUILayout.HorizontalScope())
            {
                // ---- Disc preview ----
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(_discPreviewSize + 20)))
                {
                    EditorGUILayout.LabelField("Radial Scattering (disc)", EditorStyles.miniLabel);
                    if (_discPreviewTexture != null)
                    {
                        var rect = GUILayoutUtility.GetRect(_discPreviewSize, _discPreviewSize,
                            GUILayout.ExpandWidth(false));
                        EditorGUI.DrawPreviewTexture(rect, _discPreviewTexture);
                    }

                    EditorGUILayout.LabelField($"Radius: {PreIntegrated.GetEffectiveMaxRadius():F2} mm",
                        EditorStyles.miniLabel);
                }

                // ---- Curve preview ----
                // using (new EditorGUILayout.VerticalScope())
                // {
                //     EditorGUILayout.LabelField("Falloff (1D curve)", EditorStyles.miniLabel);
                //     if (_curvePreviewTexture != null)
                //     {
                //         var rect = GUILayoutUtility.GetRect(
                //     10, 10000,
                //     _discPreviewSize, _discPreviewSize, // minHeight, maxHeight
                //     GUILayout.ExpandWidth(true), GUILayout.Height(_discPreviewSize));
                //         EditorGUI.DrawPreviewTexture(rect, _curvePreviewTexture);
                //     }
                //     EditorGUILayout.LabelField(
                //         $"X: 0..{PreIntegrated.GetEffectiveMaxRadius():F1} mm    Y: R(d) {(_previewLogScale ? "(log)" : "(linear)")}",
                //         EditorStyles.miniLabel);
                // }
            }

            EditorGUILayout.Space();
        }

        private void DrawBurleyParams()
        {
            EditorGUI.BeginChangeCheck();

            PreIntegrated._burleyParameters._scatteringColor = EditorGUILayout.ColorField(
                new GUIContent("Scattering Color",
                    "Per-channel albedo A in Burley. Drives both intensity and color of scattered light."),
                PreIntegrated._burleyParameters._scatteringColor);
            PreIntegrated._burleyParameters._scatteringMultiplier = EditorGUILayout.Slider(
                new GUIContent("Scattering Color Multiplier",
                    "Scalar boost on A. Keep effective A ≤ 1 for energy conservation."),
                PreIntegrated._burleyParameters._scatteringMultiplier, 0f, 3f);
            PreIntegrated._burleyParameters._maxRadius = EditorGUILayout.Slider(
                new GUIContent("Max Radius (mm)", "Effective scattering radius. Larger = softer."),
                PreIntegrated._burleyParameters._maxRadius, 0.1f, 20f);


            if (EditorGUI.EndChangeCheck()) MarkAllDirty();
            EditorGUILayout.Space();
        }

        private void DrawLUTGenerationSection()
        {
            EditorGUILayout.LabelField("LUT Generation", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _autoUpdateLUT = EditorGUILayout.Toggle("自动更新预积分图", _autoUpdateLUT);
                if (GUILayout.Button("点击更新预览图", GUILayout.Height(22)))
                    PreIntegrated.RegenerateLUT(ref _lutTexture, _resolution, _remapNdotL, this);
            }

            EditorGUILayout.LabelField("LUT Preview", EditorStyles.miniBoldLabel);
            if (_lutTexture != null)
            {
                float maxWidth = EditorGUIUtility.currentViewWidth - 40;
                float size = Mathf.Min(maxWidth, 384);
                var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(rect, _lutTexture);
                EditorGUILayout.LabelField(
                    _remapNdotL
                        ? "X: N·L  0~1    Y: 曲率 [最小去路-最大曲率]"
                        : "X: N·L -1~1   Y: 曲率 [最小去路-最大曲率]",
                    EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("No LUT generated yet.", MessageType.Info);
            }

            EditorGUILayout.Space();
        }

        private void DrawPaperPresets()
        {
            EditorGUILayout.LabelField("材质预设", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _selectedPaperPreset = EditorGUILayout.Popup(
                    "Material", _selectedPaperPreset, SSS_MaterialLibrary.Names);

                if (GUILayout.Button("Apply", GUILayout.Width(80)))
                {
                    PreIntegrated.LoadPaperPreset(SSS_MaterialLibrary.Get(_selectedPaperPreset));
                    MarkAllDirty();
                }
            }

            // 显示该材质的物理参数
            // var p = SSS_MaterialLibrary.Get(_selectedPaperPreset);
            // EditorGUILayout.LabelField($"光衰减 = {p.sigmaS_prime}", EditorStyles.miniLabel);
            // EditorGUILayout.LabelField($"光吸收  = {p.sigmaA}", EditorStyles.miniLabel);
            // EditorGUILayout.LabelField($"衰减系数  = {p.diffuseReflectance}   η = {p.eta}", EditorStyles.miniLabel);


            // // 显示转换后的 per-channel mean free path（核对用）
            // Vector3 albedo, mfp;
            // SSS_DipoleConverter.DipoleToBurley(p.sigmaS_prime, p.sigmaA, p.diffuseReflectance,
            //                                    out albedo, out mfp);
            // EditorGUILayout.LabelField($"自由程 (mm): R={mfp.x:F2} G={mfp.y:F2} B={mfp.z:F2}",
            //     EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                resolveParam = (DiffusionProfileParam)EditorGUILayout.ObjectField(
                    new GUIContent("SSSResolveProfileParamsAsset"),
                    resolveParam,
                    typeof(DiffusionProfileParam),
                    false);

                if (GUILayout.Button("加载", GUILayout.Width(70)))
                {
                    ApplyResolveParamsAsset(resolveParam);
                }
            }

            EditorGUILayout.Space();
        }

        private static int FindMaterialPresetIndex(string presetName)
        {
            var names = SSS_MaterialLibrary.Names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == presetName)
                    return i;
            }

            return 0;
        }

        private void DrawMultiLUTExport()
        {
            EditorGUILayout.LabelField("多 LUT 导出 (Penner + Burley Disc)", EditorStyles.boldLabel);

            _discSampleCount = EditorGUILayout.IntSlider(
                new GUIContent("Disc Sample Count", "屏幕空间 SSSS 每像素采样点数。16=快，32=均衡，64=高质量。"),
                _discSampleCount, 8, 64);

            _multiLutBasePath = EditorGUILayout.TextField("Output Base Path", _multiLutBasePath);

            EditorGUILayout.HelpBox(
                "会同时输出两张表：\n" +
                "  • <base>_PennerLUT  —— 曲面反射预积分 LUT (N·L × curvature)\n" +
                "  • <base>_BurleyDisc —— 屏幕空间 disc 采样核 (samples × 1)",
                MessageType.Info);

            if (GUILayout.Button("Export Both LUTs", GUILayout.Height(28)))
            {
                PreIntegrated.ExportBothLUTs(
                    _resolution, _remapNdotL,
                    _discSampleCount, _outputFormat, _multiLutBasePath, this);
                AssetDatabase.Refresh();
            }

            EditorGUILayout.Space();
        }

        private void DrawResolveProfileParamsExport()
        {
            EditorGUILayout.LabelField("ResolveProfileParams 参数资产导出", EditorStyles.boldLabel);


            EditorGUILayout.LabelField("Burley Parameters", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            PreIntegrated._burleyParameters._scatteringColor = EditorGUILayout.ColorField("散射颜色",
                PreIntegrated._burleyParameters._scatteringColor);
            PreIntegrated._burleyParameters._scatteringMultiplier = EditorGUILayout.Slider("散射强度乘数",
                PreIntegrated._burleyParameters._scatteringMultiplier, 0f, 3f);
            PreIntegrated._burleyParameters._maxRadius = EditorGUILayout.Slider("散射距离 (mm)",
                PreIntegrated._burleyParameters._maxRadius, 0.1f, 20f);
            PreIntegrated._burleyParameters._indexOfRefraction = EditorGUILayout.Slider("Index Of Refraction",
                PreIntegrated._burleyParameters._indexOfRefraction, 1.0f, 2.0f);
            if (EditorGUI.EndChangeCheck())
                MarkAllDirty();

            _WorldScale = EditorGUILayout.FloatField(
                new GUIContent("World Scale(世界单位)", "<=0 则使用 profile.worldScale"),
                _WorldScale);
            

            // _resolveFilterRadiusFactor = EditorGUILayout.FloatField(
            //     new GUIContent("Filter Radius Factor", "估算公式中的系数"),
            //     _resolveFilterRadiusFactor);

            _resolveParamsAssetPath = EditorGUILayout.TextField("Asset Path", _resolveParamsAssetPath);

            EditorGUILayout.HelpBox(
                "导出的资产会同时保存：\n" +
                "  • Burley 参数\n" +
                "  • GenerateDiscKernel 的采样参数\n" +
                "  • _discKernelTex 与 _discPreviewTexture 预览纹理",
                MessageType.Info);

            if (GUILayout.Button("导出 DiffusionProfileParams Asset", GUILayout.Height(24)))
            {
                if (_discKernelTex == null || _discKernelPreviewTex == null)
                    RegenerateDiscKernelAndPreview();

                bool ok = SSS_DiscSampling.SaveResolveProfileParamsAsset(
                    PreIntegrated._burleyParameters,
                    1.0f,
                    _discKernelTex,
                    _discPreviewTexture,
                    _discSampleCount,
                    PreIntegrated.GetEffectiveMaxRadius(),
                    _resolveParamsAssetPath);

                if (ok)
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(_resolveParamsAssetPath);
                    if (asset != null)
                        Selection.activeObject = asset;
                }
            }

            EditorGUILayout.Space();
        }

        private void ApplyResolveParamsAsset(DiffusionProfileParam asset)
        {
            if (asset == null)
                return;

            PreIntegrated._burleyParameters._scatteringColor = asset.scatteringColor;
            PreIntegrated._burleyParameters._scatteringMultiplier = asset.scatteringMultiplier;
            PreIntegrated._burleyParameters._indexOfRefraction = asset.indexOfRefraction;

            _indexOfRefraction = asset.indexOfRefraction;
            _WorldScale = asset.worldScale;

            _discSampleCount = asset.kernelSampleCount;

            _discKernelTex = asset.discKernelTex;
            _discPreviewTexture = asset.discPreviewTexture;


            SSS_DiscSampling.RegenerateKernelPreview(
                _discKernelTex, ref _discKernelPreviewTex,
                _discKernelPreviewSize, PreIntegrated._burleyParameters._maxRadius);
            

            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath))
                _resolveParamsAssetPath = assetPath;

            MarkAllDirty();
            Repaint();
        }

        private void RegenerateDiscKernelAndPreview()
        {
            float maxR = PreIntegrated.GetEffectiveMaxRadius();
            // 生成采样表（运行时数据）
            if (_discKernelTex != null) DestroyImmediate(_discKernelTex);
            _discKernelTex = SSS_DiscSampling.GenerateDiscKernel(PreIntegrated._burleyParameters, _discSampleCount, _discImportanceCdfResolution);

            // 生成可视化预览（仅编辑器显示）
            SSS_DiscSampling.RegenerateKernelPreview(
                _discKernelTex, ref _discKernelPreviewTex,
                _discKernelPreviewSize, maxR);

            Repaint();
        }

        private void DrawDiscKernelPreview()
        {
            EditorGUILayout.LabelField("Disc Kernel Preview (采样点分布)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _discSampleCount = EditorGUILayout.IntSlider(
                new GUIContent("Sample Count", "屏幕空间 SSSS 每像素采样点数"),
                _discSampleCount, 8, 64);
            _discImportanceCdfResolution = EditorGUILayout.IntSlider(
                new GUIContent("Importance CDF Resolution", "构建逆 CDF 的积分精度"),
                _discImportanceCdfResolution, 64, 4096);
            if (EditorGUI.EndChangeCheck())
                RegenerateDiscKernelAndPreview();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("生成 / 刷新 Disc Kernel", GUILayout.Height(22)))
                    RegenerateDiscKernelAndPreview();
            }

            if (_discKernelPreviewTex != null)
            {
                float size = Mathf.Min(EditorGUIUtility.currentViewWidth - 40, 280);
                var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(rect, _discKernelPreviewTex);
                EditorGUILayout.LabelField(
                    $"{_discSampleCount} 个采样点 · Vogel 黄金角分布 · 点色=权重 点大小=权重强度",
                    EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("点击上方按钮生成 disc kernel 预览。", MessageType.Info);
            }

            EditorGUILayout.Space();
        }
    }
}