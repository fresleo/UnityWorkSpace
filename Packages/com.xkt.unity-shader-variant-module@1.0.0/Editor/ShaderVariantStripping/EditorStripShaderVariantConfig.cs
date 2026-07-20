using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using XKT.ShaderVariantStripping.RemoveShaderPostProcessor;
using XKT.ShaderVariantStripping.Shared;

namespace XKT.ShaderVariantStripping
{
    public class EditorStripShaderVariantConfig
    {
        private const string k_WorkingDir = "XKT/ShaderVariantStripping";
        private const string k_ConfigFile = k_WorkingDir + "/config.json";

        [Serializable]
        private struct ConfigData
        {
            /// <summary>
            /// 总开关
            /// </summary>
            public bool enabled;
            
            /// <summary>
            /// 启用日志
            /// </summary>
            public bool logEnabled;
            
            /// <summary>
            /// 严格的变体剥离
            /// </summary>
            public bool strictVariantStripping;
            
            /// <summary>
            /// 禁用 Unity 的变体剥离
            /// </summary>
            public bool disableUnityStrip;
            
            /// <summary>
            /// 脚本执行顺序
            /// </summary>
            public int order;
            
            /// <summary>
            /// 忽略仅在特定阶段中使用的关键字（剥离条件更宽松，能减少被误剥离的可能性）
            /// </summary>
            public bool ignoreStageOnlyKeyword;
            
            /// <summary>
            /// 排除变体集合路径
            /// </summary>
            public List<string> excludeVariantCollection;
        }

        private static ConfigData s_currentConfig;
        
        
        private static bool m_shouldRemoveOthers;
        
        // 检查是否应该移除其它的着色器处理器
        private static void CheckShouldRemoveOthers()
        {
            m_shouldRemoveOthers = s_currentConfig.enabled & s_currentConfig.strictVariantStripping & s_currentConfig.disableUnityStrip;
            StripShaderConfigBridge.ShouldRemoveOthers = m_shouldRemoveOthers;
        }
        
        
        /// <summary>
        /// 总开关
        /// </summary>
        public static bool IsEnable
        {
            get => s_currentConfig.enabled;
            set
            {
                CheckShouldRemoveOthers();
                bool backupFlag = m_shouldRemoveOthers;
                
                s_currentConfig.enabled = value;
                SaveConfigData();

                CheckShouldRemoveOthers();
                if (backupFlag != m_shouldRemoveOthers)
                {
                    ReloadCode();
                }
            }
        }

        /// <summary>
        /// 启用日志
        /// </summary>
        public static bool IsLogEnable
        {
            get => s_currentConfig.logEnabled;
            set
            {
                s_currentConfig.logEnabled = value;
                SaveConfigData();
            }
        }
        
        /// <summary>
        /// 严格的变体剥离
        /// </summary>
        public static bool StrictVariantStripping
        {
            get => s_currentConfig.strictVariantStripping;
            set
            {
                CheckShouldRemoveOthers();
                bool backupFlag = m_shouldRemoveOthers;

                bool inputValue = value;
                s_currentConfig.strictVariantStripping = inputValue;
                if (!inputValue)
                {
                    DisableUnityStrip = false;
                }
                SaveConfigData();
                
                CheckShouldRemoveOthers();
                if (backupFlag != m_shouldRemoveOthers)
                {
                    ReloadCode();
                }
            }
        }
        
        /// <summary>
        /// 禁用 Unity 的变体剥离
        /// </summary>
        public static bool DisableUnityStrip
        {
            get => s_currentConfig.disableUnityStrip;
            set
            {
                CheckShouldRemoveOthers();
                var backupFlag = m_shouldRemoveOthers;
                
                s_currentConfig.disableUnityStrip = value;
                SaveConfigData();
                
                CheckShouldRemoveOthers();
                if (backupFlag != m_shouldRemoveOthers)
                {
                    ReloadCode();
                }
            }
        }
        
        /// <summary>
        /// 脚本执行顺序
        /// </summary>
        public static int Order
        {
            get => s_currentConfig.order;
            set
            {
                s_currentConfig.order = value;
                SaveConfigData();
            }
        }
        
        /// <summary>
        /// 忽略仅在特定阶段中使用的关键字（剥离条件更宽松，能减少被误剥离的可能性）
        /// </summary>
        public static bool IgnoreStageOnlyKeyword
        {
            get => s_currentConfig.ignoreStageOnlyKeyword;
            set
            {
                s_currentConfig.ignoreStageOnlyKeyword = value;
                SaveConfigData();
            }
        }

        
        // 重新加载代码
        private static void ReloadCode()
        {
            var targets = RecompileAssemblyUtility.GetRecompileTarget(true);
            if (targets.Count <= 0)
            {
                return;
            }

            var target = targets[0];
            targets.RemoveAt(0);
            if (targets.Count > 0)
            {
                RecompileAssemblyUtility.WriteFile(RecompileAssemblyUtility.s_tempCompileTargetFile, targets);
            }
            
            AssetDatabase.ImportAsset(target.asmDefPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"重新编译程序集: {target.asmName}");
        }

        [InitializeOnLoadMethod]
        public static void Init()
        {
            // string fullPath = Path.GetFullPath(c_configFile);
            // Debug.LogError($"配置的完整路径: {fullPath}");
            
            // 找不到配置，自己初始化设置
            if (!File.Exists(k_ConfigFile))
            {
                s_currentConfig = new ConfigData()
                {
                    enabled = false,
                    logEnabled = false,
                    strictVariantStripping = false,
                    disableUnityStrip = false,
                    order = int.MinValue,
                };
                CheckShouldRemoveOthers();
                
                return;
            }

            s_currentConfig = ReadConfigData();
            CheckShouldRemoveOthers();
            
            EditorApplication.delayCall += () =>
            {
                var targets = RecompileAssemblyUtility.ReadFromFile(RecompileAssemblyUtility.s_tempCompileTargetFile);
                if (targets.Count <= 0)
                {
                    return;
                }

                var target = targets[0];
                targets.RemoveAt(0);
                
                RecompileAssemblyUtility.WriteFile(RecompileAssemblyUtility.s_tempCompileTargetFile, targets);
                
                AssetDatabase.ImportAsset(target.asmDefPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"重新编译程序集: {target.asmName}");
            };
        }

        // 读取配置数据
        private static ConfigData ReadConfigData()
        {
            string str = File.ReadAllText(k_ConfigFile);
            var data = JsonUtility.FromJson<ConfigData>(str);
            return data;
        }

        // 保存配置数据
        private static void SaveConfigData()
        {
            string dir = Path.GetDirectoryName(k_ConfigFile);
            // Debug.LogError($"保存目录: {dir}");
            
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            string jsonStr = JsonUtility.ToJson(s_currentConfig);
            File.WriteAllText(k_ConfigFile, jsonStr);
        }

        /// <summary>
        /// 获取排除的变体集合资源
        /// </summary>
        public static List<ShaderVariantCollection> GetExcludeVariantCollectionAsset()
        {
            var list = new List<ShaderVariantCollection>();
            
            if (s_currentConfig.excludeVariantCollection != null)
            {
                foreach (var collectionPath in s_currentConfig.excludeVariantCollection)
                {
                    var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(collectionPath);
                    list.Add(svc);
                }
            }

            return list;
        }

        /// <summary>
        /// 设置排除的变体集合资源
        /// </summary>
        public static void SetExcludeVariantCollection(List<ShaderVariantCollection> list)
        {
            var assetPaths = new List<string>();
            foreach (var item in list)
            {
                string assetPath = AssetDatabase.GetAssetPath(item);
                assetPaths.Add(assetPath);
            }

            if (s_currentConfig.excludeVariantCollection == null)
            {
                s_currentConfig.excludeVariantCollection = new List<string>();
            }

            if (!IsSameList(assetPaths, s_currentConfig.excludeVariantCollection))
            {
                s_currentConfig.excludeVariantCollection = assetPaths;
                SaveConfigData();
            }
        }

        // 集合是否相同
        private static bool IsSameList(List<string> src1, List<string> src2)
        {
            if (src1 == null || src2 == null)
            {
                return false;
            }

            if (src1.Count != src2.Count)
            {
                return false;
            }

            for (int i = 0; i < src1.Count; ++i)
            {
                if (src1[i] != src2[i])
                {
                    return false;
                }
            }

            return true;
        }
        
    }
}