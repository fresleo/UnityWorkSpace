// Created By: WangYu  Date: 2025-03-11

using System;
using System.Collections;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.DataStructure;
using XKT.TOD.Utils;

namespace XKT.TOD
{
    [EditorWindowTitle(title = "TOD 资源浏览收集器", icon = "Lighting")]
    public class TimeOfDayExplorerWindow : EditorWindow
    {
        [MenuItem("Window/TA工具集/TOD/TOD 资源浏览收集器", priority = 2, secondaryPriority = 1)]
        static void CreateLightingExplorerWindow()
        {
            var window = EditorWindow.GetWindow<TimeOfDayExplorerWindow>();
            window.minSize = new Vector2(500, 250);
            window.Show();
        }

        private DefaultTimeOfDayExplorerExtension m_currentExplorerExtension;
        
        private LightingExplorerTab[] m_tableTabs;
        private GUIContent[] m_tabTitles;
        private GUIContent[] m_tabTips;
        
        private int m_selectedTab = 0;
        private string m_dateName = "";

        
        private void OnEnable()
        {
            EditorApplication.searchChanged += Repaint;
            Repaint();
        }

        private void OnDisable()
        {
            if (m_tableTabs != null)
            {
                for (int i = 0; i < m_tableTabs.Length; i++)
                {
                    m_tableTabs[i].ROnDisable();
                }
            }
            
            m_currentExplorerExtension?.OnDisable();
            
            EditorApplication.searchChanged -= Repaint;

            m_dateName = "";
        }

        private LightingExplorerTab CurrentTab
        {
            get
            {
                if (m_tableTabs != null && m_selectedTab >= 0 && m_selectedTab < m_tableTabs.Length)
                {
                    return m_tableTabs[m_selectedTab];
                }

                return null;
            }
        }
        
        private GUIContent CurrentTabTip
        {
            get
            {
                if (m_tabTips != null && m_selectedTab >= 0 && m_selectedTab < m_tabTips.Length)
                {
                    return m_tabTips[m_selectedTab];
                }

                return null;
            }
        }
        
        private void OnInspectorUpdate()
        {
            CurrentTab?.ROnInspectorUpdate();
        }

        private void OnSelectionChange()
        {
            if (m_tableTabs != null)
            {
                for (int i = 0; i < m_tableTabs.Length; i++)
                {
                    if (i == (m_tableTabs.Length - 1)) // 包含材质的 Last 选项卡
                    {
                        int[] selectedIds = TODUtils.FindObjectsOfTypeInActiveScene<MeshRenderer>()
                            .Where(
                                (MeshRenderer mr) => { return Selection.instanceIDs.Contains(mr.gameObject.GetInstanceID()); })
                            .SelectMany(meshRenderer => meshRenderer.sharedMaterials)
                            .Where(
                                (Material m) => { return m != null && (m.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) != 0; })
                            .Select(m => m.GetInstanceID())
                            .Union(Selection.instanceIDs).Distinct().ToArray();

                        m_tableTabs[i].ROnSelectionChange(selectedIds);
                    }
                    else
                    {
                        m_tableTabs[i].ROnSelectionChange();
                    }
                }
            }

            Repaint();
        }

        private void OnHierarchyChange()
        {
            if (m_tableTabs != null)
            {
                for (int i = 0; i < m_tableTabs.Length; i++)
                {
                    m_tableTabs[i].ROnHierarchyChange();
                }
            }
        }

        private void OnGUI()
        {
            UpdateTabs();
            
            EditorGUIUtility.labelWidth = 130;
            
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField("配置名", GUILayout.Width(40));
                m_dateName = EditorGUILayout.TextField("", m_dateName, GUILayout.Width(100));
                
                if (GUILayout.Button("导出", GUILayout.Width(70)))
                {
                    EditorCoroutineUtility.StartCoroutine(ExportTodData(), this);
                }
                
                GUILayout.FlexibleSpace();
            }
            
            // tab 按钮
            EditorGUILayout.Space();
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
            EditorGUILayout.HelpBox(CurrentTabTip.text, MessageType.Info);
            
            // 列表
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                CurrentTab?.ROnGUI();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.Space();
        }
        
        private void UpdateTabs()
        {
            if (m_currentExplorerExtension == null)
            {
                // 先关再开
                m_currentExplorerExtension?.OnDisable();
                m_currentExplorerExtension = new DefaultTimeOfDayExplorerExtension();
                m_currentExplorerExtension.OnEnable();

                // 2D, 3D 模式下的灯
                m_selectedTab = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D ? /* 2D Lights */ 1 : /* Lights */ 0;

                if (m_currentExplorerExtension.GetContentTabs() == null || m_currentExplorerExtension.GetContentTabs().Length == 0)
                {
                    throw new ArgumentException("必须至少定义1个 Tab 选项卡");
                }

                m_tableTabs =  m_currentExplorerExtension.GetContentTabs();
                m_tabTitles = m_tableTabs?.Select(item => item.RGetTitle()).ToArray();
                m_tabTips = m_currentExplorerExtension.GetTabTips();
            }
        }

        private IEnumerator ExportTodData()
        {
            // 配置资源实例
            var assetInstance = ScriptableObject.CreateInstance<StoredTimeOfDayData>();
            assetInstance.creationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            assetInstance.sceneName = SceneManager.GetActiveScene().name;
            assetInstance.phaseName = m_dateName;

            // 保存文件对话框
            string fileAssetPath = EditorUtility.SaveFilePanelInProject(
                "导出 TOD 数据",
                $"tod_{assetInstance.sceneName.ToLower()}_{assetInstance.phaseName.ToLower()}",
                "asset",
                "请选择保存位置"
            );
            if (string.IsNullOrEmpty(fileAssetPath))
            {
                EditorUtility.DisplayDialog("流程中断", "文件路径不能为空", "确认");
                yield break;
            }
            
            AssetDatabase.CreateAsset(assetInstance, fileAssetPath); // 创建配置的资源文件

            // 开始收集数据
            var dataCollector = new StoredTimeOfDayDataCollector();
            yield return dataCollector.Execute(assetInstance);

            // 焦点配置资源
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = assetInstance;
            EditorGUIUtility.PingObject(assetInstance);
            
            string msg = $"TOD 数据已导出至: \n{fileAssetPath}";
            EditorUtility.DisplayDialog("TOD 数据导出完成", msg, "确认");
            Debug.Log(msg);
        }
        
    }
}
