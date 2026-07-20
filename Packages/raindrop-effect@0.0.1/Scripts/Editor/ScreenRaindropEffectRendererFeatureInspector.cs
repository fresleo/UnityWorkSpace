// Created By: WangYu  Date: 2024-11-18

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaindropEffect
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScreenRaindropEffectRendererFeature))]
    public class ScreenRaindropEffectRendererFeatureInspector : Editor
    {
        private ScreenRaindropEffectRendererFeature m_script;
        
        private SerializedProperty m_renderPassEvent;
        private SerializedProperty m_rendererData;
        private SimulateParametersSP m_spSP;
        private RenderParametersSP m_rpSP;
        
        private void OnEnable()
        {
            m_script = this.target as ScreenRaindropEffectRendererFeature;
            if(!m_script) return;
            
            // 自动加载已有的渲染数据配置
            if (m_script.rendererData == null)
            {
                ResourceReloader.ReloadAllNullIn(m_script, RaindropRendererData.c_packagePath);
            }
            
            m_renderPassEvent = serializedObject.FindProperty("renderPassEvent");
            m_rendererData = serializedObject.FindProperty("rendererData");
            
            SerializedProperty simuParas = serializedObject.FindProperty("simuParas");
            m_spSP = new SimulateParametersSP(simuParas, true);
            
            SerializedProperty rendParas = serializedObject.FindProperty("rendParas");
            m_rpSP = new RenderParametersSP(rendParas, true);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            
            if(!m_script) return;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawGUI();
            if(EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawGUI()
        {
            EditorGUILayout.LabelField("屏幕雨滴特效", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(m_renderPassEvent, new GUIContent("渲染阶段"));
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_rendererData, new GUIContent("渲染数据"));
                
                EditorGUILayout.Space();
                m_spSP.DrawGUI();
                EditorGUILayout.Space();
                m_rpSP.DrawGUI();
            }
        }
        
    }
}