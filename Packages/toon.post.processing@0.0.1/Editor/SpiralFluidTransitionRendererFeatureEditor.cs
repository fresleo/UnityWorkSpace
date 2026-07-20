/*******************************************************************************
 * File: SpiralFluidTransitionRendererFeatureEditor.cs
 * Author: fan.shi
 * Date: 2026-05-25
 * Description: 旋涡流体转场效果 Inspector。
 *
 * Notice:
 *******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace ToonPostProcessing
{
    [CustomEditor(typeof(SpiralFluidTransitionRendererFeature))]
    public class SpiralFluidTransitionRendererFeatureEditor : Editor
    {
        private struct Styles
        {
            public static readonly GUIContent setting = new GUIContent("转场设置");
            public static readonly GUIContent renderEvent = new GUIContent("渲染时机");
            public static readonly GUIContent brightenDuration = new GUIContent("变亮时长（秒）");
            public static readonly GUIContent transitionDuration = new GUIContent("扭曲展开时长（秒）");
            public static readonly GUIContent progress = new GUIContent("转场进度");

            public static readonly GUIContent distortionTex = new GUIContent("扭曲贴图");
            public static readonly GUIContent distortionTiling = new GUIContent("扭曲贴图 Tiling");
            public static readonly GUIContent distortionFlow = new GUIContent("扭曲贴图流动");
            public static readonly GUIContent openingDistortionStrength = new GUIContent("开场扭曲强度");
            public static readonly GUIContent expandingDistortionStrength = new GUIContent("扩散扭曲强度");
            public static readonly GUIContent warmBrightLut = new GUIContent("变亮LUT图");
            public static readonly GUIContent alphaFadeStartRatio = new GUIContent("原图 Alpha 淡出开始");
            public static readonly GUIContent fromEndAlpha = new GUIContent("原图 Alpha 结束值");
            public static readonly GUIContent toReachClarityRatio = new GUIContent("目标图 变清晰起点");
            public static readonly GUIContent toReachClarityEndRatio = new GUIContent("目标图 变清晰终点");
            public static readonly GUIContent toReachNormalBrightRatio = new GUIContent("目标图 恢复正常亮度起点");
            public static readonly GUIContent toReachNormalBrightEndRatio = new GUIContent("目标图 恢复正常亮度终点");
            public static readonly GUIContent toBrightenIntensity = new GUIContent("目标图 中段提亮强度");
            public static readonly GUIContent toMaxBlurRadius = new GUIContent("目标图 最大模糊半径");
            public static readonly GUIContent fromRimOverlayWidth = new GUIContent("卷边宽度");
            public static readonly GUIContent exposureIntensity = new GUIContent("整体曝光强度");


            public static readonly GUIContent fromZoomStrength = new GUIContent("原图缩放强度");
            public static readonly GUIContent endRadius = new GUIContent("原图结束时的比例");

        }

        private SerializedProperty _setting;
        private SerializedProperty _renderEvent;
        private SerializedProperty _brightenDuration;
        private SerializedProperty _transitionDuration;
        private SerializedProperty _progress;
        private SerializedProperty _distortionTex;
        private SerializedProperty _distortionTiling;
        private SerializedProperty _distortionFlow;
        private SerializedProperty _openingDistortionStrength;
        private SerializedProperty _expandingDistortionStrength;
        private SerializedProperty _warmBrightLut;
        private SerializedProperty _alphaFadeStartRatio;
        private SerializedProperty _fromEndAlpha;
        private SerializedProperty _toReachClarityRatio;
        private SerializedProperty _toReachClarityEndRatio;
        private SerializedProperty _toReachNormalBrightRatio;
        private SerializedProperty _toReachNormalBrightEndRatio;
        private SerializedProperty _toBrightenIntensity;
        private SerializedProperty _toMaxBlurRadius;
        private SerializedProperty _fromRimOverlayWidth;
        private SerializedProperty _exposureIntensity;
        private SerializedProperty _fromZoomStrength;
        private SerializedProperty _endRadius;


        private void OnEnable()
        {
            _setting = serializedObject.FindProperty(nameof(SpiralFluidTransitionRendererFeature.setting));
            if (_setting == null) return;

            _setting.isExpanded = true;
            _renderEvent = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.renderEvent));
            _brightenDuration = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.brightenDuration));
            _transitionDuration = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.transitionDuration));
            _progress = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.progress));
            _distortionTex = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.distortionTex));
            _distortionTiling = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.distortionTiling));
            _distortionFlow = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.distortionFlow));
            _openingDistortionStrength = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.openingDistortionStrength));
            _expandingDistortionStrength = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.expandingDistortionStrength));
            _warmBrightLut = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.warmBrightLut));
            _alphaFadeStartRatio = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.alphaFadeStartRatio));
            _fromEndAlpha = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.fromEndAlpha));
            _toReachClarityRatio = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.toReachClarityRatio));
            _toReachClarityEndRatio = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.toReachClarityEndRatio));
            _toReachNormalBrightRatio = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.toReachNormalBrightRatio));
            _toReachNormalBrightEndRatio = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.toReachNormalBrightEndRatio));
            _toBrightenIntensity = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.toBrightenIntensity));
            _toMaxBlurRadius = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.toMaxBlurRadius));
            _fromRimOverlayWidth = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.fromRimOverlayWidth));
            _exposureIntensity = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.exposureIntensity));
            _fromZoomStrength = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.fromZoomStrength));
            _endRadius = _setting.FindPropertyRelative(nameof(SpiralFluidTransitionSettings.endRadius));

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawInspectorGUI();
            EnforceRatioOrder();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawInspectorGUI()
        {
            if (_setting == null) return;

            _setting.isExpanded = EditorGUILayout.Foldout(_setting.isExpanded, Styles.setting, true);
            if (!_setting.isExpanded) return;

            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.PropertyField(_renderEvent, Styles.renderEvent);
                EditorGUILayout.PropertyField(_brightenDuration, Styles.brightenDuration);
                EditorGUILayout.PropertyField(_transitionDuration, Styles.transitionDuration);
                EditorGUILayout.PropertyField(_progress, Styles.progress);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_distortionTex, Styles.distortionTex);
                EditorGUILayout.PropertyField(_distortionTiling, Styles.distortionTiling);
                EditorGUILayout.PropertyField(_distortionFlow, Styles.distortionFlow);
                EditorGUILayout.PropertyField(_openingDistortionStrength, Styles.openingDistortionStrength);
                EditorGUILayout.PropertyField(_expandingDistortionStrength, Styles.expandingDistortionStrength);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_warmBrightLut, Styles.warmBrightLut);
                EditorGUILayout.PropertyField(_alphaFadeStartRatio, Styles.alphaFadeStartRatio);
                EditorGUILayout.PropertyField(_fromEndAlpha, Styles.fromEndAlpha);
                EditorGUILayout.PropertyField(_toReachClarityRatio, Styles.toReachClarityRatio);
                EditorGUILayout.PropertyField(_toReachClarityEndRatio, Styles.toReachClarityEndRatio);
                EditorGUILayout.PropertyField(_toReachNormalBrightRatio, Styles.toReachNormalBrightRatio);
                EditorGUILayout.PropertyField(_toReachNormalBrightEndRatio, Styles.toReachNormalBrightEndRatio);
                EditorGUILayout.PropertyField(_toBrightenIntensity, Styles.toBrightenIntensity);
                EditorGUILayout.PropertyField(_toMaxBlurRadius, Styles.toMaxBlurRadius);
                EditorGUILayout.PropertyField(_fromRimOverlayWidth, Styles.fromRimOverlayWidth);
                EditorGUILayout.PropertyField(_exposureIntensity, Styles.exposureIntensity);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_fromZoomStrength, Styles.fromZoomStrength);
                EditorGUILayout.PropertyField(_endRadius, Styles.endRadius);
            }
        }

        private void EnforceRatioOrder()
        {
            ClampStartEndPair(_toReachClarityRatio, _toReachClarityEndRatio);
            ClampStartEndPair(_toReachNormalBrightRatio, _toReachNormalBrightEndRatio);
        }

        private static void ClampStartEndPair(SerializedProperty start, SerializedProperty end)
        {
            if (start == null || end == null)
            {
                return;
            }

            start.floatValue = Mathf.Clamp(start.floatValue, 0f, 0.99f);
            end.floatValue = Mathf.Clamp(end.floatValue, start.floatValue + 0.001f, 1f);
        }
    }
}
