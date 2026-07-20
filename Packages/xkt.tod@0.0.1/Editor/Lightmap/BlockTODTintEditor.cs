// Created By: WangYu  Date: 2025-07-02

using System;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.Lightmap
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BlockTODTint))]
    public class BlockTODTintEditor : Editor
    {
        class Styles
        {
            public static GUIContent header = new("屏蔽 TOD 对子对象的颜色调整");

            public const string helpBox = "注意：Lit 材质，可以通过\"TOD GI 调色强度\"属性来进行单独控制。";
            public static GUIContent isBlock = new("屏蔽");
        }

        private BlockTODTint CurrentTarget => this.target as BlockTODTint;

        private SerializedProperty m_isBlock;
        
        private void OnEnable()
        {
            if(CurrentTarget == null) return;
            
            m_isBlock = serializedObject.FindProperty(nameof(BlockTODTint.isBlock));
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            if (CurrentTarget == null) return;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.LabelField(Styles.header, EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.helpBox, MessageType.Warning);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_isBlock, Styles.isBlock);
            
            EditorGUILayout.Space();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
    }
}