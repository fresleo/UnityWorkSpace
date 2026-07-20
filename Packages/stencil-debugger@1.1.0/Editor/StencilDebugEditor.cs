using UnityEditor;
using UnityEngine;

namespace StencilDebugger
{
    [CustomEditor(typeof(StencilDebug))]
    public class StencilDebugEditor : Editor
    {
        private static class Styles
        {
            public const string UsageTips = "注意：该方案当前只能工作在 Vulkan API 上。";
            
            public static readonly GUIContent injectionPoint = new("注入阶段", "控制渲染过程的执行时机");
            public static readonly GUIContent showInSceneView = new("在 Scene View 中显示");
            public static readonly GUIContent scale = new("缩放", "控制模版数字的比例");
            public static readonly GUIContent margin = new("边距", "每个模板数字周围的边距");
        }
        
        private SerializedProperty m_injectionPoint;
        private SerializedProperty m_showInSceneView;
        private SerializedProperty m_scale;
        private SerializedProperty m_margin;

        private void OnEnable()
        {
            m_injectionPoint = serializedObject.FindProperty(nameof(StencilDebug.injectionPoint));
            m_showInSceneView = serializedObject.FindProperty(nameof(StencilDebug.showInSceneView));
            m_scale = serializedObject.FindProperty(nameof(StencilDebug.scale));
            m_margin = serializedObject.FindProperty(nameof(StencilDebug.margin));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInspectorGUI()
        {
            EditorGUILayout.HelpBox(Styles.UsageTips, MessageType.Warning);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_injectionPoint, Styles.injectionPoint);
            EditorGUILayout.PropertyField(m_showInSceneView, Styles.showInSceneView);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_scale, Styles.scale);
            EditorGUILayout.PropertyField(m_margin, Styles.margin);
        }
        
    }
}