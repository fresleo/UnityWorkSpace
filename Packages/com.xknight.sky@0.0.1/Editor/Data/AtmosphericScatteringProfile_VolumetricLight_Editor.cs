using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public partial class AtmosphericScatteringProfileEditor
    {
        private void DrawVolumetricLightSections(AtmosphericScatteringProfile rootProfile)
        {
            var profile = new SerializedObject(rootProfile.volumetricLightProfile);

            EditorGUILayout.PropertyField(profile.FindProperty("DitheringTex"), new GUIContent("Dithering"));
            EditorGUILayout.PropertyField(profile.FindProperty("VolumetricLightRange"), new GUIContent("范围"));
            EditorGUILayout.PropertyField(profile.FindProperty("VolumetricLightColor"), new GUIContent("颜色"));

            profile.ApplyModifiedProperties();
        }

    }
}