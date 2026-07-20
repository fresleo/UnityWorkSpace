// Created By: WangYu  Date: 2024-01-09

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Setting
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TerrainMeshConfig))]
    public class TerrainMeshConfigInspector : AbsInspector<TerrainMeshConfig>
    {
        private SerializedProperty m_mixMatPaths_prop, m_bakeVTDiffuseMatPaths_prop, m_bakeVTNormalMatPaths_prop, m_bakedVTMatPath_prop;
        private SerializedProperty m_meshDataPack_prop, m_meshPrefix_prop;
        private SerializedProperty m_treeDataPath_prop;
        private SerializedProperty m_heightMapPath_prop, m_heightmapWorldY_prop, m_heightmapScale_prop, m_heightmapResolution_prop;
        private SerializedProperty m_lightmapData_prop;
        
        protected override void ExecuteOnEnable(TerrainMeshConfig script)
        {
            base.ExecuteOnEnable(script);

            m_mixMatPaths_prop = serializedObject.FindProperty(nameof(script.mixMatPaths));
            m_bakeVTDiffuseMatPaths_prop = serializedObject.FindProperty(nameof(script.bakeVTDiffuseMatPaths));
            m_bakeVTNormalMatPaths_prop = serializedObject.FindProperty(nameof(script.bakeVTNormalMatPaths));
            m_bakedVTMatPath_prop = serializedObject.FindProperty(nameof(script.bakedVTMatPath));

            m_meshDataPack_prop = serializedObject.FindProperty(nameof(script.meshDataPack));
            m_meshPrefix_prop = serializedObject.FindProperty(nameof(script.meshPrefix));

            m_treeDataPath_prop = serializedObject.FindProperty(nameof(script.treeDataPath));

            m_heightMapPath_prop = serializedObject.FindProperty(nameof(script.heightMapPath));
            m_heightmapWorldY_prop = serializedObject.FindProperty(nameof(script.heightmapWorldY));
            m_heightmapScale_prop = serializedObject.FindProperty(nameof(script.heightmapScale));
            m_heightmapResolution_prop = serializedObject.FindProperty(nameof(script.heightmapResolution));

            m_lightmapData_prop = serializedObject.FindProperty(nameof(script.lightmapData));
        }

        protected override void DrawAutoApplyGUI(TerrainMeshConfig script)
        {
            EditorGUILayout.PropertyField(m_mixMatPaths_prop, new GUIContent("混合模式的材质", "就是地形原始的材质"));
            EditorGUILayout.Space(5);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("VT材质");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_bakeVTDiffuseMatPaths_prop, new GUIContent("烘焙漫反射纹理的材质"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_bakeVTNormalMatPaths_prop, new GUIContent("烘焙法线纹理的材质"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_bakedVTMatPath_prop, new GUIContent("渲染烘焙后纹理的材质"));
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("mesh");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_meshDataPack_prop, new GUIContent("mesh 数据包容量"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_meshPrefix_prop, new GUIContent("mesh 的前缀"));
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.PropertyField(m_treeDataPath_prop, new GUIContent("4叉树数据"));
            EditorGUILayout.Space(5);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("高度图");
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(m_heightMapPath_prop, new GUIContent("高度图数据"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_heightmapWorldY_prop, new GUIContent("高度图的世界空间Y值"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_heightmapScale_prop, new GUIContent("高度图缩放"));
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(m_heightmapResolution_prop, new GUIContent("高度图分辨率"));
                EditorGUILayout.Space(5);
                
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.PropertyField(m_lightmapData_prop, new GUIContent("Lightmap 配置数据"));
            EditorGUILayout.Space(5);
        }
    }
}