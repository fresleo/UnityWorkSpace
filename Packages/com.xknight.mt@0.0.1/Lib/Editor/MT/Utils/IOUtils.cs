// Created By: WangYu  Date: 2023-12-01

using System.IO;
using com.xknight.mt.Lib.Runtime.MT;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace com.xknight.mt.Lib.Editor.MT.Utils
{
    public static class IOUtils
    {
        /// <summary>
        /// 删除目录和它的.meta文件
        /// </summary>
        public static void DeleteDirAndMeta(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
                
                string metaPath = dir + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
        }
        
        /// <summary>
        /// 删除激活场景的输出目录
        /// </summary>
        public static void DeleteActiveSceneOutDir()
        {
            var activeScene = SceneManager.GetActiveScene();
            string sceneName = activeScene.name.ToLower();

            string outDir = string.Format(MTAssetLoadMgr.outSceneDir, sceneName);
            DeleteDirAndMeta(outDir);

            outDir = string.Format(MTAssetLoadMgr.terrainOutDir_Control, sceneName);
            DeleteDirAndMeta(outDir);
            
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 确保活动场景的输出目录存在
        /// </summary>
        /// <param name="meshOutDir">网格输出目录</param>
        /// <param name="dataOutDir">数据输出目录</param>
        /// <param name="binaryOutDir">2进制输出目录</param>
        /// <param name="materialOutDir">材质球输出目录</param>
        /// <param name="controlOutDir">控制图输出目录</param>
        public static void EnsureActiveSceneOutDir(
            out string meshOutDir, out string dataOutDir, 
            out string binaryOutDir, out string materialOutDir, 
            out string controlOutDir)
        {
            var activeScene = SceneManager.GetActiveScene();
            string sceneName = activeScene.name.ToLower();
            
            string outDir;
            
            outDir = string.Format(MTAssetLoadMgr.terrainOutDir_Mesh, sceneName);
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            meshOutDir = outDir;

            outDir = string.Format(MTAssetLoadMgr.terrainOutDir_Data, sceneName);
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            dataOutDir = outDir;

            outDir = string.Format(MTAssetLoadMgr.terrainOutDir_Binary, sceneName);
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            binaryOutDir = outDir;
            
            outDir = string.Format(MTAssetLoadMgr.terrainOutDir_Material, sceneName);
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            materialOutDir = outDir;

            outDir = string.Format(MTAssetLoadMgr.terrainOutDir_Control, sceneName);
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            controlOutDir = outDir;
            
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 完整路径 -> 相对路径
        /// </summary>
        public static string FullPathToRelativePath(string path)
        {
            string rootDir = MTAssetLoadMgr.outRootDir + "/";
            
            if (path.Contains(rootDir))
            {
                string res = path.Replace(rootDir, "");
                return res;
            }

            return path;
        }

        /// <summary>
        /// 完整路径 -> 相对路径
        /// </summary>
        public static string[] FullPathsToRelativePaths(string[] paths)
        {
            string[] array = new string[paths.Length];
            
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                
                array[i] = FullPathToRelativePath(path);
            }

            return array;
        }
        
    }
}