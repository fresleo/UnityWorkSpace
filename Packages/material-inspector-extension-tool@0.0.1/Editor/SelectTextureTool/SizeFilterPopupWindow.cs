using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    /// <summary>
    /// 提供1个按照索引选取，和条件选取的方法，单选，多选与或条件
    /// </summary>
    public class SizeFilterPopupWindow<T> : PopupWindowContent
    {
        public event Action IsToggleChange;

        private float m_rectX;

        public static bool IsAllIsFalse
        {
            set
            {
                if (value)
                {
                    PropertySelect = PropertySelect.ToDictionary(k => k.Key, v => false);
                }
            }
            get => !PropertySelect.ContainsValue(true);
        }

        /// <summary>
        /// 属性列表
        /// </summary>
        public List<T> propertys = new();

        private static Dictionary<T, bool> s_propertySelect = new();

        public static Dictionary<T, bool> PropertySelect
        {
            get => s_propertySelect;
            private set => s_propertySelect = value;
        }

        private T m_tempValue;
        /// <summary>
        /// 多选标记
        /// </summary>
        public bool multipleSelect;

        public override void OnGUI(Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            {
                using (new GUILayout.VerticalScope())
                {
                    for (int i = 0; i < propertys.Count; i++)
                    {
                        PropertySelect[propertys[i]] = GUILayout.Toggle(PropertySelect[propertys[i]], propertys[i].ToString());
                        if (PropertySelect[propertys[i]] && multipleSelect)
                        {
                            if (!propertys[i].Equals(m_tempValue) && m_tempValue != null)
                            {
                                PropertySelect[m_tempValue] = false;
                            }

                            m_tempValue = propertys[i];
                        }
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                IsToggleChange?.Invoke();
            }
        }

        /// <summary>
        /// toggle 组弹出窗口构造函数
        /// </summary>
        /// <param name="t">显示的内容列表</param>
        /// <param name="windowWidth">窗口的宽度</param>
        /// <param name="multipleSelect">是否支持多选</param>
        public SizeFilterPopupWindow(List<T> t, float windowWidth, bool multipleSelect = false)
        { 
            propertys = t;
            m_rectX = windowWidth;
            this.multipleSelect = multipleSelect;
            
            for (int i = 0; i < t.Count; i++)
            {
                var item = t[i];
                
                // 不包含才会加进字典
                if (!PropertySelect.ContainsKey(item))
                {
                    PropertySelect.Add(item, false);
                }
            }

            // list 不包含 key 时将 value 设为 false
            PropertySelect = PropertySelect.ToDictionary(k => k.Key, v => t.Contains(v.Key) ? v.Value : false);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(m_rectX, propertys.Count * 17 + 5);
        }
        
        /// <summary>
        /// 设置 toggle 组的选项
        /// </summary>
        public void SetPropertySelect(List<T> keys, bool value)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                PropertySelect = PropertySelect.ToDictionary(k => k.Key, v => v.Key.Equals(keys[i]) ? value : v.Value);
            }

            IsToggleChange?.Invoke();
        }

        public void SetPropertySelect(Func<List<T>, List<T>> func, bool value)
        {
            var keys = func(propertys);
            SetPropertySelect(keys, value);
        }
    }
}