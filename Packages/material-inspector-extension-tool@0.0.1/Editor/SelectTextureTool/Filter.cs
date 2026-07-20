using System;
using System.Collections.Generic;
using MaterialInspectorExtensionTool.Editor.PublicExtension;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class Filter<T> : GUIBase
    {
        public SizeFilterPopupWindow<T> myPopupWindowContent;
        public List<T> toggleTepyList = new();
        
        private Rect m_rect = new(0, 0, 80, 20);
        private string m_label, m_tempLabel;
        
        public override Rect Rect
        {
            get => m_rect;
            set => m_rect = value;
        }

        public event Action IsButtonClick;
        public event Action IsToggleChange;

        protected override void OnDispose()
        {
            myPopupWindowContent.IsToggleChange -= ChangeLabel;
        }
        
        public Filter(string label, List<T> toggleTepyList)
        {
            this.m_label = label;
            this.toggleTepyList = toggleTepyList;
            m_tempLabel = label;
        }

        public Filter()
        {
        }
        
        public override void OnGUI(Rect position)
        {
            base.OnGUI(position);

            if (GUI.Button(position, m_tempLabel, "ToolbarDropDownToggle"))
            {
                myPopupWindowContent = new SizeFilterPopupWindow<T>(toggleTepyList, position.width);
                IsButtonClick?.Invoke();
                myPopupWindowContent.IsToggleChange -= ChangeLabel;
                myPopupWindowContent.IsToggleChange += ChangeLabel;
                myPopupWindowContent.IsToggleChange += SendEvent;
                PopupWindow.Show(position, myPopupWindowContent);
            }
        }

        public string Label
        {
            set
            {
                m_label = value;
                m_tempLabel = m_label;
            }
        }
        
        private void ChangeLabel()
        {
            int i = 0;
            foreach (var iter in SizeFilterPopupWindow<T>.PropertySelect)
            {
                if (iter.Value)
                {
                    m_tempLabel = iter.Key.ToString();
                    i++;
                }

                if (i == 2)
                {
                    m_tempLabel += "...";
                    return;
                }
            }

            if (i == 0)
            {
                m_tempLabel = m_label;
            }
        }
        
        private void SendEvent()
        {
            IsToggleChange?.Invoke();
        }
        
    }
}