// Created By: WangYu  Date: 2025-06-09

using System;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD
{
    [CustomEditor(typeof(LightmappedLOD))]
    public class LightmappedLODEditor : Editor
    {
        static class Styles
        {
            public static readonly GUIContent lastIsBillboard = new("最后1级 LOD 是 Billboard（公告板，片）", "公告板是1个片，UV 不可能和前面的对上，所以不需要传递 Lightmap 信息了");

            public static readonly GUIContent refreshData = new("刷新数据");
        }

        private LightmappedLOD CurrentTarget => this.target as LightmappedLOD;

        private SerializedProperty m_lastIsBillboard;
        
        private void OnEnable()
        {
            if(!CurrentTarget) return;
            
            m_lastIsBillboard = serializedObject.FindProperty(nameof(LightmappedLOD.lastIsBillboard));
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            if(!CurrentTarget) return;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(m_lastIsBillboard, Styles.lastIsBillboard);
            
            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.refreshData))
            {
                CurrentTarget.RendererInfoTransfer();
            }
            
            EditorGUILayout.Space();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
    }
}