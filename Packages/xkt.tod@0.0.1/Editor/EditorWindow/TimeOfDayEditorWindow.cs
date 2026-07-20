/*******************************************************************************
 * File: TimeOfDayEditorWindow.cs
 * Author: junwei.li
 * Date: 2026/04/24 15:12
 * Description: TOD 一键导出窗口。
 *******************************************************************************/

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace XKT.TOD
{
    [EditorWindowTitle(title = "TOD 一键导出", icon = "Lighting")]
    public class TimeOfDayEditorWindow : EditorWindow
    {
        public enum EditorTab
        {
            Explorer = 0,
        }

        private TimeOfDayEditorExplorerTab m_exportPanel;
        private TimeOfDayExportWorkflow m_exportWorkflow;
        [SerializeField]
        private List<SceneAsset> m_sceneAssets = new List<SceneAsset>();

        [MenuItem("Window/TA工具集/TOD/TOD 一键导出", priority = 1)]
        private static void OpenWindow()
        {
            ShowWindow(EditorTab.Explorer);
        }

        /// <summary>
        /// 打开 TOD 一键导出窗口。
        /// </summary>
        public static TimeOfDayEditorWindow ShowWindow(EditorTab tab)
        {
            TimeOfDayEditorWindow window = GetWindow<TimeOfDayEditorWindow>();
            window.titleContent = new GUIContent("TOD 一键导出");
            window.minSize = new Vector2(650, 360);
            window.Show();
            return window;
        }

        private void OnEnable()
        {
            EditorApplication.searchChanged += Repaint;
            EnsurePanel();
            Repaint();
        }

        private void OnDisable()
        {
            if (m_exportPanel != null)
            {
                m_exportPanel.OnDisable();
            }

            EditorApplication.searchChanged -= Repaint;
        }

        private void OnInspectorUpdate()
        {
            if (m_exportPanel != null)
            {
                m_exportPanel.OnInspectorUpdate();
            }

            if (m_exportWorkflow != null && m_exportWorkflow.IsRunning)
            {
                Repaint();
            }
        }

        private void OnSelectionChange()
        {
            if (m_exportPanel != null)
            {
                m_exportPanel.OnSelectionChange();
            }

            Repaint();
        }

        private void OnHierarchyChange()
        {
            if (m_exportPanel != null)
            {
                m_exportPanel.OnHierarchyChange();
            }
        }

        private void OnGUI()
        {
            EnsurePanel();
            EditorGUIUtility.labelWidth = 130;

            EditorGUILayout.Space();
            m_exportPanel.OnGUI();
        }

        /// <summary>
        /// 初始化窗口内容。
        /// </summary>
        private void EnsurePanel()
        {
            if (m_exportPanel != null)
            {
                return;
            }

            if (m_sceneAssets == null)
            {
                m_sceneAssets = new List<SceneAsset>();
            }

            m_exportWorkflow = new TimeOfDayExportWorkflow();
            m_exportPanel = new TimeOfDayEditorExplorerTab(m_exportWorkflow, m_sceneAssets);
            m_exportPanel.Bind(this);
            m_exportPanel.OnEnable();
        }
    }
}
