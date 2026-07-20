// Created by: WangYu   Date: 2025-10-22

namespace XKT.ShaderVariantLogger
{
    public partial class EditorShaderVariantLoggerConfig
    {
        /// <summary>
        /// 启动标记
        /// </summary>
        public static bool Enabled
        {
            get => s_currentConfig.enabled;
            set
            {
                var old = s_currentConfig.enabled;
                s_currentConfig.enabled = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }

        /// <summary>
        /// 清理着色器缓存
        /// </summary>
        internal static bool ClearShaderCache
        {
            get => s_currentConfig.clearShaderCache;
            set
            {
                var old = s_currentConfig.clearShaderCache;
                s_currentConfig.clearShaderCache = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }

        /// <summary>
        /// 文件头
        /// </summary>
        internal static string FileHeader
        {
            get => s_currentConfig.fileHeader;
            set
            {
                var old = s_currentConfig.fileHeader;
                s_currentConfig.fileHeader = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }


        internal static string SvcGuid
        {
            get => s_currentConfig.svcGuid;
            set
            {
                var old = s_currentConfig.svcGuid;
                s_currentConfig.svcGuid = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }
        
        internal static bool IncludeAssets
        {
            get => s_currentConfig.includeAssets;
            set
            {
                var old = s_currentConfig.includeAssets;
                s_currentConfig.includeAssets = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }

        internal static bool IncludePackages
        {
            get => s_currentConfig.includePackages;
            set
            {
                var old = s_currentConfig.includePackages;
                s_currentConfig.includePackages = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }

        internal static bool IncludeUnityBuiltIn
        {
            get => s_currentConfig.includeUnityBuiltIn;
            set
            {
                var old = s_currentConfig.includeUnityBuiltIn;
                s_currentConfig.includeUnityBuiltIn = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }

        internal static bool IncludeUnityBuiltinExtra
        {
            get => s_currentConfig.includeUnityBuiltinExtra;
            set
            {
                var old = s_currentConfig.includeUnityBuiltinExtra;
                s_currentConfig.includeUnityBuiltinExtra = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }

        internal static bool IncludeOthers
        {
            get => s_currentConfig.includeOthers;
            set
            {
                var old = s_currentConfig.includeOthers;
                s_currentConfig.includeOthers = value;
                if (old != value)
                {
                    SaveConfigData();
                }
            }
        }
        
    }
}