/*******************************************************************************
 * File: FullScreenTransitionRendererFeatureEditor.cs
 * Author: WangYu
 * Date: 2026-03-05
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System;
using UnityEditor;
using UnityEngine;

namespace ToonPostProcessing
{
    [CustomEditor(typeof(FullScreenTransitionRendererFeature))]
    public class FullScreenTransitionRendererFeatureEditor : Editor
    {
        private struct Styles
        {
            public static readonly GUIContent header = new("全屏过渡效果");
            
            public static readonly GUIContent 
                unfoldHeader = new("展开 pass")
                , overlayHeader = new("叠加 pass");

            public static readonly GUIContent
                renderEvent = new("渲染插入的时机"), targetCamera = new("目标摄像机");
            
            public static readonly GUIContent 
                material = new("着色材质")
                , maxFarDepth = new("最远深度"), maxRadius = new("过渡的最大半径"), EdgeWidth = new("边宽度")
                , transitionTransform = new("过渡位置的 Transform"), transitionCenterPosition = new("过渡位置的坐标")
                , useBlendTex = new("启用混合纹理"), blendRT = new("混合纹理");

            public const string transitionPositionTip = "注意：[过渡位置的 Transform] 和 [过渡位置的坐标] 同一时间只能有一个生效。";
        }

        private SerializedProperty _renderEvent;
        private SerializedProperty _targetCamera;

        private SerializedProperty
            _material
            , _maxFarDepth, _maxRadius, _edgeWidth
            , _transitionTransform, _transitionCenterPosition
            , _useBlendTex, _blendRT;
        private SerializedProperty
            _backMaterial
            , _backMaxRadius
            , _backTransitionTF, _backTransitionCenterPosition
            , _backUseBlendTex, _backBlendRT;

        void OnEnable()
        {
            SerializedProperty settings = serializedObject.FindProperty(nameof(FullScreenTransitionRendererFeature.Setting));
            
            _renderEvent = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.renderEvent));
            _targetCamera = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.TargetCamera));
            
            _material = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.Material));
            _maxFarDepth = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.MaxFarDepth));
            _maxRadius = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.MaxRadius));
            _edgeWidth = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.EdgeWidth));
            _transitionTransform = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.TransitionTransform));
            _transitionCenterPosition = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.TransitionCenterPosition));
            _useBlendTex = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.UseBlendTex));
            _blendRT = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.BlendRT));
            
            _backMaterial = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.BackMaterial));
            _backMaxRadius = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.BackMaxRadius));
            _backTransitionTF = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.BackTransitionTF));
            _backTransitionCenterPosition = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.BackTransitionCenterPosition));
            _backUseBlendTex = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.BackUseBlendTex));
            _backBlendRT = settings.FindPropertyRelative(nameof(FullScreenTransitionRendererFeature.Setting.BackBlendRT));
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
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
            EditorGUILayout.LabelField(Styles.header, EditorStyles.whiteLargeLabel);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_renderEvent, Styles.renderEvent);
            EditorGUILayout.PropertyField(_targetCamera, Styles.targetCamera);
            
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(Styles.unfoldHeader, EditorStyles.whiteLargeLabel);

                EditorGUILayout.Space();
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(_material, Styles.material);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_maxFarDepth, Styles.maxFarDepth);
                    EditorGUILayout.PropertyField(_maxRadius, Styles.maxRadius);
                    EditorGUILayout.PropertyField(_edgeWidth, Styles.EdgeWidth);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_transitionTransform, Styles.transitionTransform);
                    using (new EditorGUI.DisabledScope(_transitionTransform.objectReferenceValue != null))
                    {
                        EditorGUILayout.PropertyField(_transitionCenterPosition, Styles.transitionCenterPosition);
                    }
                    EditorGUILayout.HelpBox(Styles.transitionPositionTip, MessageType.None);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_useBlendTex, Styles.useBlendTex);
                    EditorGUILayout.PropertyField(_blendRT, Styles.blendRT);
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(Styles.overlayHeader, EditorStyles.whiteLargeLabel);
                
                EditorGUILayout.Space();
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(_backMaterial, Styles.material);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_backMaxRadius, Styles.maxRadius);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_backTransitionTF, Styles.transitionTransform);
                    using (new EditorGUI.DisabledScope(_backTransitionTF.objectReferenceValue != null))
                    {
                        EditorGUILayout.PropertyField(_backTransitionCenterPosition, Styles.transitionCenterPosition);
                    }
                    EditorGUILayout.HelpBox(Styles.transitionPositionTip, MessageType.None);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_backUseBlendTex, Styles.useBlendTex);
                    EditorGUILayout.PropertyField(_backBlendRT, Styles.blendRT);
                }
            }
        }
        
    }
}