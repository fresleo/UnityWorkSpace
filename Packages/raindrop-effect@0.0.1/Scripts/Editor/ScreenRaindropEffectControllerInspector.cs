// Created By: WangYu  Date: 2024-11-20

using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RaindropEffect
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScreenRaindropEffectController))]
    public class ScreenRaindropEffectControllerInspector : Editor
    {
        static class Labels
        {
            public static readonly GUIContent presetDatas = new("预设配置");
            public static readonly GUIContent startValues = new("开始值");
            public static readonly GUIContent operation = new("操作");
            
            public static readonly GUIContent load = new("Load");

            public static readonly GUIContent timeScale = new("时间缩放");
        }
        
        private ScreenRaindropEffectController m_script;
        
        private GUIStyle m_labelCenteredStyle;
        private GUIStyle m_labelLeftStyle;
        private GUIStyle m_labelRightStyle;

        private ReorderableList m_presetDataRL;
        
        private void OnEnable()
        {
            m_script = this.target as ScreenRaindropEffectController;
            if(!m_script) return;
            
            m_presetDataRL = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(m_script.presetDatas)), true, true, true, true);
            
            m_presetDataRL.drawHeaderCallback = (Rect rect) =>
            {
                Rect newRect = new Rect(0, rect.y, 0, rect.height);

                newRect.x = rect.x;
                newRect.width = rect.width - 250;
                EditorGUI.LabelField(newRect, Labels.presetDatas, m_labelCenteredStyle);

                newRect.x = rect.x + rect.width - 160;
                newRect.width = 75;
                EditorGUI.LabelField(newRect, Labels.startValues);

                newRect.x = rect.x + rect.width - 65;
                newRect.width = 75;
                EditorGUI.LabelField(newRect, Labels.operation);
            };

            m_presetDataRL.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_presetDataRL.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;

                Rect newRect = new Rect(0, rect.y, 0, EditorGUIUtility.singleLineHeight);

                // 预设配置
                newRect.x = rect.x;
                newRect.width = rect.width - 250;
                EditorGUI.PropertyField(newRect, element, GUIContent.none);

                // 开始值
                newRect.x = rect.x + 100 + (rect.width - 230);
                newRect.width = 50;
                if (index > 0 && index < m_presetDataRL.serializedProperty.arraySize - 1)
                {
                    EditorGUI.BeginChangeCheck();
                    m_script.startValues[index] = EditorGUI.FloatField(newRect, m_script.startValues[index]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(m_script);
                    }
                }
                else
                {
                    bool isEnd = index == m_presetDataRL.serializedProperty.arraySize - 1;
                    EditorGUI.LabelField(newRect, isEnd ? "1" : "0");
                }
                
                // 操作
                newRect = new Rect(rect.x + (rect.width - 75), rect.y, 75, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(newRect, Labels.load))
                {
                    float startValue = m_script.startValues[index];
                    m_script.globalBlendFactor = startValue;
                }
            };
            
            m_presetDataRL.onRemoveCallback = (ReorderableList rl) =>
            {
                SerializedProperty presetDatas = serializedObject.FindProperty(nameof(m_script.presetDatas));
                SerializedProperty startValues = serializedObject.FindProperty(nameof(m_script.startValues));
                
                presetDatas.DeleteArrayElementAtIndex(rl.index);
                startValues.DeleteArrayElementAtIndex(rl.index);

                for (int i = 0; i < startValues.arraySize; i++)
                {
                    startValues.GetArrayElementAtIndex(i).floatValue = CalculateStartValue(startValues.arraySize, i);
                }
            };

            m_presetDataRL.onAddCallback = (ReorderableList rl) =>
            {
                ReorderableList.defaultBehaviours.DoAddButton(rl);

                SerializedProperty startValues = serializedObject.FindProperty(nameof(m_script.startValues));
                startValues.InsertArrayElementAtIndex(rl.index);

                for (int i = 0; i < startValues.arraySize; i++)
                {
                    startValues.GetArrayElementAtIndex(i).floatValue = CalculateStartValue(startValues.arraySize, i);
                }
            };

            // 如果顺序改变后，需要调用某些逻辑时，才需要这里
            // m_presetDataRL.onReorderCallback = (ReorderableList rl) =>
            // {
            //     serializedObject.FindProperty(nameof(m_script.orderChanged)).boolValue = true;
            // };
        }

        private float CalculateStartValue(int total, int index)
        {
            float result = Mathf.Round(index * (1f / (total - 1)) * 100f) / 100f;
            return result;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!m_script) return;

            InitGUIStyle();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawGUI();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void InitGUIStyle()
        {
            if (m_labelCenteredStyle == null)
            {
                m_labelCenteredStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(),
                    padding = new RectOffset(0, 0, 0, 0),
                    fontSize = 11,
                    wordWrap = true
                };
            }

            if (m_labelLeftStyle == null)
            {
                m_labelLeftStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(),
                    padding = new RectOffset(0, 0, 0, 0),
                    fontSize = 11,
                    wordWrap = true
                };
            }

            if (m_labelRightStyle == null)
            {
                m_labelRightStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    margin = new RectOffset(),
                    padding = new RectOffset(0, 0, 0, 0),
                    fontSize = 11,
                    wordWrap = true
                };
            }
        }

        private void DrawGUI()
        {
            EditorGUILayout.Space(5);
            m_presetDataRL.DoLayoutList();

            EditorGUILayout.Space(15);
            m_script.globalBlendFactor = EditorGUILayout.Slider(m_script.globalBlendFactor, 0, 1);
            Rect blendFactorSliderRect = new Rect(GUILayoutUtility.GetLastRect());

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = 0; j < m_script.presetDatas.Count; j++)
                {
                    if (m_script.presetDatas[j] == null)
                    {
                        continue;
                    }

                    Rect mark = new Rect(blendFactorSliderRect);
                    mark.size = new Vector2(1, 10);

                    float labelWidth = 100;
                    float labelHeight = 20;
                    Rect labelRect = new Rect(0, 0, labelWidth, labelHeight);

                    // 最左
                    if (j == 0)
                    {
                        labelRect.x = blendFactorSliderRect.position.x;
                        labelRect.y = blendFactorSliderRect.position.y + labelHeight;

                        mark.position = new Vector2(
                            blendFactorSliderRect.position.x,
                            blendFactorSliderRect.position.y + 15);

                        EditorGUI.LabelField(labelRect, m_script.presetDatas[j].name, m_labelLeftStyle);
                    }
                    // 最右
                    else if (j == m_script.presetDatas.Count - 1)
                    {
                        labelRect.x = blendFactorSliderRect.position.x + (blendFactorSliderRect.width - labelWidth);
                        labelRect.y = blendFactorSliderRect.position.y + labelHeight;

                        mark.position = new Vector2(
                            blendFactorSliderRect.position.x + blendFactorSliderRect.width,
                            blendFactorSliderRect.position.y + 15);

                        EditorGUI.LabelField(labelRect, m_script.presetDatas[j].name, m_labelRightStyle);
                    }
                    // 中间的
                    else
                    {
                        float startValue = m_script.startValues[j];

                        labelRect.x = blendFactorSliderRect.position.x + (blendFactorSliderRect.width * startValue) - (labelWidth * 0.5f);
                        labelRect.y = blendFactorSliderRect.position.y + labelHeight;

                        mark.position = new Vector2(
                            blendFactorSliderRect.position.x + (blendFactorSliderRect.width * startValue),
                            blendFactorSliderRect.position.y + 15);

                        EditorGUI.LabelField(
                            labelRect,
                            m_script.presetDatas[j].name,
                            m_labelCenteredStyle);
                    }

                    EditorGUI.DrawRect(mark, Color.gray); // 绘制刻度线

                    // 绘制 0, 1 之外的开始值 label
                    Rect startValueRect = new Rect(mark);
                    startValueRect.x -= 25;
                    startValueRect.y -= 30;
                    startValueRect.width = 50;
                    startValueRect.height = 15;

                    // 排除头尾
                    if (j > 0 && j < m_script.startValues.Count - 1)
                    {
                        float startValue = m_script.startValues[j];
                        EditorGUI.LabelField(startValueRect, $"{startValue}", m_labelCenteredStyle);
                    }
                }
            }
            
            EditorGUILayout.Space(50); // 上面的时间轴，没有从检查器中申请到足够的显示空间，所以需要用这个空格来挤一下
            
            EditorGUIUtility.labelWidth = 60;
            m_script.timeScale = EditorGUILayout.Slider(Labels.timeScale, m_script.timeScale, 0, 1);
        }

    }
}