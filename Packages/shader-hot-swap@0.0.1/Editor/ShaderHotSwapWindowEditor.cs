using System;
using UnityEngine;
using UnityEditor;

namespace ShaderHotSwap
{
    [CustomEditor(typeof(ShaderHotSwapWindow), true)]
    public class ShaderHotSwapWindowEditor : Editor
    {
        private SerializedProperty m_shaderDataList;
        
        private void OnEnable()
        {
            m_shaderDataList = serializedObject.FindProperty("m_shaderDataList");
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            
            serializedObject.Update();
            {
                EditorGUILayout.PropertyField(m_shaderDataList, new GUIContent("着色器"), true);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
