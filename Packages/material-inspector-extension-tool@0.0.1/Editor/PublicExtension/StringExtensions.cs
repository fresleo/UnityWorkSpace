using System.IO;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.PublicExtension
{
    public static class StringExtensions
    {
        private static string SubAssetPath(string rawAssetPath)
        {
            string assetPath = rawAssetPath.Substring(Application.dataPath.Length, rawAssetPath.Length - Application.dataPath.Length);
            return assetPath;
        }
        
        public static string ToAssetPath(this string rawAssetPath)
        {
            if (rawAssetPath.Contains(Application.dataPath))
            {
                return "Assets" + SubAssetPath(rawAssetPath);
            }

            return rawAssetPath;
        }

        public static string TryToAssetPath(this string rawAssetPath, out bool result)
        {
            if (rawAssetPath.Contains(Application.dataPath))
            {
                result = true;
                return "Assets" + SubAssetPath(rawAssetPath);
            }

            result = false;
            return rawAssetPath;
        }

        public static bool IsDirectory(this string self)
        {
            FileInfo fileInfo = new FileInfo(self);
            if ((fileInfo.Attributes & FileAttributes.Directory) != 0)
            {
                return true;
            }

            return false;
        }
    }
}