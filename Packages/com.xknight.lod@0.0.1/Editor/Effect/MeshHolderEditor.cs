using UnityEngine;
using UnityEditor;

namespace XKnight.XLOD
{
    [CustomEditor(typeof(MeshHolder))]
    [CanEditMultipleObjects]
    public class MeshHolderEditor : Editor
    {
        GUIContent highContent = new GUIContent("高质量可见");
        GUIContent mediumContent = new GUIContent("中质量可见");
        GUIContent lowContent = new GUIContent("低质量可见");

        MeshHolder script;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            script = target as MeshHolder;

            script.highConfig.enable = EditorGUILayout.Toggle(highContent, script.highConfig.enable);
            script.mediumConfig.enable = EditorGUILayout.Toggle(mediumContent, script.mediumConfig.enable);
            script.lowConfig.enable = EditorGUILayout.Toggle(lowContent, script.lowConfig.enable);
        }
    }
}