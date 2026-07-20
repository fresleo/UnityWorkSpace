// Created by: WangYu   Date: 2025-11-03

using System;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    public class UnityFogSettingsEditor : ISettingsEditor
    {
        internal class Styles
        {
            public static GUIContent header = new("Unity 的雾设置");
            
            public static GUIContent enabled = new("启用");
            public static GUIContent fogColor = new("雾的颜色");
            public static GUIContent fogDensity = new("雾的密度");
        }
        
        private SerializedProperty m_enabled;
        private SerializedProperty m_fogColor;
        private SerializedProperty m_fogDensity;

        public UnityFogSettingsEditor(SerializedProperty target)
        {
            Target = target;
        }

        public SerializedProperty Target { get; }

        public void Enable()
        {
            if(Target == null) return;
            
            m_enabled = Target.FindPropertyRelative(nameof(UnityFogSettings.enabled));
            m_fogColor = Target.FindPropertyRelative(nameof(UnityFogSettings.fogColor));
            m_fogDensity = Target.FindPropertyRelative(nameof(UnityFogSettings.fogDensity));
        }

        public void InspectorGUI()
        {
            if(Target == null) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(Styles.header);
                
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(m_enabled, Styles.enabled);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_fogColor, Styles.fogColor);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_fogDensity, Styles.fogDensity);
                }
            }
        }
        
    }
}