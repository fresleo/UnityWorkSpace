// Created By: WangYu  Date: 2024-05-07

using UnityEditor;
using UnityEngine;

namespace SpecularProbe
{
    [CustomEditor(typeof(SpecularProbeRenderer))]
    public class SpecularProbeRendererInspector : UnityEditor.Editor 
    {
        public override void OnInspectorGUI()
        {
            var obj = base.target;
            var script = obj as SpecularProbeRenderer;
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
        
        private void DrawAutoApplyGUI(SpecularProbeRenderer script)
        {
            if (m_bgGrey == null)
            {
                m_bgGrey = new GUIStyle(EditorStyles.label);
                m_bgGrey.normal.background = Texture2D.linearGrayTexture;
            }
            using (new GUILayout.VerticalScope(m_bgGrey))
            {
                GUILayout.Label("高光探针渲染器", EditorStyles.whiteLargeLabel);
            }
            
            EditorGUILayout.Space(5);
            script.radius = EditorGUILayout.FloatField(new GUIContent("半径", "将仅为该半径内的光源绘制镜面高光"), script.radius);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("注意：大多数情况下，你都不需要手动进行烘焙操作，因为在场景的 Lightmap 烘焙完成后，会自动进行高光的烘焙，除非你想要在不烘焙 Lightmap 的情况下，提前验证一下高光效果", MessageType.Warning);
            
            EditorGUILayout.Space(5);
            if (GUILayout.Button("烘焙高光探针"))
            {
                script.Bake();
            }
            
            EditorGUILayout.Space(5);
            if (GUILayout.Button("烘焙当前场景中所有的高光探针"))
            {
                script.BakeAll();
            }
        }
    }
}