// Created By: WangYu  Date: 2024-07-04

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.MaterialTool
{
    /// <summary>
    /// 材质检查器
    /// </summary>
    public static class MaterialChecker
    {
        private static bool CheckShaderKeywords(Material mat, bool isClear)
        {
            if (mat == null || mat.shader == null)
            {
                return false;
            }

            bool isDirty = false;
            
            // 着色器包含的关键字
            string[] shaderKeys = mat.shader.keywordSpace.keywordNames;

#if LWGUI_EXIST
            LWGUIExt.CheckPropertiesWithLWGUI(mat.shader, out string[] skipKeywords);
#endif // LWGUI_EXIST
            
            string[] matKeys = mat.shaderKeywords;
            foreach (string matKey in matKeys)
            {
#if LWGUI_EXIST
                if (skipKeywords.Contains(matKey))
                {
                    continue;
                }
#endif // LWGUI_EXIST

                bool noUse = !shaderKeys.Contains(matKey);
                if (noUse)
                {
                    isDirty = true;

                    if (!isClear)
                    {
                        break;
                    }

                    mat.DisableKeyword(matKey);
                }
            }

            return isDirty;
        }

        private static bool CheckTextures(SerializedObject so, Material mat, bool isClear)
        {
            if (so == null || mat == null || mat.shader == null)
            {
                return false;
            }

            bool isDirty = false;

            SerializedProperty m_TexEnvs_sp = so.FindProperty("m_SavedProperties.m_TexEnvs");
            for (int i = 0; i < m_TexEnvs_sp.arraySize; i++)
            {
                string propName = m_TexEnvs_sp.GetArrayElementAtIndex(i).displayName;
                // 如果属性不存在
                bool noUse = !mat.HasProperty(propName);
                if (noUse)
                {
                    isDirty = true;

                    if (!isClear)
                    {
                        break;
                    }

                    // 清理附带的贴图和属性
                    mat.SetTexture(propName, null);
                    m_TexEnvs_sp.DeleteArrayElementAtIndex(i);
                    i--;
                }
            }

            return isDirty;
        }

        private static bool CheckFloats(SerializedObject so, Material mat, bool isClear)
        {
            if (so == null || mat == null || mat.shader == null)
            {
                return false;
            }

            bool isDirty = false;

            SerializedProperty m_Floats_sp = so.FindProperty("m_SavedProperties.m_Floats");
            for (int i = 0; i < m_Floats_sp.arraySize; i++)
            {
                string propName = m_Floats_sp.GetArrayElementAtIndex(i).displayName;
                bool noUse = !mat.HasProperty(propName);
                if (noUse)
                {
                    isDirty = true;

                    if (!isClear)
                    {
                        break;
                    }

                    m_Floats_sp.DeleteArrayElementAtIndex(i);
                    i--;
                }
            }

            return isDirty;
        }

        private static bool CheckColors(SerializedObject so, Material mat, bool isClear)
        {
            if (so == null || mat == null || mat.shader == null)
            {
                return false;
            }

            bool isDirty = false;

            SerializedProperty m_Colors_sp = so.FindProperty("m_SavedProperties.m_Colors");
            for (int i = 0; i < m_Colors_sp.arraySize; i++)
            {
                string propName = m_Colors_sp.GetArrayElementAtIndex(i).displayName;
                bool noUse = !mat.HasProperty(propName);
                if (noUse)
                {
                    isDirty = true;

                    if (!isClear)
                    {
                        break;
                    }

                    m_Colors_sp.DeleteArrayElementAtIndex(i);
                    i--;
                }
            }

            return isDirty;
        }

        private static void ApplyModified(SerializedObject so, Material mat, bool saveAndRefresh)
        {
            if (so == null || mat == null)
            {
                return;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(mat);

            // 保存，刷新要看时机的，否则会很慢
            if (saveAndRefresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"清理了材质球: {mat.name}");
        }

        public static bool CheckMaterial(Material mat, bool isClear, bool saveAndRefresh)
        {
            if (mat == null)
            {
                return false;
            }

            bool isDirty = false;

            SerializedObject so = new SerializedObject(mat);

            isDirty |= CheckShaderKeywords(mat, isClear);
            isDirty |= CheckTextures(so, mat, isClear);
            isDirty |= CheckFloats(so, mat, isClear);
            isDirty |= CheckColors(so, mat, isClear);

            if (isDirty && isClear)
            {
                ApplyModified(so, mat, saveAndRefresh);
            }

            return isDirty;
        }

        private const string k_MenuItemName = "AssetBuild/资源预处理/清理目标目录下材质球上的无效数据和引用";
        [MenuItem(k_MenuItemName)]
        public static void CleanMaterialsInProject()
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material");
            for (int i = 0, imax = matGuids.Length; i < imax; i++)
            {
                string matGuid = matGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(matGuid);
                EditorUtility.DisplayProgressBar("检查材质...", assetPath, i / (float)imax);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                CheckMaterial(mat, true, false); // 批量处理时，先标脏，后保存
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
    }
}