// Created By: WangYu  Date: 2025-06-27

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace XKT.TOD.Lightmap
{
    [CustomEditor(typeof(AutoSwitchOfShadowOnlyAndGI))]
    public class AutoSwitchOfShadowOnlyAndGIEditor : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent header = new("在 bake 时，自动切换阴影投射和 GI 设置");

            public static readonly GUIContent automaticButton = new("自动配置 ShadowOnly 节点");
            public static readonly GUIContent refreshSceneButton = new("触发当前场景中所有的脚本，设置一遍运行时配置");

            public const string totalFormat = "总数: {0}";
            
            public const string lightmapObjects = "Lightmap 对象";
            public const string lightmapObjectsGuid = "加入/存在：设置 lightmap flag。\n用 - 移除：移除 lightmap flag。";

            public const string litAlphaTestObjects = "LitAlphaTest 对象";
            public const string litAlphaTestObjectsGuid = "加入/存在：移除 lightmap flag。\n用 - 移除：恢复 lightmap flag。";

            public const string shadowOnlyObjects = "ShadowOnly 对象";
            public const string shadowOnlyObjectsGuid = "加入/存在：移除 lightmap flag。\n用 - 移除：不处理 lightmap flag。";

            public const string changeLightmapFlags = "修改 Bake Lightmap 标记";
        }

        // 移除 lightmap 标记的模式
        private enum ERemoveLightmapFlagMode
        {
            None,
            Remove, // 移除
            Restore // 恢复
        }

        private enum EObjectListType
        {
            Lightmap,
            LitAlphaTest,
            ShadowOnly
        }

        private AutoSwitchOfShadowOnlyAndGI CurrentTarget => this.target as AutoSwitchOfShadowOnlyAndGI;

        private SerializedProperty m_lightmapObjects;
        private ReorderableList m_lightmapObjectList;

        private SerializedProperty m_litAlphaTestObjects;
        private ReorderableList m_litAlphaTestObjectList;
        
        private SerializedProperty m_shadowOnlyObjects;
        private ReorderableList m_shadowOnlyObjectList;

        
        private void OnEnable()
        {
            if (!CurrentTarget)
            {
                return;
            }

            m_lightmapObjects = serializedObject.FindProperty(nameof(AutoSwitchOfShadowOnlyAndGI.lightmapObjects));
            m_lightmapObjectList = new ReorderableList(serializedObject, m_lightmapObjects
                , true, true, true, true);
            DoReorderableList(m_lightmapObjectList, m_lightmapObjects
                , Styles.lightmapObjects, EObjectListType.Lightmap, ERemoveLightmapFlagMode.Remove);

            m_litAlphaTestObjects = serializedObject.FindProperty(nameof(AutoSwitchOfShadowOnlyAndGI.litAlphaTestObjects));
            m_litAlphaTestObjectList = new ReorderableList(serializedObject, m_litAlphaTestObjects
                , true, true, true, true);
            DoReorderableList(m_litAlphaTestObjectList, m_litAlphaTestObjects
                , Styles.litAlphaTestObjects, EObjectListType.LitAlphaTest, ERemoveLightmapFlagMode.Restore);
            
            m_shadowOnlyObjects = serializedObject.FindProperty(nameof(AutoSwitchOfShadowOnlyAndGI.shadowOnlyObjects));
            m_shadowOnlyObjectList = new ReorderableList(serializedObject, m_shadowOnlyObjects
                , true, true, true, true);
            DoReorderableList(m_shadowOnlyObjectList, m_shadowOnlyObjects
                , Styles.shadowOnlyObjects, EObjectListType.ShadowOnly);
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();

            if (!CurrentTarget)
            {
                return;
            }

            serializedObject.Update();

            EditorGUILayout.LabelField(Styles.header, EditorStyles.whiteLargeLabel);
            
            EditorGUILayout.Space();
            EditorGUILayoutExt.DrawScript(this.target, "脚本");

            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.automaticButton))
            {
                CurrentTarget.AutoConfigureShadowOnlyObjects();
                
                EditorUtility.SetDirty(CurrentTarget);
                serializedObject.Update();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.refreshSceneButton))
            {
                CurrentTarget.RefreshAllInScene();
            }

            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                m_lightmapObjectList.DoLayoutList();
                EditorGUILayout.HelpBox(Styles.lightmapObjectsGuid, MessageType.Info);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                m_litAlphaTestObjectList.DoLayoutList();
                EditorGUILayout.HelpBox(Styles.litAlphaTestObjectsGuid, MessageType.Info);
            }
            
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                m_shadowOnlyObjectList.DoLayoutList();
                EditorGUILayout.HelpBox(Styles.shadowOnlyObjectsGuid, MessageType.Info);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DoReorderableList(
            ReorderableList rl, SerializedProperty sp,
            string title = null,
            EObjectListType olt = EObjectListType.Lightmap,
            ERemoveLightmapFlagMode rlfm = ERemoveLightmapFlagMode.None)
        {
            rl.drawHeaderCallback = (Rect rect) =>
            {
                string label = "";
                if (!string.IsNullOrEmpty(title))
                {
                    label += title + " ";
                }
                label += string.Format(Styles.totalFormat, rl.count);

                EditorGUI.LabelField(rect, label);
            };
            
            rl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                Rect indexRect = new Rect(rect.x, rect.y, 14, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(indexRect, index.ToString());
                
                Rect objRect = new Rect(rect.x + indexRect.width, rect.y, rect.width - 184, EditorGUIUtility.singleLineHeight);
                UnityEngine.Object oldObject = sp.GetArrayElementAtIndex(index).objectReferenceValue;
                
                EditorGUI.BeginChangeCheck();
                EditorGUI.ObjectField(objRect, sp.GetArrayElementAtIndex(index), GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    SetLightmapFlagsByRemoveLightmapFlagMode(oldObject, rlfm);
                    RemoveFromOtherListsOnElementAssigned(sp, index, olt);
                    EditorUtility.SetDirty(target);
                }
            };

            rl.onAddCallback = (ReorderableList list) =>
            {
                sp.arraySize++;
                int index = sp.arraySize - 1;
                sp.GetArrayElementAtIndex(index).objectReferenceValue = null;
                list.index = index;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            };

            if (rlfm != ERemoveLightmapFlagMode.None)
            {
                rl.onRemoveCallback = (ReorderableList list) =>
                {
                    RemoveSelectedListItemAndSetLightmapFlagsOnRemoved(list, sp, rlfm);
                };
            }
        }
        
        // 移除队列中的项，并在移除时设置 lightmap 标志
        private void RemoveSelectedListItemAndSetLightmapFlagsOnRemoved(ReorderableList list, SerializedProperty sp, ERemoveLightmapFlagMode rlfm)
        {
            int index = list.index;
            if (index >= 0 && index < sp.arraySize)
            {
                UnityEngine.Object removeObject = sp.GetArrayElementAtIndex(index).objectReferenceValue;
                SetLightmapFlagsByRemoveLightmapFlagMode(removeObject, rlfm);
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
        
        // 列表元素 ObjectField 变更后：执行去重，清理其它队列，设置 lightmap 标志
        private void RemoveFromOtherListsOnElementAssigned(SerializedProperty sp, int index, EObjectListType listType)
        {
            if (index < 0 || index >= sp.arraySize)
            {
                return;
            }

            UnityEngine.Object obj = sp.GetArrayElementAtIndex(index).objectReferenceValue;
            GameObject gameObject = GetGameObject(obj);
            if (gameObject == null)
            {
                return;
            }

            // 去重
            RemoveDuplicateGameObjectFromList(sp, index, gameObject);
            // 清理其它队列
            RemoveGameObjectFromOtherSerializedLists(gameObject, listType);
            // 设置 lightmap 标志
            SetLightmapFlagsByListType(gameObject, listType);

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            EditorUtility.SetDirty(target);
        }

        // 列表对象去重，其中 keepIndex 索引的对象是要唯一保留的那个
        private void RemoveDuplicateGameObjectFromList(SerializedProperty sp, int keepIndex, GameObject gameObject)
        {
            if (sp == null || gameObject == null)
            {
                return;
            }

            for (int i = sp.arraySize - 1; i >= 0; --i)
            {
                if (i == keepIndex)
                {
                    continue;
                }

                UnityEngine.Object obj = sp.GetArrayElementAtIndex(i).objectReferenceValue;
                GameObject go = GetGameObject(obj);
                if (go == gameObject)
                {
                    DeleteArrayElement(sp, i);
                }
            }
        }
        
        // 从其它的序列化队列中，移除目标对象
        private void RemoveGameObjectFromOtherSerializedLists(GameObject gameObject, EObjectListType listType)
        {
            if (listType != EObjectListType.Lightmap)
            {
                RemoveGameObjectAndSetLightmapFlagFromList(m_lightmapObjects, gameObject, ERemoveLightmapFlagMode.Remove);
            }

            if (listType != EObjectListType.LitAlphaTest)
            {
                RemoveGameObjectAndSetLightmapFlagFromList(m_litAlphaTestObjects, gameObject, ERemoveLightmapFlagMode.Restore);
            }

            if (listType != EObjectListType.ShadowOnly)
            {
                RemoveGameObjectAndSetLightmapFlagFromList(m_shadowOnlyObjects, gameObject, ERemoveLightmapFlagMode.None);
            }
        }

        // 从队列里移除 gameObject 对象，并设置 lightmap 标志
        private void RemoveGameObjectAndSetLightmapFlagFromList(SerializedProperty sp, GameObject gameObject, ERemoveLightmapFlagMode rlfm)
        {
            if (sp == null || gameObject == null)
            {
                return;
            }

            for (int i = sp.arraySize - 1; i >= 0; --i)
            {
                UnityEngine.Object obj = sp.GetArrayElementAtIndex(i).objectReferenceValue;
                GameObject go = GetGameObject(obj);
                if (go == gameObject)
                {
                    SetLightmapFlagsByRemoveLightmapFlagMode(obj, rlfm);
                    DeleteArrayElement(sp, i);
                }
            }
        }
        
        // 根据 ERemoveLightmapFlagMode 设置 lightmap 标志
        private void SetLightmapFlagsByRemoveLightmapFlagMode(UnityEngine.Object obj, ERemoveLightmapFlagMode removeLightmapFlagMode)
        {
            if (removeLightmapFlagMode == ERemoveLightmapFlagMode.None)
            {
                return;
            }

            GameObject go = GetGameObject(obj);
            if (go == null)
            {
                return;
            }

            Undo.RecordObject(go, Styles.changeLightmapFlags);
            
            CurrentTarget.SetLightmapFlags(go, removeLightmapFlagMode == ERemoveLightmapFlagMode.Remove);
            
            EditorUtility.SetDirty(go);
        }
        
        // 根据 EObjectListType 设置 lightmap 标志
        private void SetLightmapFlagsByListType(GameObject gameObject, EObjectListType listType)
        {
            if (gameObject == null)
            {
                return;
            }

            Undo.RecordObject(gameObject, Styles.changeLightmapFlags);
            
            switch (listType)
            {
                case EObjectListType.Lightmap:
                    CurrentTarget.SetLightmapFlags(gameObject);
                    break;
                
                case EObjectListType.LitAlphaTest:
                case EObjectListType.ShadowOnly:
                    CurrentTarget.SetLightmapFlags(gameObject, true);
                    break;
            }
            
            EditorUtility.SetDirty(gameObject);
        }
        
        // 删除数组元素
        private void DeleteArrayElement(SerializedProperty sp, int index)
        {
            int oldSize = sp.arraySize;
            sp.DeleteArrayElementAtIndex(index);
            if (sp.arraySize == oldSize)
            {
                sp.DeleteArrayElementAtIndex(index);
            }
        }

        // 用多种手段从 obj 对象上获取 GameObject 对象
        private GameObject GetGameObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return null;
            }

            GameObject gameObject = obj as GameObject;
            if (gameObject != null)
            {
                return gameObject;
            }

            Component component = obj as Component;
            if (component != null)
            {
                return component.gameObject;
            }

            return null;
        }
        
    }
}
