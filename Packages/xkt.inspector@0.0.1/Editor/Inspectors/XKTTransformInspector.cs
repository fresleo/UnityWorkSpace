/*******************************************************************************
 * File: XKTTransformInspector.cs
 * Author: WangYu
 * Date: 2026-02-12
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace XKT.Editor.Inspectors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Transform), true)]
    public class XKTTransformInspector : UnityEditor.Editor
    {
        private Transform CurrentTarget => this.target as Transform;
        private Transform[] CurrentTargets => this.targets.Cast<Transform>().ToArray();
        
        private static class Styles
        {
            public static readonly GUIContent Empty = new(string.Empty);
            
            public static readonly GUIContent Linked = new(EditorGUIUtility.IconContent("Linked").image, "点击解锁");
            public static readonly GUIContent Unlinked = new(EditorGUIUtility.IconContent("Unlinked").image, "点击锁定");

            public const float LinkBtnWidthValue = 25;
            public const float LinkBtnHeightValue = 25;
            public static readonly GUILayoutOption LinkBtnWidth = GUILayout.Width(LinkBtnWidthValue);
            public static readonly GUILayoutOption LinkBtnHeight = GUILayout.Height(LinkBtnHeightValue);

            public const float ResetBtnWidthValue = 20;
            public static readonly GUILayoutOption ResetBtnWidth = GUILayout.Width(ResetBtnWidthValue);

            public static readonly GUIContent LocalSpace = new("Local Space");
            public static readonly GUIContent WorldSpace = new("World Space");
            public static readonly GUIContent PBtn = new("P", "位置 - 点击归0");
            public static readonly GUIContent RBtn = new("R", "旋转 - 点击归0");
            public static readonly GUIContent SBtn = new("S", "缩放 - 点击归1");
            public static readonly GUIContent X = new("X");
            public static readonly GUIContent Y = new("Y");
            public static readonly GUIContent Z = new("Z");
            public const float XYZLabelWidth = 12;
        }
        
        private SerializedProperty m_LocalPosition;
        private SerializedProperty m_LocalRotation;
        private SerializedProperty m_LocalScale;

        private TransformRotationGUI m_transformRotationGUI;
        
        private const string c_worldSpaceFoldoutKey = "XKTTransformInspector.WorldSpaceFoldout";

        private bool WorldSpaceFoldout
        {
            get => EditorPrefs.GetBool(c_worldSpaceFoldoutKey, true);
            set => EditorPrefs.SetBool(c_worldSpaceFoldoutKey, value);
        }
        
        private void OnEnable()
        {
            m_LocalPosition = serializedObject.FindProperty("m_LocalPosition");
            m_LocalRotation = serializedObject.FindProperty("m_LocalRotation");
            m_LocalScale = serializedObject.FindProperty("m_LocalScale");

            if (m_transformRotationGUI == null)
            {
                m_transformRotationGUI = new TransformRotationGUI();
            }
            m_transformRotationGUI.OnEnable(m_LocalRotation);
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true; // 界面使用宽版布局，如果是窄版，有些空间的标题和输入框可能会上下分层
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212; // 给右边的输入区域留出 212 的宽度
            }
            
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(Styles.LocalSpace, EditorStyles.boldLabel);
                DrawLocalPositionGUI();
                DrawLocalRotationGUI();
                DrawLocalScaleGUI();
                
                EditorGUILayout.Space();
                WorldSpaceFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(WorldSpaceFoldout, Styles.WorldSpace);
                if (WorldSpaceFoldout)
                {
                    bool preEnabled = GUI.enabled;
                    GUI.enabled = false;
                    DrawWorldPositionGUI();
                    DrawWorldRotationGUI();
                    DrawWorldScaleGUI();
                    GUI.enabled = preEnabled;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        // Local Space >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private void DrawLocalPositionGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Styles.PBtn, Styles.ResetBtnWidth))
                {
                    Undo.RecordObjects(this.targets, "重置位置");
                    
                    m_LocalPosition.vector3Value = Vector3.zero;
                }
                
                GUILayout.Space(Styles.LinkBtnWidthValue);
                EditorGUILayout.PropertyField(m_LocalPosition, Styles.Empty);
            }
        }

        private void DrawLocalRotationGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Styles.RBtn, Styles.ResetBtnWidth))
                {
                    Undo.RecordObjects(this.targets, "重置旋转");
                    
                    foreach (var item in this.CurrentTargets)
                    {
                        TransformUtils.SetInspectorRotation(item, Vector3.zero);
                    }
                    serializedObject.Update();
                }

                GUILayout.Space(Styles.LinkBtnWidthValue);
                m_transformRotationGUI?.RotationField();
            }
        }

        private void DrawLocalScaleGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Styles.SBtn, Styles.ResetBtnWidth))
                {
                    Undo.RecordObjects(this.targets, "重置缩放");

                    m_LocalScale.vector3Value = Vector3.one;
                }
                
                // 约束缩放比例按钮
                EditorGUI.BeginChangeCheck();
                
                bool constrained = this.CurrentTargets.Length > 0 && TransformUtils.GetConstrainProportions(this.CurrentTargets);
                
                bool newConstrained = GUILayout.Toggle(constrained
                    , constrained ? Styles.Linked : Styles.Unlinked, EditorStyles.toolbarButton
                    , Styles.LinkBtnWidth, Styles.LinkBtnHeight);

                if (EditorGUI.EndChangeCheck())
                {
                    if (CurrentTargets.Length > 0)
                    {
                        Undo.RecordObjects(this.targets, "约束缩放比例");
                        TransformUtils.SetConstrainProportions(CurrentTargets, newConstrained);
                    }
                }
                
                // 修改缩放
                Vector3 localScale = m_LocalScale.vector3Value;
                if (newConstrained)
                {
                    EditorGUI.BeginChangeCheck();
                    // Vector3 newScale = EditorGUILayout.Vector3Field(Styles.Empty, localScale);
                    
                    // 和 Vector3Field 同样式，但可以延迟确认触发的版本
                    Rect lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                    float spacing = 2f;
                    float itemWidth = (lineRect.width - 2f * spacing) / 3f;
                    Rect rx = new Rect(lineRect.x, lineRect.y, itemWidth, lineRect.height);
                    Rect ry = new Rect(lineRect.x + itemWidth + spacing, lineRect.y, itemWidth, lineRect.height);
                    Rect rz = new Rect(lineRect.x + 2f * (itemWidth + spacing), lineRect.y, itemWidth, lineRect.height);

                    float oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = Styles.XYZLabelWidth;

                    float newX = EditorGUI.DelayedFloatField(rx, Styles.X, localScale.x);
                    float newY = EditorGUI.DelayedFloatField(ry, Styles.Y, localScale.y);
                    float newZ = EditorGUI.DelayedFloatField(rz, Styles.Z, localScale.z);
                    Vector3 newScale = new Vector3(newX, newY, newZ);

                    EditorGUIUtility.labelWidth = oldLabelWidth;
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(this.targets, "修改连接情况下的缩放");
                        
                        float dx = Mathf.Abs(newScale.x - localScale.x);
                        float dy = Mathf.Abs(newScale.y - localScale.y);
                        float dz = Mathf.Abs(newScale.z - localScale.z);
                        
                        foreach (var tr in this.CurrentTargets)
                        {
                            Vector3 ls = tr.localScale;

                            if (dx >= dy && dx >= dz && ls.x != 0)
                            {
                                float ratio = newScale.x / ls.x;
                                ls = new Vector3(newScale.x, ls.y * ratio, ls.z * ratio);
                            }
                            else if (dy >= dz && ls.y != 0)
                            {
                                float ratio = newScale.y / ls.y;
                                ls = new Vector3(ls.x * ratio, newScale.y, ls.z * ratio);
                            }
                            else if (ls.z != 0)
                            {
                                float ratio = newScale.z / ls.z;
                                ls = new Vector3(ls.x * ratio, ls.y * ratio, newScale.z);
                            }
                            else
                            {
                                ls = newScale;
                            }

                            tr.localScale = ls;
                        }
                        serializedObject.Update();
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(m_LocalScale, Styles.Empty);
                }
            }
        }
        
        // World Space >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private void GetRectForXYZ(out Rect rx, out Rect ry, out Rect rz)
        {
            Rect lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            
            float spacing = 2f;
            float itemWidth = (lineRect.width - 2f * spacing) / 3f;
            
            rx = new Rect(lineRect.x, lineRect.y, itemWidth, lineRect.height);
            ry = new Rect(lineRect.x + itemWidth + spacing, lineRect.y, itemWidth, lineRect.height);
            rz = new Rect(lineRect.x + 2f * (itemWidth + spacing), lineRect.y, itemWidth, lineRect.height);
        }
        
        private void DrawWorldPositionGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Button(Styles.PBtn, Styles.ResetBtnWidth);
                GUILayout.Space(Styles.LinkBtnWidthValue);
                
                Transform[] cts = this.CurrentTargets;
                Vector3 pos = this.CurrentTarget.position;
                bool mixedX = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.position.x, cts[0].position.x));
                bool mixedY = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.position.y, cts[0].position.y));
                bool mixedZ = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.position.z, cts[0].position.z));

                GetRectForXYZ(out Rect rx, out Rect ry, out Rect rz);
                
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.XYZLabelWidth;
                
                EditorGUI.showMixedValue = mixedX;
                EditorGUI.FloatField(rx, Styles.X, pos.x);
                EditorGUI.showMixedValue = mixedY;
                EditorGUI.FloatField(ry, Styles.Y, pos.y);
                EditorGUI.showMixedValue = mixedZ;
                EditorGUI.FloatField(rz, Styles.Z, pos.z);
                EditorGUI.showMixedValue = false;
                
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        private void DrawWorldRotationGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Button(Styles.RBtn, Styles.ResetBtnWidth);
                GUILayout.Space(Styles.LinkBtnWidthValue);
                
                Transform[] cts = CurrentTargets;
                Vector3 euler = CurrentTarget.eulerAngles;
                bool mixedX = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.eulerAngles.x, cts[0].eulerAngles.x));
                bool mixedY = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.eulerAngles.y, cts[0].eulerAngles.y));
                bool mixedZ = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.eulerAngles.z, cts[0].eulerAngles.z));
                
                GetRectForXYZ(out Rect rx, out Rect ry, out Rect rz);
                
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.XYZLabelWidth;

                EditorGUI.showMixedValue = mixedX;
                EditorGUI.FloatField(rx, Styles.X, euler.x);
                EditorGUI.showMixedValue = mixedY;
                EditorGUI.FloatField(ry, Styles.Y, euler.y);
                EditorGUI.showMixedValue = mixedZ;
                EditorGUI.FloatField(rz, Styles.Z, euler.z);
                EditorGUI.showMixedValue = false;
                
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        private void DrawWorldScaleGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Button(Styles.SBtn, Styles.ResetBtnWidth);
                GUILayout.Space(Styles.LinkBtnWidthValue);
                
                Transform[] cts = CurrentTargets;
                Vector3 scale = CurrentTarget.lossyScale;
                bool mixedX = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.lossyScale.x, cts[0].lossyScale.x));
                bool mixedY = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.lossyScale.y, cts[0].lossyScale.y));
                bool mixedZ = cts.Length > 1 && cts.Any(t => !Mathf.Approximately(t.lossyScale.z, cts[0].lossyScale.z));
                
                GetRectForXYZ(out Rect rx, out Rect ry, out Rect rz);
                
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.XYZLabelWidth;

                EditorGUI.showMixedValue = mixedX;
                EditorGUI.FloatField(rx, Styles.X, scale.x);
                EditorGUI.showMixedValue = mixedY;
                EditorGUI.FloatField(ry, Styles.Y, scale.y);
                EditorGUI.showMixedValue = mixedZ;
                EditorGUI.FloatField(rz, Styles.Z, scale.z);
                EditorGUI.showMixedValue = false;
                
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }
        
    }
}