// Created By: WangYu  Date: 2023-12-07

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.InstancedObject;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.InstancedObject
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(InstancedObjectMarker))]
    public class InstancedObjectMarkerInspector : AbsInspector<InstancedObjectMarker>
    {
        private SerializedProperty m_master_prop;
        private SerializedProperty m_targetGo_prop;
        private SerializedProperty m_targetBnd_prop, m_customTriggerBnd_prop, m_triggerBnd_prop;
        private SerializedProperty m_cubeCenter_prop, m_cubeSize_prop;
        private SerializedProperty m_lightmapIndex_prop, m_lightmapScaleOffset_prop;

        
        protected override void ExecuteOnEnable(InstancedObjectMarker script)
        {
            base.ExecuteOnEnable(script);
            
            m_master_prop = serializedObject.FindProperty(InstancedObjectMarker.Master_PropName);
            m_targetGo_prop = serializedObject.FindProperty(nameof(script.targetGo));
            
            m_targetBnd_prop = serializedObject.FindProperty(InstancedObjectMarker.TargetBnd_PropName);
            m_customTriggerBnd_prop = serializedObject.FindProperty(InstancedObjectMarker.CustomTriggerBnd_PropName);
            m_triggerBnd_prop = serializedObject.FindProperty(nameof(script.triggerBnd));
            
            m_cubeCenter_prop = serializedObject.FindProperty(InstancedObjectMarker.CubeCenter_PropName);
            m_cubeSize_prop = serializedObject.FindProperty(InstancedObjectMarker.CubeSize_PropName);

            m_lightmapIndex_prop = serializedObject.FindProperty(InstancedObjectMarker.LightmapIndex_PropName);
            m_lightmapScaleOffset_prop = serializedObject.FindProperty(InstancedObjectMarker.LightmapScaleOffset_PropName);
        }

        protected override void DrawAutoApplyGUI(InstancedObjectMarker script)
        {
            EditorGUI.BeginDisabledGroup(true);
            {
                EditorGUILayout.PropertyField(m_master_prop, new GUIContent("归属的生成器"));
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(m_targetGo_prop, new GUIContent("目标对象"));
                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(m_targetBnd_prop, new GUIContent("目标包围盒"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_triggerBnd_prop, new GUIContent("触发器包围盒"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_lightmapIndex_prop, new GUIContent("Lightmap 索引"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_lightmapScaleOffset_prop, new GUIContent("Lightmap 缩放偏移"));
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_customTriggerBnd_prop, new GUIContent("自定义触发包围盒"));
                EditorGUILayout.Space(5);

                if (m_customTriggerBnd_prop.boolValue)
                {
                    var btnWidth = GUILayout.Width(90);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(m_cubeCenter_prop, new GUIContent("触发包围盒的中心位置"));
                        if (GUILayout.Button("同步目标数据", btnWidth))
                        {
                            m_cubeCenter_prop.vector3Value = m_targetBnd_prop.boundsValue.center;
                        }
                    }
                    EditorGUILayout.Space(5);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(m_cubeSize_prop, new GUIContent("触发包围盒的尺寸"));
                        if (GUILayout.Button("同步目标数据", btnWidth))
                        {
                            m_cubeSize_prop.vector3Value = m_targetBnd_prop.boundsValue.size;
                        }
                    }
                }
                
                EditorGUILayout.Space(5);
            }
        }

        protected override void ExecuteOnSceneGUI(InstancedObjectMarker script)
        {
            base.ExecuteOnSceneGUI(script);

            if (!script.enabled)
            {
                return;
            }
            
            GUIUtils.DrawCubeVolume(script);
        }
        
    }
}