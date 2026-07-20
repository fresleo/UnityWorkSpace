/*******************************************************************************
 * File: TimeOfDayEditorExplorerTab.cs
 * Author: Codex
 * Date: 2026/04/24
 * Description: TOD 一键导出面板，负责配置场景列表并发起批量导出。
 *******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XKT.TOD.Utils;

namespace XKT.TOD
{
    /// <summary>
    /// TOD 一键导出面板。
    /// </summary>
    internal sealed class TimeOfDayEditorExplorerTab : TimeOfDayEditorTabBase
    {
        private static readonly GUIContent TabTitle = new GUIContent("TOD 数据");
        private static readonly GUIContent TabTip = new GUIContent("按场景列表顺序一键导出 TOD 数据和 Lightmap 烘焙结果。");

        private readonly TimeOfDayExportWorkflow m_exportWorkflow;
        private readonly List<SceneAsset> m_sceneAssets;
        private DefaultTimeOfDayExplorerExtension m_currentExplorerExtension;
        private LightingExplorerTab[] m_tableTabs;
        private GUIContent[] m_tabTitles;
        private GUIContent[] m_tabTips;
        private Vector2 m_sceneScroll;
        private int m_selectedSceneIndex = -1;
        private int m_selectedTab;

        public TimeOfDayEditorExplorerTab(TimeOfDayExportWorkflow exportWorkflow, List<SceneAsset> sceneAssets)
        {
            m_exportWorkflow = exportWorkflow;
            m_sceneAssets = sceneAssets;
        }

        public override GUIContent Title => TabTitle;

        public override GUIContent Tip => TabTip;

        private LightingExplorerTab CurrentExplorerTab
        {
            get
            {
                if (m_tableTabs == null || m_selectedTab < 0 || m_selectedTab >= m_tableTabs.Length)
                {
                    return null;
                }

                return m_tableTabs[m_selectedTab];
            }
        }

        private GUIContent CurrentExplorerTabTip
        {
            get
            {
                if (m_tabTips == null || m_selectedTab < 0 || m_selectedTab >= m_tabTips.Length)
                {
                    return null;
                }

                return m_tabTips[m_selectedTab];
            }
        }

        public override void OnEnable()
        {
            UpdateExplorerTabs();
        }

        public override void OnDisable()
        {
            if (m_tableTabs != null)
            {
                for (int i = 0; i < m_tableTabs.Length; i++)
                {
                    m_tableTabs[i].ROnDisable();
                }
            }

            if (m_currentExplorerExtension != null)
            {
                m_currentExplorerExtension.OnDisable();
            }

            m_currentExplorerExtension = null;
            m_tableTabs = null;
            m_tabTitles = null;
            m_tabTips = null;
        }

        public override void OnInspectorUpdate()
        {
            CurrentExplorerTab?.ROnInspectorUpdate();
        }

        public override void OnSelectionChange()
        {
            if (m_tableTabs == null)
            {
                return;
            }

            for (int i = 0; i < m_tableTabs.Length; i++)
            {
                if (i == (m_tableTabs.Length - 1))
                {
                    int[] selectedIds = TODUtils.FindObjectsOfTypeInActiveScene<MeshRenderer>()
                        .Where(meshRenderer => Selection.instanceIDs.Contains(meshRenderer.gameObject.GetInstanceID()))
                        .SelectMany(meshRenderer => meshRenderer.sharedMaterials)
                        .Where(material => material != null && (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) != 0)
                        .Select(material => material.GetInstanceID())
                        .Union(Selection.instanceIDs)
                        .Distinct()
                        .ToArray();

                    m_tableTabs[i].ROnSelectionChange(selectedIds);
                    continue;
                }

                m_tableTabs[i].ROnSelectionChange();
            }
        }

        public override void OnHierarchyChange()
        {
            if (m_tableTabs == null)
            {
                return;
            }

            for (int i = 0; i < m_tableTabs.Length; i++)
            {
                m_tableTabs[i].ROnHierarchyChange();
            }
        }

        public override void OnGUI()
        {
            UpdateExplorerTabs();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawSceneList();

                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(m_exportWorkflow.IsRunning))
                {
                    if (GUILayout.Button("一键导出", GUILayout.Height(28)))
                    {
                        EditorCoroutineUtility.StartCoroutine(m_exportWorkflow.ExportScenes(m_sceneAssets), Window);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(GetExportStatus(), m_exportWorkflow.IsRunning ? MessageType.Warning : MessageType.Info);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawExplorerContent();
            }
        }

        private void DrawSceneList()
        {
            EditorGUILayout.LabelField("Unity Scene 列表", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加场景", GUILayout.Width(90)))
                {
                    m_sceneAssets.Add(null);
                    m_selectedSceneIndex = m_sceneAssets.Count - 1;
                }

                using (new EditorGUI.DisabledScope(m_sceneAssets.Count == 0))
                {
                    if (GUILayout.Button("清空列表", GUILayout.Width(90)))
                    {
                        m_sceneAssets.Clear();
                        m_selectedSceneIndex = -1;
                    }
                }
            }

            EditorGUILayout.Space();
            if (m_sceneAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("请添加需要批量导出的 Unity Scene。", MessageType.Info);
                return;
            }

            float listHeight = GetSceneListHeight();
            m_sceneScroll = EditorGUILayout.BeginScrollView(m_sceneScroll, GUILayout.Height(listHeight));
            for (int i = 0; i < m_sceneAssets.Count; i++)
            {
                DrawSceneRow(i);
            }
            EditorGUILayout.EndScrollView();
        }

        private float GetSceneListHeight()
        {
            float rowHeight = EditorGUIUtility.singleLineHeight + 8f;
            float contentHeight = m_sceneAssets.Count * rowHeight + 8f;
            return Mathf.Clamp(contentHeight, 60f, 240f);
        }

        private void DrawSceneRow(int index)
        {
            const float rowPadding = 4f;
            const float indexWidth = 30f;
            const float openWidth = 46f;
            const float moveWidth = 28f;
            const float deleteWidth = 48f;
            const float gap = 4f;

            float rowHeight = EditorGUIUtility.singleLineHeight + 6f;
            Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
            bool selected = index == m_selectedSceneIndex;
            Color rowColor = selected ? new Color(0.24f, 0.48f, 0.90f, 0.22f) : (index % 2 == 0 ? new Color(0f, 0f, 0f, 0.06f) : Color.clear);
            if (rowColor.a > 0f)
            {
                EditorGUI.DrawRect(rowRect, rowColor);
            }

            float y = rowRect.y + 3f;
            float x = rowRect.x + rowPadding;
            float height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(new Rect(x, y, indexWidth, height), (index + 1).ToString());
            x += indexWidth + gap;

            float fixedWidth = openWidth + moveWidth * 2f + deleteWidth + gap * 4f + rowPadding;
            float sceneFieldWidth = Mathf.Max(120f, rowRect.width - indexWidth - fixedWidth);
            EditorGUI.BeginChangeCheck();
            SceneAsset newSceneAsset = EditorGUI.ObjectField(new Rect(x, y, sceneFieldWidth, height), m_sceneAssets[index], typeof(SceneAsset), false) as SceneAsset;
            if (EditorGUI.EndChangeCheck())
            {
                m_sceneAssets[index] = newSceneAsset;
                m_selectedSceneIndex = index;
                OpenSelectedScene(index);
            }
            x += sceneFieldWidth + gap;

            if (GUI.Button(new Rect(x, y, openWidth, height), "打开"))
            {
                SelectScene(index);
            }
            x += openWidth + gap;

            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUI.Button(new Rect(x, y, moveWidth, height), "↑"))
                {
                    SwapScenes(index, index - 1);
                    m_selectedSceneIndex = index - 1;
                }
            }
            x += moveWidth + gap;

            using (new EditorGUI.DisabledScope(index == m_sceneAssets.Count - 1))
            {
                if (GUI.Button(new Rect(x, y, moveWidth, height), "↓"))
                {
                    SwapScenes(index, index + 1);
                    m_selectedSceneIndex = index + 1;
                }
            }
            x += moveWidth + gap;

            if (GUI.Button(new Rect(x, y, deleteWidth, height), "删除"))
            {
                m_sceneAssets.RemoveAt(index);
                if (m_sceneAssets.Count == 0)
                {
                    m_selectedSceneIndex = -1;
                }
                else
                {
                    m_selectedSceneIndex = Mathf.Clamp(m_selectedSceneIndex, 0, m_sceneAssets.Count - 1);
                }
            }
        }

        private void DrawExplorerContent()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (m_tabTitles != null)
                {
                    m_selectedTab = GUILayout.Toolbar(m_selectedTab, m_tabTitles, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
                }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space();
            GUIContent currentTip = CurrentExplorerTabTip;
            if (currentTip != null)
            {
                EditorGUILayout.HelpBox(currentTip.text, MessageType.Info);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                CurrentExplorerTab?.ROnGUI();
                EditorGUILayout.Space();
            }
        }

        private void SwapScenes(int sourceIndex, int targetIndex)
        {
            SceneAsset temp = m_sceneAssets[sourceIndex];
            m_sceneAssets[sourceIndex] = m_sceneAssets[targetIndex];
            m_sceneAssets[targetIndex] = temp;
        }

        private void SelectScene(int index)
        {
            if (index < 0 || index >= m_sceneAssets.Count)
            {
                return;
            }

            m_selectedSceneIndex = index;
            OpenSelectedScene(index);
        }

        private void OpenSelectedScene(int index)
        {
            if (m_exportWorkflow.IsRunning)
            {
                return;
            }

            SceneAsset sceneAsset = m_sceneAssets[index];
            if (sceneAsset == null)
            {
                return;
            }

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }

            if (!CustomSceneUtility.SaveModifiedScenesDialog())
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            OnHierarchyChange();
            Window.Repaint();
        }

        /// <summary>
        /// 初始化 TOD Explorer 的内部 Tab 配置。
        /// </summary>
        private void UpdateExplorerTabs()
        {
            if (m_currentExplorerExtension != null)
            {
                return;
            }

            m_currentExplorerExtension = new DefaultTimeOfDayExplorerExtension();
            m_currentExplorerExtension.OnEnable();

            m_selectedTab = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D ? 1 : 0;

            LightingExplorerTab[] contentTabs = m_currentExplorerExtension.GetContentTabs();
            if (contentTabs == null || contentTabs.Length == 0)
            {
                throw new ArgumentException("必须至少定义 1 个 TOD Explorer Tab。");
            }

            m_tableTabs = contentTabs;
            m_tabTitles = m_tableTabs.Select(item => item.RGetTitle()).ToArray();
            m_tabTips = m_currentExplorerExtension.GetTabTips();
        }

        /// <summary>
        /// 获取当前一键导出状态。
        /// </summary>
        private string GetExportStatus()
        {
            if (!m_exportWorkflow.IsRunning)
            {
                return $"状态: {m_exportWorkflow.Status}\nLightmap: {TimeOfDayLightmapBakeService.Status}";
            }

            return $"正在一键导出...\n{m_exportWorkflow.Status}\n{TimeOfDayLightmapBakeService.Status}";
        }
    }
}
