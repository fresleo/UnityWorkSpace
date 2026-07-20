using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using XKnight.Reflection.Runtime;

[CustomEditor(typeof(XKTReflections))]
public class XKTReflectionEditor : Editor
{
    SerializedProperty ignore, scope, layerMask, nameFilter, subMeshMask;
    SerializedProperty smoothness,overrideSmoothness;

    SerializedProperty materialSmoothnessMapPropertyName,
        materialSmoothnessIntensityPropertyName,
        materialReflectivityPropertyName;

    SerializedProperty fresnel, fuzzyness, contactHardening;
    SerializedProperty overrideGlobalSettings;

    SerializedProperty sampleCount,
        maxRayLength,
        thickness,
        binarySearchIterations,
        refineThickness,
        thicknessFine,
        decay,
        jitter;
    SerializedProperty uvDistortionSpeed;
    Volume volume;
    XKTReflections ssr;


    private void OnEnable()
    {
        ignore = serializedObject.FindProperty("ignore");
        scope = serializedObject.FindProperty("scope");
        layerMask = serializedObject.FindProperty("layerMask");
        nameFilter = serializedObject.FindProperty("nameFilter");
        subMeshMask = serializedObject.FindProperty("subMeshMask");
        smoothness = serializedObject.FindProperty("smoothness");

        // smoothnessSource = serializedObject.FindProperty("smoothnessSource");
        // customSmoothnessMap = serializedObject.FindProperty("customSmoothnessMap");
        overrideSmoothness = serializedObject.FindProperty("overrideSmoothness");
        //materialSmoothnessMapPropertyName = serializedObject.FindProperty("materialSmoothnessMapPropertyName");
        materialSmoothnessIntensityPropertyName =
            serializedObject.FindProperty("materialSmoothnessIntensityPropertyName");
        materialReflectivityPropertyName = serializedObject.FindProperty("materialReflectivityPropertyName");
        fresnel = serializedObject.FindProperty("fresnel");
        fuzzyness = serializedObject.FindProperty("fuzzyness");
        contactHardening = serializedObject.FindProperty("contactHardening");
        overrideGlobalSettings = serializedObject.FindProperty("overrideGlobalSettings");
        sampleCount = serializedObject.FindProperty("sampleCount");
        maxRayLength = serializedObject.FindProperty("maxRayLength");
        binarySearchIterations = serializedObject.FindProperty("binarySearchIterations");
        thickness = serializedObject.FindProperty("thickness");
        refineThickness = serializedObject.FindProperty("refineThickness");
        thicknessFine = serializedObject.FindProperty("thicknessFine");
        decay = serializedObject.FindProperty("decay");
        jitter = serializedObject.FindProperty("jitter");

        ssr = (XKTReflections)target;
        XKTReflections.currentEditingXktReflections = ssr;
        FindXKTSSRRVolume();
    }

    void FindXKTSSRRVolume()
    {
        Volume[] vols = FindObjectsOfType<Volume>(true);
        foreach (Volume volume in vols)
        {
            if (volume.sharedProfile != null && volume.sharedProfile.Has<XKTSSR>())
            {
                this.volume = volume;
                return;
            }
        }
    }

    private void OnDisable()
    {
        XKTReflections.currentEditingXktReflections = null;
    }

    public override void OnInspectorGUI()
    {
        XKnightRenderPipelineAsset pipe = GraphicsSettings.currentRenderPipeline as XKnightRenderPipelineAsset;
        if (!XKTSSRRenderFeature.installed)
        {
            EditorGUILayout.HelpBox("需要在RenderFeature中添加XKRSSRRenderFeaure", MessageType.Error);
            if (GUILayout.Button("Go to Universal Rendering Pipeline Asset"))
            {
                Selection.activeObject = pipe;
            }

            EditorGUILayout.Separator();
            GUI.enabled = false;
        }
        else
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (volume != null)
            {
                if (GUILayout.Button("跳转后处理设置"))
                {
                    Selection.SetActiveObjectWithContext(volume, null);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ensure submesh array size matches materials count
        XKTReflections refl = (XKTReflections)target;
       
        XKTReflections.currentEditingXktReflections = refl;

        serializedObject.Update();

        EditorGUILayout.PropertyField(ignore);
        if (!ignore.boolValue)
        {
            if (refl.renderers?.Count == 0)
            {
                if (scope.intValue == (int)Scope.OnlyThisObject)
                {
                    EditorGUILayout.HelpBox(
                        "当前节点没有发现需要渲染的Render",MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("当前节点及其子节点没有需要渲染的Render", MessageType.Warning);
                }
            }

            EditorGUILayout.PropertyField(scope);
            if (scope.intValue == (int)Scope.IncludeChildren)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(layerMask);
                EditorGUILayout.PropertyField(nameFilter);
                EditorGUILayout.PropertyField(subMeshMask);
                EditorGUI.indentLevel--;
            }

            //TODO:后续支持smoothness贴图自定义
            // EditorGUILayout.PropertyField(smoothnessSource);
            // bool usesMaterialSmoothness = smoothnessSource.intValue == (int)SmoothnessSource.Material;
            EditorGUI.indentLevel++;
            // switch (smoothnessSource.intValue)
            // {
            //     case (int)SmoothnessSource.Material:
            //         EditorGUILayout.LabelField("Default values (used only if not present in the material):",
            //             EditorStyles.miniLabel);
            //         EditorGUILayout.PropertyField(customSmoothnessMap, new GUIContent("Smoothness Map (A)"));
            //         EditorGUILayout.PropertyField(metallic);
            //         EditorGUILayout.PropertyField(smoothness);
            //         break;
            //     case (int)SmoothnessSource.Custom:
            //         EditorGUILayout.PropertyField(customSmoothnessMap, new GUIContent("Smoothness Map (A)"));
            //         if (!perSubMeshSmoothness.boolValue)
            //         {
            //             EditorGUILayout.PropertyField(metallic);
            //             EditorGUILayout.PropertyField(smoothness);
            //         }
            //
            //         EditorGUILayout.PropertyField(perSubMeshSmoothness, new GUIContent("Per SubMesh Values"));
            //         if (perSubMeshSmoothness.boolValue)
            //         {
            //             EditorGUILayout.PropertyField(subMeshSettings, new GUIContent("Intensities"), true);
            //         }
            //
            //         break;
            // }

            EditorGUI.indentLevel--;
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("获取材质属性", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(overrideSmoothness,new GUIContent("自定义Smoothness"));
            bool usesMaterialSmoothness = overrideSmoothness.boolValue ;
            if (usesMaterialSmoothness)
            {
                EditorGUILayout.PropertyField(smoothness,new GUIContent("smoothness"));
            }
            else
            {
                // EditorGUILayout.PropertyField(materialSmoothnessMapPropertyName,
                //     new GUIContent("材质上SmoothnessMap名称" ));
                EditorGUILayout.PropertyField(materialSmoothnessIntensityPropertyName,
                    new GUIContent("材质上Smoothness名称"));
            }
        
            // if (usesMaterialSmoothness || perSubMeshSmoothness.boolValue || usesMaterialNormal)
            // {
            //     EditorGUILayout.Separator();
            //     EditorGUILayout.LabelField("Material Property Names", EditorStyles.miniBoldLabel);
            //     if (usesMaterialSmoothness || perSubMeshSmoothness.boolValue)
            //     {
            //         EditorGUILayout.PropertyField(materialSmoothnessMapPropertyName,
            //             new GUIContent("Smoothness Map", "The material property name for the smoothness map"));
            //         EditorGUILayout.PropertyField(materialReflectivityPropertyName,
            //             new GUIContent("Metallic", "The material property name for the metallic intensity"));
            //         EditorGUILayout.PropertyField(materialSmoothnessIntensityPropertyName,
            //             new GUIContent("Smoothness", "The material property name for the smoothness intensity"));
            //     }
            //
            // }

            EditorGUILayout.PropertyField(overrideGlobalSettings, new GUIContent("当前节点自定义SSR属性"));
            if (overrideGlobalSettings.boolValue)
            {
                EditorGUI.indentLevel++;
            
                EditorGUILayout.PropertyField(sampleCount, new GUIContent("步进次数"));
                EditorGUILayout.PropertyField(maxRayLength,new GUIContent("最大反射距离"));
                EditorGUILayout.PropertyField(thickness ,new GUIContent("最大碰撞厚度"));
                EditorGUILayout.PropertyField(binarySearchIterations, new GUIContent("二分搜索迭代"));
                EditorGUILayout.PropertyField(refineThickness, new GUIContent("开启精确碰撞检测"));
                if (refineThickness.boolValue)
                {
                    EditorGUILayout.PropertyField(thicknessFine,new GUIContent("精确碰撞阈值"));
                }

                EditorGUILayout.PropertyField(jitter, new GUIContent("抖动"));
                EditorGUILayout.PropertyField(fresnel, new GUIContent("菲涅尔强度"));
                EditorGUILayout.PropertyField(decay, new GUIContent("反射图像保留(变大能够清除一些错误反射，若没有明显瑕疵或暂看不出问题使用默认值）","变大能够清除一些错误反射，若没有明显瑕疵或暂看不出问题使用默认值"));
                EditorGUILayout.PropertyField(fuzzyness, new GUIContent("模糊边界控制"));
                EditorGUILayout.PropertyField(contactHardening,new GUIContent("模糊距离控制"));
                EditorGUI.indentLevel--;
            }
        }

        //EditorGUILayout.PropertyField(uvDistortionSpeed,new GUIContent("UV Distortion Speed"));

        serializedObject.ApplyModifiedProperties();
    }


   
}