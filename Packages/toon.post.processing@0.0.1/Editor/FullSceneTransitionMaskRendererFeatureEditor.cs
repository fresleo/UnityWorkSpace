/*******************************************************************************
 * File: FullSceneTransitionMaskRendererFeatureEditor.cs
 * Author: WangYu
 * Date: 2026-03-12
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System;
using UnityEditor;
using UnityEngine;

namespace ToonPostProcessing
{
    [CustomEditor(typeof(FullSceneTransitionMaskRendererFeature))]
    public class FullSceneTransitionMaskRendererFeatureEditor : Editor
    {
        private struct Styles
        {
            public static readonly GUIContent header = new("基于遮罩的全屏过渡效果");
            
            public static readonly GUIContent 
                unfoldHeader = new("展开 pass")
                , overlayHeader = new("叠加 pass");

            public static readonly GUIContent
                renderPassEvent = new("渲染插入的时机")
                , targetCamera = new("目标摄像机");

            public static readonly GUIContent
                raySphereMaskMaterial = new("射线球的遮罩材质"), transitionMaterial = new("过渡材质");

            public static readonly GUIContent sphereTransform = new("球 Transform");

            public static readonly GUIContent
                poleThresholdInner = new("分界线 - 内进度（溶解带内侧）"), poleThresholdOuter = new("分界线 - 外进度（真实边界）")
                , irregularEdgeWidth = new("分界线"), fillFogFlowDirection = new("填充雾效-流动方向");
            public static readonly GUIContent 
                useBlendTex = new("使用混合 RT"), blendRT = new("混合 RT");
        }

        private SerializedProperty _renderPassEvent, _targetCamera;
        
        private SerializedProperty _raySphereMaskMaterial, _transitionMaterial;
        private SerializedProperty _sphereTransform;
        private SerializedProperty _poleThresholdInner, _poleThresholdOuter, _irregularEdgeWidth;
        private SerializedProperty _fillFogFlowDirection;
        private SerializedProperty _useBlendTex, _blendRT;
        
        private SerializedProperty _backRaySphereMaskMaterial, _backTransitionMaterial;
        private SerializedProperty _backSphereTransform;
        private SerializedProperty _backPoleThresholdInner, _backPoleThresholdOuter;
        private SerializedProperty _backUseBlendTex, _backBlendRT;
        
        void OnEnable()
        {
            SerializedProperty settings = serializedObject.FindProperty(nameof(FullSceneTransitionMaskRendererFeature.settings));
            
            _renderPassEvent = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.renderPassEvent));
            _targetCamera = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.TargetCamera));
            
            // 
            _raySphereMaskMaterial = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.raySphereMaskMaterial));
            _transitionMaterial = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.transitionMaterial));
            
            _sphereTransform = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.sphereTransform));
            _poleThresholdInner = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.poleThresholdInner));
            _poleThresholdOuter = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.poleThresholdOuter));
            _irregularEdgeWidth = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.irregularEdgeWidth));
            _fillFogFlowDirection = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.fillFogFlowDirection));
            
            _useBlendTex = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.useBlendTex));
            _blendRT = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.blendRT));
            
            // 
            _backRaySphereMaskMaterial = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.backRaySphereMaskMaterial));
            _backTransitionMaterial = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.backTransitionMaterial));
            
            _backSphereTransform  = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.backSphereTransform));
            _backPoleThresholdInner = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.backPoleThresholdInner));
            _backPoleThresholdOuter = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.backPoleThresholdOuter));
            
            _backUseBlendTex = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.backUseBlendTex));
            _backBlendRT = settings.FindPropertyRelative(nameof(FullSceneTransitionMaskRendererFeature.settings.backBlendRT));
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawInspectorGUI();
            
            if(EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawInspectorGUI()
        {
            EditorGUILayout.LabelField(Styles.header, EditorStyles.whiteLargeLabel);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_renderPassEvent, Styles.renderPassEvent);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_targetCamera, Styles.targetCamera);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.unfoldHeader, EditorStyles.whiteLargeLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(_raySphereMaskMaterial, Styles.raySphereMaskMaterial);
                EditorGUILayout.PropertyField(_transitionMaterial, Styles.transitionMaterial);
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_sphereTransform, Styles.sphereTransform);
                EditorGUILayout.PropertyField(_poleThresholdInner, Styles.poleThresholdInner);
                EditorGUILayout.PropertyField(_poleThresholdOuter, Styles.poleThresholdOuter);
                EditorGUILayout.PropertyField(_irregularEdgeWidth, Styles.irregularEdgeWidth);
                // EditorGUILayout.PropertyField(_fillFogFlowDirection, Styles.fillFogFlowDirection);
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_useBlendTex, Styles.useBlendTex);
                if (_useBlendTex.boolValue)
                {
                    EditorGUILayout.PropertyField(_blendRT, Styles.blendRT);
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.overlayHeader, EditorStyles.whiteLargeLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(_backRaySphereMaskMaterial, Styles.raySphereMaskMaterial);
                EditorGUILayout.PropertyField(_backTransitionMaterial, Styles.transitionMaterial);
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_backSphereTransform, Styles.sphereTransform);
                EditorGUILayout.PropertyField(_backPoleThresholdInner, Styles.poleThresholdInner);
                EditorGUILayout.PropertyField(_backPoleThresholdOuter, Styles.poleThresholdOuter);
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_backUseBlendTex, Styles.useBlendTex);
                if (_backUseBlendTex.boolValue)
                {
                    EditorGUILayout.PropertyField(_backBlendRT, Styles.blendRT);
                }
            }
        }
        
    }
}