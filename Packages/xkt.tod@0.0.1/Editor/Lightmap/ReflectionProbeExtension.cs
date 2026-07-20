// Created By: WangYu  Date: 2025-06-28

using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using XKT.TOD.Utils;

namespace XKT.TOD.Lightmap
{
    [CustomEditor(typeof(ReflectionProbe))]
    public class ReflectionProbeExtension : Editor
    {
        private ReflectionProbe CurrentTarget => this.target as ReflectionProbe;
        private Editor m_editor;
        private BindingFlags m_methodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;


        private void OnDestroy()
        {
            if (m_editor != null)
            {
                Editor.DestroyImmediate(m_editor);
                m_editor = null;
            }
        }

        private void OnEnable()
        {
            if (m_editor == null)
            {
                var assembly = Assembly.GetAssembly(typeof(Editor));
                var type1 = assembly.GetType("UnityEditor.ReflectionProbeEditor", true);
                m_editor = Editor.CreateEditor(target, type1);
            }

            if (m_editor)
            {
                var method = m_editor.GetType().GetMethod("OnEnable", m_methodFlags);
                method?.Invoke(m_editor, null);
            }
        }
        
        private void OnDisable()
        {
            if (m_editor)
            {
                var method = m_editor.GetType().GetMethod("OnDisable", m_methodFlags);
                method?.Invoke(m_editor, null);
            }
        }
        
        private void OnSceneGUI()
        {
            if (m_editor)
            {
                var method = m_editor.GetType().GetMethod("OnSceneGUI", m_methodFlags);
                method?.Invoke(m_editor, null);
            }
        }
        
        
        public override bool HasPreviewGUI()
        {
            if (m_editor)
            {
                var method = m_editor.GetType().GetMethod("HasPreviewGUI", m_methodFlags);
                var returnValue = method?.Invoke(m_editor, null);
                return returnValue != null && (bool)returnValue;
            }
            
            return base.HasPreviewGUI();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            // base.OnPreviewGUI(r, background);
            
            if (m_editor)
            {
                var method = m_editor.GetType().GetMethod("OnPreviewGUI", m_methodFlags);
                method?.Invoke(m_editor, new object[]{r, background});
            }
        }
        
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            if(!CurrentTarget) return;
            
            m_editor.OnInspectorGUI(); // 原始的 Inspector GUI
            
            EditorGUIDrawHelper.DividerLine(Color.yellow); // 分割线
            
            serializedObject.Update();
            DrawCustomGUI();
            serializedObject.ApplyModifiedProperties();
        }

        
        private void DrawCustomGUI()
        {
            if (GUILayout.Button("先切 ShadowOnly 再 Bake"))
            {
                // 切 ShadowOnly 对象
                var components = TODUtils.FindObjectsOfTypeInActiveScene<AutoSwitchOfShadowOnlyAndGI>();
                foreach (var component in components)
                {
                    component.BakeStarted();
                }

                // 触发反射探针的 bake
                bool result = RLightmapping.BakeReflectionProbeSnapshot(CurrentTarget);

                // 把 ShadowOnly 再切回来
                UnityEditor.EditorApplication.delayCall += ResetAllAutoSwitchOfShadowOnlyAndGI;
            }

            EditorGUILayout.Space();
        }

        private static void ResetAllAutoSwitchOfShadowOnlyAndGI()
        {
            var components = TODUtils.FindObjectsOfTypeInActiveScene<AutoSwitchOfShadowOnlyAndGI>();
            foreach (var component in components)
            {
                component.BakeCompleted();
            }
        }
        
    }
}