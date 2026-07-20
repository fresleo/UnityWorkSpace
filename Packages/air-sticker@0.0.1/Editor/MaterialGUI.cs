// Created By: WangYu  Date: 2025-02-19

using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AirSticker
{
    public class MaterialGUI
    {
        private MaterialEditor m_materialEditor;
        private Material m_lastMaterial;
        
        public void OnInspectorGUI(Material material)
        {
            // 记录材质的更新，并创建材质编辑器对象
            if (material != m_lastMaterial)
            {
                if (m_materialEditor != null)
                {
                    UnityObject.DestroyImmediate(m_materialEditor);
                }
                if (material != null)
                {
                    m_materialEditor = (MaterialEditor)Editor.CreateEditor(material);
                }

                m_lastMaterial = material;
            }

            // 显示材质的检查器菜单
            if (m_materialEditor != null)
            {
                EditorGUILayout.Separator();
                m_materialEditor.DrawHeader();

                // 如果是 unity 内置的材质，则不允许修改
                bool isDefaultMaterial = !AssetDatabase.GetAssetPath(material).StartsWith("Assets");
                using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                {
                    m_materialEditor.OnInspectorGUI();
                }
            }
        }
        
    }
}