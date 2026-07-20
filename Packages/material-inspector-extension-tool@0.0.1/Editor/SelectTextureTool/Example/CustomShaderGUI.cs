using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool.Example
{
    public class CustomShaderGUI : ShaderGUI
    {
        private MaterialProperty.PropFlags m_flags = MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUIUtility.fieldWidth = 64f;
            EditorGUIUtility.labelWidth = Screen.width - EditorGUIUtility.fieldWidth - 40f;

            // 遍历材质球上的所有属性
            for (int i = 0; i < properties.Length; i++)
            {
                if ((properties[i].flags & m_flags) == 0)
                {
                    float propertyHeight = materialEditor.GetPropertyHeight(properties[i], properties[i].displayName);
                    Rect controlRect = EditorGUILayout.GetControlRect(true, propertyHeight, EditorStyles.layerMaskField);

                    // 当是纹理属性时
                    if (properties[i].type == MaterialProperty.PropType.Texture)
                    {
                        materialEditor.ShaderProperty(controlRect, properties[i], properties[i].displayName);

                        var selectRect = controlRect;
                        selectRect.width = 50;
                        selectRect.height = 15;
                        selectRect.x = controlRect.xMax - 115;

                        if (GUI.Button(selectRect, "Select"))
                        {
                            Material material = materialEditor.target as Material;
                            string propertyName = properties[i].name;
                            SelectTextureWindow.Open(material, propertyName);
                        }
                    }
                    else
                    {
                        materialEditor.ShaderProperty(controlRect, properties[i], properties[i].displayName);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // 公共选项
            if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
            {
                materialEditor.RenderQueueField();
            }
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }
    }
}