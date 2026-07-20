using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool.Example
{
    //[CustomEditor(typeof(Material))]
    public class SelectTextureCustomMaterialInspector : MaterialEditor
    {
        // 反射 m_CustomShaderGUI 字段
        private static FieldInfo s_myCustomShaderGUI;
        // 用于替换的 ShaderGUI
        private ShaderGUI m_shaderGUI = Activator.CreateInstance(typeof(CustomShaderGUI)) as ShaderGUI;


        [InitializeOnLoadMethod]
        private static void GetShaderGUI()
        {
            var o = typeof(MaterialEditor);
            s_myCustomShaderGUI = o.GetField("m_CustomShaderGUI", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            s_myCustomShaderGUI.SetValue(this, m_shaderGUI);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (this.customShaderGUI == null)
            {
                s_myCustomShaderGUI.SetValue(this, m_shaderGUI);
            }
        }
    }
}