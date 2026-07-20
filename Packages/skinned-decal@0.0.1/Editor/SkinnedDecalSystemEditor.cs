using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace SkinnedDecals
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SkinnedDecalSystem))]
    public class SkinnedDecalSystemEditor : Editor
    {
        private SkinnedDecalSystem m_script;
        
        private SerializedProperty m_findAllChildSkinnedMeshes;
        private SerializedProperty m_skipSmrWithTag;
        
        private ReorderableList m_skinnedMeshList;
        
        private SerializedProperty m_runThreaded;
        private SerializedProperty m_markDynamic;
        private SerializedProperty m_instantiateMaterial;
        private SerializedProperty m_waitTimeout;
        private SerializedProperty m_usedToCollectTheApproximatelyOfTheCenterPoint;
        
        private SerializedProperty m_editorDecal;
        private SerializedProperty m_editorAngle;

        private void OnEnable()
        {
            m_script = this.target as SkinnedDecalSystem;
            if(!m_script) return;
            
            m_findAllChildSkinnedMeshes = serializedObject.FindProperty(nameof(m_script.findAllChildSkinnedMeshes));
            m_skipSmrWithTag = serializedObject.FindProperty(nameof(m_script.skipSmrWithTag));
            
            // 蒙皮网格渲染器列表
            m_skinnedMeshList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(m_script.skinnedMeshes)), true, true, true, true);
            m_skinnedMeshList.drawHeaderCallback += (Rect rect) => { EditorGUI.LabelField(rect, "受控的蒙皮网格渲染器"); };
            m_skinnedMeshList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_skinnedMeshList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element);
            };
            
            m_runThreaded = serializedObject.FindProperty(nameof(m_script.runThreaded));
            m_markDynamic = serializedObject.FindProperty(nameof(m_script.markDynamic));
            m_instantiateMaterial = serializedObject.FindProperty(nameof(m_script.instantiateMaterial));
            m_waitTimeout = serializedObject.FindProperty(nameof(m_script.waitTimeout));
            m_usedToCollectTheApproximatelyOfTheCenterPoint = serializedObject.FindProperty(nameof(m_script.usedToCollectTheApproximatelyOfTheCenterPoint));
            
            m_editorDecal = serializedObject.FindProperty(nameof(m_script.editorDecal));
            m_editorAngle = serializedObject.FindProperty(nameof(m_script.editorAngle));
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            if (!m_script) return;

            serializedObject.Update();
            float lastFieldWidth = EditorGUIUtility.fieldWidth;
            float lastLabelWidth = EditorGUIUtility.labelWidth;
            if (true)
            {
                EditorGUILayout.HelpBox(
                    "此组件允许将贴花添加到一个或多个 SkinnedMeshRenderers。\n" +
                    "通过调用 CreateDecal() 添加贴花。",
                    MessageType.Info);

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("蒙皮网格", EditorStyles.boldLabel);
                {
                    EditorGUIUtility.labelWidth = 240;
                    EditorGUILayout.PropertyField(m_findAllChildSkinnedMeshes, new GUIContent("查找所有子蒙皮网格"));

                    if (m_findAllChildSkinnedMeshes.boolValue)
                    {
                        EditorGUILayout.PropertyField(m_skipSmrWithTag, new GUIContent("根据 tag 跳过蒙皮网格渲染器"));
                        EditorGUILayout.HelpBox($"需要跳过的把 tag 标记为: {SkinnedDecalSystem.c_skipTag}", MessageType.Info);
                    }

                    EditorGUILayout.Space();
                    EditorGUI.BeginDisabledGroup(m_findAllChildSkinnedMeshes.boolValue);
                    EditorGUIUtility.labelWidth = 80;
                    m_skinnedMeshList.DoLayoutList();
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.HelpBox("添加特定的蒙皮网格，或允许系统在 hierarchy 中查找此网格下的所有蒙皮网格。", MessageType.Info);
                }

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("设置", EditorStyles.boldLabel);
                {
                    EditorGUIUtility.labelWidth = 240;
                    EditorGUILayout.PropertyField(m_runThreaded, new GUIContent("启用多线程"));
                    EditorGUILayout.PropertyField(m_markDynamic, new GUIContent("动态 mesh", "如果对 mesh 的修改很频繁，有好处"));
                    EditorGUILayout.PropertyField(m_instantiateMaterial, new GUIContent("每个贴花都实例化单独的材质球"));
                    EditorGUILayout.PropertyField(m_waitTimeout, new GUIContent("闲置等待时间", "太闲了就继续回收部分资源"));
                    
                    EditorGUIUtility.fieldWidth = 50;
                    EditorGUILayout.PropertyField(m_usedToCollectTheApproximatelyOfTheCenterPoint, new GUIContent("用于收集中心点的近似值"));
                }

                EditorGUILayout.Separator();
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("编辑器测试", EditorStyles.boldLabel);

                    EditorGUIUtility.labelWidth = 80;
                    m_editorDecal.objectReferenceValue = EditorGUILayout.ObjectField("测试贴花", m_editorDecal.objectReferenceValue, typeof(SkinnedDecal), false);
                    EditorGUILayout.PropertyField(m_editorAngle, new GUIContent("旋转角度"));

                    EditorGUILayout.HelpBox(
                        "先把要测试的贴花拖上来，在 Scene 视图中按住 ALT 键并单击模型，就可以在模型上生成贴花。",
                        MessageType.Info);
                    EditorGUILayout.HelpBox(
                        "要将网格物体保存到文件中，找到 SkinnedDecalMesh 组件，并按下保存按钮",
                        MessageType.Info);
                }
            }
            EditorGUIUtility.fieldWidth = lastFieldWidth;
            EditorGUIUtility.labelWidth = lastLabelWidth;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if(!m_script) return;

            Event even = Event.current;
            if (even.alt)
            {
                int controlID = GUIUtility.GetControlID(FocusType.Passive);

                switch (Event.current.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                    {
                        GUIUtility.hotControl = controlID;
                        {
                            SkinnedDecal sd = (SkinnedDecal)m_editorDecal.objectReferenceValue;
                            Ray ray = HandleUtility.GUIPointToWorldRay(even.mousePosition);
                            m_script.CreateDecal(sd, ray.origin, ray.direction, m_editorAngle.floatValue);
                        }
                        Event.current.Use();
                    }
                        break;

                    case EventType.MouseUp:
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                        break;
                }
            }
        }
        
    }
}