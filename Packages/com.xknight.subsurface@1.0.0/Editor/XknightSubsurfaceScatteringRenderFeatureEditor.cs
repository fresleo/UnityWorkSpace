//<summary>
//name: XknightSubsurfaceScatteringRenderFeatureEditor.cs
//date: 2026-6-15
//author: calvin
//description: 在ui面板上的显示
//</summary>

using UnityEditor;
using UnityEngine;

namespace XKnight.TA.SSS
{
    [CustomEditor(typeof(XknightSubsurfaceScatteringRenderFeature))]
    public class XknightSubsurfaceScatteringRenderFeatureEditor : Editor
    {
        SerializedProperty preferCompute;
        SerializedProperty injectWay;
        SerializedProperty sssStrenth;
        Styles _styles = new Styles();

        private void OnEnable()
        {
            preferCompute = serializedObject.FindProperty("preferCompute");
            injectWay = serializedObject.FindProperty("injectWay");
            sssStrenth = serializedObject.FindProperty("SSS_Strenth");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(preferCompute, _styles.preferCompute);
            EditorGUILayout.PropertyField(injectWay,_styles.injectWay);

            // 只有当 injectWay 为 DeferredComposition 时才显示 SSS_Strenth
            if (injectWay.enumValueIndex == (int)UInjectWay.DeferredComposition)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(sssStrenth,_styles.sssStrenth);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private sealed class Styles
        {
            public readonly GUIContent preferCompute = new("是否使用Computeshader加速");
            public readonly GUIContent injectWay = new("注入pass方式");
            public readonly GUIContent sssStrenth = new("散射强度");
        }
    }
}