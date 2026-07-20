/********************************************************
 * File:    T4MConverterPanel.cs
 * Description: T4M 转换器面板 UI
 *********************************************************/

using T4MEditor.Data;
using T4MEditor.Services;
using UnityEditor;
using UnityEngine;

namespace T4MEditor.Panels
{
    /// <summary>
    /// 转换器面板，处理 Unity Terrain 和 OBJ 模型的转换
    /// </summary>
    public class T4MConverterPanel
    {
        #region Fields

        private T4MEditorState _state;
        private T4MConversionSettings _settings;

        private string _terrainName = "";
        private int _resolution = 90;
        private bool _keepTexture = true;
        private bool _deleteOriginTerrain = false;
        private bool _hideOriginTerrain = true;
        private bool _needCreateNewMat = true;
        private string _newMatPath = "Assets/ArtRes/T4MOBJ/Materials/";

        private Vector2 _scrollPos;

        #endregion

        #region Constructor

        public T4MConverterPanel(T4MEditorState state)
        {
            _state = state;
            _settings = new T4MConversionSettings();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 绘制面板
        /// </summary>
        public void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawSelectionInfo();
            EditorGUILayout.Space(10);

            if (_state.CurrentSelect != null)
            {
                bool isTerrain = _state.CurrentSelect.GetComponent<Terrain>() != null;
                _terrainName = _state.CurrentSelect.name;
                if (isTerrain)
                {
                    DrawTerrainConverterUI();
                }
                else
                {
                    DrawModelConverterUI();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请在场景中选择一个 Unity Terrain 或 OBJ 模型", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 更新选中对象
        /// </summary>
        public void UpdateSelection()
        {
            if (Selection.activeTransform != null)
            {
                _state.SetCurrentSelect(Selection.activeTransform);
            }
        }

        #endregion

        #region Private Methods - UI

        /// <summary>
        /// 绘制当前选中对象信息
        /// </summary>
        private void DrawSelectionInfo()
        {
            EditorGUILayout.LabelField("当前选中", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("对象:", GUILayout.Width(50));
            EditorGUILayout.ObjectField(_state.CurrentSelect, typeof(Transform), true);
            EditorGUILayout.EndHorizontal();

            if (_state.CurrentSelect != null)
            {
                bool isTerrain = _state.CurrentSelect.GetComponent<Terrain>() != null;
                EditorGUILayout.LabelField("类型: " + (isTerrain ? "Unity Terrain" : "OBJ 模型"));
            }
        }

        /// <summary>
        /// 绘制 Unity Terrain 转换器 UI
        /// </summary>
        private void DrawTerrainConverterUI()
        {
            EditorGUILayout.LabelField("Unity Terrain → 模型", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            _terrainName = EditorGUILayout.TextField("地形名称", _terrainName);
            _resolution = EditorGUILayout.IntSlider("分辨率", _resolution, 10, 200);

            EditorGUILayout.Space(5);

            _keepTexture = EditorGUILayout.Toggle(new GUIContent("保留原始贴图", "保留 Unity Terrain 的前4层贴图"), _keepTexture);
            _hideOriginTerrain = EditorGUILayout.Toggle("转换后隐藏原地形", _hideOriginTerrain);
            _deleteOriginTerrain = EditorGUILayout.Toggle("转换后删除原地形", _deleteOriginTerrain);

            EditorGUILayout.Space(10);

            _settings.OutputFolder = EditorGUILayout.TextField("输出文件夹", _settings.OutputFolder);

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Unity Terrain → 模型", GUILayout.Height(30)))
            {
                ConvertTerrain();
            }
            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// 绘制 OBJ 模型转换器 UI
        /// </summary>
        private void DrawModelConverterUI()
        {
            EditorGUILayout.LabelField("模型兼容多层材质", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            _terrainName = EditorGUILayout.TextField("模型名称", _terrainName);

            EditorGUILayout.Space(5);

            _needCreateNewMat = EditorGUILayout.Toggle("创建新材质", _needCreateNewMat);

            if (_needCreateNewMat)
            {
                _newMatPath = EditorGUILayout.TextField("材质路径", _newMatPath);
            }

            EditorGUILayout.Space(10);

            _settings.OutputFolder = EditorGUILayout.TextField("输出文件夹", _settings.OutputFolder);

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("开始处理", GUILayout.Height(30)))
            {
                ConvertModel();
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Private Methods - Actions

        /// <summary>
        /// 执行 Unity Terrain 到 T4M 的转换
        /// </summary>
        private void ConvertTerrain()
        {
            if (_state.CurrentSelect == null) return;

            Terrain terrain = _state.CurrentSelect.GetComponent<Terrain>();
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("错误", "选中的对象不是 Unity Terrain", "确定");
                return;
            }

            _settings.TerrainName = _terrainName;
            _settings.Resolution = _resolution;
            _settings.KeepTexture = _keepTexture;
            _settings.DeleteOriginTerrain = _deleteOriginTerrain;
            _settings.HideOriginTerrain = _hideOriginTerrain;

            try
            {
                EditorUtility.DisplayProgressBar("T4M 转换", "正在转换...", 0);

                T4MTerrainConverter.ConvertUnityTerrain(terrain, _settings, (progress, message) =>
                {
                    EditorUtility.DisplayProgressBar("T4M 转换", message, progress);
                });

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("完成", "Unity Terrain 转换完成", "确定");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误", $"转换失败: {ex.Message}", "确定");
                Debug.LogError($"[T4MConverterPanel] 转换失败: {ex}");
            }
        }

        /// <summary>
        /// 执行 OBJ 模型到 T4M 的转换
        /// </summary>
        private void ConvertModel()
        {
            if (_state.CurrentSelect == null) return;

            _settings.TerrainName = _terrainName;
            _settings.CreateNewMaterial = _needCreateNewMat;
            _settings.NewMaterialPath = _newMatPath;

            try
            {
                EditorUtility.DisplayProgressBar("T4M 转换", "正在转换...", 0);

                T4MTerrainConverter.ConvertModelToT4M(_state.CurrentSelect, _settings, (progress, message) =>
                {
                    EditorUtility.DisplayProgressBar("T4M 转换", message, progress);
                });

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("完成", "模型转换完成", "确定");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("错误", $"转换失败: {ex.Message}", "确定");
                Debug.LogError($"[T4MConverterPanel] 转换失败: {ex}");
            }
        }

        #endregion
    }
}
