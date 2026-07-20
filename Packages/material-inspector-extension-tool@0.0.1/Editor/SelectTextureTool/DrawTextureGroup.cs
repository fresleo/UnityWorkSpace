using System;
using System.Collections.Generic;
using MaterialInspectorExtensionTool.Editor.PublicExtension;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class DrawTextureGroup
    {
        public int selectIndex;
        
        public GetTextureList getTextureList;
        public List<TextureBox> nowTextureBoxs;
        /// <summary>
        /// 已加载标记
        /// </summary>
        public bool isLoaded;
        
        private string m_path;
        
        private Vector2 m_scrollViewValue = Vector2.zero;

        private int m_start, m_end;
        private int m_cunstomWidth = 10, m_cunstomHeight = 10;
        private int m_darwHeight;
        
        private Rect m_mouseRect, m_selectRect;
        private bool m_isSelectionChange;
        private Rect m_lastRect;
        
        public static event Action<TextureBox> IsTextureChange;
        
        
        public DrawTextureGroup(string path)
        {
            m_path = path;
            getTextureList = new GetTextureList();
            nowTextureBoxs = getTextureList.textureBoxs;
            
            SelectTextureWindow.skin.customStyles[0].fixedWidth = SelectTextureWindow.s_windowData.textureSize;
            SelectTextureWindow.skin.customStyles[0].fixedHeight = SelectTextureWindow.s_windowData.textureSize;
        }

        /// <summary>
        /// 加载
        /// </summary>
        public void Load()
        {
            if (getTextureList.TextureArrayLength != this.nowTextureBoxs.Count || getTextureList.TextureArrayLength == 0)
            {
                EditorCoroutineRunner.StartEditorCoroutine(getTextureList.GetAssetTextureInPath(m_path));
            }
            
            isLoaded = true;
        }
        
        /// <summary>
        /// 绘制
        /// </summary>
        public void Draw(Rect texturesRect)
        {
            var even = Event.current;
            if (SelectTextureWindow.s_windowData.windowBackgroundTexture != null)
            {
                GUI.DrawTexture(texturesRect, SelectTextureWindow.s_windowData.windowBackgroundTexture, ScaleMode.ScaleAndCrop); // 背景图
            }

            EditorGUI.DrawRect(texturesRect, SelectTextureWindow.s_windowData.windowBackgroundColor);

            // 没筛选时用最开始获得的长度，筛选时用实时长度， 这样刚打开滚动条不会 随着图片加载缩短
            var nowLength = nowTextureBoxs.Count == getTextureList.TextureArrayLength ? getTextureList.TextureArrayLength : nowTextureBoxs.Count;
            if (nowLength == 0)
            {
                return;
            }

            // 真实绘制行数
            m_darwHeight = Mathf.CeilToInt((float)nowLength / m_cunstomWidth);
            // 1行多少个 -1 可以让左右的滑动条不出现，不会出现多渲染的情况  取整有可能是0
            m_cunstomWidth = Mathf.FloorToInt((texturesRect.width - 15) / SelectTextureWindow.s_windowData.textureSize);

            SelectTextureWindow.skin.customStyles[0].margin.left = (int)(((texturesRect.width - 15) % SelectTextureWindow.s_windowData.textureSize) / m_cunstomWidth);

            m_cunstomHeight = Mathf.CeilToInt(texturesRect.height / SelectTextureWindow.s_windowData.textureSize) > nowLength / m_cunstomWidth
                ? Mathf.CeilToInt((float)nowLength / m_cunstomWidth) + 1
                : Mathf.CeilToInt(texturesRect.height / SelectTextureWindow.s_windowData.textureSize) + 1; // 多渲染一行，不能铺满屏幕时，按少的来

            var bigRect = new Rect(texturesRect.x, texturesRect.y, texturesRect.width - 20, (m_darwHeight + 1) * SelectTextureWindow.s_windowData.textureSize);
            m_scrollViewValue = GUI.BeginScrollView(texturesRect, m_scrollViewValue, bigRect);
            {
                GUILayout.Space((int)(m_scrollViewValue.y / SelectTextureWindow.s_windowData.textureSize) * SelectTextureWindow.s_windowData.textureSize);

                m_start = Mathf.FloorToInt(m_scrollViewValue.y / SelectTextureWindow.s_windowData.textureSize) * m_cunstomWidth; // 开始渲染索引，确保是整数 * 每行个数
                m_end = m_start + m_cunstomWidth * m_cunstomHeight;
                m_end = Mathf.Clamp(m_end, 0, nowLength); // 为什么限制

                using (new GUILayout.VerticalScope())
                {
                    for (int i = m_start; i < m_end; i += 0)
                    {
                        if (i >= nowLength && Event.current.type != EventType.Layout)
                        {
                            break;
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            for (int j = 0; j < m_cunstomWidth; j++) //渲染横向图片
                            {
                                if (i >= nowLength)
                                {
                                    if (i >= nowLength && Event.current.type != EventType.Layout)
                                    {
                                        break;
                                    }

                                    break;
                                }

                                m_isSelectionChange = getTextureList.textureBoxs[i].isSelect;
                                var textureRect = GUILayoutUtility.GetRect(
                                    SelectTextureWindow.s_windowData.textureSize,
                                    SelectTextureWindow.s_windowData.textureSize,
                                    SelectTextureWindow.skin.customStyles[0]);
                                if (textureRect.Contains(even.mousePosition))
                                {
                                    m_mouseRect = textureRect;
                                }

                                getTextureList.textureBoxs[i].isSelect = GUI.Toggle(
                                    textureRect, getTextureList.textureBoxs[i].isSelect, nowTextureBoxs[i].t2d, SelectTextureWindow.skin.customStyles[0]);

                                // 选中状态位置
                                if (getTextureList.textureBoxs[i].isSelect)
                                {
                                    m_selectRect = m_lastRect;
                                }

                                if (EditorWindow.focusedWindow != null && even.alt && EditorWindow.focusedWindow.ToString().Contains("SelectTextureWindow"))
                                {
                                    m_lastRect.x += 2;
                                    m_lastRect.y = m_lastRect.y + m_lastRect.height - 17;
                                    m_lastRect.width = 20;
                                    m_lastRect.height = 15;
                                    var sizeRect = new Rect(m_lastRect);
                                    sizeRect.x += 22;
                                    sizeRect.width = 36;

                                    if (nowTextureBoxs[i].t2d.wrapMode.ToString() == "Clamp")
                                    {
                                        GUI.Label(m_lastRect, "C", "AssetLabel Partial");
                                    }
                                    else
                                    {
                                        GUI.Label(m_lastRect, "R", "AssetLabel Partial");
                                    }

                                    // 贴图 MaxSize
                                    string sizeStr = (nowTextureBoxs[i].t2d.width > nowTextureBoxs[i].t2d.height
                                        ? nowTextureBoxs[i].t2d.width
                                        : nowTextureBoxs[i].t2d.height).ToString();
                                    GUI.Label(sizeRect, sizeStr, "AssetLabel Partial");
                                }

                                if (getTextureList.textureBoxs[i].isSelect != m_isSelectionChange) // 单选
                                {
                                    IsTextureChange?.Invoke(nowTextureBoxs[i]);

                                    getTextureList.textureBoxs[selectIndex].isSelect = false;
                                    selectIndex = i;
                                    getTextureList.textureBoxs[i].isSelect = true;
                                }

                                i++;
                            }
                        }
                    }
                }

                // 鼠标选择贴图时的选中框
                if (texturesRect.Contains(even.mousePosition - m_scrollViewValue))
                {
                    if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.ToString().Contains(nameof(SelectTextureWindow)))
                    {
                        DrawLine(m_mouseRect, 4f, SelectTextureWindow.s_windowData.selectColor);
                    }
                }
                // 合成时显示的选中框
                DrawLine(m_selectRect, 6, SelectTextureWindow.s_windowData.selectColor);

                SelectTextureWindow.skin.customStyles[0].fixedWidth = SelectTextureWindow.s_windowData.textureSize;
                SelectTextureWindow.skin.customStyles[0].fixedHeight = SelectTextureWindow.s_windowData.textureSize;
            }
            GUI.EndScrollView();
        }
        
        /// <summary>
        /// 画线框
        /// </summary>
        public static void DrawLine(Rect rect, float width, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(width, new Vector3(rect.x, rect.y), new Vector3(rect.xMax, rect.y));
            Handles.DrawAAPolyLine(width, new Vector3(rect.xMax, rect.y), new Vector3(rect.xMax, rect.yMax));
            Handles.DrawAAPolyLine(width, new Vector3(rect.xMax, rect.yMax), new Vector3(rect.x, rect.yMax));
            Handles.DrawAAPolyLine(width, new Vector3(rect.x, rect.yMax), new Vector3(rect.x, rect.y));
            Handles.EndGUI();
        }
        
    }
}