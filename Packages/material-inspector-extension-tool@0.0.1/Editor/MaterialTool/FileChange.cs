using System;
using System.IO;
using UnityEditor;

namespace MaterialInspectorExtensionTool.Editor.MaterialTool
{
    /// <summary>
    /// 监测配置文件的改动
    /// </summary>
    static class FileChange
    {
        // 监控配置文件是否有改动
        [InitializeOnLoadMethod]
        static void FileChangeWatcher()
        {
            var watcher = new FileSystemWatcher(PreferencesPath); 
            watcher.BeginInit();
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += OnChange;
            watcher.EndInit();
        }

        public static string PreferencesPath
        {
            get
            {
                // 必须是文件夹 不然有的机器会有问题
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Editor-5.x/Preferences");
                return path;
            }
        }

        private static void OnChange(object sender, FileSystemEventArgs e)
        {
            PaseFavoritePathExtension.ReadYAML();
        }
    }
}