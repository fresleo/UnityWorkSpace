using System;
using System.Collections.Generic;
using System.Diagnostics;
using MaterialInspectorExtensionTool.Editor.PublicExtension;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class SearchArea : GUIBase
    {
        public event Action<List<string>> SearchHistoryIsChange;
        public event Action<string> SearchTextIsChange;
        
        public static string SearchString;
        
        public List<string> SearchHistory = new();

        //public static bool IsTextFieldChange;
        private Stopwatch m_sw = new(); //搜索历史的计时器
        private Rect m_rect = new(0, 0, 600, 20);

        public override Rect Rect
        {
            get => m_rect;
            set => m_rect = value;
        }

        private Rect SeachRect => new(this.mPosition.x + 5, this.mPosition.x + 2, 200, 20);

        private Rect SeachCancelRect => new(SeachRect.xMax + 5, SeachRect.y, 20, 20);
        
        private Rect SeachStringsRect => new(SeachCancelRect.x + SeachCancelRect.width, SeachCancelRect.y, 350, 20);
        
        
        protected override void OnDispose()
        {
        }
        
        /// <summary>
        /// 绘制搜索框
        /// </summary>
        public override void OnGUI(Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            {
                SearchString = EditorGUI.TextField(SeachRect, SearchString, new GUIStyle("ToolbarSearchTextField"));
                if (!string.IsNullOrEmpty(SearchString))
                {
                    if (GUI.Button(SeachCancelRect, "", "ToolbarSearchCancelButton"))
                    {
                        SearchString = "";
                        SearchTextIsChange?.Invoke(SearchString);
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                SearchTextIsChange?.Invoke(SearchString);
                SelectTextureWindow.RefreshFilter();

                if (!string.IsNullOrEmpty(SearchString))
                {
                    m_sw.Start();
                }
                else
                {
                    m_sw.Stop();
                    m_sw.Reset();
                }
            }

            //搜索历史
            if (m_sw.Elapsed >= TimeSpan.FromSeconds(2.0) && !string.IsNullOrEmpty(SearchString))
            {
                AddSeachList(SearchString);
                SearchHistoryIsChange?.Invoke(SearchHistory);
                m_sw.Stop();
                m_sw.Reset();
            }

            using (new GUILayout.AreaScope(SeachStringsRect))
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (int i = 0; i < SearchHistory.Count; i++)
                    {
                        if (GUILayout.Button(SearchHistory[i], "sv_label_" + i % 7, GUILayout.MaxWidth(40)))
                        {
                            SearchString = SearchHistory[i];
                            SearchTextIsChange?.Invoke(SearchString);
                            //   IsTextFieldChange = true;
                            SelectTextureWindow.RefreshFilter();
                        }

                        if (GUILayout.Button("", "ToolbarSearchCancelButton", GUILayout.Width(10)))
                        {
                            SearchHistory.RemoveAt(i);
                            SearchHistoryIsChange?.Invoke(SearchHistory);
                        }
                    }
                }
            }
        }

        private void AddSeachList(string SeachStrin)
        {
            if (SearchHistory.Count >= 5) //数量限制
            {
                SearchHistory.RemoveAt(0);
            }

            if (SearchHistory.Contains(SeachStrin)) //有没有一样的
            {
                SearchHistory.RemoveAt(SearchHistory.IndexOf(SeachStrin)); //移除之前一样的
                SearchHistory.Add(SeachStrin); //再加进去 （移到最后
            }
            else
            {
                SearchHistory.Add(SeachStrin);
            }
        }
        
    }
}