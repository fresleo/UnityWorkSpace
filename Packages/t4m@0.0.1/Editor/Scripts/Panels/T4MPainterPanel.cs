/********************************************************
 * File:    T4MPainterPanel.cs
 * Description: T4M 绘制面板 UI
 *********************************************************/

using T4MEditor.Data;
using T4MEditor.Services;
using UnityEditor;
using UnityEngine;

namespace T4MEditor.Panels
{
    /// <summary>
    /// 绘制面板，处理笔刷绘制相关 UI
    /// </summary>
    public class T4MPainterPanel
    {
        #region Fields

        private T4MEditorState _state;
        private Vector2 _scrollPos;
        private int _subTabIndex = 0;
        private string[] _subTabNames = { "绘制", "材质设置" };

        private Texture2D[] _brushTextures;
        private int _selectedBrushIndex = 0;

        #endregion

        #region Constructor

        public T4MPainterPanel(T4MEditorState state)
        {
            _state = state;
            LoadBrushTextures();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 绘制面板
        /// </summary>
        public void OnGUI()
        {
            if (_state.CurrentSelect == null)
            {
                EditorGUILayout.HelpBox("请选择一个对象", MessageType.Info);
                return;
            }

            var t4mObj = _state.CurrentSelect.GetComponent<T4MObjSC>();
            if (t4mObj == null)
            {
                EditorGUILayout.HelpBox("选中的对象不是 T4M 对象(没有T4MObjSC组件)", MessageType.Warning);
                return;
            }

            _subTabIndex = GUILayout.Toolbar(_subTabIndex, _subTabNames);
            EditorGUILayout.Space(10);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_subTabIndex == 0)
            {
                DrawPaintTab();
            }
            else
            {
                DrawMaterialTab();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Private Methods - Paint Tab

        /// <summary>
        /// 绘制绘制页签的完整内容
        /// </summary>
        private void DrawPaintTab()
        {
            if (_state.CurrentSelect != null)
            {
                T4MPreviewService.EnsurePreview(_state);
                T4MPreviewService.SyncPreview(_state);
            }

            DrawActivationToggle();
            EditorGUILayout.Space(10);
            DrawControlMapSaveActions();
            EditorGUILayout.Space(10);
            DrawPreviewModeSelector();
            EditorGUILayout.Space(10);
            DrawBrushSettings();
            EditorGUILayout.Space(10);
            DrawBrushSelector();
            EditorGUILayout.Space(10);
            DrawTextureSelector();
        }

        /// <summary>
        /// 绘制绘制激活状态切换按钮
        /// </summary>
        private void DrawActivationToggle()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("绘制状态:", GUILayout.Width(80));

            GUI.backgroundColor = _state.IsActivated ? Color.green : Color.red;
            string statusText = _state.IsActivated ? "已激活 (按 T 切换)" : "已禁用 (按 T 切换)";
            if (GUILayout.Button(statusText, GUILayout.Width(150)))
            {
                _state.IsActivated = !_state.IsActivated;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制控制图保存按钮。
        /// </summary>
        private void DrawControlMapSaveActions()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("控制图保存:", GUILayout.Width(80));

            using (new EditorGUI.DisabledScope(!_state.HasUnsavedControlMapChanges))
            {
                if (GUILayout.Button("保存控制图", GUILayout.Width(150)))
                {
                    if (!_state.SaveDirtyControlMaps())
                    {
                        Debug.LogWarning("[T4MPainterPanel] 控制图保存失败，请检查 Console 中的错误信息");
                    }
                }
            }

            string status = _state.HasUnsavedControlMapChanges ? "有未保存修改" : "已保存";
            EditorGUILayout.LabelField(status);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制预览模式选择器
        /// </summary>
        private void DrawPreviewModeSelector()
        {
            EditorGUILayout.LabelField("预览模式", EditorStyles.boldLabel);

            string[] previewModeNames = { "常规", "圆面", "圆圈", "不显示" };
            int currentMode = (int)_state.Brush.PreviewMode;

            EditorGUI.BeginChangeCheck();
            currentMode = EditorGUILayout.Popup("预览类型", currentMode, previewModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                _state.Brush.PreviewMode = (T4MPaintHandle)currentMode;
            }
        }

        /// <summary>
        /// 绘制笔刷大小和强度设置
        /// </summary>
        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("笔刷设置", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _state.Brush.SetSize(EditorGUILayout.Slider(
                "笔刷大小",
                _state.Brush.Size,
                T4MBrushSettings.C_MIN_SIZE,
                T4MBrushSettings.C_MAX_SIZE));
            _state.Brush.Strength = EditorGUILayout.Slider("笔刷强度", _state.Brush.Strength, 0.05f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                UpdateBrushAlpha();
            }

            EditorGUILayout.HelpBox("快捷键: 小键盘 +/- 调整笔刷大小", MessageType.Info);
        }

        /// <summary>
        /// 绘制笔刷形状选择器（网格布局）
        /// </summary>
        private void DrawBrushSelector()
        {
            EditorGUILayout.LabelField("笔刷形状", EditorStyles.boldLabel);

            if (_brushTextures == null || _brushTextures.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到笔刷纹理", MessageType.Warning);
                if (GUILayout.Button("重新加载笔刷"))
                {
                    LoadBrushTextures();
                }
                return;
            }

            int columns = 4;
            int rows = Mathf.CeilToInt((float)_brushTextures.Length / columns);

            EditorGUI.BeginChangeCheck();

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    if (index >= _brushTextures.Length) break;

                    bool isSelected = index == _selectedBrushIndex;
                    GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

                    if (GUILayout.Button(_brushTextures[index], GUILayout.Width(50), GUILayout.Height(50)))
                    {
                        _selectedBrushIndex = index;
                        _state.Brush.SelectedBrush = index;
                        UpdateBrushAlpha();
                    }

                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                _state.Brush.BrushTextures = _brushTextures;
            }
        }

        /// <summary>
        /// 绘制图层纹理选择器
        /// </summary>
        private void DrawTextureSelector()
        {
            EditorGUILayout.LabelField("绘制图层", EditorStyles.boldLabel);

            if (_state.Layers == null) return;

            int columns = 3;
            int validLayerCount = Mathf.Min(_state.LayerCount, _state.Layers.Length);

            for (int i = 0; i < validLayerCount; i++)
            {
                if (i % columns == 0)
                {
                    if (i > 0) EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                var layer = _state.Layers[i];
                bool isSelected = i == _state.Brush.SelectedTexture;

                GUI.backgroundColor = isSelected ? Color.yellow : Color.white;

                EditorGUILayout.BeginVertical("box", GUILayout.Width(80));

                if (layer != null && layer.Albedo != null)
                {
                    if (GUILayout.Button(layer.Albedo as Texture2D, GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        SelectTexture(i);
                    }
                }
                else
                {
                    if (GUILayout.Button($"图层 {i + 1}", GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        SelectTexture(i);
                    }
                }

                EditorGUILayout.LabelField($"图层 {i + 1}", GUILayout.Width(64));
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = Color.white;
            }

            if (validLayerCount > 0)
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Private Methods - Material Tab

        /// <summary>
        /// 绘制材质设置页签的完整内容
        /// </summary>
        private void DrawMaterialTab()
        {
            EditorGUILayout.LabelField("图层纹理设置", EditorStyles.boldLabel);

            if (_state.Layers == null) return;

            int validLayerCount = Mathf.Min(_state.LayerCount, _state.Layers.Length);

            for (int i = 0; i < validLayerCount; i++)
            {
                var layer = _state.Layers[i];
                if (layer == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"图层 {i + 1}", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();

                layer.Albedo = EditorGUILayout.ObjectField("颜色贴图", layer.Albedo, typeof(Texture), false) as Texture;
                layer.Normal = EditorGUILayout.ObjectField("法线贴图", layer.Normal, typeof(Texture), false) as Texture;
                layer.Tile = EditorGUILayout.Vector2Field("UV 平铺", layer.Tile);

                if (EditorGUI.EndChangeCheck() && _state.CurrentMaterial != null)
                {
                    layer.ApplyToMaterial(_state.CurrentMaterial);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("应用到材质"))
            {
                ApplyLayersToMaterial();
            }
        }

        #endregion

        #region Private Methods - Helpers

        /// <summary>
        /// 从编辑器资源加载笔刷纹理
        /// </summary>
        private void LoadBrushTextures()
        {
            string brushPath = "Packages/t4m@0.0.1/Editor/Brushes/";
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { brushPath });

            if (guids.Length == 0)
            {
                // 尝试备用路径
                brushPath = "Packages/T4M/Editor/Brushes/";
                guids = AssetDatabase.FindAssets("t:Texture2D", new[] { brushPath });
            }

            _brushTextures = new Texture2D[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _brushTextures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            _state.Brush.BrushTextures = _brushTextures;
        }

        /// <summary>
        /// 根据当前选中的笔刷更新 Alpha 数组
        /// </summary>
        private void UpdateBrushAlpha()
        {
            if (_brushTextures == null || _brushTextures.Length == 0) return;
            if (_selectedBrushIndex >= _brushTextures.Length) return;

            Texture2D brushTex = _brushTextures[_selectedBrushIndex];
            if (brushTex != null)
            {
                int mapW = _state.MaskTex != null ? _state.MaskTex.width : 1024;
                _state.Brush.UpdateBrushAlpha(brushTex, mapW);
            }
        }

        /// <summary>
        /// 选中指定索引的图层纹理
        /// </summary>
        /// <param name="index">图层索引</param>
        private void SelectTexture(int index)
        {
            _state.Brush.SelectedTexture = index;
            _state.Brush.UpdateTargetColor();
        }

        /// <summary>
        /// 将所有图层数据应用到当前材质
        /// </summary>
        private void ApplyLayersToMaterial()
        {
            if (_state.CurrentMaterial == null) return;

            T4MMaterialService.ApplyLayers(_state.CurrentMaterial, _state.Layers);
            Debug.Log("[T4MPainterPanel] 图层已应用到材质");
        }

        #endregion
    }
}
