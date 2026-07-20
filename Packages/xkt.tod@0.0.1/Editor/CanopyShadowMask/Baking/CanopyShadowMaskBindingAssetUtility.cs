/*******************************************************************************
 * File: CanopyShadowMaskBindingAssetUtility.cs
 * Author: WangYu
 * Date: 2026-07-08
 * Description: 
 ******************************************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 创建或更新树冠 shadowmask 映射 asset
    /// </summary>
    internal static class CanopyShadowMaskBindingAssetUtility
    {
        private const string C_BINDING_ASSET_NAME = "CanopyShadowMaskBinding.asset";

        /// <summary>
        /// 根据烘焙结果更新当前场景对应的树冠 shadowmask 映射资源
        /// </summary>
        /// <param name="modifiedBuffers">本次实际写回的 shadowmask 缓冲区</param>
        /// <param name="processedLightmapIndices">本次参与合成的 lightmap 索引</param>
        /// <returns>创建或更新后的映射资源</returns>
        public static CanopyShadowMaskBindingAsset SyncBindingAsset(
            List<CanopyShadowMaskWriteback.ShadowMaskBuffer> modifiedBuffers,
            ICollection<int> processedLightmapIndices)
        {
            if (processedLightmapIndices == null || processedLightmapIndices.Count == 0)
            {
                return null;
            }

            string bindingAssetPath = ResolveBindingAssetPath(modifiedBuffers, processedLightmapIndices);
            if (string.IsNullOrEmpty(bindingAssetPath))
            {
                return null;
            }

            CanopyShadowMaskBindingAsset asset = AssetDatabase.LoadAssetAtPath<CanopyShadowMaskBindingAsset>(bindingAssetPath);
            List<CanopyShadowMaskBindingAssetEntry> previousEntries = null;
            if (asset != null && asset.Bindings != null)
            {
                previousEntries = new List<CanopyShadowMaskBindingAssetEntry>(asset.Bindings);
            }

            HashSet<int> processedSet = new HashSet<int>(processedLightmapIndices);
            List<CanopyShadowMaskBindingAssetEntry> modifiedEntries = BuildBindingEntries(modifiedBuffers);
            List<CanopyShadowMaskBindingAssetEntry> mergedEntries = MergeBindings(previousEntries, modifiedEntries, processedSet);

            if (mergedEntries.Count == 0)
            {
                if (asset != null)
                {
                    AssetDatabase.DeleteAsset(bindingAssetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                return null;
            }

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CanopyShadowMaskBindingAsset>();
                AssetDatabase.CreateAsset(asset, bindingAssetPath);
            }

            asset.SetBindings(mergedEntries);
            
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return asset;
        }

        // 合并绑定
        private static List<CanopyShadowMaskBindingAssetEntry> MergeBindings(
            IReadOnlyList<CanopyShadowMaskBindingAssetEntry> previousEntries,
            List<CanopyShadowMaskBindingAssetEntry> modifiedEntries,
            HashSet<int> processedIndices)
        {
            var merged = new List<CanopyShadowMaskBindingAssetEntry>();
            if (previousEntries != null)
            {
                for (int i = 0; i < previousEntries.Count; i++)
                {
                    CanopyShadowMaskBindingAssetEntry previous = previousEntries[i];
                    if (previous == null)
                    {
                        continue;
                    }

                    if (processedIndices.Contains(previous.lightmapIndex))
                    {
                        continue;
                    }

                    merged.Add(previous);
                }
            }

            if (modifiedEntries != null)
            {
                for (int i = 0; i < modifiedEntries.Count; i++)
                {
                    CanopyShadowMaskBindingAssetEntry entry = modifiedEntries[i];
                    if (entry != null)
                    {
                        merged.Add(entry);
                    }
                }
            }

            merged.Sort(CompareBindings);
            return merged;
        }

        private static int CompareBindings(
            CanopyShadowMaskBindingAssetEntry left,
            CanopyShadowMaskBindingAssetEntry right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.lightmapIndex.CompareTo(right.lightmapIndex);
        }

        private static List<CanopyShadowMaskBindingAssetEntry> BuildBindingEntries(
            List<CanopyShadowMaskWriteback.ShadowMaskBuffer> buffers)
        {
            var entries = new List<CanopyShadowMaskBindingAssetEntry>();
            if (buffers == null)
            {
                return entries;
            }

            for (int i = 0; i < buffers.Count; i++)
            {
                CanopyShadowMaskWriteback.ShadowMaskBuffer buffer = buffers[i];
                if (buffer == null || buffer.modifiedTexelCount <= 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(buffer.officialAssetPath)
                    || string.IsNullOrEmpty(buffer.canopyAssetPath))
                {
                    continue;
                }

                var entry = new CanopyShadowMaskBindingAssetEntry
                {
                    lightmapIndex = buffer.lightmapIndex,
                    officialShadowMaskPath = buffer.officialAssetPath,
                    canopyShadowMaskPath = buffer.canopyAssetPath
                };
                entries.Add(entry);
            }

            return entries;
        }

        private static string ResolveBindingAssetPath(
            List<CanopyShadowMaskWriteback.ShadowMaskBuffer> modifiedBuffers,
            ICollection<int> processedLightmapIndices)
        {
            string referencePath = ResolveReferencePath(modifiedBuffers);
            if (string.IsNullOrEmpty(referencePath))
            {
                referencePath = ResolveReferencePath(processedLightmapIndices);
            }

            if (string.IsNullOrEmpty(referencePath))
            {
                return string.Empty;
            }

            string dir = Path.GetDirectoryName(referencePath);
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }

            if (string.IsNullOrEmpty(dir))
            {
                return C_BINDING_ASSET_NAME;
            }

            return dir + "/" + C_BINDING_ASSET_NAME;
        }

        private static string ResolveReferencePath(
            List<CanopyShadowMaskWriteback.ShadowMaskBuffer> modifiedBuffers)
        {
            if (modifiedBuffers == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < modifiedBuffers.Count; i++)
            {
                CanopyShadowMaskWriteback.ShadowMaskBuffer buffer = modifiedBuffers[i];
                if (buffer == null || string.IsNullOrEmpty(buffer.officialAssetPath))
                {
                    continue;
                }

                return buffer.officialAssetPath;
            }

            return string.Empty;
        }

        private static string ResolveReferencePath(ICollection<int> processedLightmapIndices)
        {
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            if (lightmaps == null || processedLightmapIndices == null)
            {
                return string.Empty;
            }

            foreach (int lightmapIndex in processedLightmapIndices)
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

                string assetPath = AssetDatabase.GetAssetPath(shadowMask);
                assetPath = CanopyShadowMaskWriteback.ResolveOfficialAssetPath(assetPath);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    return assetPath;
                }
            }

            return string.Empty;
        }
        
    }
}
