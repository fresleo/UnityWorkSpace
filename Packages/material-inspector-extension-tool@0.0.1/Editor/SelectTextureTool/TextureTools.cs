using MaterialInspectorExtensionTool.Editor.PublicExtension;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public abstract class TextureTools : GUIBase
    {
        public bool ToggleValue { set; get; }
        
        protected abstract GUIContent ToggleContent { set; get; }
        
        private GUIStyle m_toggleStyle = new GUIStyle("AppToolbarButtonLeft");
        
        protected bool IsToggleChange { set; get; }

        public void ShowToggle()
        {
            EditorGUI.BeginChangeCheck();
            {
                ToggleValue = GUILayout.Toggle(ToggleValue, ToggleContent, m_toggleStyle, GUILayout.Height(20), GUILayout.Width(24));
            }
            IsToggleChange = EditorGUI.EndChangeCheck();
            
            if (IsToggleChange)
            {
                if (ToggleValue)
                {
                    OnEnable();
                }
                else
                {
                    OnDispose();
                }
            }
        }

        protected override void OnDispose()
        {
        }

        protected virtual void OnEnable()
        {
        }
        
    }
}