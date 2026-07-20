/*******************************************************************************
 * File: TimeOfDayLightmapBakeService.cs
 * Author: Codex
 * Date: 2026/04/24
 * Description: TOD Lightmap 烘焙服务，负责烘焙场景准备、自动烘焙和数据回写。
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
using XKT.TOD.Lightmap;
using XKT.TOD.Utils;

namespace XKT.TOD
{
    /// <summary>
    /// TOD Lightmap 烘焙流程服务。
    /// </summary>
    internal static class TimeOfDayLightmapBakeService
    {
        /// <summary>
        /// 当前 Lightmap 烘焙流程状态。
        /// </summary>
        public static string Status { get; private set; } = "空闲";

        /// <summary>
        /// 准备 Lightmap 专用场景，自动烘焙，并将结果写回指定 TOD 配置。
        /// </summary>
        public static IEnumerator BakeAndWriteToConfig(StoredTimeOfDayData todData, Action<bool> onCompleted)
        {
            if (todData == null)
            {
                Status = "没有可写入的 TOD 配置。";
                TimeOfDayEditorProgress.Report(Status, 0.25f);
                onCompleted?.Invoke(false);
                yield break;
            }

            string bakeScenePath = string.Empty;
            yield return ExportSceneForBake(path => bakeScenePath = path);
            if (string.IsNullOrEmpty(bakeScenePath))
            {
                onCompleted?.Invoke(false);
                yield break;
            }

            bool bakeSucceeded = false;
            yield return BakeCurrentScene(result => bakeSucceeded = result);
            if (!bakeSucceeded)
            {
                onCompleted?.Invoke(false);
                yield break;
            }

            bool writeSucceeded = false;
            yield return WriteLightmapData(todData, result => writeSucceeded = result);
            if (!writeSucceeded)
            {
                onCompleted?.Invoke(false);
                yield break;
            }

            Status = "Lightmap 数据已写入 TOD 配置。";
            TimeOfDayEditorProgress.Report(Status, 0.98f);
            onCompleted?.Invoke(true);
        }

        /// <summary>
        /// 导出当前场景的 Lightmap 专用副本，并切换到副本场景。
        /// </summary>
        public static IEnumerator ExportSceneForBake(Action<string> onCompleted)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            string rawScenePath = activeScene.path;

            if (string.IsNullOrEmpty(rawScenePath))
            {
                Status = "当前场景未保存，无法生成 Lightmap 烘焙场景。";
                TimeOfDayEditorProgress.Report(Status, 0.30f);
                EditorUtility.DisplayDialog("流程中断", Status, "OK");
                onCompleted?.Invoke(string.Empty);
                yield break;
            }

            ReportProgress("保存当前场景。", 0.30f);
            if (!CustomSceneUtility.SaveModifiedScenesDialog())
            {
                Status = "用户取消保存场景，流程中断。";
                TimeOfDayEditorProgress.Report(Status, 0.30f);
                onCompleted?.Invoke(string.Empty);
                yield break;
            }

            string sceneExtension = Path.GetExtension(rawScenePath);
            string newScenePath = rawScenePath.Replace(sceneExtension, $"_lightmap{sceneExtension}");

            ReportProgress("生成 Lightmap 烘焙场景。", 0.35f);
            EditorSceneManager.SaveScene(activeScene, newScenePath);
            Scene lightmapScene = EditorSceneManager.OpenScene(newScenePath);

            ReportProgress("清理 Lightmap 烘焙场景。", 0.40f);
            yield return RemoveMissingGameObjects(lightmapScene);

            ReportProgress("调整 Renderer GI 参数。", 0.45f);
            yield return SwitchGIOfRenderers();

            Lightmapping.Clear();
            Lightmapping.lightingDataAsset = null;

            EditorSceneManager.SaveScene(lightmapScene);
            AssetDatabase.Refresh();

            ReportProgress($"Lightmap 烘焙场景已准备: {newScenePath}", 0.50f);
            onCompleted?.Invoke(newScenePath);
        }

        /// <summary>
        /// 自动启动当前场景的 Lightmap 烘焙并等待完成。
        /// </summary>
        public static IEnumerator BakeCurrentScene(Action<bool> onCompleted)
        {
            ReportProgress("开始 Lightmap 烘焙。", 0.52f);
            if (!Lightmapping.BakeAsync())
            {
                Status = "Lightmap 烘焙启动失败。";
                TimeOfDayEditorProgress.Report(Status, 0.52f);
                EditorUtility.DisplayDialog("流程中断", Status, "OK");
                onCompleted?.Invoke(false);
                yield break;
            }

            while (Lightmapping.isRunning)
            {
                Status = $"Lightmap 烘焙中... {Lightmapping.buildProgress:P0}";
                TimeOfDayEditorProgress.Report(Status, 0.52f + Lightmapping.buildProgress * 0.33f);
                yield return null;
            }

            ReportProgress("Lightmap 烘焙完成。", 0.85f);
            onCompleted?.Invoke(true);
        }

        /// <summary>
        /// 将当前场景中带 LightmapTag 的 MeshRenderer 实际引用的 Lightmap 数据写入 TOD 配置。
        /// </summary>
        public static IEnumerator WriteLightmapData(StoredTimeOfDayData todData, Action<bool> onCompleted)
        {
            if (todData == null)
            {
                Status = "没有设置 TOD 数据配置，无法写入配置。";
                TimeOfDayEditorProgress.Report(Status, 0.86f);
                Debug.LogError(Status);
                EditorUtility.DisplayDialog("流程中断", Status, "OK");
                onCompleted?.Invoke(false);
                yield break;
            }

            ReportProgress("写入 LightmapTag 数据。", 0.88f);
            if (todData.lightmapUniquenessDatas == null)
            {
                Status = "TOD 配置缺少 LightmapTag 数据，无法回写。";
                TimeOfDayEditorProgress.Report(Status, 0.88f);
                Debug.LogError(Status);
                EditorUtility.DisplayDialog("流程中断", Status, "OK");
                onCompleted?.Invoke(false);
                yield break;
            }

            LightmapData[] sceneLightmaps = LightmapSettings.lightmaps;
            Dictionary<int, int> lightmapIndexMap = new Dictionary<int, int>();
            List<LightmapDataCopy> lightmapDataCopys = new List<LightmapDataCopy>();

            List<LightmapTag> lightmapTags = TODUtils.FindObjectsOfTypeInActiveScene<LightmapTag>();
            foreach (LightmapTag lightmapTag in lightmapTags)
            {
                MeshRenderer meshRenderer = lightmapTag.gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    continue;
                }

                LightmapUniquenessData item = todData.lightmapUniquenessDatas.Find(dataItem => dataItem.scriptId == lightmapTag.scriptId);
                if (item == null)
                {
                    Debug.LogError($"存在脚本唯一 id 不匹配的情况: {lightmapTag.scriptId}");
                    continue;
                }

                int oldLightmapIndex = meshRenderer.lightmapIndex;
                if (!TryGetRemappedLightmapIndex(oldLightmapIndex, sceneLightmaps, lightmapIndexMap, lightmapDataCopys, out int newLightmapIndex))
                {
                    item.lightmapIndex = oldLightmapIndex;
                    item.lightmapScaleOffset = meshRenderer.lightmapScaleOffset;
                    Debug.LogWarning($"LightmapTag {lightmapTag.scriptId} 引用的 Lightmap 索引无效: {oldLightmapIndex}");
                    continue;
                }

                item.lightmapIndex = newLightmapIndex;
                item.lightmapScaleOffset = meshRenderer.lightmapScaleOffset;
            }

            ReportProgress("写入 Lightmap 贴图引用。", 0.93f);
            todData.lightmapDataCopys = lightmapDataCopys;

            EditorUtility.SetDirty(todData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ReportProgress("Lightmap 数据写入完成。", 0.97f);
            onCompleted?.Invoke(true);
            yield return null;
        }

        /// <summary>
        /// 将场景原始 Lightmap 索引转换为 TOD 配置中的压缩索引。
        /// </summary>
        private static bool TryGetRemappedLightmapIndex(
            int oldLightmapIndex,
            LightmapData[] sceneLightmaps,
            Dictionary<int, int> lightmapIndexMap,
            List<LightmapDataCopy> lightmapDataCopys,
            out int newLightmapIndex)
        {
            newLightmapIndex = -1;
            if (oldLightmapIndex < 0 || sceneLightmaps == null || oldLightmapIndex >= sceneLightmaps.Length)
            {
                return false;
            }

            if (lightmapIndexMap.TryGetValue(oldLightmapIndex, out newLightmapIndex))
            {
                return true;
            }

            LightmapData sceneLightmapData = sceneLightmaps[oldLightmapIndex];
            LightmapDataCopy lightmapDataCopy = new LightmapDataCopy();
            lightmapDataCopy.lightmapColor = sceneLightmapData.lightmapColor;
            lightmapDataCopy.lightmapDir = sceneLightmapData.lightmapDir;
            lightmapDataCopy.shadowMask = sceneLightmapData.shadowMask;

            newLightmapIndex = lightmapDataCopys.Count;
            lightmapDataCopys.Add(lightmapDataCopy);
            lightmapIndexMap.Add(oldLightmapIndex, newLightmapIndex);
            return true;
        }

        /// <summary>
        /// 更新流程状态和编辑器进度条。
        /// </summary>
        private static void ReportProgress(string status, float progress)
        {
            Status = status;
            TimeOfDayEditorProgress.Report(status, progress);
        }

        /// <summary>
        /// 删除目标场景中 Missing Prefab 实例。
        /// </summary>
        private static IEnumerator RemoveMissingGameObjects(Scene targetScene)
        {
            yield return null;

            List<GameObject> goList = new List<GameObject>();
            foreach (GameObject rootGameObject in targetScene.GetRootGameObjects())
            {
                FindMissingPrefabsRecursive(rootGameObject, goList);
            }

            goList.Sort((a, b) => b.transform.hierarchyCount - a.transform.hierarchyCount);

            foreach (GameObject item in goList)
            {
                TODUtils.DestroyUnityObject(item);
            }
        }

        /// <summary>
        /// 递归收集 Missing Prefab 实例。
        /// </summary>
        private static void FindMissingPrefabsRecursive(GameObject go, List<GameObject> missingPrefabs)
        {
            if (go == null)
            {
                return;
            }

            if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
            {
                missingPrefabs.Add(go);
                return;
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                FindMissingPrefabsRecursive(child.gameObject, missingPrefabs);
            }
        }

        /// <summary>
        /// 调整非 LightmapTag Renderer 的 GI 参数，避免它们占用主要 Lightmap 空间。
        /// </summary>
        private static IEnumerator SwitchGIOfRenderers()
        {
            yield return null;

            List<LightmapVolume> volumes = TODUtils.FindObjectsOfTypeInActiveScene<LightmapVolume>();
            foreach (LightmapVolume volume in volumes)
            {
                TODUtils.DestroyUnityObject(volume);
            }

            yield return null;

            List<LightmapTag> lightmapTags = TODUtils.FindObjectsOfTypeInActiveScene<LightmapTag>();
            List<Renderer> taggedRenderers = new List<Renderer>();
            foreach (LightmapTag lightmapTag in lightmapTags)
            {
                Renderer renderer = lightmapTag.GetComponent<Renderer>();
                if (renderer != null)
                {
                    taggedRenderers.Add(renderer);
                }
            }

            List<Renderer> renderers = TODUtils.FindObjectsOfTypeInActiveScene<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                GameObject rendererGameObject = renderer.gameObject;

                bool unpacked = UnpackUtil.UnpackPrefabs(rendererGameObject);
                if (!unpacked)
                {
                    continue;
                }

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(rendererGameObject);
                bool contributesToGI = (flags & StaticEditorFlags.ContributeGI) != 0;
                if (!contributesToGI || taggedRenderers.Contains(renderer))
                {
                    continue;
                }

                SerializedObject serializedRenderer = new SerializedObject(renderer);
                SerializedProperty scaleProperty = serializedRenderer.FindProperty("m_ScaleInLightmap");
                if (scaleProperty == null)
                {
                    continue;
                }

                scaleProperty.floatValue = 0.01f;
                serializedRenderer.ApplyModifiedProperties();
                EditorUtility.SetDirty(renderer);
            }
        }
    }
}
