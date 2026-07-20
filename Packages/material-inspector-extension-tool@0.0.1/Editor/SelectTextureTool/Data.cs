using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class Data
    {
        public const string c_dataFileName = "SelectTextureWindowData.asset";
        private const string c_iconFileName = "SelectTextureWindowIcon";
        
        //public static string DataPath;
        public SelectTextureWindowData selectTextureWindowData;
        
        // public Data()
        // {
        //     SelectTextureWindowData =  AssetDatabase.LoadAssetAtPath<SelectTextureWindowData>(DataPath);
        // }
        
        /// <summary>
        /// 添加路径和名字数据
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="name">别名</param>
        public void SavePathDataInAsset(string path, string name = "null")
        {
            // 没写名字就读取文件夹的名字
            if (name == "null")
            {
                name = path.Substring(path.LastIndexOf('/') + 1);
            }

            selectTextureWindowData.names.Add(name);
            selectTextureWindowData.paths.Add(path);
        }

        /// <summary>
        /// 创建默认数据，默认添加文件夹 “Assets” 名字 “Assets” ，data 创建在 Assets
        /// </summary>
        /// <returns>返回成功创建数据的全路径</returns>
        public static string CreateDefaultData(Material material)
        {
            string dataPath = GetDataPath();
            
            SelectTextureWindowData data = ScriptableObject.CreateInstance<SelectTextureWindowData>();
            data.names = new List<string>();
            data.paths = new List<string>();
            data.seachString = new List<string>();
            //data.NowMaterial=material;
            data.textureSize = 85f;
            data.names.Add("Assets");
            data.paths.Add("Assets");
            data.windowBackgroundColor = new Color(0, 0, 0, 0);
            data.splitSize = 150;

            AssetDatabase.CreateAsset(data, dataPath + "/" + c_dataFileName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return dataPath;
        }

        public static string GetDataPath()
        {
            var iconT2d = Resources.Load<Texture2D>(c_iconFileName);
            string iconAssetPath = AssetDatabase.GetAssetPath(iconT2d);
            string dir = Path.GetDirectoryName(iconAssetPath);
            
            return dir;
        }

        /// <summary>
        /// 尝试获取数据路径，如果有就返回true并额外返回全路径
        /// </summary>
        /// <param name="path">数据全路径</param>
        /// <returns>是否存在数据</returns>
        // public  static bool TryGetDataPath(out string path)
        // {
        //     var s = GetPath.GetScriptPath("SelectTextureWindow");
        //     if (s != null)
        //     {
        //         if (File.Exists(s + @"/SelectTextureWindowData.asset"))
        //         {
        //             path = s + @"/SelectTextureWindowData.asset";
        //             return true;
        //         }
        //         else if (File.Exists("Assets/SelectTextureWindowData.asset"))
        //         {
        //             path = "Assets/SelectTextureWindowData.asset";
        //             return true;
        //         }
        //     }
        //
        //     path = null;
        //     return false;
        // }

        /// <summary>
        /// 读取数据并返回其中的数据
        /// </summary>
        /// <param name="path">数据全路径</param>
        /// <param name="names">返回的名字数组</param>
        /// <param name="paths">返回的路径数组</param>
        // public  void ReadData(ref List<string> names, ref List<string> paths, ref float pix)
        // {
        //     names.AddRange(SelectTextureWindowData.Names);
        //     paths.AddRange(SelectTextureWindowData.Paths);
        //     pix = SelectTextureWindowData.TextureSize;

        // }

        // public  void ChangeTextureSizeData(float pix)
        // {
        //     SelectTextureWindowData data = AssetDatabase.LoadAssetAtPath<SelectTextureWindowData>(DataPath);

        //     data.TextureSize = pix;

        //     // AssetDatabase.DeleteAsset(dataPath);
        //     AssetDatabase.CreateAsset(data, DataPath);
        //     AssetDatabase.Refresh();
        // }
        
    }
}