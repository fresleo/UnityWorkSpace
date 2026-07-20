// Created By: WangYu  Date: 2024-05-07

using UnityEditor;
using UnityEngine;

namespace SpecularProbe
{
    [CustomEditor(typeof(SphereSpecularLight))]
    public class SphereSpecularLightInspector : UnityEditor.Editor 
    {
        public override void OnInspectorGUI()
        {
            var obj = base.target;
            var script = obj as SphereSpecularLight;
            if (script != null)
            {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                
                DrawAutoApplyGUI(script);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(obj, $"对象 \"{obj.name}\" 上的 \"{obj.GetType()}\" 脚本发生改变");
                    EditorUtility.SetDirty(obj);
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
            //base.OnInspectorGUI();
        }

        private GUIStyle m_bgGrey;
        
        private void DrawAutoApplyGUI(SphereSpecularLight script)
        {
            if (m_bgGrey == null)
            {
                m_bgGrey = new GUIStyle(EditorStyles.label);
                m_bgGrey.normal.background = Texture2D.linearGrayTexture;
            }
            using (new GUILayout.VerticalScope(m_bgGrey))
            {
                GUILayout.Label("球形高光假灯", EditorStyles.whiteLargeLabel);
            }
            
            EditorGUILayout.Space(5);
            script.intensityMultiplier = EditorGUILayout.FloatField(new GUIContent("强度乘数", "建议小灯调大，大灯调小"), script.intensityMultiplier);
            
            EditorGUILayout.Space(5);
            script.radius = EditorGUILayout.FloatField(new GUIContent("半径", "高光灯的球形几何半径"), script.radius);
        }
    }
}