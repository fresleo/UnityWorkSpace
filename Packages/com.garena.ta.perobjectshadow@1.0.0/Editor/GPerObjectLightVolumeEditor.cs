/*******************************************************************************
 * File: GPerObjectLightVolumeEditor.cs
 * Author: Codex
 * Date: 2026-05-08
 * Description: 逐物体阴影局部光照 Volume 的 Inspector 和 SceneView 编辑句柄。
 *
 * Notice: 编辑器脚本只负责场景编辑体验，不参与 Player 运行时编译。
 *******************************************************************************/

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Garena.TA
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(GPerObjectLightVolume))]
    public class GPerObjectLightVolumeEditor : Editor
    {
        private static readonly Color s_boundsColor = new Color(1f, 0.72f, 0.12f, 0.9f);
        private static readonly Color s_directionColor = new Color(1f, 0.95f, 0.35f, 0.95f);

        private readonly BoxBoundsHandle _boundsHandle = new BoxBoundsHandle();

        private SerializedProperty _centerProperty;
        private SerializedProperty _sizeProperty;
        private SerializedProperty _lightRotationProperty;
        private SerializedProperty _intensityProperty;
        private SerializedProperty _weightProperty;
        private SerializedProperty _showSolidGizmoProperty;

        [MenuItem("GameObject/Garena TA/Per Object Light Volume", false, 10)]
        private static void CreateVolume(MenuCommand command)
        {
            GameObject go = new GameObject("GPerObjectLightVolume");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<GPerObjectLightVolume>();
            Undo.RegisterCreatedObjectUndo(go, "Create Per Object Light Volume");
            Selection.activeGameObject = go;
        }

        private void OnEnable()
        {
            _centerProperty = serializedObject.FindProperty(nameof(GPerObjectLightVolume.center));
            _sizeProperty = serializedObject.FindProperty(nameof(GPerObjectLightVolume.size));
            _lightRotationProperty = serializedObject.FindProperty(nameof(GPerObjectLightVolume.lightRotation));
            _intensityProperty = serializedObject.FindProperty(nameof(GPerObjectLightVolume.intensity));
            _weightProperty = serializedObject.FindProperty(nameof(GPerObjectLightVolume.weight));
            _showSolidGizmoProperty = serializedObject.FindProperty(nameof(GPerObjectLightVolume.showSolidGizmo));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_centerProperty, new GUIContent("中心"));
            EditorGUILayout.PropertyField(_sizeProperty, new GUIContent("尺寸"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_lightRotationProperty, new GUIContent("光照朝向"));
            EditorGUILayout.PropertyField(_intensityProperty, new GUIContent("强度"));
            EditorGUILayout.PropertyField(_weightProperty, new GUIContent("权重"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_showSolidGizmoProperty, new GUIContent("选中时绘制实体"));

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            GPerObjectLightVolume volume = target as GPerObjectLightVolume;
            if (volume == null)
            {
                return;
            }

            DrawTransformHandle(volume);
            DrawBoundsHandle(volume);
            DrawLightDirection(volume);
        }

        private void DrawTransformHandle(GPerObjectLightVolume volume)
        {
            Transform volumeTransform = volume.transform;

            if (Tools.current == Tool.Move)
            {
                Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local
                    ? volumeTransform.rotation
                    : Quaternion.identity;

                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(volumeTransform.position, handleRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volumeTransform, "Move Per Object Light Volume");
                    volumeTransform.position = newPosition;
                    EditorUtility.SetDirty(volumeTransform);
                }
            }

            if (Tools.current == Tool.Rotate)
            {
                EditorGUI.BeginChangeCheck();
                Quaternion newRotation = Handles.RotationHandle(volumeTransform.rotation, volumeTransform.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volumeTransform, "Rotate Per Object Light Volume");
                    volumeTransform.rotation = newRotation;
                    EditorUtility.SetDirty(volumeTransform);
                }
            }
        }

        private void DrawBoundsHandle(GPerObjectLightVolume volume)
        {
            Matrix4x4 previousMatrix = Handles.matrix;
            Color previousColor = Handles.color;

            Handles.matrix = volume.transform.localToWorldMatrix;
            Handles.color = s_boundsColor;

            _boundsHandle.center = volume.center;
            _boundsHandle.size = volume.size;
            _boundsHandle.handleColor = s_boundsColor;
            _boundsHandle.wireframeColor = s_boundsColor;

            EditorGUI.BeginChangeCheck();
            _boundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(volume, "Resize Per Object Light Volume");
                volume.center = _boundsHandle.center;
                volume.size = SanitizeSize(_boundsHandle.size);
                EditorUtility.SetDirty(volume);
            }

            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
        }

        private void DrawLightDirection(GPerObjectLightVolume volume)
        {
            Vector3 worldCenter = volume.transform.TransformPoint(volume.center);
            float handleSize = HandleUtility.GetHandleSize(worldCenter);
            Color previousColor = Handles.color;

            Handles.color = s_directionColor;
            Handles.ArrowHandleCap(
                0,
                worldCenter,
                volume.LightRotation,
                handleSize,
                EventType.Repaint);

            Handles.color = previousColor;
        }

        private static Vector3 SanitizeSize(Vector3 size)
        {
            const float C_MIN_SIZE = 0.01f;
            size.x = Mathf.Max(C_MIN_SIZE, Mathf.Abs(size.x));
            size.y = Mathf.Max(C_MIN_SIZE, Mathf.Abs(size.y));
            size.z = Mathf.Max(C_MIN_SIZE, Mathf.Abs(size.z));
            return size;
        }
    }
}
