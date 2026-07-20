// Created By: WangYu  Date: 2024-11-22

using System;
using UnityEditor;

namespace RaindropEffect
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RainfallData))]
    public class RainfallDataInspector : Editor
    {
        private RainfallData m_script;
        
        private SimulateParametersSP m_spSP;
        private RenderParametersSP m_rpSP;
        
        private void OnEnable()
        {
            m_script = this.target as RainfallData;
            if(!m_script) return;
            
            SerializedProperty simuParas = serializedObject.FindProperty("simuParas");
            m_spSP = new SimulateParametersSP(simuParas, false);
            
            SerializedProperty rendParas = serializedObject.FindProperty("rendParas");
            m_rpSP = new RenderParametersSP(rendParas, false);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            
            if(!m_script) return;
            
            serializedObject.Update();
            {
                EditorGUILayout.LabelField("雨量配置", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.Space();
                    m_spSP.DrawGUI();
                    EditorGUILayout.Space();
                    m_rpSP.DrawGUI();
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
        
    }
}