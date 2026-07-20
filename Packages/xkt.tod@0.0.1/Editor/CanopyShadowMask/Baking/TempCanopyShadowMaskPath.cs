/*******************************************************************************
 * File: TempCanopyShadowMaskPath.cs
 * Author: WangYu
 * Date: 2026-07-06
 * Description: 
 ******************************************************************************/

using System.IO;
using UnityEngine;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 树冠 Shadowmask 临时文件目录（在 Library 里）。
    /// </summary>
    internal static class TempCanopyShadowMaskPath
    {
        private const string C_TEMP_FOLDER_NAME = "CanopyShadowMaskTemp";

        /// <summary>
        /// 创建临时可读文件路径
        /// </summary>
        /// <param name="sourceAssetPath">源纹理资源路径</param>
        /// <returns>临时文件绝对路径</returns>
        public static string CreateTempReadableFilePath(string sourceAssetPath)
        {
            string tempRootFullPath = GetLibrarySubFolderFullPath(C_TEMP_FOLDER_NAME);
            EnsureDirectoryExists(tempRootFullPath);
            
            string sourceFileName = Path.GetFileName(sourceAssetPath);
            if (string.IsNullOrEmpty(sourceFileName))
            {
                sourceFileName = "shadowmask.png";
            }

            string fileName = "read_" + System.Guid.NewGuid().ToString("N") + "_" + sourceFileName;

            string tempPath = Path.Combine(tempRootFullPath, fileName);
            return tempPath;
        }

        private static string GetLibrarySubFolderFullPath(string folderName)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string folder = Path.Combine(projectRoot, "Library", folderName);
            return folder;
        }

        // 确保目录存在
        private static void EnsureDirectoryExists(string folderFullPath)
        {
            if (string.IsNullOrEmpty(folderFullPath))
            {
                return;
            }

            if (!Directory.Exists(folderFullPath))
            {
                Directory.CreateDirectory(folderFullPath);
            }
        }

    }
}
