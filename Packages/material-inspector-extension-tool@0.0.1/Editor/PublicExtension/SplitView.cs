using System;
using UnityEditor;
using UnityEngine;
using static MaterialInspectorExtensionTool.Editor.PublicExtension.RectExtension;

namespace MaterialInspectorExtensionTool.Editor.PublicExtension
{
    /// <summary>
    /// 分屏视图
    /// </summary>
    public class SplitView : GUIBase
    {
        // 声明调整大小的回调事件
        public event Action OnBeginResize, OnEndResize;
        public event Action<Rect> DrawDragAndDropRect;
        public event Action<Rect> FirstArea, SecondArea;
        
        public static float SplitSize;
        public float minSplitWidth = 100;
        public float padding = 8;
        
        private bool m_resizing;

        public bool Dragging
        {
            get => m_resizing;
            set
            {
                if (m_resizing != value)
                {
                    m_resizing = value;
                    
                    if (value)
                    {
                        if (OnBeginResize != null)
                        {
                            OnBeginResize();
                        }
                    }
                    else
                    {
                        if (OnEndResize != null)
                        {
                            OnEndResize();
                        }
                    }
                }
            }
        }

        private EAutoFillRect m_autoFillRect;
        private ESplitType m_splitType;

        /// <summary>
        /// 传入分割的类型是垂直分割还是水平分割，
        /// </summary>
        public SplitView(ESplitType splitType, EAutoFillRect autoFillRect = EAutoFillRect.FirstRect)
        {
            m_splitType = splitType;
            m_autoFillRect = autoFillRect;
        }

        protected override void OnDispose()
        {
            FirstArea = null;
            SecondArea = null;
            OnBeginResize = null;
            OnEndResize = null;
        }
        
        public override void OnGUI(Rect position)
        {
            // 绘制切割出来的2个区域
            Rect[] rects = position.Split(m_splitType, SplitSize, padding, true, m_autoFillRect);
            if (FirstArea != null)
            {
                FirstArea(rects[0]);
            }
            if (SecondArea != null)
            {
                SecondArea(rects[1]);
            }

            var mid = rects.GetMidTowRect(m_splitType);
            if (DrawDragAndDropRect != null)
            {
                DrawDragAndDropRect(mid);
            }

            Event even = Event.current;

            if (mid.Contains(even.mousePosition))
            {
                if (m_splitType == ESplitType.Vertical)
                {
                    EditorGUIUtility.AddCursorRect(mid, MouseCursor.ResizeVertical);
                }
                else
                {
                    EditorGUIUtility.AddCursorRect(mid, MouseCursor.ResizeHorizontal);
                }
            }

            switch (even.type)
            {
                case EventType.MouseDown:
                {
                    if (mid.Contains(even.mousePosition))
                    {
                        Dragging = true;
                    }
                }
                    break;

                case EventType.MouseDrag:
                {
                    if (Dragging)
                    {
                        if (m_splitType == ESplitType.Vertical)
                        {
                            SplitSize = m_autoFillRect == EAutoFillRect.FirstRect ? SplitSize + even.delta.y : SplitSize - even.delta.y;
                            // SplitSize += e.delta.y;
                            // 限制窗口最小
                            SplitSize = Mathf.Clamp(SplitSize, 0 + minSplitWidth, position.height - minSplitWidth);
                        }
                        else
                        {
                            SplitSize = m_autoFillRect == EAutoFillRect.FirstRect ? SplitSize + even.delta.x : SplitSize - even.delta.x;
                            // SplitSize += e.delta.x;
                            // 限制窗口最小
                            SplitSize = Mathf.Clamp(SplitSize, rects[0].xMin + minSplitWidth, rects[1].xMax - minSplitWidth);
                        }
                        
                        even.Use();
                    }
                }
                    break;

                case EventType.MouseUp:
                {
                    if (Dragging)
                    {
                        Dragging = false;
                    }
                }
                    break;
            }
        }
        
    }
}