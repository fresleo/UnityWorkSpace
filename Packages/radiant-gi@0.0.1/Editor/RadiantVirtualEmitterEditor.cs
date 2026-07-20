using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Rendering;

namespace RadiantGI.Universal
{
    [CustomEditor(typeof(RadiantVirtualEmitter))]
    public class RadiantVirtualEmitterEditor : Editor
    {
        private SerializedProperty m_color, m_intensity, m_range;
        private SerializedProperty m_addMaterialEmission, m_targetRenderer, m_material, m_emissionPropertyName, m_materialIndex;
        private SerializedProperty m_boxCenter, m_boxSize, m_boundsInLocalSpace;

        private readonly BoxBoundsHandle m_boundsHandle = new BoxBoundsHandle();


        private void OnEnable()
        {
            var sop = new PropertyFetcher<RadiantVirtualEmitter>(serializedObject);
            
            m_color = sop.Find(x => x.color);
            m_intensity = sop.Find(x => x.intensity);
            m_range = sop.Find(x => x.range);
            m_addMaterialEmission = sop.Find(x => x.addMaterialEmission);
            m_targetRenderer = sop.Find(x => x.targetRenderer);
            m_material = sop.Find(x => x.material);
            m_emissionPropertyName = sop.Find(x => x.emissionPropertyName);
            m_materialIndex = sop.Find(x => x.materialIndex);
            m_boxCenter = sop.Find(x => x.boxCenter);
            m_boxSize = sop.Find(x => x.boxSize);
            m_boundsInLocalSpace = sop.Find(x => x.boundsInLocalSpace);
        }

        protected virtual void OnSceneGUI()
        {
            RadiantVirtualEmitter vi = (RadiantVirtualEmitter)target;

            Bounds bounds = vi.GetBounds();
            m_boundsHandle.center = bounds.center;
            m_boundsHandle.size = bounds.size;

            // draw the handle
            EditorGUI.BeginChangeCheck();
            m_boundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                // record the target object before setting new values so changes can be undone/redone
                Undo.RecordObject(vi, "Change Bounds");

                // copy the handle's updated data back to the target object
                Bounds newBounds = new Bounds();
                newBounds.center = m_boundsHandle.center;
                newBounds.size = m_boundsHandle.size;
                vi.SetBounds(newBounds);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIContent label;

            label = new GUIContent("GI 颜色");
            EditorGUILayout.PropertyField(m_color, label);
            
            label = new GUIContent("添加材质自发光", "启用此选项可将此对象材质的自发光颜色添加到全局照明中。");
            EditorGUILayout.PropertyField(m_addMaterialEmission, label);
            if (m_addMaterialEmission.boolValue)
            {
                EditorGUI.indentLevel++;
                
                label = new GUIContent("目标渲染器", "同步发射颜色的渲染器");
                EditorGUILayout.PropertyField(m_targetRenderer, label);
                
                label = new GUIContent("材质", "指定自发光颜色的材质");
                EditorGUILayout.PropertyField(m_material, label);

                label = new GUIContent("自发光的 shader 属性名");
                EditorGUILayout.PropertyField(m_emissionPropertyName, label);

                label = new GUIContent("材质索引", "在游戏对象使用多种材质的情况下很有用");
                EditorGUILayout.PropertyField(m_materialIndex, label);
                
                EditorGUI.indentLevel--;
            }

            label = new GUIContent("强度");
            EditorGUILayout.PropertyField(m_intensity, label);

            label = new GUIContent("范围");
            EditorGUILayout.PropertyField(m_range, label);

            label = new GUIContent("盒子中心", "影响范围");
            EditorGUILayout.PropertyField(m_boxCenter, label);

            label = new GUIContent("盒子的尺寸");
            EditorGUILayout.PropertyField(m_boxSize, label);
            
            EditorGUI.BeginChangeCheck();
            label = new GUIContent("本地空间中的边界");
            EditorGUILayout.PropertyField(m_boundsInLocalSpace, label);
            if (EditorGUI.EndChangeCheck())
            {
                RadiantVirtualEmitter vi = (RadiantVirtualEmitter)target;
                if (m_boundsInLocalSpace.boolValue)
                {
                    m_boxCenter.vector3Value = Vector3.zero;
                }
                else
                {
                    m_boxCenter.vector3Value = vi.transform.position;
                }
                vi.SetBounds(new Bounds(m_boxCenter.vector3Value, m_boxSize.vector3Value));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }


    public static class RadiantVirtualEmitterEditorExtension
    {
        [MenuItem("GameObject/Create Other/Radiant GI/Virtual Emitter")]
        static void CreateEmitter(MenuCommand menuCommand)
        {
            GameObject emitter = new GameObject("Radiant Virtual Emitter", typeof(RadiantVirtualEmitter));

            GameObjectUtility.SetParentAndAlign(emitter, menuCommand.context as GameObject);

            Undo.RegisterCreatedObjectUndo(emitter, "Create Virtual Emitter");
            Selection.activeObject = emitter;
        }
    }
}