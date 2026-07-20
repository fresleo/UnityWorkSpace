using System;
using UnityEngine;
using UnityEditor;

namespace SkinnedDecals
{
    [CustomEditor(typeof(SkinnedDecalMesh))]
    public class SkinnedDecalMeshEditor : Editor
    {
        private SerializedProperty m_decalSystem, m_decalBuilder;
        private SerializedProperty m_decalMesh;
        private SerializedProperty m_updateWhenOffscreen;
        private SerializedProperty m_currentDecalUniqueKey;
        
        private void OnEnable()
        {
            var script = (SkinnedDecalMesh)target;

            m_decalSystem = serializedObject.FindProperty(nameof(script.decalSystem));
            m_decalBuilder = serializedObject.FindProperty(nameof(script.decalBuilder));
            
            m_decalMesh = serializedObject.FindProperty(nameof(script.decalMesh));

            m_updateWhenOffscreen = serializedObject.FindProperty(nameof(script.updateWhenOffscreen));
            
            m_currentDecalUniqueKey = serializedObject.FindProperty(nameof(script.currentDecalUniqueKey));
        }

        public override void OnInspectorGUI()
        {
            var script = (SkinnedDecalMesh)target;
            serializedObject.Update();
            {
                // 这些数据都是要跟随父控制器变化的，所以就不让改了
                EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.PropertyField(m_decalSystem, new GUIContent("所属贴花系统"));
                    EditorGUILayout.PropertyField(m_decalBuilder, new GUIContent("所属构建器"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_decalMesh, new GUIContent("贴花 Mesh"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_updateWhenOffscreen, new GUIContent("在屏幕外更新"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_currentDecalUniqueKey, new GUIContent("当前的唯一键"));
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"SubMesh 数: {script.decalTriangles.Count}\n总顶点数: {script.allDecalVertices.Count}", MessageType.Info);
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("您可以使用此组件将 Mesh 保存为编辑器中的资源。", MessageType.Info);
            if (GUILayout.Button("保存 mesh 为资源"))
            {
                string path = EditorUtility.SaveFilePanel("保存 mesh 资源", "Assets", "NewDecalMesh", "");
                if (path.Length == 0)
                {
                    return;
                }

                int assetsStart = path.IndexOf("Assets", StringComparison.Ordinal);
                path = path.Substring(assetsStart, path.Length - assetsStart);
                
                Mesh mesh = script.decalMesh;
                if (mesh == null)
                {
                    EditorUtility.DisplayDialog("保存 mesh 为资源", "错误", "Mesh 为空，无法保存。");
                    return;
                }
                
                AssetDatabase.CreateAsset(mesh, $"{path}.asset");
                
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("保存 mesh 为资源", "成功", $"Mesh 被保存到: {path}.asset");
            }
        }
        
    }
}