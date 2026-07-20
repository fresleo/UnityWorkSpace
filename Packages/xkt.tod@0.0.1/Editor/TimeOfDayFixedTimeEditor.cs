/*******************************************************************************
 * File: TimeOfDayFixedTimeEditor.cs
 * Author: WangYu
 * Date: 2026-01-09
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.Utils;

namespace XKT.TOD
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TimeOfDayFixedTime))]
    public class TimeOfDayFixedTimeEditor : Editor
    {
        class Styles
        {
            public static GUIContent Header = new("TOD 时间固定");

            public static GUIContent FixedIndex = new("时间点");
            
            public static GUIContent Morning = new("清晨");
            public static GUIContent Daytime = new("白天");
            public static GUIContent Nightfall = new("黄昏");
            public static GUIContent Night = new("夜晚");

            public static GUIContent ConflictObject = new("有冲突的组件");
            public static readonly string ConflictObjectTips = $"{nameof(TimeOfDayFixedTime)} 和 {nameof(TimeOfDayManager)} 是不能同时工作的，只能保留1种";
        }
        
        private TimeOfDayFixedTime CurrentTarget => this.target as TimeOfDayFixedTime;

        private SerializedProperty _fixedIndex;

        private const int FIND_INTERVAL = 2;
        private float _lastFindTime;
        private TimeOfDayManager _foundScript;

        
        private void OnEnable()
        {
            _fixedIndex = serializedObject.FindProperty(nameof(TimeOfDayFixedTime.fixedIndex));
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();

            if (CurrentTarget == null)
            {
                return;
            }
            
            EditorGUILayout.LabelField(Styles.Header, EditorStyles.whiteLargeLabel);
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(_fixedIndex, Styles.FixedIndex);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                Rect totalRect = EditorGUILayout.GetControlRect();
                float labelWidth = totalRect.width / 4f; // 分成4份
                
                EditorGUI.LabelField(GUILayoutUtility.GetRect(labelWidth, EditorGUIUtility.singleLineHeight), Styles.Morning, GetSelectedStyle(0));
                EditorGUI.LabelField(GUILayoutUtility.GetRect(labelWidth, EditorGUIUtility.singleLineHeight), Styles.Daytime, GetSelectedStyle(1));
                EditorGUI.LabelField(GUILayoutUtility.GetRect(labelWidth, EditorGUIUtility.singleLineHeight), Styles.Nightfall, GetSelectedStyle(2));
                EditorGUI.LabelField(GUILayoutUtility.GetRect(labelWidth, EditorGUIUtility.singleLineHeight), Styles.Night, GetSelectedStyle(3));
            }

            EditorGUILayout.Space();
            FindConflictObject();
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private GUIStyle GetSelectedStyle(int index)
        {
            int currentIndex = _fixedIndex.intValue;
            if (currentIndex == index)
            {
                GUIStyle selectedStyle = new GUIStyle(EditorStyles.label);
                selectedStyle.normal.textColor = Color.green;

                return selectedStyle;
            }

            return EditorStyles.label;
        }

        private void FindConflictObject()
        {
            if (Time.realtimeSinceStartup - _lastFindTime >= FIND_INTERVAL)
            {
                _lastFindTime = Time.realtimeSinceStartup;
                
                Scene currentScene = CurrentTarget.gameObject.scene;
                _foundScript = TODUtils.FindObjectOfTypeInTargetScene<TimeOfDayManager>(currentScene);
            }

            if (_foundScript != null)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.ObjectField(Styles.ConflictObject, _foundScript, typeof(TimeOfDayManager), true);
                    EditorGUILayout.HelpBox(Styles.ConflictObjectTips, MessageType.Error);
                }
            }
        }
        
    }
}