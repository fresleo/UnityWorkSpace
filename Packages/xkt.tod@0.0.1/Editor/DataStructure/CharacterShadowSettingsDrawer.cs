// Created by: WangYu   Date: 2025-11-03

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    public class CharacterShadowSettingsDrawer : IArrayElementDrawer
    {
        private class Styles
        {
            public static GUIContent shadow1Color = new("阴影1 - 颜色");
            public static GUIContent shadow1Step = new("阴影1 - 步进");
            public static GUIContent shadow1Feather = new("阴影1 - 羽化");

            public static GUIContent modelRenderPart = new("选择应用的角色部位");
        }

        private static GUILayoutOption s_toggleWidth, s_labelFieldWidth;

        public void DrawElement(SerializedProperty property, int index)
        {
            SerializedProperty override_shadow1Color = property.FindPropertyRelative(nameof(CharacterShadowSettings.override_shadow1Color));
            SerializedProperty shadow1Color = property.FindPropertyRelative(nameof(CharacterShadowSettings.shadow1Color));

            SerializedProperty override_shadow1Step = property.FindPropertyRelative(nameof(CharacterShadowSettings.override_shadow1Step));
            SerializedProperty shadow1Step = property.FindPropertyRelative(nameof(CharacterShadowSettings.shadow1Step));

            SerializedProperty override_shadow1Feather = property.FindPropertyRelative(nameof(CharacterShadowSettings.override_shadow1Feather));
            SerializedProperty shadow1Feather = property.FindPropertyRelative(nameof(CharacterShadowSettings.shadow1Feather));

            SerializedProperty modelRenderPart = property.FindPropertyRelative(nameof(CharacterShadowSettings.modelRenderPart));

            if (s_toggleWidth == null) s_toggleWidth = GUILayout.Width(30);
            if (s_labelFieldWidth == null) s_labelFieldWidth = GUILayout.Width(100);

            using (new EditorGUILayout.HorizontalScope())
            {
                override_shadow1Color.boolValue = EditorGUILayout.Toggle(override_shadow1Color.boolValue, s_toggleWidth);
                EditorGUILayout.LabelField(Styles.shadow1Color, s_labelFieldWidth);

                using (new EditorGUI.DisabledScope(!override_shadow1Color.boolValue))
                {
                    EditorGUILayout.PropertyField(shadow1Color, new GUIContent());
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                override_shadow1Step.boolValue = EditorGUILayout.Toggle(override_shadow1Step.boolValue, s_toggleWidth);
                EditorGUILayout.LabelField(Styles.shadow1Step, s_labelFieldWidth);

                using (new EditorGUI.DisabledScope(!override_shadow1Step.boolValue))
                {
                    EditorGUILayout.PropertyField(shadow1Step, new GUIContent());
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                override_shadow1Feather.boolValue = EditorGUILayout.Toggle(override_shadow1Feather.boolValue, s_toggleWidth);
                EditorGUILayout.LabelField(Styles.shadow1Feather, s_labelFieldWidth);

                using (new EditorGUI.DisabledScope(!override_shadow1Feather.boolValue))
                {
                    EditorGUILayout.PropertyField(shadow1Feather, new GUIContent());
                }
            }

            string[] rawOptions = CharacterShadowSettings.s_modelRenderParts;
            int rawTotalCount = rawOptions.Length;
            
            // 中间的部分，去掉“无”和“全部”
            int middleCount = rawTotalCount - 2;
            string[] displayedOptions = new string[middleCount];
            for (int i = 0; i < middleCount; i++)
            {
                displayedOptions[i] = rawOptions[i + 1];
            }
            
            // 中间部分的位掩码
            int middleMaskBits = (middleCount > 0) ? ((1 << middleCount) - 1) : 0;
            
            // 存储的值，如果有超出中间部分的位，就当它是全选了
            int rawStored = modelRenderPart.intValue;
            int storedMask = (rawStored & ~middleMaskBits) != 0 ? middleMaskBits : (rawStored & middleMaskBits);
            
            // 显示时，用没有头，尾的掩码和选项
            int displayedMask = storedMask;
            int newDisplayedMask = EditorGUILayout.MaskField(Styles.modelRenderPart, displayedMask, displayedOptions);

            // 映射回存储掩码：Nothing -> 0，Everything -> 全选
            if (newDisplayedMask == 0)
            {
                storedMask = 0;
            }
            else
            {
                storedMask = newDisplayedMask & middleMaskBits;
            }

            modelRenderPart.intValue = storedMask;

            string label = GetModelRenderPartLabel(modelRenderPart.intValue, rawOptions);
            EditorGUILayout.HelpBox(label, MessageType.Info);
        }
        
        private string GetModelRenderPartLabel(int mask, string[] rawOptions)
        {
            int totalCount = rawOptions.Length;
            if (totalCount == 0) return "";

            // 中间项
            int actualCount = Mathf.Max(0, totalCount - 2);
            int validBits = actualCount > 0 ? ((1 << actualCount) - 1) : 0;

            if (mask == 0) return rawOptions.Length > 0 ? rawOptions[0] : "无"; // 第 0 项为 "无"
            if (mask == validBits && actualCount > 0) return rawOptions[totalCount - 1]; // 最后 1 项为 "全部"
            
            var names = new List<string>();
            for (int i = 0; i < actualCount; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    names.Add(rawOptions[i + 1]); // 中间项在原数组中的偏移 +1
                }
            }

            string label = names.Count == 0 ? "无" : string.Join(", ", names);
            return label;
        }
        
    }
}