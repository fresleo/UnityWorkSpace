// Created By: WangYu  Date: 2025-04-28

using AirSticker.Runtime.Logic;
using UnityEditor;
using UnityEngine;

namespace AirSticker
{
    public abstract class AbsDecalConfigGUI
    {
        protected AbsDecalConfig m_config;
        protected SerializedObject m_configSo;
        
        private SerializedProperty m_boxWidth, m_boxHeight, m_boxDepth;
        private SerializedProperty m_material;
        private SerializedProperty m_projectionBackside;
        private SerializedProperty m_duration;
        
        public AbsDecalConfigGUI(AbsDecalConfig config)
        {
            m_config = config;
            m_configSo = new SerializedObject(m_config);
            
            m_boxWidth = m_configSo.FindProperty(nameof(AbsDecalConfig.boxWidth));
            m_boxHeight = m_configSo.FindProperty(nameof(AbsDecalConfig.boxHeight));
            m_boxDepth = m_configSo.FindProperty(nameof(AbsDecalConfig.boxDepth));
            
            m_material = m_configSo.FindProperty(nameof(AbsDecalConfig.material));
            m_projectionBackside = m_configSo.FindProperty(nameof(AbsDecalConfig.projectionBackside));
            m_duration = m_configSo.FindProperty(nameof(AbsDecalConfig.duration));
        }

        public virtual void OnInspectorGUI()
        {
            m_configSo.Update();
            EditorGUI.BeginChangeCheck();
            
            EditorGUIUtility.labelWidth = 160;
            EditorGUIUtility.fieldWidth = 120;
            
            EditorGUILayout.HelpBox("空气贴纸系统\n静态网格贴花配置", MessageType.Info);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("尺寸", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(m_boxWidth, new GUIContent("box 宽"));
                EditorGUILayout.PropertyField(m_boxHeight, new GUIContent("box 高"));
                EditorGUILayout.PropertyField(m_boxDepth, new GUIContent("box 深"));
                EditorGUILayout.HelpBox("注意：深度不宜太小，太小可能会导致贴花画不出来或在表面上被截断的情况。", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("通用功能", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                m_material.objectReferenceValue = EditorGUILayout.ObjectField("材质", m_material.objectReferenceValue, typeof(Material), false);
                
                EditorGUILayout.PropertyField(m_projectionBackside, new GUIContent("是否可以投射到背面"));
                EditorGUILayout.PropertyField(m_duration, new GUIContent("持续时间"));
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                m_configSo.ApplyModifiedProperties();
            }
        }
        
    }
}