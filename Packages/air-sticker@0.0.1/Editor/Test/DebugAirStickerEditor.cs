// Created By: WangYu  Date: 2025-02-18

using System;
using AirSticker.Runtime.Logic;
using AirSticker.Runtime.Test;
using UnityEditor;
using UnityEngine;

namespace AirSticker.Test
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DebugAirSticker))]
    public class DebugAirStickerEditor : Editor
    {
        private SerializedProperty m_layerMask;
        private SerializedProperty m_mdConfig;

        private AbsDecalConfigGUI m_configGui;
        private MaterialGUI m_materialGui;
        
        private DebugAirSticker CurrentTarget => this.target as DebugAirSticker;
        
        private void OnEnable()
        {
            if(CurrentTarget == null) return;
            
            m_layerMask = serializedObject.FindProperty(nameof(DebugAirSticker.layerMask));
            m_mdConfig = serializedObject.FindProperty(nameof(DebugAirSticker.mdConfig));

            m_configGui = DecalConfigGUIFactory.Create(CurrentTarget.mdConfig);
            m_materialGui = new MaterialGUI();
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            
            if(CurrentTarget == null) return;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.LabelField("调试空气贴纸", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("只有编辑器正在启动的情况下，才能正常工作。", MessageType.Warning);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_layerMask, new GUIContent("碰撞层"));
            
            // 配置文件不同时，GUI 也不一样
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_mdConfig, new GUIContent("静态网格贴花配置"));
            if (EditorGUI.EndChangeCheck())
            {
                m_configGui = DecalConfigGUIFactory.Create(m_mdConfig.objectReferenceValue as AbsDecalConfig);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("贴1个有生命周期的"))
                        {
                            CurrentTarget.Cast(false);
                        }
                        
                        if (GUILayout.Button("贴1个永久的"))
                        {
                            CurrentTarget.Cast(true);
                        }
                    }

                    if (GUILayout.Button("全部销毁"))
                    {
                        AirStickerSystem.DecalMeshPool.Dispose();
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (m_configGui != null)
            {
                EditorGUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    m_configGui.OnInspectorGUI();
                }
            }
            
            if (m_materialGui != null && CurrentTarget.mdConfig != null)
            {
                EditorGUILayout.Space(10);
                m_materialGui.OnInspectorGUI(CurrentTarget.mdConfig.material);
            }
        }
        
    }
}
