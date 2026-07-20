// Created by: WangYu   Date: 2025-11-03

using UnityEditor;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    public class LightProbeDataEditor : ISettingsEditor
    {
        internal class Styles
        {
            public static GUIContent header = new("光照探针数据");

            public static GUIContent initialLightProbesArrayPosition = new("初始光探针阵列位置");
            public static GUIContent lightProbes = new("灯光探针球谐波数据");
            public static GUIContent lightProbes1D = new("1维数据", "如果后面搞 lerp 模式的话，这种数据结构会更方便");
        }

        private SerializedProperty m_initialLightProbesArrayPosition;
        private SerializedProperty m_lightProbes;
        private SerializedProperty m_lightProbes1D;

        public LightProbeDataEditor(SerializedProperty target)
        {
            Target = target;
        }
        
        public SerializedProperty Target { get; }
        
        public void Enable()
        {
            if(Target == null) return;
            
            m_initialLightProbesArrayPosition = Target.FindPropertyRelative(nameof(LightProbeData.initialLightProbesArrayPosition));
            m_lightProbes = Target.FindPropertyRelative(nameof(LightProbeData.lightProbes));
            m_lightProbes1D = Target.FindPropertyRelative(nameof(LightProbeData.lightProbes1D));
        }

        public void InspectorGUI()
        {
            if(Target == null) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(Styles.header);

                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(m_initialLightProbesArrayPosition, Styles.initialLightProbesArrayPosition);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_lightProbes, Styles.lightProbes);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_lightProbes1D, Styles.lightProbes1D);
                }
            }
        }
        
    }
}