/********************************************************
 * File:    T4MControlMapService.cs
 * Description: T4M 控制图服务（创建/保存/导入设置）
 *********************************************************/

using System.IO;
using UnityEditor;
using UnityEngine;

namespace T4MEditor.Services
{
    /// <summary>
    /// 控制图服务，处理控制图的创建、保存和导入设置
    /// </summary>
    public static class T4MControlMapService
    {
        /// <summary>
        /// 创建空白控制图
        /// </summary>
        /// <param name="path">保存路径</param>
        /// <param name="size">纹理尺寸</param>
        /// <param name="defaultColor">默认颜色（通常为红色表示第一层）</param>
        /// <returns>创建的纹理</returns>
        public static Texture2D CreateControlMap(string path, int size = 512, Color? defaultColor = null)
        {
            Color fillColor = defaultColor ?? new Color(1, 0, 0, 0);

            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, true);
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = fillColor;
            }

            texture.SetPixels(colors);

            SaveControlMap(texture, path);

            return texture;
        }

        /// <summary>
        /// 保存控制图到文件
        /// </summary>
        /// <param name="texture">要保存的纹理</param>
        /// <param name="path">保存路径</param>
        /// <returns>是否成功</returns>
        public static bool SaveControlMap(Texture2D texture, string path)
        {
            if (texture == null)
            {
                Debug.LogError("[T4MControlMapService] 纹理为空，无法保存");
                return false;
            }

            try
            {
                byte[] bytes = texture.EncodeToPNG();
                if (bytes == null)
                {
                    Debug.LogError("[T4MControlMapService] 编码 PNG 失败");
                    return false;
                }

                // 确保目录存在
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(path, bytes);

                string importPath = path;
                EditorApplication.delayCall += () =>
                {
                    if (!string.IsNullOrEmpty(importPath))
                    {
                        AssetDatabase.ImportAsset(importPath, ImportAssetOptions.ForceUpdate);
                    }
                };

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[T4MControlMapService] 保存控制图失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存已有的控制图（从现有资源路径）
        /// </summary>
        /// <param name="texture">要保存的纹理</param>
        /// <returns>是否成功</returns>
        public static bool SaveExistingControlMap(Texture2D texture)
        {
            if (texture == null) return false;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[T4MControlMapService] 无法获取纹理的资源路径");
                return false;
            }

            return SaveControlMap(texture, assetPath);
        }

        /// <summary>
        /// 配置控制图的 TextureImporter 设置
        /// </summary>
        /// <param name="path">纹理路径</param>
        public static void SetupTextureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[T4MControlMapService] 无法获取 TextureImporter: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Default;

            // 平台设置
            TextureImporterPlatformSettings platformSettings = new TextureImporterPlatformSettings
            {
                overridden = true,
                format = TextureImporterFormat.RGBA32
            };
            importer.SetPlatformTextureSettings(platformSettings);

            // sRGB 必须关闭，否则采样结果会经过伽马矫正导致融合效果错误
            importer.sRGBTexture = false;
            importer.isReadable = true;
            importer.anisoLevel = 9;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;

            importer.SaveAndReimport();
        }

        /// <summary>
        /// 从 Unity Terrain 的 SplatAlpha 创建控制图
        /// </summary>
        /// <param name="terrain">Unity 地形</param>
        /// <param name="outputPath">输出路径</param>
        /// <returns>创建的纹理</returns>
        public static Texture2D CreateFromUnityTerrain(Terrain terrain, string outputPath)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError("[T4MControlMapService] Terrain 或 TerrainData 为空");
                return null;
            }

            string terrainDataPath = AssetDatabase.GetAssetPath(terrain.terrainData);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(terrainDataPath);

            Texture2D splatAlpha = null;
            if (assets != null && assets.Length > 1)
            {
                foreach (var asset in assets)
                {
                    if (asset.name == "SplatAlpha 0")
                    {
                        splatAlpha = asset as Texture2D;
                        break;
                    }
                }
            }

            if (splatAlpha != null)
            {
                // 从现有 SplatAlpha 复制
                byte[] bytes = splatAlpha.EncodeToPNG();
                File.WriteAllBytes(outputPath, bytes);
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            }
            else
            {
                // 创建空白控制图
                CreateControlMap(outputPath, 512);
            }

            SetupTextureImporter(outputPath);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }

        /// <summary>
        /// 创建第二张控制图（用于5-6层）
        /// </summary>
        /// <param name="outputPath">输出路径</param>
        /// <param name="size">纹理尺寸</param>
        /// <returns>创建的纹理</returns>
        public static Texture2D CreateSecondControlMap(string outputPath, int size = 512)
        {
            // 第二张控制图默认全黑（无任何层）
            return CreateControlMap(outputPath, size, new Color(0, 0, 0, 0));
        }

        /// <summary>
        /// 验证纹理是否可用于控制图
        /// </summary>
        /// <param name="texture">要验证的纹理</param>
        /// <returns>验证结果消息，null 表示通过</returns>
        public static string ValidateControlMapTexture(Texture2D texture)
        {
            if (texture == null)
                return "纹理为空";

            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path))
                return "无法获取纹理路径";

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return "无法获取 TextureImporter";

            if (!importer.isReadable)
                return "纹理的 Read/Write 标志未勾选";

            // 检查是否使用了压缩格式
            TextureImporterPlatformSettings settings = importer.GetDefaultPlatformTextureSettings();
            if (settings.format != TextureImporterFormat.RGBA32 &&
                settings.format != TextureImporterFormat.ARGB32 &&
                settings.format != TextureImporterFormat.RGB24 &&
                settings.format != TextureImporterFormat.Automatic)
            {
                return "纹理使用了压缩格式，请使用 RGBA32 或无压缩格式";
            }

            return null;
        }
    }
}
