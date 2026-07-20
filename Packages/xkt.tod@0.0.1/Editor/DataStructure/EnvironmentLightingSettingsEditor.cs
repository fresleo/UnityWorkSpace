// Created by: WangYu   Date: 2025-11-03

using UnityEditor;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    public class EnvironmentLightingSettingsEditor : ISettingsEditor
    {
        internal class Styles
        {
            public static GUIContent header = new("环境光设置");

            public static GUIContent ambientMode = new("环境光模式");

            public static GUIContent ambientIntensity = new("环境光强度");

            public static GUIContent skyColor = new("天空颜色");
            public static GUIContent equatorColor = new("赤道颜色");
            public static GUIContent groundColor = new("地面颜色");
            
            public static GUIContent ambientColor = new("环境光颜色");
        }

        private SerializedProperty m_ambientMode;
        private SerializedProperty m_ambientIntensity;
        private SerializedProperty m_skyColor, m_equatorColor, m_groundColor;
        private SerializedProperty m_ambientColor;

        public EnvironmentLightingSettingsEditor(SerializedProperty target)
        {
            Target = target;
        }

        public SerializedProperty Target { get; }

        public void Enable()
        {
            if(Target == null) return;
            
            m_ambientMode = Target.FindPropertyRelative(nameof(EnvironmentLightingSettings.ambientMode));
            m_ambientIntensity = Target.FindPropertyRelative(nameof(EnvironmentLightingSettings.ambientIntensity));
            
            m_skyColor = Target.FindPropertyRelative(nameof(EnvironmentLightingSettings.skyColor));
            m_equatorColor = Target.FindPropertyRelative(nameof(EnvironmentLightingSettings.equatorColor));
            m_groundColor = Target.FindPropertyRelative(nameof(EnvironmentLightingSettings.groundColor));
            
            m_ambientColor = Target.FindPropertyRelative(nameof(EnvironmentLightingSettings.ambientColor));
        }

        public void InspectorGUI()
        {
            if(Target == null) return;
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(Styles.header);
                
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(m_ambientMode, Styles.ambientMode);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_ambientIntensity, Styles.ambientIntensity);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_skyColor, Styles.skyColor);
                    EditorGUILayout.PropertyField(m_equatorColor, Styles.equatorColor);
                    EditorGUILayout.PropertyField(m_groundColor, Styles.groundColor);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_ambientColor, Styles.ambientColor);
                }
            }
        }
        
    }
}