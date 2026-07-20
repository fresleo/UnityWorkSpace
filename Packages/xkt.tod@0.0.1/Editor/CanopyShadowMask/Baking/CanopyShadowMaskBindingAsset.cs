/*******************************************************************************
 * File: CanopyShadowMaskBindingAsset.cs
 * Author: WangYu
 * Date: 2026-07-08
 * Description: 
 * Notice: 
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 单张 lightmap 的官方 shadowmask 与树冠 shadowmask 路径映射。
    /// </summary>
    [Serializable]
    public sealed class CanopyShadowMaskBindingAssetEntry
    {
        /// <summary>
        /// LightmapSettings.lightmaps 中的索引。
        /// </summary>
        public int lightmapIndex;

        /// <summary>
        /// Unity 官方烘焙生成的 shadowmask 资源路径。
        /// </summary>
        public string officialShadowMaskPath;

        /// <summary>
        /// 树冠二次烘焙生成的 shadowmask 资源路径。
        /// </summary>
        public string canopyShadowMaskPath;
    }

    /// <summary>
    /// 树冠 shadowmask 路径映射资源，可手动切换当前场景的 shadowmask 。
    /// </summary>
    public sealed class CanopyShadowMaskBindingAsset : ScriptableObject
    {
        private const string C_OFFICIAL_BACKUP_SUFFIX = "_unity_official";

        [SerializeField]
        private List<CanopyShadowMaskBindingAssetEntry> _bindings = new ();

        /// <summary>
        /// 当前记录 lightmap shadowmask 的路径映射
        /// </summary>
        public IReadOnlyList<CanopyShadowMaskBindingAssetEntry> Bindings => _bindings;

        /// <summary>
        /// 覆盖路径映射数据
        /// </summary>
        /// <param name="newBindings">新的路径映射列表</param>
        public void SetBindings(List<CanopyShadowMaskBindingAssetEntry> newBindings)
        {
            if (newBindings == null)
            {
                _bindings = new List<CanopyShadowMaskBindingAssetEntry>();
            }
            else
            {
                _bindings = new List<CanopyShadowMaskBindingAssetEntry>(newBindings);
            }

            SortBindings();
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 用树冠二次烘焙贴图内容覆盖官方 shadowmask 文件
        /// </summary>
        /// <returns>实际替换的 lightmap 数量</returns>
        public int ApplyCanopyShadowMasks()
        {
            return ApplyFileSwap(true);
        }

        /// <summary>
        /// 用备份的官方贴图内容还原官方 shadowmask 文件
        /// </summary>
        /// <returns>实际还原的 lightmap 数量</returns>
        public int ApplyOfficialShadowMasks()
        {
            return ApplyFileSwap(false);
        }

        private int ApplyFileSwap(bool useCanopy)
        {
            if (_bindings == null || _bindings.Count == 0)
            {
                return 0;
            }

            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            if (lightmaps == null || lightmaps.Length == 0)
            {
                return 0;
            }

            int appliedCount = 0;
            for (int i = 0; i < _bindings.Count; i++)
            {
                CanopyShadowMaskBindingAssetEntry entry = _bindings[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.lightmapIndex < 0 || entry.lightmapIndex >= lightmaps.Length)
                {
                    continue;
                }

                bool applied = useCanopy
                    ? ApplyCanopyFile(entry)
                    : ApplyOfficialFile(entry);
                if (!applied)
                {
                    continue;
                }

                Texture2D officialTexture =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(entry.officialShadowMaskPath);
                if (officialTexture == null)
                {
                    Debug.LogWarning("无法加载官方 shadowmask: " + entry.officialShadowMaskPath);
                    continue;
                }

                lightmaps[entry.lightmapIndex].shadowMask = officialTexture;
                appliedCount++;
            }

            if (appliedCount <= 0)
            {
                return 0;
            }

            LightmapSettings.lightmaps = lightmaps;
            if (Lightmapping.lightingDataAsset != null)
            {
                EditorUtility.SetDirty(Lightmapping.lightingDataAsset);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return appliedCount;
        }

        private static bool ApplyCanopyFile(CanopyShadowMaskBindingAssetEntry entry)
        {
            if (!EnsureOfficialBackup(entry))
            {
                return false;
            }

            if (!AssetExists(entry.canopyShadowMaskPath))
            {
                Debug.LogWarning("缺少树冠 shadowmask: " + entry.canopyShadowMaskPath);
                return false;
            }

            if (!CopyTextureContent(entry.canopyShadowMaskPath, entry.officialShadowMaskPath))
            {
                return false;
            }

            AssetDatabase.DeleteAsset(entry.canopyShadowMaskPath);
            return true;
        }

        private static bool ApplyOfficialFile(CanopyShadowMaskBindingAssetEntry entry)
        {
            string backupPath = GetOfficialBackupAssetPath(entry.officialShadowMaskPath);
            if (!AssetExists(backupPath))
            {
                Debug.LogWarning("缺少官方 shadowmask 备份: " + backupPath);
                return false;
            }

            if (!AssetExists(entry.canopyShadowMaskPath)
                && !AssetDatabase.CopyAsset(entry.officialShadowMaskPath, entry.canopyShadowMaskPath))
            {
                Debug.LogError(
                    "恢复树冠 shadowmask 文件失败: "
                    + entry.officialShadowMaskPath + " -> " + entry.canopyShadowMaskPath);
                return false;
            }

            if (!CopyTextureContent(backupPath, entry.officialShadowMaskPath))
            {
                return false;
            }

            AssetDatabase.DeleteAsset(backupPath);
            return true;
        }

        private static bool EnsureOfficialBackup(CanopyShadowMaskBindingAssetEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.officialShadowMaskPath))
            {
                return false;
            }

            string backupPath = GetOfficialBackupAssetPath(entry.officialShadowMaskPath);
            if (AssetExists(backupPath))
            {
                return true;
            }

            if (!AssetExists(entry.officialShadowMaskPath))
            {
                Debug.LogWarning("缺少官方 shadowmask: " + entry.officialShadowMaskPath);
                return false;
            }

            if (!AssetDatabase.CopyAsset(entry.officialShadowMaskPath, backupPath))
            {
                Debug.LogError(
                    "创建官方 shadowmask 备份失败: "
                    + entry.officialShadowMaskPath + " -> " + backupPath);
                return false;
            }

            AssetDatabase.ImportAsset(backupPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static bool CopyTextureContent(string sourceAssetPath, string targetAssetPath)
        {
            if (string.IsNullOrEmpty(sourceAssetPath) || string.IsNullOrEmpty(targetAssetPath))
            {
                return false;
            }

            string sourceFullPath = Path.GetFullPath(sourceAssetPath);
            string targetFullPath = Path.GetFullPath(targetAssetPath);
            if (!File.Exists(sourceFullPath))
            {
                Debug.LogWarning("缺少 shadowmask 源文件: " + sourceAssetPath);
                return false;
            }

            if (!File.Exists(targetFullPath))
            {
                Debug.LogWarning("缺少 shadowmask 目标文件: " + targetAssetPath);
                return false;
            }

            try
            {
                File.Copy(sourceFullPath, targetFullPath, true);
                AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "替换 shadowmask 文件失败: "
                    + sourceAssetPath + " -> " + targetAssetPath
                    + "\n" + ex.Message);
                return false;
            }
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string fullPath = Path.GetFullPath(assetPath);
            return File.Exists(fullPath);
        }

        private static string GetOfficialBackupAssetPath(string officialAssetPath)
        {
            if (string.IsNullOrEmpty(officialAssetPath))
            {
                return string.Empty;
            }

            string dir = Path.GetDirectoryName(officialAssetPath);
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }

            string fileName = Path.GetFileNameWithoutExtension(officialAssetPath);
            string ext = Path.GetExtension(officialAssetPath);
            string backupName = fileName + C_OFFICIAL_BACKUP_SUFFIX + ext;
            if (string.IsNullOrEmpty(dir))
            {
                return backupName;
            }

            return dir + "/" + backupName;
        }

        private void SortBindings()
        {
            if (_bindings == null || _bindings.Count <= 1)
            {
                return;
            }

            _bindings.Sort(CompareBindings);
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
        
    }
}