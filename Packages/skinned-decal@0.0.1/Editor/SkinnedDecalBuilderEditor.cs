// Created By: WangYu  Date: 2024-09-26

using System;
using UnityEditor;
using UnityEngine;

namespace SkinnedDecals
{
    [CustomEditor(typeof(SkinnedDecalBuilder))]
    public class SkinnedDecalBuilderEditor : Editor
    {
        private SerializedProperty m_decalSystem;
        private SerializedProperty m_debugBoneBounds, m_debugBoneLine;
        
        private void OnEnable()
        {
            var script = (SkinnedDecalBuilder)target;
            
            m_decalSystem = serializedObject.FindProperty(nameof(script.decalSystem));
            
            m_debugBoneBounds = serializedObject.FindProperty(nameof(script.debugBoneBounds));
            m_debugBoneLine = serializedObject.FindProperty(nameof(script.debugBoneLine));
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            
            serializedObject.Update();
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(m_decalSystem, new GUIContent("所属贴花系统"));
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug");
                EditorGUILayout.PropertyField(m_debugBoneBounds, new GUIContent("绘制骨骼包围盒"));
                EditorGUILayout.PropertyField(m_debugBoneLine, new GUIContent("绘制骨骼连线"));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}