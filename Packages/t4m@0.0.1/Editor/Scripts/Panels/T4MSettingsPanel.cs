/********************************************************
 * File:    T4MSettingsPanel.cs
 * Description: T4M 设置面板 UI（材质设置等）
 *********************************************************/

using T4MEditor.Data;
using T4MEditor.Services;
using UnityEditor;
using UnityEngine;

namespace T4MEditor.Panels
{
    /// <summary>
    /// 设置面板，处理 T4M 材质和通用设置
    /// </summary>
    public class T4MSettingsPanel
    {
        #region Fields

        private T4MEditorState _state;
        private Vector2 _scrollPos;

        #endregion

        #region Constructor

        public T4MSettingsPanel(T4MEditorState state)
        {
            _state = state;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 绘制面板
        /// </summary>
        public void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_state.CurrentSelect == null)
            {
                EditorGUILayout.HelpBox("请选择要修改的对象", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            var t4mObj = _state.CurrentSelect.GetComponent<T4MObjSC>();
            if (t4mObj == null)
            {
                EditorGUILayout.HelpBox("请选择转换过的对象进行修改(包含T4MObjSC组件)", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawMaterialSettings();
            EditorGUILayout.Space(10);
            DrawUV4Settings();
            EditorGUILayout.Space(10);
            DrawLayerSettings();
            EditorGUILayout.Space(10);
            DrawControlMapSettings();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 绘制材质设置区域
        /// </summary>
        private void DrawMaterialSettings()
        {
            EditorGUILayout.LabelField("材质设置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前材质:", GUILayout.Width(80));
            EditorGUILayout.ObjectField(_state.CurrentMaterial, typeof(Material), false);
            EditorGUILayout.EndHorizontal();

            if (_state.CurrentMaterial != null)
            {
                EditorGUILayout.LabelField("Shader: " + _state.CurrentMaterial.shader.name);
            }
        }

        /// <summary>
        /// 绘制 UV4 设置区域
        /// </summary>
        private void DrawUV4Settings()
        {
            EditorGUILayout.LabelField("UV4 设置", EditorStyles.boldLabel);

            bool currentUV4 = _state.CurrentMaterial != null && T4MMaterialService.IsUsingUV4(_state.CurrentMaterial);

            EditorGUI.BeginChangeCheck();
            bool useUV4 = EditorGUILayout.Toggle("使用 UV4", currentUV4);
            if (EditorGUI.EndChangeCheck() && _state.CurrentMaterial != null)
            {
                T4MMaterialService.SyncUV4Toggle(_state.CurrentMaterial, useUV4);
                _state.Brush.UseUV4 = useUV4;
            }

            EditorGUILayout.HelpBox("UV4 模式可用于精确控制控制图的 UV 映射", MessageType.Info);
        }

        /// <summary>
        /// 绘制图层设置区域
        /// </summary>
        private void DrawLayerSettings()
        {
            EditorGUILayout.LabelField("图层设置", EditorStyles.boldLabel);

            if (_state.Layers == null) return;

            int validLayers = 0;
            foreach (var layer in _state.Layers)
            {
                if (layer != null && layer.IsValid) validLayers++;
            }

            EditorGUILayout.LabelField($"有效图层数: {validLayers}");

            EditorGUILayout.Space(5);

            for (int i = 0; i < _state.Layers.Length; i++)
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
                EditorGUILayout.Space(3);
            }
        }

        /// <summary>
        /// 绘制控制图设置区域
        /// </summary>
        private void DrawControlMapSettings()
        {
            EditorGUILayout.LabelField("控制图设置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("控制图1:", GUILayout.Width(80));
            EditorGUILayout.ObjectField(_state.MaskTex, typeof(Texture2D), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("控制图2:", GUILayout.Width(80));
            EditorGUILayout.ObjectField(_state.MaskTex2, typeof(Texture2D), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            _state.MaskUVCoord = EditorGUILayout.FloatField("UV 缩放", _state.MaskUVCoord);

            EditorGUILayout.Space(5);

            if (_state.MaskTex != null)
            {
                string validation = T4MControlMapService.ValidateControlMapTexture(_state.MaskTex);
                if (validation != null)
                {
                    EditorGUILayout.HelpBox($"控制图1警告: {validation}", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("创建第二控制图"))
            {
                CreateSecondControlMap();
            }
            if (GUILayout.Button("重新加载材质"))
            {
                ReloadMaterial();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 创建第二张控制图（用于5-6层）
        /// </summary>
        private void CreateSecondControlMap()
        {
            if (_state.CurrentMaterial == null) return;

            string path = EditorUtility.SaveFilePanel("保存控制图2", "Assets", "Control2", "png");
            if (string.IsNullOrEmpty(path)) return;

            // 转换为相对路径
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            Texture2D control2 = T4MControlMapService.CreateSecondControlMap(path);
            T4MControlMapService.SetupTextureImporter(path);

            _state.MaskTex2 = control2;
            T4MMaterialService.SetControlMap(_state.CurrentMaterial, control2, true);

            Debug.Log($"[T4MSettingsPanel] 创建控制图2: {path}");
        }

        /// <summary>
        /// 重新加载当前选中对象的材质数据
        /// </summary>
        private void ReloadMaterial()
        {
            if (_state.CurrentSelect != null)
            {
                _state.SetCurrentSelect(_state.CurrentSelect);
            }
        }

        #endregion
    }
}
