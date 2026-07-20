using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class AssetPostprocessorExtension : AssetPostprocessor
    {
        /// <summary>
        /// 有贴图文件导入时，将路径包含贴图路径的文件夹的 lod 状态置成 false
        /// 删除时也会调用这个
        /// </summary>
        /// <param name="texture"></param>
        private void OnPostprocessTexture(Texture2D texture)
        {
            if (SelectTextureWindow.s_windowData == null)
            {
                return;
            }
            
            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < SelectTextureWindow.s_windowData.paths.Count; i++)
            {
                string path = SelectTextureWindow.s_windowData.paths[i];
                if (assetPath.Contains(path) && SelectTextureWindow.drawTextures[i] != null)
                {
                    SelectTextureWindow.drawTextures[i].isLoaded = false;
                }
            }
            
            sw.Stop();
            Debug.LogWarning($"[AssetPostprocessor] AssetPostprocessorExtension PostprocessAllAssets cost {sw.ElapsedMilliseconds} ms");
        }
    }
}