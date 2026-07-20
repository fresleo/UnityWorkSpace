/*******************************************************************************
 * File: CanopyShadowMaskWriteback.cs
 * Author: WangYu
 * Date: 2026-06-30
 * Description: 
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 读取官方 shadowmasks 并合并结果到 _canopy 附属纹理中
    /// </summary>
    public static class CanopyShadowMaskWriteback
    {
        private const string C_CANOPY_SUFFIX = "_canopy";

        /// <summary>
        /// 一个 shadowmask 图集的可变 RGBA 浮点缓冲区
        /// </summary>
        public sealed class ShadowMaskBuffer
        {
            public int lightmapIndex;
            public int width;
            public int height;

            /// <summary>
            /// 官方 Unity 烘焙 shadowmask 地址（从未被此工具写入过）
            /// </summary>
            public string officialAssetPath;

            /// <summary>
            /// 接收合成像素的树冠输出路径
            /// </summary>
            public string canopyAssetPath;

            /// <summary>
            /// 像素缓冲区
            /// </summary>
            public float[] pixels;

            /// <summary>
            /// 本次烘焙在该图集上变暗的像素数量。
            /// </summary>
            public int modifiedTexelCount;

            /// <summary>
            /// 读取在 atlas 像素坐标上的一个通道值。
            /// </summary>
            public float GetChannel(int x, int y, EShadowMaskChannel channel)
            {
                int idx = (y * width + x) * 4 + (int)channel;
                return pixels[idx];
            }

            /// <summary>
            /// 在图集像素坐标写入一个通道值。
            /// </summary>
            private void SetChannel(int x, int y, EShadowMaskChannel channel, float value)
            {
                int idx = (y * width + x) * 4 + (int)channel;
                pixels[idx] = Mathf.Clamp01(value);
            }

            /// <summary>
            /// 变暗一个通道：取现有值和遮挡值的最小值。
            /// </summary>
            public void ApplyMinChannel(int x, int y, EShadowMaskChannel channel, float occlusion)
            {
                float current = GetChannel(x, y, channel);
                SetChannel(x, y, channel, Mathf.Min(current, occlusion));
            }
        }

        /// <summary>
        /// 从官方的 shadowmask 资源加载缓冲区（官方文件只读）
        /// </summary>
        public static List<ShadowMaskBuffer> LoadAllShadowMaskBuffers()
        {
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            if (lightmaps == null)
            {
                return new List<ShadowMaskBuffer>();
            }

            var indices = new HashSet<int>();
            for (int i = 0; i < lightmaps.Length; i++)
            {
                if (lightmaps[i].shadowMask != null)
                {
                    indices.Add(i);
                }
            }

            return LoadShadowMaskBuffersForLightmapIndices(indices);
        }

        /// <summary>
        /// 通过 lightmap 索引来加载对应的 shadowmask
        /// </summary>
        public static List<ShadowMaskBuffer> LoadShadowMaskBuffersForLightmapIndices(ICollection<int> lightmapIndices)
        {
            var result = new List<ShadowMaskBuffer>();
            if (lightmapIndices == null || lightmapIndices.Count == 0)
            {
                return result;
            }

            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            if (lightmaps == null)
            {
                return result;
            }

            foreach (int lightmapIndex in lightmapIndices)
            {
                if (lightmapIndex < 0 || lightmapIndex >= lightmaps.Length)
                {
                    continue;
                }

                Texture2D shadowMask = lightmaps[lightmapIndex].shadowMask;
                if (shadowMask == null)
                {
                    continue;
                }

                string officialPath = ResolveOfficialAssetPath(AssetDatabase.GetAssetPath(shadowMask));
                ShadowMaskBuffer buffer = LoadShadowMaskBuffer(lightmapIndex, officialPath);
                if (buffer != null)
                {
                    result.Add(buffer);
                }
            }

            return result;
        }

        /// <summary>
        /// 写入带树冠的 shadowMask 资源文件（不修改 LightmapSettings，由 Binder 负责绑定）。
        /// </summary>
        public static bool WriteShadowMaskBuffers(List<ShadowMaskBuffer> buffers)
        {
            if (buffers == null || buffers.Count == 0)
            {
                return true;
            }

            bool allSucceeded = true;
            for (int i = 0; i < buffers.Count; i++)
            {
                ShadowMaskBuffer buffer = buffers[i];
                if (!WriteShadowMaskBuffer(buffer))
                {
                    allSucceeded = false;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return allSucceeded;
        }

        /// <summary>
        /// 根据官方 shadowmask 路径获取树冠版的资源路径
        /// </summary>
        public static string GetCanopyAssetPath(string officialAssetPath)
        {
            string dir = Path.GetDirectoryName(officialAssetPath);
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }
            string fileName = Path.GetFileNameWithoutExtension(officialAssetPath);
            string ext = Path.GetExtension(officialAssetPath);
            if (string.IsNullOrEmpty(ext))
            {
                ext = ".png";
            }

            if (!fileName.EndsWith(C_CANOPY_SUFFIX, StringComparison.Ordinal))
            {
                fileName = fileName + C_CANOPY_SUFFIX;
            }

            if (string.IsNullOrEmpty(dir))
            {
                return fileName + ext;
            }

            string result = dir + "/" + fileName + ext;
            return result;
        }

        /// <summary>
        /// 将树冠版的路径解析回其官方的 Unity 烘焙资源路径。
        /// </summary>
        public static string ResolveOfficialAssetPath(string canopyAssetPath)
        {
            if (string.IsNullOrEmpty(canopyAssetPath))
            {
                return canopyAssetPath;
            }

            string dir = Path.GetDirectoryName(canopyAssetPath);
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }
            string fileName = Path.GetFileNameWithoutExtension(canopyAssetPath);
            string ext = Path.GetExtension(canopyAssetPath);
            if (fileName.EndsWith(C_CANOPY_SUFFIX, StringComparison.Ordinal))
            {
                fileName = fileName.Substring(0, fileName.Length - C_CANOPY_SUFFIX.Length);
            }

            if (string.IsNullOrEmpty(dir))
            {
                return fileName + ext;
            }

            string result = dir + "/" + fileName + ext;
            return result;
        }

        // 加载 shadowMask 缓冲区
        private static ShadowMaskBuffer LoadShadowMaskBuffer(int lightmapIndex, string officialPath)
        {
            if (string.IsNullOrEmpty(officialPath))
            {
                Debug.LogWarning($"Shadowmask 在索引 {lightmapIndex} 处，没有资产路径。");
                return null;
            }

            string canopyPath = GetCanopyAssetPath(officialPath);
            Texture2D readable = LoadReadableCopy(officialPath);
            if (readable == null)
            {
                Debug.LogError($"无法读取 shadowmask: {officialPath}");
                return null;
            }

            int w = readable.width;
            int h = readable.height;
            
            Color[] colors = readable.GetPixels();
            var pixels = new float[colors.Length * 4];
            for (int i = 0; i < colors.Length; i++)
            {
                int baseIdx = i * 4;
                pixels[baseIdx] = colors[i].r;
                pixels[baseIdx + 1] = colors[i].g;
                pixels[baseIdx + 2] = colors[i].b;
                pixels[baseIdx + 3] = colors[i].a;
            }

            UnityEngine.Object.DestroyImmediate(readable);

            return new ShadowMaskBuffer
            {
                lightmapIndex = lightmapIndex,
                width = w,
                height = h,
                officialAssetPath = officialPath,
                canopyAssetPath = canopyPath,
                pixels = pixels
            };
        }

        // 用官方资源刷新树冠资源
        private static bool RefreshCanopyAssetFromOfficial(string officialPath, string canopyPath)
        {
            if (string.IsNullOrEmpty(officialPath) || string.IsNullOrEmpty(canopyPath))
            {
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(canopyPath) != null)
            {
                if (!AssetDatabase.DeleteAsset(canopyPath))
                {
                    Debug.LogError($"烘焙前刷新树冠的包边失败: {canopyPath}");
                    return false;
                }
            }

            if (!AssetDatabase.CopyAsset(officialPath, canopyPath))
            {
                Debug.LogError($"创建树冠的包边失败: {canopyPath}");
                return false;
            }

            AssetDatabase.ImportAsset(canopyPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        // 加载可读的拷贝
        private static Texture2D LoadReadableCopy(string officialPath)
        {
            Texture2D gpuCopy = LoadGpuReadableCopy(officialPath);
            if (gpuCopy != null)
            {
                return gpuCopy;
            }

            string sourceFullPath = Path.GetFullPath(officialPath);
            if (!File.Exists(sourceFullPath))
            {
                return null;
            }

            string tempFullPath = TempCanopyShadowMaskPath.CreateTempReadableFilePath(officialPath);
            try
            {
                File.Copy(sourceFullPath, tempFullPath, true);
                byte[] bytes = File.ReadAllBytes(tempFullPath);
                var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                if (!readable.LoadImage(bytes))
                {
                    UnityEngine.Object.DestroyImmediate(readable);
                    return null;
                }

                Texture2D copy = CopyReadablePixels(readable);
                UnityEngine.Object.DestroyImmediate(readable);
                return copy;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"文件回退读取 shadowmask 失败: \n{ex.Message}");
                return null;
            }
            finally
            {
                if (File.Exists(tempFullPath))
                {
                    File.Delete(tempFullPath);
                }
            }
        }

        // 拷贝可读像素
        private static Texture2D CopyReadablePixels(Texture2D readable)
        {
            if (readable == null)
            {
                return null;
            }

            try
            {
                var copy = new Texture2D(readable.width, readable.height, TextureFormat.RGBAFloat, false, true);
                copy.SetPixels(readable.GetPixels());
                copy.Apply();
                return copy;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"可读写的 shadowmask 导入失败，回退到 GPU 回读: \n{ex.Message}");
                return null;
            }
        }

        // 加载 GPU 可读拷贝
        private static Texture2D LoadGpuReadableCopy(string officialPath)
        {
            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(officialPath);
            if (source == null || source.width <= 0 || source.height <= 0)
            {
                return null;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture rt = RenderTexture.GetTemporary(
                source.width, source.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                copy.Apply();
                return copy;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GPU shadowmask 回读失败: \n{ex.Message}");
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
        
        // 写入 Shadowmask 缓冲
        private static bool WriteShadowMaskBuffer(ShadowMaskBuffer buffer)
        {
            if (buffer == null || buffer.pixels == null)
            {
                return false;
            }

            if (!RefreshCanopyAssetFromOfficial(buffer.officialAssetPath, buffer.canopyAssetPath))
            {
                return false;
            }

            TextureImporterSnapshot canopySnapshot = TextureImporterSnapshot.Capture(buffer.canopyAssetPath);

            int pixelCount = buffer.width * buffer.height;
            var colors = new Color[pixelCount];
            for (int i = 0; i < pixelCount; i++)
            {
                int baseIdx = i * 4;
                colors[i] = new Color(
                    buffer.pixels[baseIdx],
                    buffer.pixels[baseIdx + 1],
                    buffer.pixels[baseIdx + 2],
                    buffer.pixels[baseIdx + 3]);
            }

            TextureFormat textureFormat = GetWritableTextureFormat(buffer.canopyAssetPath);
            var tex = new Texture2D(buffer.width, buffer.height, textureFormat, false, true);
            tex.SetPixels(colors);
            tex.Apply();

            byte[] bytes = EncodeTextureToFile(tex, buffer.canopyAssetPath);
            UnityEngine.Object.DestroyImmediate(tex);

            TextureImporterSnapshot.WriteBytesPreservingMeta(buffer.canopyAssetPath, bytes);
            canopySnapshot.Restore();

            string logTxt =
                $"已写入树冠 shadowmask 索引 {buffer.lightmapIndex} -> {buffer.canopyAssetPath} 。"
                + $"\n官方未修改: {buffer.officialAssetPath}";
            Debug.Log(logTxt);
            
            return true;
        }

        // 获取可写纹理格式
        private static TextureFormat GetWritableTextureFormat(string assetPath)
        {
            string ext = Path.GetExtension(assetPath).ToLowerInvariant();
            if (ext == ".exr")
            {
                return TextureFormat.RGBAFloat;
            }

            return TextureFormat.RGBA32;
        }

        // 编码纹理到文件
        private static byte[] EncodeTextureToFile(Texture2D tex, string assetPath)
        {
            string ext = Path.GetExtension(assetPath).ToLowerInvariant();
            if (ext == ".exr")
            {
                return tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            }

            if (ext == ".tga")
            {
                return tex.EncodeToTGA();
            }

            return tex.EncodeToPNG();
        }
        
    }
}

