
/// <summary>
/// author :calvin
/// date : 2026-05-26
/// description : SSS LUT图导出工具
/// </summary>


using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace Garena.TA.SSS
{

    public static class SSS_ImageExportTools
    {
        public static string SystemPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        public static bool SaveToAsset(OutputFormat outputFormat, ref Texture2D previewTexture, string path = "Assets/PreIntegratedSkinLUT")
        {

            if (previewTexture == null)
            {
                Debug.LogError("[SSS LUT] No texture to save.");
                return false;
            }
            switch (outputFormat)
            {
                case OutputFormat.PNG:
                    SaveAsPNG(path + ".png", ref previewTexture);
                    PingAsset(path);
                    return true;

                case OutputFormat.EXR:
                    SaveAsExr(path + ".exr", ref previewTexture);
                    PingAsset(path);
                    return true;
                case OutputFormat.Asset:
                    SaveAsAsset(path + ".asset", ref previewTexture);
                    PingAsset(path);
                    return true;
                default:
                    return false;
            }

        }

        private static void PingAsset(string assetPath)
        {
            //加载这个路径的资源
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            //高亮显示这个资源
            if (obj != null) EditorGUIUtility.PingObject(obj);
        }

        // 导出为asset文件
        public static bool SaveAsAsset(string outPutPath, ref Texture2D previewTexture)
        {
            string path = EnsureExtension(outPutPath, ".asset");
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(previewTexture, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"SSS LUT saved to : {path}");
            return true;
        }
        // 导出为png文件
        public static bool SaveAsPNG(string outPutPath, ref Texture2D previewTexture)
        {
            string path = EnsureExtension(outPutPath, ".png");

            Color[] src = previewTexture.GetPixels();
            Color[] dst = new Color[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                dst[i] = new Color(
                    Mathf.Clamp01(src[i].r),
                    Mathf.Clamp01(src[i].g),
                    Mathf.Clamp01(src[i].b),
                    1f);
            }
            previewTexture.SetPixels(dst);
            previewTexture.Apply();
            File.WriteAllBytes(SystemPath(path), previewTexture.EncodeToPNG());
            AssetDatabase.Refresh();
            Debug.Log($"SSS LUT saved to : {path}");
            return true;
        }

        // 导出为exr文件
        public static bool SaveAsExr(string outPutPath, ref Texture2D previewTexture)
        {
            string path = EnsureExtension(outPutPath, ".exr");
            File.WriteAllBytes(SystemPath(path), previewTexture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));

            AssetDatabase.Refresh();
            Debug.Log($"SSS LUT saved to : {path}");
            return true;
        }


        private static string EnsureExtension(string path, string ext)
        {
            if (string.IsNullOrEmpty(path))
                path = "Assets/PreIntegratedSkinLUT";
            if (!path.StartsWith("Assets"))
                path = "Assets/" + path;
            string cur = Path.GetExtension(path);
            if (string.IsNullOrEmpty(cur) ||
                !cur.Equals(ext, System.StringComparison.OrdinalIgnoreCase))
                path = Path.ChangeExtension(path, ext);
            return path.Replace('\\', '/');
        }
    }
}

