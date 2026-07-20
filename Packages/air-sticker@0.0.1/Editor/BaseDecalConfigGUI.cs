// Created By: WangYu  Date: 2025-04-28

using AirSticker.Runtime.Render;
using UnityEditor;
using UnityEngine;

namespace AirSticker
{
    public class BaseDecalConfigGUI : AbsDecalConfigGUI
    {
        private SerializedProperty m_fadeinTime;
        private SerializedProperty m_fadeinCurve;

        private SerializedProperty m_fadeoutTime;
        private SerializedProperty m_fadeoutCurve;
        
        public BaseDecalConfigGUI(BaseDecalConfig config) : base(config)
        {
            m_fadeinTime = m_configSo.FindProperty(nameof(BaseDecalConfig.fadeinTime));
            m_fadeinCurve = m_configSo.FindProperty(nameof(BaseDecalConfig.fadeinCurve));
            
            m_fadeoutTime = m_configSo.FindProperty(nameof(BaseDecalConfig.fadeoutTime));
            m_fadeoutCurve = m_configSo.FindProperty(nameof(BaseDecalConfig.fadeoutCurve));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            m_configSo.Update();
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("渲染器专用功能", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.LabelField("淡入");
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(m_fadeinTime, new GUIContent("时间"));
                    EditorGUILayout.PropertyField(m_fadeinCurve, new GUIContent("速度曲线"));
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("淡出");
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(m_fadeoutTime, new GUIContent("时间"));
                    EditorGUILayout.PropertyField(m_fadeoutCurve, new GUIContent("速度曲线"));
                }
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                m_configSo.ApplyModifiedProperties();
            }
        }
        
    }
}