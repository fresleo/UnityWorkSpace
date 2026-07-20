// Created by: WangYu   Date: 2025-11-03

using UnityEditor;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    public class EnvironmentReflectionsSettingsEditor : ISettingsEditor
    {
        internal class Styles
        {
            public static GUIContent header = new("环境反射设置");

            public static GUIContent defaultReflectionMode = new("默认反射模式");

            public static GUIContent defaultReflectionResolution = new("默认反射分辨率");
            public static GUIContent customReflectionTexture = new("自定义反射纹理");

            public static GUIContent reflectionIntensity = new("反射强度");
            public static GUIContent reflectionBounces = new("反射反弹次数");
        }
        
        private SerializedProperty m_defaultReflectionMode;
        
        private SerializedProperty m_defaultReflectionResolution;
        private SerializedProperty m_customReflectionTexture;

        private SerializedProperty m_reflectionIntensity;
        private SerializedProperty m_reflectionBounces;

        public EnvironmentReflectionsSettingsEditor(SerializedProperty target)
        {
            Target = target;
        }

        public SerializedProperty Target { get; }

        public void Enable()
        {
            if(Target == null) return;
            
            m_defaultReflectionMode = Target.FindPropertyRelative(nameof(EnvironmentReflectionsSettings.defaultReflectionMode));
            
            m_defaultReflectionResolution = Target.FindPropertyRelative(nameof(EnvironmentReflectionsSettings.defaultReflectionResolution));
            m_customReflectionTexture = Target.FindPropertyRelative(nameof(EnvironmentReflectionsSettings.customReflectionTexture));
            
            m_reflectionIntensity = Target.FindPropertyRelative(nameof(EnvironmentReflectionsSettings.reflectionIntensity));
            m_reflectionBounces = Target.FindPropertyRelative(nameof(EnvironmentReflectionsSettings.reflectionBounces));
        }

        public void InspectorGUI()
        {
            if(Target == null) return;
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(Styles.header);
                
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(m_defaultReflectionMode, Styles.defaultReflectionMode);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_defaultReflectionResolution, Styles.defaultReflectionResolution);
                    EditorGUILayout.PropertyField(m_customReflectionTexture, Styles.customReflectionTexture);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_reflectionIntensity, Styles.reflectionIntensity);
                    EditorGUILayout.PropertyField(m_reflectionBounces, Styles.reflectionBounces);
                }
            }
        }
        
    }
}