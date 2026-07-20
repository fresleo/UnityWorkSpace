using System;
using System.Collections.Generic;
using System.IO;
using MaterialInspectorExtensionTool.Editor.PublicExtension;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class ToolbarArea : GUIBase
    {
        public override Rect Rect
        {
            get => new(0, 0, 1000, 20);
            set => base.Rect = value;
        }
        
        public int selectedIndex;

        private bool m_isToolbarEnabled = true;
        private bool m_addDirectoryToggleValue;
        
        private static FoldeField s_foldeField = new();
        private List<string> m_nameList;
        private List<string> m_pathList;
        
        private Rect m_toolbarRect;
        private Rect m_addDirectoryRect;
        
        private static Rect[] s_toolbarRects; // toolbar 单个区域
        private static Rect[] s_toolbarClosRects;
        
        public event Action IsListChange;
        public event Action IsListAdd;
        public event Action IsSelectChange;
        public event Action<int> IsListRemoveIndex;
        
        protected override void OnDispose()
        {
        }
        
        public ToolbarArea(List<string> names, List<string> paths, int index)
        {
            m_nameList = names;
            m_pathList = paths;
            selectedIndex = index;
        }

        public override void OnGUI(Rect position)
        {
            base.OnGUI(position);
            
            using (new GUILayout.AreaScope(position))
            {
                using (new GUILayout.HorizontalScope())
                {
                    var even = Event.current;
                    
                    EditorGUI.BeginChangeCheck();
                    {
                        GUI.enabled = m_isToolbarEnabled;
                        selectedIndex = GUILayout.Toolbar(selectedIndex, m_nameList.ToArray(), EditorStyles.toolbarButton,
                            GUILayout.ExpandWidth(false),
                            GUILayout.MaxWidth(100 * m_nameList.Count),
                            GUILayout.MinWidth(40 * m_nameList.Count));
                        if (even.type == EventType.Repaint)
                        {
                            m_toolbarRect = GUILayoutUtility.GetLastRect();
                        }

                        GUI.enabled = true;

                        // +号
                        m_addDirectoryToggleValue = GUILayout.Toggle(
                            m_addDirectoryToggleValue, EditorGUIUtility.IconContent("d_CreateAddNew"), EditorStyles.toolbarButton, GUILayout.Width(20));

                        if (s_foldeField.IsGetPath)
                        {
                            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                            {
                                m_addDirectoryToggleValue = false;
                                s_foldeField.IsGetPath = false;

                                var name = Path.GetFileName(s_foldeField.Path);
                                m_nameList.Add(name);

                                m_pathList.Add(s_foldeField.Path);

                                IsListAdd?.Invoke();
                                IsListChange?.Invoke();
                            }
                        }

                        if (m_addDirectoryToggleValue)
                        {
                            s_foldeField.OnGUI(m_addDirectoryRect);
                        }

                        var addRect = GUILayoutUtility.GetRect(20, 200, 20, 20, GUILayout.ExpandWidth(false));
                        if (even.type == EventType.Repaint)
                        {
                            m_addDirectoryRect = addRect;
                        }
                    }
                    if (EditorGUI.EndChangeCheck()) // 增加删除，或者切换 都刷新筛选
                    {
                        IsSelectChange?.Invoke();
                    }

                    // 获取删除文件夹按钮的位置
                    s_toolbarRects = m_toolbarRect.Split(m_nameList.Count, RectExtension.ESplitType.Horizontal);
                    s_toolbarClosRects = new Rect[s_toolbarRects.Length];
                    for (int i = 0; i < s_toolbarRects.Length; i++)
                    {
                        s_toolbarClosRects[i] = s_toolbarRects[i];
                        s_toolbarClosRects[i].x = s_toolbarRects[i].xMax - 20;
                        s_toolbarClosRects[i].height = 20;
                        s_toolbarClosRects[i].width = 20;
                    }

                    // 双击出现
                    if ((m_toolbarRect.Contains(even.mousePosition) && even.clickCount == 2) || !m_isToolbarEnabled)
                    {
                        for (int i = 0; i < m_nameList.Count; i++)
                        {
                            using (new GUILayout.AreaScope(s_toolbarRects[i]))
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    EditorGUI.BeginChangeCheck();
                                    m_nameList[i] = EditorGUILayout.DelayedTextField(m_nameList[i]);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        IsListChange?.Invoke();
                                    }

                                    m_isToolbarEnabled = false;
                                    // 删除文件夹按钮
                                    if (GUILayout.Button("", "WinBtnClose"))
                                    {
                                        IsListRemoveIndex?.Invoke(i);
                                        IsListChange?.Invoke();
                                    }
                                }
                            }
                        }
                    }

                    if (!m_toolbarRect.Contains(even.mousePosition) && even.clickCount == 1)
                    {
                        m_isToolbarEnabled = true;
                    }
                }
            }
        }
        
    }
}