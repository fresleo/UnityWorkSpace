using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public partial class AtmosphericScatteringProfileEditor
    {
        private void DrawTimeSettingSections(AtmosphericScatteringProfile rootProfile)
        {
            var timeProfile = new SerializedObject(rootProfile.timeProfile);

            var timeOfDay = timeProfile.FindProperty("TimeOfDay");
            EditorGUILayout.PropertyField(timeOfDay, new GUIContent("Time Of Day", "开启后灯光参数会根据时间自动计算"));
            if (timeOfDay.boolValue)
            {
                EditorGUILayout.PropertyField(timeProfile.FindProperty("Timeline"), new GUIContent("时间"));

                EditorGUILayout.Separator();
                EditorGUILayout.PropertyField(timeProfile.FindProperty("Latitude"), new GUIContent("纬度"));
                EditorGUILayout.PropertyField(timeProfile.FindProperty("Longitude"), new GUIContent("经度"));
                EditorGUILayout.PropertyField(timeProfile.FindProperty("UTC"), new GUIContent("时区"));

                EditorGUILayout.Separator();
                EditorGUILayout.PropertyField(timeProfile.FindProperty("SunLightColor"), new GUIContent("白天灯光颜色"));
                EditorGUILayout.PropertyField(timeProfile.FindProperty("SunLightIntensity"), new GUIContent("白天灯光强度"));
                EditorGUILayout.PropertyField(timeProfile.FindProperty("MoonLightColor"), new GUIContent("夜晚灯光颜色"));
                EditorGUILayout.PropertyField(timeProfile.FindProperty("MoonLightIntensity"), new GUIContent("夜晚灯光强度"));

                EditorGUILayout.Separator();
                var playTime = timeProfile.FindProperty("PlayTime");
                EditorGUILayout.PropertyField(playTime, new GUIContent("时间流逝"));
                if (playTime.boolValue)
                {
                    EditorGUILayout.PropertyField(timeProfile.FindProperty("DayLengthInMinutes"), new GUIContent("单次昼夜循环时长(分钟)"));
                }
            }

            timeProfile.ApplyModifiedProperties();
        }

    }
}