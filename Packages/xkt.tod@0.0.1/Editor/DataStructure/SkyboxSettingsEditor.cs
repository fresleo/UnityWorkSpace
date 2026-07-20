// Created by: WangYu   Date: 2025-11-03

using UnityEditor;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    public class SkyboxSettingsEditor : ISettingsEditor
    {
        internal class Styles
        {
            public static GUIContent header = new("天空盒设置");

            public static GUIContent enabled = new("启用");
            public static GUIContent tint = new("调色");
            public static GUIContent exposure = new("曝光");
            public static GUIContent rotation = new("旋转");
            public static GUIContent skyboxTexture = new("天空盒立方体贴图");
        }
        
        private SerializedProperty m_enabled;
        private SerializedProperty m_tint;
        private SerializedProperty n_exposure;
        private SerializedProperty m_rotation;
        private SerializedProperty m_skyboxTexture;

        public SkyboxSettingsEditor(SerializedProperty target)
        {
            Target = target;
        }

        public SerializedProperty Target { get; }

        public void Enable()
        {
            if(Target == null) return;
            
            m_enabled = Target.FindPropertyRelative(nameof(SkyboxSettings.enabled));
            m_tint = Target.FindPropertyRelative(nameof(SkyboxSettings.tint));
            n_exposure = Target.FindPropertyRelative(nameof(SkyboxSettings.exposure));
            m_rotation = Target.FindPropertyRelative(nameof(SkyboxSettings.rotation));
            m_skyboxTexture = Target.FindPropertyRelative(nameof(SkyboxSettings.skyboxTexture));
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
                    EditorGUILayout.PropertyField(m_tint, Styles.tint);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(n_exposure, Styles.exposure);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_rotation, Styles.rotation);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_skyboxTexture, Styles.skyboxTexture);
                }
            }
        }
        
    }
}