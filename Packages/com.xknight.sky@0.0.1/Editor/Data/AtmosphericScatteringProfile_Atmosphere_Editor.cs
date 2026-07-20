using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public partial class AtmosphericScatteringProfileEditor
    {
        public void DrawAtmosphereSections(AtmosphericScatteringProfile rootProfile)
        {
            var rootObject = new SerializedObject(rootProfile.atmosphereProfile);

            EditorGUILayout.PropertyField(rootObject.FindProperty("SkyboxMaterial"), new GUIContent("天空盒材质"));

            // Rayleigh
            CoreEditorUtils.DrawHeader("瑞丽散射(Rayleigh Scattering)");

            EditorGUILayout.Separator();
            GUI.backgroundColor = Color.red;
            EditorGUILayout.PropertyField(rootObject.FindProperty("WavelengthR"), new GUIContent("光波长 R"));
            GUI.backgroundColor = Color.green;
            EditorGUILayout.PropertyField(rootObject.FindProperty("WavelengthG"), new GUIContent("光波长 G"));
            GUI.backgroundColor = Color.blue;
            EditorGUILayout.PropertyField(rootObject.FindProperty("WavelengthB"), new GUIContent("光波长 B"));
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("AtmosphereThickness"), new GUIContent("大气厚度"));

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("SunBrightness"), new GUIContent("太阳亮度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("SunAtmosphereTint"), new GUIContent("太阳大气颜色"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("SunIntensityFactor"), new GUIContent("太阳强度"));

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("MoonBrightness"), new GUIContent("月亮亮度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("MoonAtmosphereTint"), new GUIContent("月亮大气颜色"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("MoonIntensityFactor"), new GUIContent("月亮强度"));

            // Mie
            CoreEditorUtils.DrawHeader("米氏散射(Mie Scattering)");

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("Mie"), new GUIContent("Mie系数"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("Turbidity"), new GUIContent("浑浊度"));

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("SunMieColor"), new GUIContent("太阳Mie颜色"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("SunMieAnisotropy"), new GUIContent("太阳Mie各向异性"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("SunMieScattering"), new GUIContent("太阳Mie散射"));

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("MoonMieColor"), new GUIContent("月亮Mie颜色"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("MoonMieAnisotropy"), new GUIContent("月亮Mie各向异性"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("MoonMieScattering"), new GUIContent("月亮Mie散射"));

            CoreEditorUtils.DrawHeader("辅助参数");

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("AtmosphereExponent"), new GUIContent("大气指数"));

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("HorizonOffset"), new GUIContent("地平线偏移"));

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(rootObject.FindProperty("RayleighZenithLength"), new GUIContent("Rayleigh天顶长度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("MieZenithLength"), new GUIContent("Mie天顶长度"));

            rootObject.ApplyModifiedProperties();
        }
    }
}