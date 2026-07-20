// Created By: WangYu  Date: 2025-04-10

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.Lightmap;
using XKT.TOD.Utils;

namespace XKT.TOD.Tag
{
    /// <summary>
    /// Tag 追踪器
    /// </summary>
    [InitializeOnLoad]
    public class TagTracker
    {
        static TagTracker()
        {
            s_lastTimer = 0;
            
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        }
        
        private const float c_updateInterval = 1f;
        private static float s_lastTimer;
        
        private static void OnUpdate()
        {
            if (Application.isPlaying) return;
            
            if (Time.realtimeSinceStartup - s_lastTimer < c_updateInterval)
            {
                return;
            }
            s_lastTimer = Time.realtimeSinceStartup;
            
            // 更新 LightmapVolume 范围内所有 Renderer 上的 LightmapTag 脚本
            LightmapVolume.UpdateTagScripts();
        }
        
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            s_lastTimer = 0;
        }
        
        private static List<string> s_cacheHierarchyPaths = new();
        private static HashSet<int> s_duplicatedObjectIds = new();
        private static HashSet<int> s_duplicatedParentIds = new();

        private static bool SkipCheck()
        {
            var todMgr = TODUtils.FindObjectsOfTypeInActiveScene<TimeOfDayManager>();
            return todMgr.Count > 0;
        }
        
        private static void OnHierarchyChanged()
        {
            if (Application.isPlaying) return;
            
            s_cacheHierarchyPaths.Clear();
            var atts = TODUtils.FindObjectsOfTypeInActiveScene<AbsTodTag>();
            foreach (var att in atts)
            {
                att.UpdateHierarchyPath(); // 为 tag 组件生成 HierarchyPath
                s_cacheHierarchyPaths.Add(att.hierarchyPath); // 缓存生成的 HierarchyPath
            }
            
            s_duplicatedObjectIds.Clear();
            s_duplicatedParentIds.Clear();
            
            if(!m_displayDuplicatedHierarchyPath) return;
            if (SkipCheck()) return;
            
            foreach (var att in atts)
            {
                var results = s_cacheHierarchyPaths.FindAll(item => item == att.hierarchyPath);
                if (results.Count > 1)
                {
                    // 挂了 tag 节点的 iid
                    int selfId = att.gameObject.GetInstanceID();
                    s_duplicatedObjectIds.Add(selfId);

                    // 所有父节点的 iid
                    Transform currentT = att.transform.parent;
                    while (currentT != null)
                    {
                        int parentId = currentT.gameObject.GetInstanceID();
                        s_duplicatedParentIds.Add(parentId);
                        
                        currentT = currentT.parent;
                    }
                }
            }
        }
        
        private static Color s_duplicatedHierarchyPathColor = new(1, 0, 0, 0.3f);
        private static Color s_duplicatedHierarchyPathParentColor = new(1, 0.5f, 0, 0.2f);

        private static void OnHierarchyWindowItemOnGUI(int instanceId, Rect selectionRect)
        {
            if (Application.isPlaying) return;
            if (!m_displayDuplicatedHierarchyPath) return;
            if (SkipCheck()) return;
            
            GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null) return;

            // 创建背景矩形
            Rect backgroundRect = selectionRect;
            backgroundRect.x = 0;
            backgroundRect.width = selectionRect.width + selectionRect.x;
            
            int iid = go.GetInstanceID();
            if (s_duplicatedObjectIds.Contains(iid))
            {
                EditorGUI.DrawRect(backgroundRect, s_duplicatedHierarchyPathColor);
                DrawDuplicatedIcon(selectionRect);
            }
            else if (s_duplicatedParentIds.Contains(iid))
            {
                EditorGUI.DrawRect(backgroundRect, s_duplicatedHierarchyPathParentColor);
            }
        }

        /// <summary>
        /// 在对象名称前添加图标
        /// </summary>
        /// <param name="selectionRect">OnHierarchyWindowItemOnGUI 给的选区矩形</param>
        private static void DrawDuplicatedIcon(Rect selectionRect)
        {
            Rect iconRect = selectionRect;
            iconRect.width = 16;
            iconRect.x = selectionRect.x - 16;
            GUI.Label(iconRect, EditorGUIUtility.IconContent("d_FilterByType"));
        }

        
        private const string c_key = "EditorPrefs_TagTracker_DisplayDuplicatedHierarchyPath";
        private const string c_menuItemName = "Window/TA工具集/TOD/高亮 Hierarchy Path 重复的 Tag 对象";

        private static bool m_displayDuplicatedHierarchyPath;
        
        [InitializeOnLoadMethod]
        private static void RefreshMenu()
        {
            EditorApplication.delayCall += () =>
            {
                int saveValue = EditorPrefs.GetInt(c_key);
                m_displayDuplicatedHierarchyPath = saveValue > 0;
                Menu.SetChecked(c_menuItemName, m_displayDuplicatedHierarchyPath);
            };
        }
        
        [MenuItem(c_menuItemName)]
        private static void SwitchDisplayDuplicatedHierarchyPath()
        {
            m_displayDuplicatedHierarchyPath = !m_displayDuplicatedHierarchyPath;
            Menu.SetChecked(c_menuItemName, m_displayDuplicatedHierarchyPath);
            EditorPrefs.SetInt(c_key, m_displayDuplicatedHierarchyPath ? 1 : 0);
        }
        
    }
}