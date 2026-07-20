// Created by: WangYu   Date: 2025-11-03

using System;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    [CustomEditor (typeof(StoredTimeOfDayData))]
    [CanEditMultipleObjects]
    public class StoredTimeOfDayDataEditor : Editor
    {
        internal class Styles
        {
            public static GUIContent header = new("TOD 存储的时间段数据");

            public static GUIContent creationDate = new("创建日期");

            public static GUIContent sceneName = new("场景名");
            public static GUIContent phaseName = new("阶段名");

            public static GUIContent sunSourceName = new("太阳光源名");

            public static GUIContent reflectionProbeDatas = new("反射探针数据");
            public static GUIContent lightSourceDatas = new("光源数据");
            public static GUIContent volumeDatas = new("后处理配置");

            public static GUIContent bakedGITint = new("中性图调色");
            public static GUIContent characterOverrideGIData = new("覆盖角色 GI 的数据");

            public static GUIContent activeDatas = new("激活数据");
            public static GUIContent lightmapUniquenessDatas = new("Lightmap 烘焙数据");
            public static GUIContent lightmapDataCopys = new("LightmapData 的拷贝");
            public static GUIContent characterShadowSettings = new("角色的阴影设置");
        }

        private SerializedProperty m_creationDate;
        
        private SerializedProperty m_sceneName;
        private SerializedProperty m_phaseName;

        private SerializedProperty m_unityFogSettings;
        private ISettingsEditor m_unityFogSettingsEditor;

        private SerializedProperty m_sunSourceName;

        private SerializedProperty m_skyboxSettings;
        private ISettingsEditor m_skyboxSettingsEditor;

        private SerializedProperty m_environmentLightingSettings;
        private ISettingsEditor m_environmentLightingSettingsEditor;

        private SerializedProperty m_environmentReflectionsSettings;
        private ISettingsEditor m_environmentReflectionsSettingsEditor;

        private SerializedProperty m_lightProbeData;
        private ISettingsEditor m_lightProbeDataEditor;

        private SerializedProperty m_reflectionProbeDatas;
        private ISettingsEditor m_reflectionProbeDatasEditor;

        private SerializedProperty m_lightSourceDatas;
        private ISettingsEditor m_lightSourceDatasEditor;

        private SerializedProperty m_volumeDatas;
        private ISettingsEditor m_volumeDatasEditor;

        private SerializedProperty m_bakedGITint;
        private SerializedProperty m_characterOverrideGIData;

        private SerializedProperty m_activeDatas;
        private ISettingsEditor m_activeDatasEditor;

        private SerializedProperty m_lightmapUniquenessDatas;
        private ISettingsEditor m_lightmapUniquenessDatasEditor;

        private SerializedProperty m_lightmapDataCopys;
        private ISettingsEditor m_lightmapDataCopysEditor;

        private SerializedProperty m_characterShadowSettings;
        private ISettingsEditor m_characterShadowSettingsEditor;
        
        StoredTimeOfDayData CurrentTarget => this.target as StoredTimeOfDayData;
        
        private void OnEnable()
        {
            if (CurrentTarget == null) return;
            
            m_creationDate = serializedObject.FindProperty(nameof(StoredTimeOfDayData.creationDate));
            
            m_sceneName = serializedObject.FindProperty(nameof(StoredTimeOfDayData.sceneName));
            m_phaseName = serializedObject.FindProperty(nameof(StoredTimeOfDayData.phaseName));
            
            m_unityFogSettings = serializedObject.FindProperty(nameof(StoredTimeOfDayData.unityFogSettings));
            m_unityFogSettingsEditor = new UnityFogSettingsEditor(m_unityFogSettings);
            m_unityFogSettingsEditor.Enable();
            
            m_sunSourceName = serializedObject.FindProperty(nameof(StoredTimeOfDayData.sunSourceName));
            
            m_skyboxSettings = serializedObject.FindProperty(nameof(StoredTimeOfDayData.skyboxSettings));
            m_skyboxSettingsEditor = new SkyboxSettingsEditor(m_skyboxSettings);
            m_skyboxSettingsEditor.Enable();
            
            m_environmentLightingSettings = serializedObject.FindProperty(nameof(StoredTimeOfDayData.environmentLightingSettings));
            m_environmentLightingSettingsEditor = new EnvironmentLightingSettingsEditor(m_environmentLightingSettings);
            m_environmentLightingSettingsEditor.Enable();
            
            m_environmentReflectionsSettings = serializedObject.FindProperty(nameof(StoredTimeOfDayData.environmentReflectionsSettings));
            m_environmentReflectionsSettingsEditor = new EnvironmentReflectionsSettingsEditor(m_environmentReflectionsSettings);
            m_environmentReflectionsSettingsEditor.Enable();

            m_lightProbeData = serializedObject.FindProperty(nameof(StoredTimeOfDayData.lightProbeData));
            m_lightProbeDataEditor = new LightProbeDataEditor(m_lightProbeData);
            m_lightProbeDataEditor.Enable();
            
            m_reflectionProbeDatas = serializedObject.FindProperty(nameof(StoredTimeOfDayData.reflectionProbeDatas));
            m_reflectionProbeDatasEditor = new DrawArrayElementEditor(m_reflectionProbeDatas, Styles.reflectionProbeDatas);
            m_reflectionProbeDatasEditor.Enable();
            
            m_lightSourceDatas = serializedObject.FindProperty(nameof(StoredTimeOfDayData.lightSourceDatas));
            m_lightSourceDatasEditor = new DrawArrayElementEditor(m_lightSourceDatas, Styles.lightSourceDatas);
            m_lightSourceDatasEditor.Enable();
            
            m_volumeDatas = serializedObject.FindProperty(nameof(StoredTimeOfDayData.volumeDatas));
            m_volumeDatasEditor = new DrawArrayElementEditor(m_volumeDatas, Styles.volumeDatas);
            m_volumeDatasEditor.Enable();
            
            m_bakedGITint = serializedObject.FindProperty(nameof(StoredTimeOfDayData.bakedGITint));
            m_characterOverrideGIData = serializedObject.FindProperty(nameof(StoredTimeOfDayData.characterOverrideGIData));
            
            m_activeDatas = serializedObject.FindProperty(nameof(StoredTimeOfDayData.activeDatas));
            m_activeDatasEditor = new DrawArrayElementEditor(m_activeDatas, Styles.activeDatas);
            m_activeDatasEditor.Enable();
            
            m_lightmapUniquenessDatas = serializedObject.FindProperty(nameof(StoredTimeOfDayData.lightmapUniquenessDatas));
            m_lightmapUniquenessDatasEditor = new DrawArrayElementEditor(m_lightmapUniquenessDatas, Styles.lightmapUniquenessDatas);
            m_lightmapUniquenessDatasEditor.Enable();
            
            m_lightmapDataCopys = serializedObject.FindProperty(nameof(StoredTimeOfDayData.lightmapDataCopys));
            m_lightmapDataCopysEditor = new DrawArrayElementEditor(m_lightmapDataCopys, Styles.lightmapDataCopys);
            m_lightmapDataCopysEditor.Enable();
            
            m_characterShadowSettings = serializedObject.FindProperty(nameof(StoredTimeOfDayData.characterShadowSettings));
            m_characterShadowSettingsEditor = new DrawArrayElementEditor(m_characterShadowSettings, Styles.characterShadowSettings, new CharacterShadowSettingsDrawer());
            m_characterShadowSettingsEditor.Enable();
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            if (CurrentTarget == null) return;
            
            EditorGUILayout.LabelField(Styles.header, EditorStyles.boldLabel);
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_creationDate, Styles.creationDate);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_sceneName, Styles.sceneName);
            EditorGUILayout.PropertyField(m_phaseName, Styles.phaseName);
            
            EditorGUILayout.Space();
            m_unityFogSettingsEditor.InspectorGUI();
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_sunSourceName, Styles.sunSourceName);
            
            EditorGUILayout.Space();
            m_skyboxSettingsEditor.InspectorGUI();
            
            EditorGUILayout.Space();
            m_environmentLightingSettingsEditor.InspectorGUI();
            EditorGUILayout.Space();
            m_environmentReflectionsSettingsEditor.InspectorGUI();
            EditorGUILayout.Space();
            m_lightProbeDataEditor.InspectorGUI();
            EditorGUILayout.Space();
            m_reflectionProbeDatasEditor.InspectorGUI();
            EditorGUILayout.Space();
            m_lightSourceDatasEditor.InspectorGUI();
            EditorGUILayout.Space();
            m_volumeDatasEditor.InspectorGUI();
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_bakedGITint, Styles.bakedGITint);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_characterOverrideGIData, Styles.characterOverrideGIData);
            
            EditorGUILayout.Space();
            m_activeDatasEditor.InspectorGUI();
            EditorGUILayout.Space();
            m_lightmapUniquenessDatasEditor.InspectorGUI();
            EditorGUILayout.Space();
            m_lightmapDataCopysEditor.InspectorGUI();
            EditorGUILayout.Space();
            m_characterShadowSettingsEditor.InspectorGUI();
            
            EditorGUILayout.Space();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
    }
}