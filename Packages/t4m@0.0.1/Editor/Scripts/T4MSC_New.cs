/********************************************************
 * File:    T4MSC_New.cs
 * Description: T4M 编辑器窗口（重构版 - 协调者模式）
 * Note:    完成测试后，重命名为 T4MSC.cs 替换原文件
 *********************************************************/

using T4MEditor.Data;
using T4MEditor.Panels;
using UnityEditor;
using UnityEngine;

namespace T4MEditor
{
    /// <summary>
    /// T4M 编辑器主窗口 - 协调各个面板
    /// </summary>
    public class T4MSC_New : EditorWindow
    {
        private const string MenuToolbarPrefKey = "T4M_MenuToolbar";

        #region Menu

        [MenuItem("Window/T4M Terrain Tool New")]
        public static void ShowWindow()
        {
            var window = GetWindow<T4MSC_New>("T4M SC");
            window.minSize = new Vector2(386, 585);
            window.Show();
        }

        #endregion

        #region Fields

        private T4MEditorState _state;
        private T4MConverterPanel _converterPanel;
        private T4MSettingsPanel _settingsPanel;
        private T4MPainterPanel _painterPanel;

        private int _menuToolbar = 0;
        private string[] _menuNames = { "My T4M", "Converter", "Painter" };
        private GUIContent[] _menuIcons;

        private bool _initialized = false;

        #endregion

        #region Compatibility Properties (for T4MExtendsSC)

        /// <summary>
        /// 当前选中的 Transform（兼容旧代码）
        /// </summary>
        public static Transform CurrentSelect => T4MEditorState.Instance.CurrentSelect;

        /// <summary>
        /// 菜单工具栏索引（兼容旧代码）
        /// </summary>
        public static int T4MMenuToolbar => T4MEditorState.Instance.MenuToolbar;

        /// <summary>
        /// 控制图1（兼容旧代码）
        /// </summary>
        public static Texture2D T4MMaskTex => T4MEditorState.Instance.MaskTex;

        /// <summary>
        /// 控制图2（兼容旧代码）
        /// </summary>
        public static Texture2D T4MMaskTex2 => T4MEditorState.Instance.MaskTex2;

        /// <summary>
        /// UV 坐标缩放（兼容旧代码）
        /// </summary>
        public static float T4MMaskTexUVCoord => T4MEditorState.Instance.MaskUVCoord;

        /// <summary>
        /// 预览投影器（兼容旧代码）
        /// </summary>
        public static Projector T4MPreview => T4MEditorState.Instance.Preview;

        /// <summary>
        /// 是否激活（兼容旧代码）
        /// </summary>
        public static string T4MActived => T4MEditorState.Instance.IsActivated ? "Activated" : "Deactivated";

        /// <summary>
        /// 笔刷大小（兼容旧代码）
        /// </summary>
        public static int brushSize
        {
            get => Mathf.RoundToInt(T4MEditorState.Instance.Brush.Size);
            set => T4MEditorState.Instance.Brush.SetSize(value);
        }

        /// <summary>
        /// 笔刷大小百分比（兼容旧代码）
        /// </summary>
        public static int T4MBrushSizeInPourcent => T4MEditorState.Instance.Brush.SizeInPourcent;

        /// <summary>
        /// 笔刷强度（兼容旧代码）
        /// </summary>
        public static float T4MStronger => T4MEditorState.Instance.Brush.Strength;

        /// <summary>
        /// 笔刷 Alpha（兼容旧代码）
        /// </summary>
        public static float[] T4MBrushAlpha => T4MEditorState.Instance.Brush.Alpha;

        /// <summary>
        /// 选中纹理索引（兼容旧代码）
        /// </summary>
        public static int T4MselTexture => T4MEditorState.Instance.Brush.SelectedTexture;

        /// <summary>
        /// 目标颜色（兼容旧代码）
        /// </summary>
        public static Color T4MtargetColor => T4MEditorState.Instance.Brush.TargetColor;

        /// <summary>
        /// 目标颜色2（兼容旧代码）
        /// </summary>
        public static Color T4MtargetColor2 => T4MEditorState.Instance.Brush.TargetColor2;

        /// <summary>
        /// 是否使用 UV4（兼容旧代码）
        /// </summary>
        public static bool useUV4 => T4MEditorState.Instance.Brush.UseUV4;

        /// <summary>
        /// 预览模式（兼容旧代码）
        /// </summary>
        public static T4MPaintHandle PaintPrev => T4MEditorState.Instance.Brush.PreviewMode;

        /// <summary>
        /// 笔刷纹理数组（兼容旧代码）
        /// </summary>
        public static Texture[] TexBrush => T4MEditorState.Instance.Brush.BrushTextures;

        /// <summary>
        /// 保存控制图（兼容旧代码）
        /// </summary>
        public static void SaveTexture()
        {
            if (!T4MEditorState.Instance.SaveCurrentControlMaps())
            {
                Debug.LogWarning("[T4MSC_New] 控制图保存失败，请检查 Console 中的错误信息");
            }
        }

        /// <summary>
        /// 获取笔刷基础纹理（兼容旧代码）
        /// </summary>
        public static Texture GetBrushBaseTexture(int brushIndex)
        {
            return T4MEditorState.Instance.Brush.GetBrushTexture(brushIndex);
        }

        /// <summary>
        /// 获取选中笔刷基础纹理（兼容旧代码）
        /// </summary>
        public static Texture GetSelectedBrushBaseTexture()
        {
            return T4MEditorState.Instance.Brush.GetSelectedBrushTexture();
        }

        /// <summary>
        /// 获取选中笔刷索引（兼容旧代码）
        /// </summary>
        public static int GetSelectedBrushIndex()
        {
            return T4MEditorState.Instance.Brush.SelectedBrush;
        }

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// Unity 编辑器启动后注册应用级失焦自动保存。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterEditorFocusAutoSave()
        {
            EditorApplication.focusChanged -= OnEditorFocusChanged;
            EditorApplication.focusChanged += OnEditorFocusChanged;
        }

        /// <summary>
        /// Unity Editor 应用失去焦点时保存未写入磁盘的控制图。
        /// </summary>
        private static void OnEditorFocusChanged(bool focused)
        {
            if (focused)
            {
                return;
            }

            var state = T4MEditorState.Instance;
            if (!state.HasUnsavedControlMapChanges)
            {
                return;
            }

            if (!state.SaveDirtyControlMaps())
            {
                Debug.LogWarning("[T4MSC_New] Unity Editor 失去焦点时自动保存 T4M 控制图失败，请检查 Console 中的错误信息");
            }
        }

        /// <summary>
        /// 窗口启用时调用，初始化并注册选择变更事件
        /// </summary>
        private void OnEnable()
        {
            Initialize();
            Selection.selectionChanged += OnSelectionChanged;
        }

        /// <summary>
        /// 窗口禁用时调用，取消注册选择变更事件
        /// </summary>
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        /// <summary>
        /// 绘制窗口 GUI
        /// </summary>
        private void OnGUI()
        {
            if (!_initialized)
            {
                Initialize();
            }
            
            EditorGUILayout.Space(10);
            DrawMenuToolbar();
            EditorGUILayout.Space(10);

            if (_state.IsActivated)
            {
                DrawCurrentPanel();
            }
        }

        /// <summary>
        /// 编辑器选择变更时调用，更新当前选中对象
        /// </summary>
        private void OnSelectionChanged()
        {
            UpdateSelection();
            Repaint();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化编辑器窗口，创建各个面板实例
        /// </summary>
        private void Initialize()
        {
            _state = T4MEditorState.Instance;

            _converterPanel = new T4MConverterPanel(_state);
            _settingsPanel = new T4MSettingsPanel(_state);
            _painterPanel = new T4MPainterPanel(_state);

            LoadMenuIcons();
            UpdateSelection();

            _menuToolbar = EditorPrefs.GetInt(MenuToolbarPrefKey, 0);
            _state.MenuToolbar = _menuToolbar;

            _initialized = true;
        }

        /// <summary>
        /// 加载菜单工具栏图标
        /// </summary>
        private void LoadMenuIcons()
        {
            _menuIcons = new GUIContent[3];

            // 尝试加载图标
            string iconPath = "Packages/T4M/Editor/Icons/";

            var icon0 = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath + "myt4m.png");
            var icon1 = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath + "conv.png");
            var icon2 = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath + "paint.png");

            _menuIcons[0] = new GUIContent(icon0 ?? EditorGUIUtility.IconContent("d_Terrain Icon").image, "My T4M");
            _menuIcons[1] = new GUIContent(icon1 ?? EditorGUIUtility.IconContent("d_Mesh Icon").image, "Converter");
            _menuIcons[2] = new GUIContent(icon2 ?? EditorGUIUtility.IconContent("d_Brush Icon").image, "Painter");
        }

        /// <summary>
        /// 更新当前选中对象，同步到编辑器状态
        /// </summary>
        private void UpdateSelection()
        {
            if (Selection.activeTransform != null)
            {
                _state.SetCurrentSelect(Selection.activeTransform);
            }

            _converterPanel?.UpdateSelection();
        }

        #endregion

        #region UI Drawing

        /// <summary>
        /// 绘制菜单工具栏（My T4M / Converter / Painter）
        /// </summary>
        private void DrawMenuToolbar()
        {
            GUILayout.BeginHorizontal();

            if (_menuIcons != null && _menuIcons.Length == 3)
            {
                _menuToolbar = GUILayout.Toolbar(_menuToolbar, _menuIcons, "gridlist", GUILayout.Width(66), GUILayout.Height(18));
            }
            else
            {
                _menuToolbar = GUILayout.Toolbar(_menuToolbar, _menuNames);
            }

            _state.MenuToolbar = _menuToolbar;
            EditorPrefs.SetInt(MenuToolbarPrefKey, _menuToolbar);

            GUILayout.FlexibleSpace();

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = _state.IsActivated ? Color.green : Color.red;
            string statusLabel = _state.IsActivated ? "Activated" : "Deactivated";
            if (GUILayout.Button(statusLabel, GUILayout.Height(18)))
            {
                _state.IsActivated = !_state.IsActivated;
            }
            GUI.backgroundColor = prevColor;

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 根据当前选中的菜单绘制对应面板
        /// </summary>
        private void DrawCurrentPanel()
        {
            switch (_menuToolbar)
            {
                case 0:
                    _settingsPanel?.OnGUI();
                    break;
                case 1:
                    _converterPanel?.OnGUI();
                    break;
                case 2:
                    _painterPanel?.OnGUI();
                    break;
            }
        }

        #endregion
    }
}
