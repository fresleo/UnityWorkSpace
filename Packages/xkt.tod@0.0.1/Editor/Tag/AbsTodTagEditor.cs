// Created By: WangYu  Date: 2025-04-10

using System;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.Tag
{
    public class AbsTodTagEditor : Editor
    {
        AbsTodTag CurrentTarget => this.target as AbsTodTag;
        
        static class Styles
        {
            public static readonly GUIContent scriptId = new("唯一Id");
            public static readonly GUIContent hierarchyPath = new("Hierarchy Path");
        }
        
        private SerializedProperty m_scriptId;
        private SerializedProperty m_hierarchyPath;

        protected virtual void OnEnable()
        {
            if(!CurrentTarget) return;
            
            m_scriptId = serializedObject.FindProperty(nameof(AbsTodTag.scriptId));
            m_hierarchyPath = serializedObject.FindProperty(nameof(AbsTodTag.hierarchyPath));
        }

        public override void OnInspectorGUI()
        {
            if(!CurrentTarget) return;
            
            // base.OnInspectorGUI();
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_scriptId, Styles.scriptId);
                EditorGUILayout.PropertyField(m_hierarchyPath, Styles.hierarchyPath);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
    }
}