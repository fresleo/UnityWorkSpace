// Created By: WangYu  Date: 2025-03-25

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.DataStructure;
using XKT.TOD.Utils;

namespace XKT.TOD
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TimeOfDayManager))]
    public class TimeOfDayManagerEditor : Editor
    {
        internal class Styles
        {
            public static GUIContent header = new("TOD 管理器");
            
            public static GUIContent startLaunch = new("开始时启动");
            public static GUIContent mainLightAsset = new("主灯资源");
            public static GUIContent rawLightmapCount = new("当前场景的 Lightmap 计数");
            
            public static GUIContent listHeaderText = new("TOD 数据列表");
            public static GUIContent listModifyText = new("修改中");
            public static GUIContent listSetModifyText = new("设置为修改");
            public static GUIContent listLaunchText = new("启动中");
            public static GUIContent listSetLaunchText = new("设置为启动");

            public const string displayModifyIndex = "当前激活的 TOD 数据: {0}";

            public const string modifyConfigHelpBox = "中性图调色默认来自太阳的颜色，但允许手动对齐进行微调，以满足美术风格的需要。";
            public static GUIContent customTint = new("中性图调色");
            public static GUIContent resetTint = new("重置调色");
            public static GUIContent saveTint = new("保存调色");
            
            public static GUIContent ConflictObject = new("有冲突的组件");
            public static readonly string ConflictObjectTips = $"{nameof(TimeOfDayFixedTime)} 和 {nameof(TimeOfDayManager)} 是不能同时工作的，只能保留1种";
        }
        
        private TimeOfDayManager CurrentTarget => this.target as TimeOfDayManager;

        private SerializedProperty m_startLaunch;
        private SerializedProperty m_mainLightAsset;
        private SerializedProperty m_rawLightmapCount;
        
        private SerializedProperty m_todDatas;
        private ReorderableList m_todDataList;
        private int m_modifyIndex;
        
        private bool m_configModified;
        private Color m_customTint;
        
        private const int FIND_INTERVAL = 2;
        private float _lastFindTime;
        private TimeOfDayFixedTime _foundScript;

        
        private void OnEnable()
        {
            if (CurrentTarget == null)
            {
                return;
            }

            m_startLaunch = serializedObject.FindProperty(nameof(TimeOfDayManager.startLaunch));
            m_mainLightAsset = serializedObject.FindProperty(nameof(TimeOfDayManager.mainLightAsset));
            m_rawLightmapCount = serializedObject.FindProperty(nameof(TimeOfDayManager.rawLightmapCount));
            
            m_todDatas = serializedObject.FindProperty(nameof(TimeOfDayManager.todDatas));
            m_modifyIndex = m_startLaunch.boolValue ? 0 : -1; // 当前开机启动永远是起第1个
            
            m_todDataList = new ReorderableList(serializedObject, m_todDatas, true, true, true, true);
            DoTodDataLayoutList(m_todDataList, m_todDatas);
            
            m_configModified = true;
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();

            if (CurrentTarget == null)
            {
                return;
            }

            EditorGUILayout.LabelField(Styles.header, EditorStyles.whiteLargeLabel);
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_startLaunch, Styles.startLaunch);
            
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(m_mainLightAsset, Styles.mainLightAsset);
                EditorGUILayout.PropertyField(m_rawLightmapCount, Styles.rawLightmapCount);
            }
            
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            m_todDataList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                m_configModified = true;
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                string str = string.Format(Styles.displayModifyIndex, m_modifyIndex);
                EditorGUILayout.LabelField(str);
            }
            
            EditorGUILayout.Space();
            DrawModifyConfig();

            EditorGUILayout.Space();
            FindConflictObject();
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        // 设置列表的布局
        private void DoTodDataLayoutList(ReorderableList list, SerializedProperty prop)
        {
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                Rect indexRect = new Rect(
                    rect.x, rect.y, 
                    15, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(indexRect, index.ToString());
                
                Rect objRect = new Rect(
                    rect.x + indexRect.width, rect.y, 
                    rect.width - 180, EditorGUIUtility.singleLineHeight);
                EditorGUI.BeginChangeCheck();
                EditorGUI.ObjectField(objRect, prop.GetArrayElementAtIndex(index), GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                }
                
                float spaceWidth = 5;
                float buttonWidth = 160;
                if (Application.isPlaying)
                {
                    // 启动激活按钮
                    Rect launchButtonRect = new Rect(
                        rect.x + indexRect.width + objRect.width + spaceWidth, rect.y, 
                        buttonWidth, EditorGUIUtility.singleLineHeight);

                    GUI.enabled = index != m_modifyIndex;
                    var label = !GUI.enabled ? Styles.listLaunchText : Styles.listSetLaunchText;
                    if (GUI.Button(launchButtonRect, label))
                    {
                        m_modifyIndex = index;
                        if(target is TimeOfDayManager todMgr)
                        {
                            todMgr.Launch(index);
                        }
                    }
                    GUI.enabled = true;
                }
                else
                {
                    // 选择修改按钮
                    Rect modifyButtonRect = new Rect(
                        rect.x + indexRect.width + objRect.width + spaceWidth, rect.y, 
                        buttonWidth, EditorGUIUtility.singleLineHeight);
                    
                    GUI.enabled = index != m_modifyIndex;
                    var label = !GUI.enabled ? Styles.listModifyText : Styles.listSetModifyText;
                    if (GUI.Button(modifyButtonRect, label))
                    {
                        m_modifyIndex = index;
                        EditorUtility.SetDirty(target);
                    }
                    GUI.enabled = true;
                }
                
                // 确保选择的对象，能被分配到正确的插槽里
                if (Event.current.commandName == "ObjectSelectorUpdated"
                    && EditorGUIUtility.GetObjectPickerControlID() == index)
                {
                    prop.GetArrayElementAtIndex(index).objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                }
            };

            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, Styles.listHeaderText);
            };

            list.onCanAddCallback = li => true;
            list.onCanRemoveCallback = li => li.count > 0;
            
            list.onRemoveCallback = li =>
            {
                int indexToRemove = li.index;

                // 如果要删除当前修改的索引，则更新修改索引
                if (indexToRemove == m_modifyIndex)
                {
                    if (m_modifyIndex > 0)
                    {
                        m_modifyIndex--;
                    }
                }
                // 如果删除的索引小于当前修改索引，则修改索引需要减一
                else if (indexToRemove < m_modifyIndex)
                {
                    m_modifyIndex--;
                }

                // 实际删除所选元素
                prop.DeleteArrayElementAtIndex(indexToRemove);
                // 设置列表索引
                li.index = Mathf.Min(indexToRemove, prop.arraySize - 1);
                EditorUtility.SetDirty(target);
            };
        }

        private void DrawModifyConfig()
        {
            if (m_todDatas.arraySize == 0 || m_modifyIndex < 0 || m_modifyIndex >= m_todDatas.arraySize)
            {
                return;
            }

            var todData = m_todDatas.GetArrayElementAtIndex(m_modifyIndex).objectReferenceValue as StoredTimeOfDayData;
            if (todData == null)
            {
                return;
            }
            
            if (m_configModified)
            {
                m_customTint = todData.bakedGITint;
                m_configModified = false;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(Styles.modifyConfigHelpBox, MessageType.Info);

                EditorGUILayout.Space();
                m_customTint = EditorGUILayout.ColorField(Styles.customTint, m_customTint, true, false, false);

                string leftHex = ColorUtility.ToHtmlStringRGB(todData.bakedGITint);
                string rightHex = ColorUtility.ToHtmlStringRGB(m_customTint);
                bool isSame = leftHex == rightHex;
                if (!isSame)
                {
                    EditorGUILayout.Space();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var btnGw = GUILayout.Width(70);

                        if (GUILayout.Button(Styles.resetTint, btnGw))
                        {
                            m_customTint = todData.bakedGITint;
                        }

                        EditorGUILayout.Space();
                        if (GUILayout.Button(Styles.saveTint, btnGw))
                        {
                            todData.bakedGITint = m_customTint;

                            EditorUtility.SetDirty(todData);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                    }
                }

                EditorGUILayout.Space();
            }
        }
        
        [MenuItem("GameObject/TOD/Time Of Day Manager")]
        private static void CreateTargetGO()
        {
            // 获取当前活动的 SceneView
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                Debug.LogError("找不到活动的 Scene View");
                return;
            }

            // 默认距离（如果没有碰撞）
            float defaultDistance = 10f;
            Vector3 position = Vector3.zero;
            
            // 获取场景视图相机
            Camera camera = sceneView.camera;
            // 计算场景视图中心点的射线
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            // 尝试与场景中的物体相交
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                position = hit.point;
            }
            else
            {
                // 没有碰撞，使用默认距离
                position = ray.origin + ray.direction * defaultDistance;
            }

            // 创建对象
            var parent = Selection.activeTransform;
            var newGo = new GameObject(nameof(TimeOfDayManager));
            if (parent != null)
            {
                newGo.transform.SetParent(parent);
            }
            
            newGo.transform.position = position;
            newGo.transform.rotation = Quaternion.identity;
            newGo.transform.localScale = Vector3.one;
            
            newGo.AddComponent<TimeOfDayManager>();
            
            // 选中新创建的对象
            Selection.activeGameObject = newGo;
            
            // 标记场景已修改
            EditorSceneManager.MarkSceneDirty(newGo.scene);
        }
        
        private void FindConflictObject()
        {
            if (Time.realtimeSinceStartup - _lastFindTime >= FIND_INTERVAL)
            {
                _lastFindTime = Time.realtimeSinceStartup;
                
                Scene currentScene = CurrentTarget.gameObject.scene;
                _foundScript = TODUtils.FindObjectOfTypeInTargetScene<TimeOfDayFixedTime>(currentScene);
            }

            if (_foundScript != null)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.ObjectField(Styles.ConflictObject, _foundScript, typeof(TimeOfDayFixedTime), true);
                    EditorGUILayout.HelpBox(Styles.ConflictObjectTips, MessageType.Error);
                }
            }
        }

    }
}