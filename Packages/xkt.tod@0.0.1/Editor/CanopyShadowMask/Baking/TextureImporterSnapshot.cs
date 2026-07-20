/*******************************************************************************
 * File: TextureImporterSnapshot.cs
 * Author: WangYu
 * Date: 2026-06-30
 * Description: 
 ******************************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// TextureImporter 设置的快照
    /// </summary>
    internal sealed class TextureImporterSnapshot
    {
        // 平台设置快照
        private sealed class PlatformSnapshot
        {
            /// <summary>
            /// 平台
            /// </summary>
            public string platform;
            
            /// <summary>
            /// 设置
            /// </summary>
            public TextureImporterPlatformSettings settings;
        }

        private string _assetPath;
        
        private bool _isReadable;
        private TextureImporterType _textureType;
        private bool _sRgb;
        private bool _mipmapEnabled;
        private FilterMode _filterMode;
        private TextureWrapMode _wrapMode;
        private int _anisoLevel;
        private int _maxTextureSize;
        private TextureImporterCompression _textureCompression;
        private bool _alphaIsTransparency;
        
        private readonly List<PlatformSnapshot> _platforms = new ();

        private static readonly string[] C_DEFAULT_PLATFORMS =
        {
            "DefaultTexturePlatform",
            "Standalone",
            "Android",
            "iPhone",
            "WebGL"
        };

        /// <summary>
        /// 捕获
        /// </summary>
        public static TextureImporterSnapshot Capture(string assetPath)
        {
            var snapshot = new TextureImporterSnapshot { _assetPath = assetPath };
            
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return snapshot;
            }

            snapshot._isReadable = importer.isReadable;
            snapshot._textureType = importer.textureType;
            snapshot._sRgb = importer.sRGBTexture;
            snapshot._mipmapEnabled = importer.mipmapEnabled;
            snapshot._filterMode = importer.filterMode;
            snapshot._wrapMode = importer.wrapMode;
            snapshot._anisoLevel = importer.anisoLevel;
            snapshot._maxTextureSize = importer.maxTextureSize;
            snapshot._textureCompression = importer.textureCompression;
            snapshot._alphaIsTransparency = importer.alphaIsTransparency;

            for (int i = 0; i < C_DEFAULT_PLATFORMS.Length; i++)
            {
                string platform = C_DEFAULT_PLATFORMS[i];
                
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
                if (settings == null)
                {
                    continue;
                }

                PlatformSnapshot ps = new PlatformSnapshot
                {
                    platform = platform,
                    settings = ClonePlatformSettings(settings)
                };
                snapshot._platforms.Add(ps);
            }

            return snapshot;
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Restore()
        {
            if (string.IsNullOrEmpty(_assetPath))
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(_assetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);
                return;
            }

            importer.isReadable = _isReadable;
            importer.textureType = _textureType;
            importer.sRGBTexture = _sRgb;
            importer.mipmapEnabled = _mipmapEnabled;
            importer.filterMode = _filterMode;
            importer.wrapMode = _wrapMode;
            importer.anisoLevel = _anisoLevel;
            importer.maxTextureSize = _maxTextureSize;
            importer.textureCompression = _textureCompression;
            importer.alphaIsTransparency = _alphaIsTransparency;

            for (int i = 0; i < _platforms.Count; i++)
            {
                PlatformSnapshot platform = _platforms[i];
                if (platform.settings != null)
                {
                    TextureImporterPlatformSettings settings = ClonePlatformSettings(platform.settings);
                    importer.SetPlatformTextureSettings(settings);
                }
            }

            importer.SaveAndReimport();
        }

        /// <summary>
        /// 写入 bytes 并保留元信息
        /// </summary>
        public static void WriteBytesPreservingMeta(string assetPath, byte[] bytes)
        {
            string fullPath = Path.GetFullPath(assetPath);
            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        // 克隆平台设置
        private static TextureImporterPlatformSettings ClonePlatformSettings(TextureImporterPlatformSettings source)
        {
            return new TextureImporterPlatformSettings
            {
                name = source.name,
                overridden = source.overridden,
                maxTextureSize = source.maxTextureSize,
                resizeAlgorithm = source.resizeAlgorithm,
                format = source.format,
                textureCompression = source.textureCompression,
                compressionQuality = source.compressionQuality,
                crunchedCompression = source.crunchedCompression,
                allowsAlphaSplitting = source.allowsAlphaSplitting,
                androidETC2FallbackOverride = source.androidETC2FallbackOverride
            };
        }
        
    }
}
