// Created By: WangYu  Date: 2025-04-01

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.DataStructure;
using XKT.TOD.Utils;

namespace XKT.TOD.Lightmap
{
    [EditorWindowTitle(title = "Lightmap Volume 烘焙窗口", icon = "Lighting")]
    public class LightmapVolumeBakeWindow : EditorWindow
    {
        [MenuItem("Window/TA工具集/TOD/Lightmap Volume 烘焙窗口")]
        static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<LightmapVolumeBakeWindow>();
            window.minSize = new Vector2(500, 250);
            window.Show();
        }

        private StoredTimeOfDayData m_todData;
        
        private void OnGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("导出烘焙用场景"))
            {
                EditorCoroutineUtility.StartCoroutine(ExportScene(), this);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                m_todData = EditorGUILayout.ObjectField(new GUIContent("TOD 数据配置"), m_todData, typeof(StoredTimeOfDayData), false) as StoredTimeOfDayData;
                
                EditorGUILayout.Space();
                if (GUILayout.Button("将烘焙结果写入配置"))
                {
                    EditorCoroutineUtility.StartCoroutine(ExportLightmapData(m_todData), this);
                }
            }
        }

        private IEnumerator ExportScene()
        {
            // 创建新场景
            var activeScene = SceneManager.GetActiveScene();
            string rawScenePath = activeScene.path;
            
            // 保存当前场景
            if (!CustomSceneUtility.SaveModifiedScenesDialog())
            {
                yield break;
            }
            
            string sceneExtension = Path.GetExtension(rawScenePath);
            string newScenePath = rawScenePath.Replace(sceneExtension, $"_lightmap{sceneExtension}");
            
            EditorSceneManager.SaveScene(activeScene, newScenePath);
            Scene lightmapScene = EditorSceneManager.OpenScene(newScenePath);
            
            // 销毁 missing 的预设
            yield return RemoveMissingGameObjects(lightmapScene);
            // 销毁 Renderer 对象
            // yield return RemoveRendererGameObjects();
            yield return SwitchGIOfRenderers();

            // 清除当前光照设置
            Lightmapping.Clear();
            Lightmapping.lightingDataAsset = null;
            
            // 保存场景
            EditorSceneManager.SaveScene(lightmapScene);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("导出完成", $"导出了 Lightmap 的专用烘焙场景:\n{newScenePath}", "OK");
        }
        
        // 删除 missing 的预设
        private IEnumerator RemoveMissingGameObjects(Scene targetScene)
        {
            yield return null;
            
            List<GameObject> goList = new();
            foreach (var rootGameObject in targetScene.GetRootGameObjects())
            {
                FindMissingPrefabsRecursive(rootGameObject, goList);
            }
            
            // 从深到浅排序，避免先删除父物体导致的问题
            goList.Sort((a, b) => b.transform.hierarchyCount - a.transform.hierarchyCount);
            
            foreach (var item in goList)
            {
                TODUtils.DestroyUnityObject(item);
            }
        }
        
        private void FindMissingPrefabsRecursive(GameObject go, List<GameObject> missingPrefabs)
        {
            if (go == null) return;

            // 检查当前对象是否是 Missing 预制体
            if (PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
            {
                missingPrefabs.Add(go);
                return; // 如果整个对象是 Missing 预制体，不再检查子对象
            }

            // 递归检查所有子对象
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);
                FindMissingPrefabsRecursive(child.gameObject, missingPrefabs);
            }
        }

        // 切换渲染器的 GI
        private IEnumerator SwitchGIOfRenderers()
        {
            yield return null;
            // 避免有新的变化，产生干扰
            var lvs = TODUtils.FindObjectsOfTypeInActiveScene<LightmapVolume>();
            foreach (var lv in lvs)
            {
                TODUtils.DestroyUnityObject(lv);
            }
            
            yield return null;
            var lts = TODUtils.FindObjectsOfTypeInActiveScene<LightmapTag>();
            List<Renderer> ltrs = new();
            foreach (var lt in lts)
            {
                var ltr = lt.GetComponent<Renderer>();
                ltrs.Add(ltr);
            }
            
            var renderers = TODUtils.FindObjectsOfTypeInActiveScene<Renderer>();
            foreach (var renderer in renderers)
            {
                GameObject rgo = renderer.gameObject;
                
                // todo: 1次性循环断所有 Renderer 的关系，反而无法阻止 m_ScaleInLightmap 被改到预设上，原因暂时未知。
                bool isUpack = UnpackUtil.UnpackPrefabs(rgo);
                if (!isUpack)
                {
                    continue;
                }
                
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(rgo);
                bool contributesToGI = (flags & StaticEditorFlags.ContributeGI) != 0;
                if (!contributesToGI)
                {
                    continue;
                }
                
                if (ltrs.Contains(renderer))
                {
                    continue;
                }
                
                SerializedObject serializedRenderer = new SerializedObject(renderer);
                SerializedProperty scaleProperty = serializedRenderer.FindProperty("m_ScaleInLightmap");
                if (scaleProperty != null)
                {
                    scaleProperty.floatValue = 0.01f;
                    serializedRenderer.ApplyModifiedProperties();
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private IEnumerator ExportLightmapData(StoredTimeOfDayData todData)
        {
            if (todData == null)
            {
                string msg = "没有设置 TOD 数据配置，无法写入配置。";
                Debug.LogError(msg);
                EditorUtility.DisplayDialog("流程中断", msg, "OK");
                yield break;
            }

            // 保存 Lightmap 的加载数据
            var lts = TODUtils.FindObjectsOfTypeInActiveScene<LightmapTag>();
            foreach (var lt in lts)
            {
                MeshRenderer mr = lt.gameObject.GetComponent<MeshRenderer>();
                
                var item = todData.lightmapUniquenessDatas.Find(item => item.scriptId == lt.scriptId);
                if (item == null)
                {
                    Debug.LogError($"有脚本唯一 id 不匹配的情况: {lt.scriptId}");
                    continue;
                }
                
                item.lightmapIndex = mr.lightmapIndex;
                item.lightmapScaleOffset = mr.lightmapScaleOffset;
            }
            
            // 记录 Lightmap 纹理的引用
            todData.lightmapDataCopys = new List<LightmapDataCopy>();
            foreach (var item in LightmapSettings.lightmaps)
            {
                var ldc = new LightmapDataCopy();
                ldc.lightmapColor = item.lightmapColor;
                ldc.lightmapDir = item.lightmapDir;
                ldc.shadowMask = item.shadowMask;
                
                todData.lightmapDataCopys.Add(ldc);
            }
            
            EditorUtility.SetDirty(todData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("写入完成", "已将 Lightmap 数据写入配置", "OK");
        }
        
    }
}
