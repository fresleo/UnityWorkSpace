// Created By: WangYu  Date: 2024-07-16

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonPostProcessing
{
    [CustomEditor(typeof(ToonPostProcessingRendererFeature))]
    public class ToonPostProcessingRendererFeatureEditor : Editor
    {
        private struct Styles
        {
            public static readonly GUIContent rendererData = new("渲染数据", "包含渲染所需的资源");
        }

        private ToonPostProcessingRendererFeature CurrentTarget => this.target as ToonPostProcessingRendererFeature;
        
        private SerializedProperty m_rendererData;

        private void OnEnable()
        {
            if(!CurrentTarget) return;
            
            if (CurrentTarget.settings != null)
            {
                if (CurrentTarget.settings.waterColorGroupRendererData == null)
                {
                    ResourceReloader.ReloadAllNullIn(CurrentTarget.settings, PackageConst.c_packagePath);
                }
                if (CurrentTarget.settings.waterColorGroupRendererData != null && !CurrentTarget.settings.waterColorGroupRendererData.renderResources.HasAllLoaded)
                {
                    ResourceReloader.ReloadAllNullIn(CurrentTarget.settings.waterColorGroupRendererData, PackageConst.c_packagePath);
                }
            }
            
            SerializedProperty settings = serializedObject.FindProperty(nameof(ToonPostProcessingRendererFeature.settings));
            m_rendererData = settings.FindPropertyRelative(nameof(ToonPostProcessingRendererFeature.settings.waterColorGroupRendererData));
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            if(!CurrentTarget) return;
            
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
            if (m_rendererData != null)
            {
                EditorGUILayout.PropertyField(m_rendererData, Styles.rendererData);
            }
        }
        
    }
}