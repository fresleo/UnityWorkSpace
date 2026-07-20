#if UNITY_EDITOR

/*******************************************************************************
 * File: LightmapGroupEditorUtils.cs
 * Author: junwei.li
 * Date: 2026/04/22 14:17
 * Description: Editor下使用的lightmap bake tag设置
 *
 * Notice: 
*******************************************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.Lightmap
{
    public static class LightmapGroupEditorUtils
    {

        /// <summary>
        /// 设置renderer bake tag
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="tag"></param>
        public static void SetRendererBakeTag(MeshRenderer renderer, int tag)
        {
            if (renderer == null)
            {
                return;
            }
            
            SerializedObject so = new SerializedObject(renderer);
            SerializedProperty prop = so.FindProperty("m_LightmapParameters");
            LightmapParameters lmp = null;
            if (prop != null && prop.objectReferenceValue != null)
            {
                lmp = (LightmapParameters)prop.objectReferenceValue;
            }

            if (lmp == null)
            {
                lmp = new LightmapParameters();
            }

            if (lmp.bakedLightmapTag == tag)
            {
                return;
            }
            
            var oldPath = AssetDatabase.GetAssetPath(lmp.GetInstanceID());
            var newPath = GetNewLightmapParametersPath(oldPath, tag);
            if (File.Exists(newPath))
            {
                var newLmp = AssetDatabase.LoadAssetAtPath<LightmapParameters>(newPath);
                prop.objectReferenceValue = newLmp;
                so.ApplyModifiedProperties();
            }
            else
            {
                var newLmp = Object.Instantiate(lmp);
                newLmp.bakedLightmapTag = tag;
                prop.objectReferenceValue = newLmp;
                so.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(newLmp, newPath);
            }
        }

        /// <summary>
        /// 获取新的Parameters路径
        /// </summary>
        /// <param name="oldPath"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static string GetNewLightmapParametersPath(string oldPath, int tag)
        {
            //如果旧路径为空，则去当前scene目录
            if (string.IsNullOrEmpty(oldPath))
            {
                var curScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                var curRootPath = Path.GetDirectoryName(curScenePath);
                return Path.Combine(curRootPath, $"DefaultLightmapParameters_Group_{tag}.giparams");
            }

            var rootPath = Path.GetDirectoryName(oldPath);
            var name = Path.GetFileNameWithoutExtension(oldPath);
            
            if (name.Contains("Group_"))
            {
                var idx = name.LastIndexOf('_');
                name = $"{name.Substring(0,idx)}_{tag}.giparams";
            }
            else
            {
                name = $"{name}_Group_{tag}.giparams";
            }
            var newPath = Path.Combine(rootPath, name);
            return newPath;
        }

    }
}

#endif