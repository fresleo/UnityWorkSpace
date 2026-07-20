using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace XKT.ShaderVariantLogger
{
    /// <summary>
    /// 编辑器变体记录器配置
    /// </summary>
    public partial class EditorShaderVariantLoggerConfig
    {
        private const string k_WorkingDir = "XKT/ShaderVariantLogger";
        private const string k_ConfigFile = k_WorkingDir + "/config.json";
        public static readonly string k_LogSaveDir = k_WorkingDir + "/logs";
        
        /// <summary>
        /// 配置数据
        /// </summary>
        [Serializable]
        struct ConfigData
        {
            /// <summary>
            /// 启动标记
            /// </summary>
            public bool enabled;
            /// <summary>
            /// 清理着色器缓存
            /// </summary>
            public bool clearShaderCache;
            /// <summary>
            /// 文件头
            /// </summary>
            public string fileHeader;
            
            public string svcGuid;
            
            public bool includeAssets;
            public bool includePackages;
            public bool includeUnityBuiltIn;
            public bool includeUnityBuiltinExtra;
            public bool includeOthers;
        }
        
        private static ConfigData s_currentConfig;

        
        [InitializeOnLoadMethod]
        internal static void Init()
        {
            if (!File.Exists(k_ConfigFile))
            {
                FileHeader = GetDefaultHeader();
                
                IncludeAssets = true;
                IncludePackages = true;
                return;
            }

            s_currentConfig = ReadConfigData();
            
            // 确保总有1个头存在
            if (string.IsNullOrEmpty(FileHeader))
            {
                FileHeader = GetDefaultHeader();
            }
        }
        
        private static string GetDefaultHeader()
        {
            // 用设备名当文件头
            string txt = SystemInfo.deviceName.Replace("/", "").Replace("\\", "");
            return txt;
        }

        private static void SaveConfigData()
        {
            var str = JsonUtility.ToJson(s_currentConfig);
            string dir = Path.GetDirectoryName(k_ConfigFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(k_ConfigFile, str);
        }
        
        private static ConfigData ReadConfigData()
        {
            string str = File.ReadAllText(k_ConfigFile);
            var data = JsonUtility.FromJson<ConfigData>(str);
            return data;
        }
        
    }
}