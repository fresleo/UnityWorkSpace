// Created By: WangYu  Date: 2025-04-01

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace XKT.TOD.Lightmap
{
    [CustomEditor(typeof(LightmapVolume))]
    public class LightmapVolumeEditor : Editor
    {
        static class Styles
        {
            public static readonly GUIContent addColliderFixMessage = new(
                "向此 GameObject 添加 Collider 以设置 Volume 的边界。", CoreEditorStyles.iconWarn);
            public static readonly GUIContent enableColliderFixMessage = new(
                "Volume 需要启用碰撞器。启用碰撞器。", CoreEditorStyles.iconWarn);
            
            public static readonly GUIContent addBoxCollider = new("添加1个 Box Collider");
            public static readonly GUIContent sphereBoxCollider = new("添加1个 Sphere Collider");

            public static readonly GUIContent gizmosHeaderText = new("Gizmos 辅助");
            public static readonly GUIContent drawWireFrame = new("画线框");
            public static readonly GUIContent drawSolid = new("画实心");
            
            public static GUIContent listHeaderText = new("在体积中收集到的渲染器");
        }
        
        LightmapVolume CurrentTarget => this.target as LightmapVolume;

        public static bool s_drawWireFrame = true, s_drawSolid = true;
        
        private SerializedProperty m_containedRenderers;
        private ReorderableList m_containedRendererList;
        
        // 在类中添加变量跟踪折叠状态
        private static bool s_rendererListFoldout;

        private void OnEnable()
        {
            if(!CurrentTarget) return;
            
            m_containedRenderers = serializedObject.FindProperty(nameof(LightmapVolume.containedRenderers));
            m_containedRendererList = new ReorderableList(serializedObject, m_containedRenderers, false, true, false, false);
            DoContainedRendererList(m_containedRendererList, m_containedRenderers);
        }

        private void OnDisable()
        {
            if(!CurrentTarget) return;
            
            CurrentTarget.containedRenderers.Clear();
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            if(!CurrentTarget) return;
            
            serializedObject.Update();
            
            if (CurrentTarget.TryGetComponent<Collider>(out var collider))
            {
                if (!collider.enabled)
                {
                    CoreEditorUtils.DrawFixMeBox(Styles.enableColliderFixMessage, () => collider.enabled = true);
                }
            }
            else
            {
                CoreEditorUtils.DrawFixMeBox(Styles.addColliderFixMessage, AddOverride);
            }

            EditorGUILayout.LabelField(Styles.gizmosHeaderText);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUI.BeginChangeCheck();
                bool drawWireFrame = EditorGUILayout.Toggle(Styles.drawWireFrame, s_drawWireFrame);
                bool drawSolid = EditorGUILayout.Toggle(Styles.drawSolid, s_drawSolid);
                if (EditorGUI.EndChangeCheck())
                {
                    s_drawWireFrame = drawWireFrame;
                    s_drawSolid = drawSolid;
                }
            }
            
            // 收集列表
            EditorGUILayout.Space();
            s_rendererListFoldout = EditorGUILayout.Foldout(s_rendererListFoldout, Styles.listHeaderText, true);
            if (s_rendererListFoldout)
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    m_containedRendererList.DoLayoutList();
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AddOverride()
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.addBoxCollider, false,
                () =>
                {
                    var coll = Undo.AddComponent<BoxCollider>(CurrentTarget.gameObject);
                    coll.isTrigger = true;
                });
            menu.AddItem(Styles.sphereBoxCollider, false, 
                () =>
                {
                    var coll = Undo.AddComponent<SphereCollider>(CurrentTarget.gameObject);
                    coll.isTrigger = true;
                });
            menu.ShowAsContext();
        }

        private void DoContainedRendererList(ReorderableList list, SerializedProperty prop)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"总数: {list.count}");
            };
            
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                Rect indexRect = new Rect(rect.x, rect.y, 14, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(indexRect, index.ToString());
                
                Rect objRect = new Rect(rect.x + indexRect.width, rect.y, rect.width - 184, EditorGUIUtility.singleLineHeight);
                EditorGUI.BeginChangeCheck();
                EditorGUI.ObjectField(objRect, prop.GetArrayElementAtIndex(index), GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                }
                
                // 确保选择的对象，能被分配到正确的插槽里
                if (Event.current.commandName == "ObjectSelectorUpdated"
                    && EditorGUIUtility.GetObjectPickerControlID() == index)
                {
                    prop.GetArrayElementAtIndex(index).objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                }
            };
        }
        
        [MenuItem("GameObject/TOD/Lightmap Volume")]
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
            var newGo = new GameObject(nameof(LightmapVolume));
            if (parent != null)
            {
                newGo.transform.SetParent(parent);
            }
            
            newGo.transform.position = position;
            newGo.transform.rotation = Quaternion.identity;
            newGo.transform.localScale = Vector3.one;
            
            newGo.AddComponent<LightmapVolume>();
            var coll = newGo.AddComponent<BoxCollider>();
            coll.isTrigger = true;
            
            // 选中新创建的对象
            Selection.activeGameObject = newGo;
            
            // 标记场景已修改
            EditorSceneManager.MarkSceneDirty(newGo.scene);
        }
        
    }
}