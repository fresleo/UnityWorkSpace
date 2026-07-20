using System.IO;
using UnityEngine;
using UnityEditor;

namespace XKT.ShaderVariantLogger
{
    internal class EditorShaderVariantLogger
    {
        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        }

        private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            // 在编辑器 [进入播放模式之前]，[退出编辑模式时] 发生
            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                Execute();
            }
        }

        private static void Execute()
        {
            if (!EditorShaderVariantLoggerConfig.Enabled)
            {
                ShaderVariantLoggerInterface.SetEnable(false);
                return;
            }

            if (!EditorSettings.asyncShaderCompilation)
            {
                EditorSettings.asyncShaderCompilation = true;
                EditorUtility.DisplayDialog("自动修改编辑器设置", "[EditorSettings] Asynchronous Shader Compilation [ Disable -> Enable ]", "OK");
            }

            ShaderUtil.allowAsyncCompilation = true;
            ReinitializeAllShaders(EditorShaderVariantLoggerConfig.ClearShaderCache);
            
            SetupLogger();
        }
        
        
        // 重新初始化所有着色器
        // 注意：有尝试通过单独调用来达到清理缓存的目的，结果发现反而会导致某些变体没有被收集到的情况，所以暂时跟随 EditorApplication.playModeStateChanged 事件来执行是最稳妥的方式
        private static void ReinitializeAllShaders(bool clearShaderCache)
        {
            if (clearShaderCache)
            {
                System.IO.Directory.Delete("Library/ShaderCache", true);
            }
            
            var bindingFlags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var method = typeof(ShaderUtil).GetMethod("ReloadAllShaders", bindingFlags);
            method?.Invoke(null, null);
        }
        
        private static void SetupLogger()
        {
            ShaderVariantLoggerInterface.SetEnable(true);
            ShaderVariantLoggerInterface.SetFrame(0);

            string logSaveDir = EditorShaderVariantLoggerConfig.k_LogSaveDir;
            
            if (!Directory.Exists(logSaveDir))
            {
                Directory.CreateDirectory(logSaveDir);
            }

            var currentTime = System.DateTime.Now;
            string filePath = logSaveDir + "/" + EditorShaderVariantLoggerConfig.FileHeader + currentTime.ToString("_yyyyMMdd_HHmmss") + ".log";
            
            ShaderVariantLoggerInterface.SetupFile(filePath);
        }
        
    }
}