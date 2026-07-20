// Created By: WangYu  Date: 2025-04-10

using System;
using System.Collections;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.Utils;

namespace XKT.TOD.Tag
{
    [EditorWindowTitle(title = "TOD 标记同步窗口", icon = "Lighting")]
    public class TodTagSyncWindow : EditorWindow
    {
        [MenuItem("Window/TA工具集/TOD/TOD 标记同步窗口")]
        static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<TodTagSyncWindow>();
            window.minSize = new Vector2(500, 250);
            window.Show();
        }
        
        private TodTagConfig m_currentTTR;
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField(new GUIContent("TOD 标记同步窗口"), EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            if (GUILayout.Button("销毁所有标记"))
            {
                EditorCoroutineUtility.StartCoroutine(DestroyAllTags(), this);
            }
            
            EditorGUILayout.Space();
            m_currentTTR = EditorGUILayout.ObjectField(new GUIContent("TOD 标记配置"), m_currentTTR, typeof(TodTagConfig), false) as TodTagConfig;
            
            EditorGUILayout.Space();
            if (m_currentTTR == null)
            {
                if (GUILayout.Button("创建新配置"))
                {
                    EditorCoroutineUtility.StartCoroutine(CreateConfig(), this);
                }
            }
            else
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("根据配置同步标记到当前场景中"))
                {
                    EditorCoroutineUtility.StartCoroutine(SyncTags(), this);
                }
            }
        }

        // 销毁所有标记
        private IEnumerator DestroyAllTags()
        {
            yield return null;

            var todTags = TODUtils.FindObjectsOfTypeInActiveScene<AbsTodTag>(true);
            bool hasDirty = todTags.Count > 0;
            foreach (var todTag in todTags)
            {
                TODUtils.DestroyUnityObject(todTag);
            }

            if (hasDirty)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

        // 创建配置
        private IEnumerator CreateConfig()
        {
            yield return null;
            
            Scene activeScene = SceneManager.GetActiveScene();

            // 保存文件对话框
            string fileAssetPath = EditorUtility.SaveFilePanelInProject(
                "导出 TOD 数据",
                $"tod_tag_{activeScene.name}",
                "asset",
                "请选择保存位置"
            );
            if (string.IsNullOrEmpty(fileAssetPath))
            {
                EditorUtility.DisplayDialog("流程中断", "文件路径不能为空", "确认");
                yield break;
            }

            var assetInstance = ScriptableObject.CreateInstance<TodTagConfig>();

            var todTags = TODUtils.FindObjectsOfTypeInActiveScene<AbsTodTag>();
            foreach (var todTag in todTags)
            {
                TodTagItem ti = NewTagItem(todTag);
                assetInstance.todTagList.Add(ti);
            }

            AssetDatabase.CreateAsset(assetInstance, fileAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 焦点配置资源
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = assetInstance;
            EditorGUIUtility.PingObject(assetInstance);

            m_currentTTR = assetInstance;

            EditorUtility.DisplayDialog("创建完成", $"配置创建到位置: {fileAssetPath}", "OK");
        }

        private TodTagItem NewTagItem(AbsTodTag todTag)
        {
            TodTagItem tti = new TodTagItem();
            
            tti.hierarchyPath = TODUtils.GetHierarchyPath(todTag.transform);
            
            tti.scriptId = todTag.scriptId;
            tti.typeFullName = todTag.GetType().FullName;

            return tti;
        }
        
        // 同步标记
        private IEnumerator SyncTags()
        {
            yield return null;
            
            Assembly runtimeAssembly = Assembly.GetAssembly(typeof(TimeOfDayManager));
            
            int hierarchyPathErrorCount = 0;
            int typeErrorCount = 0;
            bool hasDirty = false;
            
            // 重新设置脚本
            foreach (var ti in m_currentTTR.todTagList)
            {
                Transform tf = TODUtils.FindHierarchyPath(ti.hierarchyPath);
                if (tf == null)
                {
                    hierarchyPathErrorCount++;
                    Debug.LogError($"路径没找到: {ti.hierarchyPath}");
                    continue;
                }
                
                Type type = runtimeAssembly.GetType(ti.typeFullName);
                if (type == null)
                {
                    typeErrorCount++;
                    Debug.LogError($"类型没找到: {ti.typeFullName}");
                    continue;
                }
                
                if (!typeof(AbsTodTag).IsAssignableFrom(type))
                {
                    typeErrorCount++;
                    Debug.LogError($"类型 {ti.typeFullName} 不是 {nameof(AbsTodTag)} 的子类！");
                    continue;
                }
                
                // 挂组件
                Component component = tf.gameObject.AddComponent(type);
                
                // 设置数据
                var att = component as AbsTodTag;
                att.scriptId = ti.scriptId;
                att.hierarchyPath = ti.hierarchyPath;
                
                hasDirty = true;
            }
            
            if (hasDirty)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
            
            if (hierarchyPathErrorCount == 0 && typeErrorCount == 0)
            {
                EditorUtility.DisplayDialog("同步完成", "已将配置同步到当前场景中", "OK");
            }
            else
            {
                string msg = "同步了部分配置";
                msg += $"\nhierarchy 路径错误: {hierarchyPathErrorCount}";
                msg += $"\nTag 类型错误: {typeErrorCount}";
                EditorUtility.DisplayDialog("同步完成", msg, "OK");
            }
        }

    }
}
