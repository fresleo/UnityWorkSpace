// Created by: WangYu   Date: 2025-10-11

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace com.garena.gcommon.engine.tool.assetimporter
{
    sealed class TemporaryDeselectScope : System.IDisposable
    {
        private readonly UnityEngine.Object[] m_prevSelection;
        private readonly EditorWindow m_prevFocused;
        private readonly List<(EditorWindow win, bool locked)> m_inspectors = new();
        
        private Type m_inspectorWindowType;

        private Type InspectorWindowType
        {
            get
            {
                if (m_inspectorWindowType == null)
                {
                    m_inspectorWindowType = Type.GetType("UnityEditor.InspectorWindow, UnityEditor");
                }
                return m_inspectorWindowType;
            }
        }

        private const BindingFlags k_bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        
        private PropertyInfo InspectorWindow_isLocked
        {
            get
            {
                var isLockedProp = this.InspectorWindowType.GetProperty("isLocked", k_bindingFlags);
                return isLockedProp;
            }
        }
        
        public TemporaryDeselectScope(bool focusProject = true)
        {
            m_prevSelection = Selection.objects;
            m_prevFocused = EditorWindow.focusedWindow;

            try
            {
                // 解锁所有 Inspector，避免释放不掉选中的焦点
                var inspectorType = this.InspectorWindowType;
                if (inspectorType != null)
                {
                    var isLockedProp = InspectorWindow_isLocked;
                    foreach (var item in Resources.FindObjectsOfTypeAll(inspectorType))
                    {
                        var ew = item as EditorWindow;
                        if (ew == null || isLockedProp == null) continue;

                        bool wasLocked = false;
                        try
                        {
                            wasLocked = (bool)isLockedProp.GetValue(ew, null);
                        }
                        catch
                        {
                            /* 忽略异常 */
                        }

                        m_inspectors.Add((ew, wasLocked));
                        try
                        {
                            if (wasLocked) isLockedProp.SetValue(ew, false, null);
                        }
                        catch
                        {
                            /* 忽略异常 */
                        }

                        ew.Repaint();
                    }
                }

                // 切到 Project 窗口
                if (focusProject)
                {
                    try
                    {
                        EditorUtility.FocusProjectWindow();
                    }
                    catch
                    {
                        EditorApplication.ExecuteMenuItem("Window/General/Project");
                    }
                }

                // 清空选中
                Selection.instanceIDs = Array.Empty<int>();
                Selection.objects = Array.Empty<UnityEngine.Object>();
                Selection.activeObject = null;

                // 强制重建编辑器追踪，确保 Inspector 跟随变更
                ActiveEditorTracker.sharedTracker.ForceRebuild();
            }
            catch
            {
                /* 忽略异常 */
            }
        }

        public void Dispose()
        {
            try
            {
                // 恢复历史选中
                Selection.objects = m_prevSelection ?? Array.Empty<UnityEngine.Object>();

                // 恢复 Inspector 锁定状态
                var inspectorType = this.InspectorWindowType;
                if (inspectorType != null)
                {
                    var isLockedProp = InspectorWindow_isLocked;
                    foreach (var (win, locked) in m_inspectors)
                    {
                        try
                        {
                            if (isLockedProp != null) isLockedProp.SetValue(win, locked, null);
                        }
                        catch
                        {
                        }

                        win.Repaint();
                    }
                }

                // 恢复焦点
                if (m_prevFocused != null) m_prevFocused.Focus();
            }
            catch
            {
                /* 忽略异常 */
            }
        }
        
    }
}