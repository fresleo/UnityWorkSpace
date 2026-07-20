// Created by: WangYu   Date: 2025-12-16

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToonPostProcessing
{
    [CustomEditor(typeof(OutlineDistortRendererFeature))]
    public class OutlineDistortRendererFeatureEditor : Editor
    {
        private struct Styles
        {
            public static readonly GUIContent outlineDistortRenderData = new("渲染数据", "包含渲染所需的资源");
            
            public static readonly GUIContent renderPassEvent = new("渲染插入的时机");
            
            public const string drawMaskTooltip = 
                "绘制遮罩的目的主要是为了能够比较容易，高效的改变扭曲轮廓的形状，和尺寸。" 
                + "\n代价就是消耗更多的性能，所以请在确实需要时才考虑开启。";
            public static readonly GUIContent drawMask = new("绘制遮罩");
            public static readonly GUIContent targetLayerMask = new("目标层遮罩");

            public const string targetEffectIdTooltip =
                "ScreenEffect ID 筛选（0 = 不筛选，使用目标层遮罩；大于 0 = 只对通过"
                + " XKnightScreenEffectIdMaskPass.SetId 注册了对应 ID 的 Renderer 生效）。";
            public static readonly GUIContent targetEffectId = new("ScreenEffect ID 筛选", targetEffectIdTooltip);
        }
        
        private OutlineDistortRendererFeature CurrentTarget => this.target as OutlineDistortRendererFeature;
        
        private SerializedProperty m_renderPassEvent;
        private SerializedProperty m_drawMask;
        //private SerializedProperty m_targetLayerMask;
        private SerializedProperty m_targetEffectId;
        
        private void OnEnable()
        {
            if(!CurrentTarget) return;

            SerializedProperty settings = serializedObject.FindProperty(nameof(OutlineDistortRendererFeature.settings));
            
            m_renderPassEvent = settings.FindPropertyRelative(nameof(OutlineDistortRendererFeature.settings.renderPassEvent));
            
            m_drawMask = settings.FindPropertyRelative(nameof(OutlineDistortRendererFeature.settings.drawMask));
            //m_targetLayerMask = settings.FindPropertyRelative(nameof(OutlineDistortRendererFeature.settings.targetLayerMask));
            m_targetEffectId = settings.FindPropertyRelative(nameof(OutlineDistortRendererFeature.settings.targetEffectId));
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
            EditorGUILayout.PropertyField(m_renderPassEvent, Styles.renderPassEvent);
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.drawMaskTooltip, MessageType.Warning);
            EditorGUILayout.PropertyField(m_drawMask, Styles.drawMask);
            using (new EditorGUI.IndentLevelScope(1))
            {
                bool useEffectId = m_targetEffectId != null && m_targetEffectId.intValue > 0;

                // targetLayerMask 仅在未启用 effectId 筛选时有意义
                //using (new EditorGUI.DisabledScope(useEffectId))
                //{
                //    EditorGUILayout.PropertyField(m_targetLayerMask, Styles.targetLayerMask);
                //}

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(Styles.targetEffectIdTooltip, MessageType.Info);
                if (m_targetEffectId != null)
                {
                    EditorGUILayout.PropertyField(m_targetEffectId, Styles.targetEffectId);
                }
            }
        }
        
    }
}