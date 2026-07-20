// Created By: WangYu  Date: 2024-11-18

using UnityEditor;
using UnityEngine;

namespace RaindropEffect
{
    public class RenderParametersSP
    {
        private SerializedProperty m_root;
        private bool m_highlightSomeProperty;

        private SerializedProperty m_renderTextureWidth, m_renderTextureHeight;
        private SerializedProperty m_dropletsSpawnRate, m_dropletSizeRange;
        private SerializedProperty m_refraction, m_lightPosition, m_raindropColor, m_alphaSmoothRange;
        
        public RenderParametersSP(SerializedProperty root, bool highlightSomeProperty)
        {
            m_root = root;
            m_highlightSomeProperty = highlightSomeProperty;

            var tempObj = new RenderParameters();
            
            m_renderTextureWidth = m_root.FindPropertyRelative(nameof(tempObj.renderTextureWidth));
            m_renderTextureHeight = m_root.FindPropertyRelative(nameof(tempObj.renderTextureHeight));
            
            m_dropletsSpawnRate = m_root.FindPropertyRelative(nameof(tempObj.dropletsSpawnRate));
            m_dropletSizeRange = m_root.FindPropertyRelative(nameof(tempObj.dropletSizeRange));
            
            m_refraction = m_root.FindPropertyRelative(nameof(tempObj.refraction));
            m_lightPosition = m_root.FindPropertyRelative(nameof(tempObj.lightPosition));
            m_raindropColor = m_root.FindPropertyRelative(nameof(tempObj.raindropColor));
            m_alphaSmoothRange = m_root.FindPropertyRelative(nameof(tempObj.alphaSmoothRange));
        }

        public void DrawGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("渲染参数", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                using (new EditorGUI.IndentLevelScope(1))
                {
                    if (m_renderTextureWidth != null || m_renderTextureHeight != null)
                    {
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUILayoutExt.PropertyField(m_renderTextureWidth, new GUIContent("RT 的宽"));
                            EditorGUILayoutExt.PropertyField(m_renderTextureHeight, new GUIContent("RT 的高"));
                        }
                    }
                    
                    EditorGUILayoutExt.PropertyField(m_dropletsSpawnRate, new GUIContent("液滴的生成速率 (每秒)"), m_highlightSomeProperty);
                    EditorGUILayoutExt.PropertyField(m_dropletSizeRange, new GUIContent("液滴的尺寸范围"));
                    
                    EditorGUILayout.Space();
                    EditorGUILayoutExt.PropertyField(m_refraction, new GUIContent("折射范围"));
                    EditorGUILayoutExt.PropertyField(m_lightPosition, new GUIContent("灯光的位置"));
                    EditorGUILayoutExt.PropertyField(m_raindropColor, new GUIContent("雨滴颜色"));
                    EditorGUILayoutExt.PropertyField(m_alphaSmoothRange, new GUIContent("Alpha 平滑范围"));
                }
            }
        }
        
    }
}