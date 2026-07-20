using UnityEngine;
using UnityEditor;

namespace XKnight.XLOD
{
    [CustomEditor(typeof(TrailHolder))]
    [CanEditMultipleObjects]
    public class TrailHolderEditor : Editor
    {
        GUIContent highEnableContent = new GUIContent("高质量可见");
        GUIContent mediumEnableContent = new GUIContent("中质量可见");
        GUIContent lowEnableContent = new GUIContent("低质量可见");

        TrailHolder script;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            script = target as TrailHolder;

            script.highConfig.enable = EditorGUILayout.Toggle(highEnableContent, script.highConfig.enable);
            EditorGUILayout.Separator();
            script.mediumConfig.enable = EditorGUILayout.Toggle(mediumEnableContent, script.mediumConfig.enable);
            EditorGUILayout.Separator();
            script.lowConfig.enable = EditorGUILayout.Toggle(lowEnableContent, script.lowConfig.enable);
        }
    }
}
