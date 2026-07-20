/*******************************************************************************
 * File: TimeOfDayExportWorkflow.cs
 * Author: Codex
 * Date: 2026/04/24
 * Description: TOD 一键导出工作流，按场景列表批量导出 TOD 数据和 Lightmap 烘焙结果。
 *******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.DataStructure;
using XKT.TOD.Utils;

namespace XKT.TOD
{
    /// <summary>
    /// TOD 一键导出流程。
    /// </summary>
    internal sealed class TimeOfDayExportWorkflow
    {
        public bool IsRunning { get; private set; }

        public string Status { get; private set; } = "空闲";

        public StoredTimeOfDayData CurrentTodData { get; private set; }

        public string CurrentTodDataPath { get; private set; } = string.Empty;

        /// <summary>
        /// 按用户配置的场景列表顺序批量导出 TOD 数据。
        /// </summary>
        public IEnumerator ExportScenes(IReadOnlyList<SceneAsset> sceneAssets)
        {
            if (IsRunning)
            {
                yield break;
            }

            List<string> scenePaths = GetValidScenePaths(sceneAssets);
            if (scenePaths.Count == 0)
            {
                EditorUtility.DisplayDialog("流程中断", "请至少配置一个有效的 Unity Scene。", "确认");
                yield break;
            }

            IsRunning = true;
            CurrentTodData = null;
            CurrentTodDataPath = string.Empty;

            try
            {
                if (!CustomSceneUtility.SaveModifiedScenesDialog())
                {
                    Status = "用户取消保存当前场景，流程中断。";
                    yield break;
                }

                for (int i = 0; i < scenePaths.Count; i++)
                {
                    string scenePath = scenePaths[i];
                    ReportProgress($"打开场景 ({i + 1}/{scenePaths.Count}): {Path.GetFileNameWithoutExtension(scenePath)}", 0.02f);
                    EditorSceneManager.OpenScene(scenePath);
                    yield return null;

                    yield return ExportCurrentScene(scenePath, i + 1, scenePaths.Count);
                    if (CurrentTodData == null)
                    {
                        yield break;
                    }
                }

                CompleteExport($"TOD 批量一键导出完成，共导出 {scenePaths.Count} 个场景。");
            }
            finally
            {
                IsRunning = false;
                TimeOfDayEditorProgress.Clear();
            }
        }

        private IEnumerator ExportCurrentScene(string sourceScenePath, int sceneIndex, int sceneCount)
        {
            CurrentTodData = null;
            CurrentTodDataPath = string.Empty;

            ReportProgress($"检查并修复场景重名对象 ({sceneIndex}/{sceneCount})。", 0.05f);
            int renamedCount = TimeOfDaySceneDuplicateNameUtility.RenameDuplicatedHierarchyPathObjects();
            if (renamedCount > 0)
            {
                Debug.Log($"TOD 一键导出前自动重命名了 {renamedCount} 个重复层级对象。");
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            yield return CreateAndCollectTodData(sourceScenePath, sceneIndex, sceneCount);
            if (CurrentTodData == null)
            {
                yield break;
            }

            if (!HasLightmapTagData())
            {
                ReportProgress($"当前场景没有 LightmapTag，跳过 Lightmap 烘焙 ({sceneIndex}/{sceneCount})。", 1f);
                Debug.Log($"场景 {SceneManager.GetActiveScene().name} 没有 LightmapTag，跳过 Lightmap 烘焙。");
                yield break;
            }

            bool lightmapSucceeded = false;
            yield return TimeOfDayLightmapBakeService.BakeAndWriteToConfig(CurrentTodData, result => lightmapSucceeded = result);
            if (!lightmapSucceeded)
            {
                Status = "Lightmap 烘焙或回写失败，一键导出中断。";
                CurrentTodData = null;
                yield break;
            }
        }

        private IEnumerator CreateAndCollectTodData(string sourceScenePath, int sceneIndex, int sceneCount)
        {
            string sceneName = Path.GetFileNameWithoutExtension(sourceScenePath);
            string phaseName = GetPhaseName(sceneName);
            string fileAssetPath = GetDefaultTodAssetPath(sourceScenePath);

            StoredTimeOfDayData assetInstance = ScriptableObject.CreateInstance<StoredTimeOfDayData>();
            assetInstance.creationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            assetInstance.sceneName = SceneManager.GetActiveScene().name;
            assetInstance.phaseName = phaseName;

            ReportProgress($"创建 TOD 数据配置 ({sceneIndex}/{sceneCount})。", 0.10f);
            AssetDatabase.CreateAsset(assetInstance, fileAssetPath);

            ReportProgress($"收集 TOD 场景数据 ({sceneIndex}/{sceneCount})。", 0.18f);
            StoredTimeOfDayDataCollector dataCollector = new StoredTimeOfDayDataCollector();
            yield return dataCollector.Execute(assetInstance);

            CurrentTodData = assetInstance;
            CurrentTodDataPath = fileAssetPath;
            ReportProgress($"TOD 数据配置已创建，准备 Lightmap 烘焙 ({sceneIndex}/{sceneCount})。", 0.25f);
        }

        private List<string> GetValidScenePaths(IReadOnlyList<SceneAsset> sceneAssets)
        {
            List<string> scenePaths = new List<string>();
            if (sceneAssets == null)
            {
                return scenePaths;
            }

            for (int i = 0; i < sceneAssets.Count; i++)
            {
                SceneAsset sceneAsset = sceneAssets[i];
                if (sceneAsset == null)
                {
                    continue;
                }

                string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                if (!string.IsNullOrEmpty(scenePath))
                {
                    scenePaths.Add(scenePath);
                }
            }

            return scenePaths;
        }

        private string GetDefaultTodAssetPath(string sourceScenePath)
        {
            string sceneDirectory = Path.GetDirectoryName(sourceScenePath)?.Replace("\\", "/");
            string sceneName = Path.GetFileNameWithoutExtension(sourceScenePath);
            string folderName = $"{GetSceneBaseName(sceneName)}_TOD_Config";
            string folderPath = $"{sceneDirectory}/{folderName}";

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(sceneDirectory, folderName);
            }

            string assetPath = $"{folderPath}/tod_{sceneName}.asset";
            return AssetDatabase.GenerateUniqueAssetPath(assetPath);
        }

        private string GetSceneBaseName(string sceneName)
        {
            int lastUnderlineIndex = sceneName.LastIndexOf('_');
            if (lastUnderlineIndex <= 0)
            {
                return sceneName;
            }

            return sceneName.Substring(0, lastUnderlineIndex);
        }

        private string GetPhaseName(string sceneName)
        {
            int lastUnderlineIndex = sceneName.LastIndexOf('_');
            if (lastUnderlineIndex < 0 || lastUnderlineIndex >= sceneName.Length - 1)
            {
                return sceneName;
            }

            return sceneName.Substring(lastUnderlineIndex + 1);
        }

        private void ReportProgress(string status, float progress)
        {
            Status = status;
            TimeOfDayEditorProgress.Report(status, progress);
        }

        private bool HasLightmapTagData()
        {
            return CurrentTodData.lightmapUniquenessDatas != null && CurrentTodData.lightmapUniquenessDatas.Count > 0;
        }

        private void CompleteExport(string message)
        {
            if (CurrentTodData != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = CurrentTodData;
                EditorGUIUtility.PingObject(CurrentTodData);
            }

            Status = $"{message}\n最后导出配置: {CurrentTodDataPath}";
            TimeOfDayEditorProgress.Report(Status, 1f);
            EditorUtility.DisplayDialog("TOD 一键导出完成", Status, "确认");
            Debug.Log(Status);
        }
    }
}
