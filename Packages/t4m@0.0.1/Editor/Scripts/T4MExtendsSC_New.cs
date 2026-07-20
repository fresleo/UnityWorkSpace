/********************************************************
 * File:    T4MExtendsSC_New.cs
 * Description: T4M 对象 CustomEditor（重构版）
 * Note:    完成测试后，重命名为 T4MExtendsSC.cs 替换原文件
 *********************************************************/

using T4MEditor;
using T4MEditor.Data;
using T4MEditor.Services;
using UnityEditor;
using UnityEngine;

/// <summary>
/// T4M 对象的 CustomEditor，处理场景视图绘制
/// </summary>
[CustomEditor(typeof(T4MObjSC))]
[CanEditMultipleObjects]
public class T4MExtendsSC_New : Editor
{
    private const string MenuToolbarPrefKey = "T4M_MenuToolbar";
    private const float C_BRUSH_SIZE_FINE_STEP_THRESHOLD = 2f;
    private const float C_BRUSH_SIZE_FINE_STEP = 0.1f;
    private const float C_BRUSH_SIZE_NORMAL_STEP = 1f;

    #region Fields

    private int _layerMask = 0;
    private bool _isPainting;
    private bool _strokeUndoRegistered;
    private Texture2D[] _undoObj;
    private int _state, _oldState;

    private static Color[] _terrainBay2;

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// 场景视图 GUI 回调，当处于 Painter 面板时处理绘制交互
    /// </summary>
    private void OnSceneGUI()
    {
        var state = T4MEditorState.Instance;

        state.MenuToolbar = EditorPrefs.GetInt(MenuToolbarPrefKey, 0);

        SyncInspectorTargetToPaintState(state);

        if (state.MenuToolbar == 2)
        {
            T4MPreviewService.EnsurePreview(state);
            T4MPreviewService.SyncPreview(state);
            Painter();
        }
        else
        {
            T4MPreviewService.ReleasePreview(state);
            _state = 3;
        }

        // 状态变化时清理预览对象
        if (_oldState != _state)
        {
            CleanupPreviewObjects();
            _oldState = _state;
        }
    }

    #endregion

    #region Painter

    /// <summary>
    /// 将当前 Inspector 选中的 T4M 对象同步到全局绘制状态（不依赖 T4M 主窗口是否打开）。
    /// </summary>
    private void SyncInspectorTargetToPaintState(T4MEditorState state)
    {
        var t4m = target as T4MObjSC;
        if (t4m == null) return;

        if (state.CurrentSelect != t4m.transform)
        {
            state.SetCurrentSelect(t4m.transform);
        }
    }

    /// <summary>
    /// 主绘制逻辑入口，处理场景视图中的笔刷绘制
    /// </summary>
    private void Painter()
    {
        var state = T4MEditorState.Instance;

        if (state.CurrentSelect == null)
        {
            return;
        }

        _state = 1;

        Event eve = Event.current;

        // T 键切换激活状态
        if (eve.type == EventType.KeyDown && eve.keyCode == KeyCode.T)
        {
            state.IsActivated = !state.IsActivated;
        }

        // 更新预览投影器状态
        UpdatePreviewProjector(state);

        if (!state.IsActivated)
        {
            return;
        }

        HandleUtility.AddDefaultControl(0);

        // 快捷键调整笔刷大小
        HandleBrushSizeShortcuts(eve, state);

        // 射线检测
        RaycastHit raycastHit;
        Ray terrain = HandleUtility.GUIPointToWorldRay(eve.mousePosition);
        int layerMask = T4MBrushPainter.GetPaintLayerMask(state.CurrentSelect);

        if (Physics.Raycast(terrain, out raycastHit, Mathf.Infinity, layerMask))
        {
            // 更新预览位置
            UpdatePreviewPosition(raycastHit, state);

            // 绘制预览圆
            DrawPreviewDisc(raycastHit, state);

            // 处理绘制
            HandlePainting(eve, raycastHit, state);
        }
    }

    /// <summary>
    /// 根据预览模式更新投影器的启用状态
    /// </summary>
    /// <param name="state">编辑器状态</param>
    private void UpdatePreviewProjector(T4MEditorState state)
    {
        if (state.Preview == null) return;

        bool shouldEnable = state.IsActivated &&
            state.Brush.PreviewMode != T4MPaintHandle.Follow_Normal_Circle &&
            state.Brush.PreviewMode != T4MPaintHandle.Follow_Normal_WireCircle &&
            state.Brush.PreviewMode != T4MPaintHandle.Hide_preview;

        if (state.Brush.PreviewMode == T4MPaintHandle.Classic)
        {
            shouldEnable = true;
        }

        state.Preview.enabled = shouldEnable;
    }

    /// <summary>
    /// 处理小键盘 +/- 调整笔刷大小的快捷键
    /// </summary>
    /// <param name="eve">当前事件</param>
    /// <param name="state">编辑器状态</param>
    private void HandleBrushSizeShortcuts(Event eve, T4MEditorState state)
    {
        if (eve.type == EventType.KeyDown && eve.keyCode == KeyCode.KeypadPlus)
        {
            state.Brush.SetSize(GetShortcutBrushSize(state.Brush.Size, 1f));
            RefreshBrushAlphaFromSelectedBrush(state);
        }
        else if (eve.type == EventType.KeyDown && eve.keyCode == KeyCode.KeypadMinus)
        {
            state.Brush.SetSize(GetShortcutBrushSize(state.Brush.Size, -1f));
            RefreshBrushAlphaFromSelectedBrush(state);
        }
    }

    private static float GetShortcutBrushSize(float currentSize, float direction)
    {
        float step = currentSize <= C_BRUSH_SIZE_FINE_STEP_THRESHOLD
            ? C_BRUSH_SIZE_FINE_STEP
            : C_BRUSH_SIZE_NORMAL_STEP;
        return Mathf.Round((currentSize + direction * step) * 10f) / 10f;
    }

    private static void RefreshBrushAlphaFromSelectedBrush(T4MEditorState state)
    {
        var tex = state.Brush.GetSelectedBrushTexture() as Texture2D;
        if (tex != null)
        {
            int mapW = state.MaskTex != null ? state.MaskTex.width : 1024;
            state.Brush.UpdateBrushAlpha(tex, mapW);
        }
    }

    /// <summary>
    /// 更新预览投影器的位置和旋转
    /// </summary>
    /// <param name="raycastHit">射线命中信息</param>
    /// <param name="state">编辑器状态</param>
    private void UpdatePreviewPosition(RaycastHit raycastHit, T4MEditorState state)
    {
        if (state.Preview == null) return;

        var t4mObjSC = state.CurrentSelect.GetComponent<T4MObjSC>();
        if (t4mObjSC != null && t4mObjSC.ConvertType != "UT")
        {
            state.Preview.transform.localEulerAngles = new Vector3(90, 180 + state.CurrentSelect.localEulerAngles.y, 0);
        }
        else
        {
            state.Preview.transform.localEulerAngles = new Vector3(90, -90 + state.CurrentSelect.localEulerAngles.y, 0);
        }

        state.Preview.transform.position = raycastHit.point;
    }

    /// <summary>
    /// 根据预览模式绘制笔刷预览圆盘
    /// </summary>
    /// <param name="raycastHit">射线命中信息</param>
    /// <param name="state">编辑器状态</param>
    private void DrawPreviewDisc(RaycastHit raycastHit, T4MEditorState state)
    {
        if (state.Preview == null) return;

        switch (state.Brush.PreviewMode)
        {
            case T4MPaintHandle.Follow_Normal_Circle:
                Handles.color = new Color(1f, 1f, 0f, 0.05f);
                Handles.DrawSolidDisc(raycastHit.point, raycastHit.normal, state.Preview.orthographicSize * 0.9f);
                break;

            case T4MPaintHandle.Follow_Normal_WireCircle:
                Handles.color = new Color(1f, 1f, 0f, 1f);
                Handles.DrawWireDisc(raycastHit.point, raycastHit.normal, state.Preview.orthographicSize * 0.9f);
                break;
        }
    }

    /// <summary>
    /// 处理鼠标绘制事件，调用 T4MBrushPainter 执行绘制
    /// </summary>
    /// <param name="eve">当前事件</param>
    /// <param name="raycastHit">射线命中信息</param>
    /// <param name="state">编辑器状态</param>
    private void HandlePainting(Event eve, RaycastHit raycastHit, T4MEditorState state)
    {
        bool shouldPaint = (eve.type == EventType.MouseDrag && !eve.alt && !eve.shift && eve.button == 0) ||
                          (!eve.shift && !eve.alt && eve.button == 0 && !_isPainting);

        if (shouldPaint)
        {
            RefreshBrushAlphaFromSelectedBrush(state);
            if (state.Brush.Alpha == null || state.Brush.Alpha.Length == 0)
            {
                return;
            }

            // 计算 UV 坐标
            int uvIndex = state.Brush.UseUV4 ? 4 : 1;
            if (!T4MBrushPainter.CalculateRaycastHitTexcoord(ref raycastHit, uvIndex, out Vector2 raycastHitTexcoord))
            {
                return;
            }

            // 创建绘制上下文
            var context = new T4MPaintContext
            {
                ControlMap = state.MaskTex,
                ControlMap2 = state.MaskTex2,
                UVCoord = state.MaskUVCoord,
                CurrentSelect = state.CurrentSelect
            };

            if (!_strokeUndoRegistered)
            {
                try
                {
                    if (state.MaskTex2 != null)
                    {
                        Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { state.MaskTex, state.MaskTex2 }, "T4MMask");
                    }
                    else if (state.MaskTex != null)
                    {
                        Undo.RegisterCompleteObjectUndo(state.MaskTex, "T4MMask");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"撤销注册异常:\n{ex}");
                }

                _strokeUndoRegistered = true;
            }

            if (T4MBrushPainter.Paint(context, raycastHitTexcoord, state.Brush))
            {
                state.MarkControlMapsDirty();
                _isPainting = true;
            }
            else
            {
                state.MenuToolbar = 0;
            }
        }
        else if (eve.type == EventType.MouseUp && !eve.alt && eve.button == 0)
        {
            _isPainting = false;
            _strokeUndoRegistered = false;
        }
    }

    /// <summary>
    /// 清理场景中的预览对象（previewT4M 等）
    /// </summary>
    private void CleanupPreviewObjects()
    {
        MeshRenderer[] prev = FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];
        foreach (MeshRenderer go in prev)
        {
            if (go.hideFlags == HideFlags.HideInHierarchy || go.name == "previewT4M")
            {
                go.hideFlags = 0;
                DestroyImmediate(go.gameObject);
            }
        }
    }

    #endregion
}
