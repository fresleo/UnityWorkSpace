using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace XKnight.XLOD
{
    [InitializeOnLoad]
    public static class EffectLODToolbar
    {
        private static readonly Type s_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject s_currentToolbar;
        private static int s_currentToolbarId;
        private static VisualElement s_currentToolbarParent;

        static EffectLODToolbar()
        {
            s_currentToolbar = null;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            AddToolbarButtons();
        }

        private static void AddToolbarButtons()
        {
            if (s_currentToolbar != null) return;
            
            var toolbars = Resources.FindObjectsOfTypeAll(s_toolbarType);
            s_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (s_currentToolbar == null) return;
            
            if (s_currentToolbarParent != null && s_currentToolbarId != s_currentToolbar.GetInstanceID())
            {
                s_currentToolbarId = s_currentToolbar.GetInstanceID();

                s_currentToolbarParent.RemoveFromHierarchy();
                s_currentToolbarParent = null;
            }

            if (s_currentToolbarParent != null) return;
            
            FieldInfo root = s_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            if (root == null) return;

            VisualElement rawRoot = root.GetValue(s_currentToolbar) as VisualElement;
            VisualElement toolbarZone = rawRoot.Q("ToolbarZonePlayMode");

            s_currentToolbarParent = new VisualElement()
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                }
            };
            toolbarZone.Add(s_currentToolbarParent);
            
            IMGUIContainer container = new IMGUIContainer();
            s_currentToolbarParent.Add(container);
            
            container.onGUIHandler -= OnGuiBody;
            container.onGUIHandler += OnGuiBody;
        }
        
        private static string[] s_qualityStrs = { "特效质量: 低", "特效质量: 中", "特效质量: 高" };
        
        private static void OnGuiBody()
        {
            var lastColor = GUI.color;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = Color.red;
                
                int effLOD = LODManager.Inst.effectLOD.GetLOD();
                if (GUILayout.Button(new GUIContent(s_qualityStrs[effLOD])))
                {
                    effLOD = effLOD == (int)EffectQuality.LOW ? (int)EffectQuality.HIGH : effLOD - 1;
                    LODManager.Inst.effectLOD.SetLOD(effLOD);
                }
            }
            GUI.color = lastColor;
        }
        
    }
}