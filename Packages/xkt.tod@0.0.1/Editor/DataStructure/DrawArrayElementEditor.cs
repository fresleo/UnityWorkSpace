// Created by: WangYu   Date: 2025-11-03

using UnityEditor;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    /// <summary>
    /// 自定义数组元素绘制器
    /// </summary>
    public interface IArrayElementDrawer
    {
        /// <summary>
        /// 绘制元素
        /// </summary>
        /// <param name="property">元素的 SerializedProperty</param>
        /// <param name="index">元素索引</param>
        void DrawElement(SerializedProperty property, int index);
    }
    
    /// <summary>
    /// 绘制数组元素
    /// </summary>
    public class DrawArrayElementEditor : ISettingsEditor
    {
        private GUIContent m_header;
        private IArrayElementDrawer m_elementDrawer;
        
        public DrawArrayElementEditor(SerializedProperty target, GUIContent header, IArrayElementDrawer elementDrawer = null)
        {
            Target = target;
            m_header = header;
            m_elementDrawer = elementDrawer;
        }
        
        private bool m_showSizeControl = true;
        private bool m_showFoldout = true;
        private bool m_foldoutState;
        
        /// <summary>
        /// 是否显示数组大小控制（添加/删除 按钮）
        /// </summary>
        public bool ShowSizeControl
        {
            get => m_showSizeControl;
            set => m_showSizeControl = value;
        }
        
        /// <summary>
        /// 是否显示折叠控制
        /// </summary>
        public bool ShowFoldout
        {
            get => m_showFoldout;
            set => m_showFoldout = value;
        }
        
        /// <summary>
        /// 折叠状态
        /// </summary>
        public bool FoldoutState
        {
            get => m_foldoutState;
            set => m_foldoutState = value;
        }
        
        private int m_currentArraySize;
        private int m_newArraySize;
        
        public SerializedProperty Target { get; }
        
        public void Enable()
        {
            if (Target == null) return;
            if (!Target.isArray) return;
            
            m_currentArraySize = Target.arraySize; // 初始化尺寸输入值
        }

        public void InspectorGUI()
        {
            if (Target == null) return;
            if (!Target.isArray) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 头部区域（标题 + 大小控制）
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 折叠控制
                    if (m_showFoldout)
                    {
                        m_foldoutState = EditorGUILayout.Foldout(m_foldoutState, m_header, true);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(m_header);
                    }

                    // 数组大小显示和控制
                    if (m_showSizeControl)
                    {
                        GUILayout.FlexibleSpace();
                        
                        EditorGUILayout.LabelField("Size:", GUILayout.Width(35));
                        
                        // 同步输入框的值与数组实际大小
                        m_currentArraySize = Target.arraySize;
                        EditorGUI.BeginChangeCheck();
                        int newArraySize = EditorGUILayout.IntField(m_currentArraySize, GUILayout.Width(50));
                        if (EditorGUI.EndChangeCheck())
                        {
                            // 确保输入值不为负数
                            m_newArraySize = Mathf.Max(newArraySize, 0);
                        }
                        
                        if (GUILayout.Button("修改", GUILayout.Width(50)))
                        {
                            Target.arraySize = m_newArraySize;
                            Target.serializedObject.ApplyModifiedProperties();
                        }

                        if (GUILayout.Button("清空", GUILayout.Width(50)))
                        {
                            if (EditorUtility.DisplayDialog("确认", $"确定要清空数组 {m_header.text} 吗？", "确定", "取消"))
                            {
                                Target.arraySize = 0;
                                m_currentArraySize = 0;
                                Target.serializedObject.ApplyModifiedProperties();
                            }
                        }
                    }
                }

                // 折叠内容
                if (!m_showFoldout || m_foldoutState)
                {
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        EditorGUILayout.Space(2);

                        // 绘制数组元素
                        for (int i = 0; i < Target.arraySize; i++)
                        {
                            DrawArrayElement(i);
                        }
                        
                        // 如果数组为空，显示提示
                        if (Target.arraySize == 0)
                        {
                            EditorGUILayout.HelpBox("数组为空，请添加数据", MessageType.Info);
                        }
                    }
                }
            }
        }
        
        // 绘制数组元素
        private void DrawArrayElement(int index)
        {
            SerializedProperty element = Target.GetArrayElementAtIndex(index);
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 删除按钮
                if (m_showSizeControl && GUILayout.Button("×", GUILayout.Width(20)))
                {
                    Target.DeleteArrayElementAtIndex(index);
                    m_currentArraySize = Target.arraySize;
                    Target.serializedObject.ApplyModifiedProperties();
                    return; // 删除后直接返回，避免访问已删除的元素
                }
                
                EditorGUILayout.Space(2);
                
                DrawElementContent(element, index); // 绘制元素内容
            }
            
            EditorGUILayout.Space(2);
        }
        
        // 绘制元素内容
        private void DrawElementContent(SerializedProperty property, int index)
        {
            // 优先使用接口绘制器
            if (m_elementDrawer != null)
            {
                m_elementDrawer.DrawElement(property, index);
                return;
            }
            
            DefaultDrawElement(property); // 使用默认的绘制方式
        }
        
        // 默认绘制元素（递归显示所有子属性）
        private void DefaultDrawElement(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Generic)
            {
                // 对于复杂对象，递归绘制所有子属性
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();
                bool enterChildren = true;
                
                while (iterator.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        break;
                    }
                    
                    enterChildren = false;
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
            else
            {
                // 简单类型直接绘制
                EditorGUILayout.PropertyField(property, true);
            }
        }
        
    }
}