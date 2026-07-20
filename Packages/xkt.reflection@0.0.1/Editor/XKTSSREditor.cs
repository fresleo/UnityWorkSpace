using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using XKnight.Reflection.Runtime;

namespace XKT.Reflection.Editor{
[CustomEditor(typeof(XKTSSR))]
public class XKTSSREditor : VolumeComponentEditor
{
    XKTReflections[] reflections;
    readonly StringBuilder sb = new StringBuilder();
    SerializedDataParameter reflectionsMultiplier, reflectionsMinIntensity, reflectionsMaxIntensity;
    SerializedDataParameter smoothnessThreshold;
    SerializedDataParameter refineThickness, thicknessFine;
    SerializedDataParameter downsampling;
    SerializedDataParameter depthBias;
    SerializedDataParameter outputMode, separationPos, stopNaN;

    SerializedDataParameter
        sampleCount, maxRayLength, thickness, binarySearchIterations, decay, jitter; //, animatedJitter;

    SerializedDataParameter fresnel, fuzzyness, contactHardening;

    SerializedDataParameter
        blurDownsampling,
        minimumBlur,
        blurStrength,
        specularControl,
        specularSoftenPower; //, metallicBoost, metallicBoostThreshold;

    SerializedDataParameter skyboxIntensity,
        skyboxResolution,
        skyboxUpdateMode,
        skyboxUpdateInterval,
        skyboxContributionPass,
        skyboxCustomCubemap;

    SerializedDataParameter nearCameraAttenuationStart, nearCameraAttenuationRange;

    SerializedDataParameter vignetteSize, vignettePower;
    SerializedDataParameter reflectionsScriptsLayerMask;

    public override void OnEnable()
    {
        base.OnEnable();

        var o = new PropertyFetcher<XKTSSR>(serializedObject);

        reflectionsMultiplier = Unpack(o.Find(x => x.reflectionsMultiplier));

        smoothnessThreshold = Unpack(o.Find(x => x.smoothnessThreshold));
        reflectionsMinIntensity = Unpack(o.Find(x => x.reflectionsMinIntensity));
        reflectionsMaxIntensity = Unpack(o.Find(x => x.reflectionsMaxIntensity));
        //computeBackFaces = Unpack(o.Find(x => x.computeBackFaces));
        //computeBackFacesLayerMask = Unpack(o.Find(x => x.computeBackFacesLayerMask));
        refineThickness = Unpack(o.Find(x => x.refineThickness));
        thicknessFine = Unpack(o.Find(x => x.thicknessFine));
        downsampling = Unpack(o.Find(x => x.downsampling));
        depthBias = Unpack(o.Find(x => x.depthBias));
        outputMode = Unpack(o.Find(x => x.outputMode));
        separationPos = Unpack(o.Find(x => x.separationPos));
        //lowPrecision = Unpack(o.Find(x => x.lowPrecision));
        stopNaN = Unpack(o.Find(x => x.stopNaN));

        // temporalFilter = Unpack(o.Find(x => x.temporalFilter));
        // temporalFilterResponseSpeed = Unpack(o.Find(x => x.temporalFilterResponseSpeed));
        sampleCount = Unpack(o.Find(x => x.sampleCount));
        maxRayLength = Unpack(o.Find(x => x.maxRayLength));
        binarySearchIterations = Unpack(o.Find(x => x.binarySearchIterations));
        thickness = Unpack(o.Find(x => x.thickness));
        //thicknessFine = Unpack(o.Find(x => x.thicknessFine));
        //refineThickness = Unpack(o.Find(x => x.refineThickness));
        decay = Unpack(o.Find(x => x.decay));
        fresnel = Unpack(o.Find(x => x.fresnel));
        fuzzyness = Unpack(o.Find(x => x.fuzzyness));
        contactHardening = Unpack(o.Find(x => x.contactHardening));
        minimumBlur = Unpack(o.Find(x => x.minimumBlur));
        jitter = Unpack(o.Find(x => x.jitter));
        //animatedJitter = Unpack(o.Find(x => x.animatedJitter));
        blurDownsampling = Unpack(o.Find(x => x.blurDownsampling));
        blurStrength = Unpack(o.Find(x => x.blurStrength));
        specularControl = Unpack(o.Find(x => x.specularControl));
        specularSoftenPower = Unpack(o.Find(x => x.specularSoftenPower));
        // metallicBoost = Unpack(o.Find(x => x.metallicBoost));
        // metallicBoostThreshold = Unpack(o.Find(x => x.metallicBoostThreshold));
        skyboxIntensity = Unpack(o.Find(x => x.skyboxIntensity));
        skyboxResolution = Unpack(o.Find(x => x.skyboxResolution));
        skyboxUpdateMode = Unpack(o.Find(x => x.skyboxUpdateMode));
        skyboxUpdateInterval = Unpack(o.Find(x => x.skyboxUpdateInterval));
        skyboxContributionPass = Unpack(o.Find(x => x.skyboxContributionPass));
        skyboxCustomCubemap = Unpack(o.Find(x => x.skyboxCustomCubemap));
        // useCustomBounds = Unpack(o.Find(x => x.useCustomBounds));
        nearCameraAttenuationStart = Unpack(o.Find(x => x.nearCameraAttenuationStart));
        nearCameraAttenuationRange = Unpack(o.Find(x => x.nearCameraAttenuationRange));
        // boundsMin = Unpack(o.Find(x => x.boundsMin));
        // boundsMax = Unpack(o.Find(x => x.boundsMax));
        vignetteSize = Unpack(o.Find(x => x.vignetteSize));
        vignettePower = Unpack(o.Find(x => x.vignettePower));
        reflectionsScriptsLayerMask = Unpack(o.Find(x => x.reflectionsScriptsLayerMask));

        reflections = UnityEngine.Object.FindObjectsOfType<XKTReflections>(true);
    }

    public override void OnInspectorGUI()
    {
        var pipe = XKnightRenderPipeline.asset;
        if (!pipe.supportsCameraDepthTexture)
        {
            EditorGUILayout.HelpBox("需要深度纹理", MessageType.Warning);
        }

        if (pipe != null)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (GUILayout.Button("Show XKnight Renderer Settings"))
            {
                var so = new SerializedObject(pipe);
                var prop = so.FindProperty("m_RendererDataList");
                if (prop != null && prop.arraySize > 0)
                {
                    var o = prop.GetArrayElementAtIndex(0);
                    if (o != null)
                    {
                        Selection.SetActiveObjectWithContext(o.objectReferenceValue, null);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }


        serializedObject.Update();

        int reflectionsCount = reflections != null ? reflections.Length : 0;


        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("质量设置", EditorStyles.miniLabel);
        PropertyField(sampleCount, new GUIContent("步进次数"));
        PropertyField(maxRayLength, new GUIContent("最大反射距离"));
        PropertyField(thickness, new GUIContent("最大碰撞厚度"));
        PropertyField(depthBias, new GUIContent("深度偏移"));
        PropertyField(binarySearchIterations, new GUIContent("二分搜索迭代"));
        PropertyField(refineThickness, new GUIContent("开启精确检测"));
        if (refineThickness.value.boolValue) 
        {
            EditorGUI.indentLevel++;
            PropertyField(thicknessFine,new GUIContent("最大碰撞厚度变化系数（越小对于小物体的精准检测越友好"));
            EditorGUI.indentLevel--;
        }
        PropertyField(jitter, new GUIContent("抖动"));
        PropertyField(downsampling, new GUIContent("降分辨率"));
        PropertyField(decay, new GUIContent("反射图像保留(变大能够清除一些错误反射，若没有明显瑕疵或暂看不出问题使用默认值）","变大能够清除一些错误反射，若没有明显瑕疵或暂看不出问题使用默认值"));

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("反射强度调整", EditorStyles.miniLabel);
        PropertyField(reflectionsMultiplier, new GUIContent("反射强度倍数"));
        PropertyField(smoothnessThreshold, new GUIContent("Smoothness 阈值", "低于阈值不反射"));
        PropertyField(reflectionsMinIntensity, new GUIContent("最小强度限制", "默认0"));
        PropertyField(reflectionsMaxIntensity, new GUIContent("最大强度限制", "默认1"));
        PropertyField(fresnel, new GUIContent("菲涅尔强度"));
  
        PropertyField(specularControl);
        if (specularControl.value.boolValue)
        {
            EditorGUI.indentLevel++;
            PropertyField(specularSoftenPower, new GUIContent("Soften Power"));
            EditorGUI.indentLevel--;
        }

        //天空球反射
        //PropertyField(skyboxIntensity);
        // if (skyboxIntensity.value.floatValue > 0)
        // {
        //     EditorGUI.indentLevel++;
        //     PropertyField(skyboxUpdateMode, new GUIContent("Update Mode"));
        //     if (skyboxUpdateMode.value.intValue == (int)SkyboxUpdateMode.CustomCubemap)
        //     {
        //         PropertyField(skyboxCustomCubemap, new GUIContent("Cubemap"));
        //     }
        //     else
        //     {
        //         if (skyboxUpdateMode.value.intValue == (int)SkyboxUpdateMode.Interval)
        //         {
        //             PropertyField(skyboxUpdateInterval, new GUIContent("Interval"));
        //         }
        //
        //         PropertyField(skyboxResolution, new GUIContent("Resolution"));
        //     }
        //
        //     PropertyField(skyboxContributionPass, new GUIContent("Contribution Pass"));
        //
        //     EditorGUI.indentLevel--;
        // }

        PropertyField(nearCameraAttenuationStart, new GUIContent("反射开始距离"));
        if (nearCameraAttenuationStart.value.floatValue > 0)
        {
            EditorGUI.indentLevel++;
            PropertyField(nearCameraAttenuationRange, new GUIContent("反射范围"));
            EditorGUI.indentLevel--;
        }

        PropertyField(vignetteSize,new GUIContent("反射图像边缘渐隐(↑显示，↓渐隐)"));
        PropertyField(vignettePower,new GUIContent("边缘渐隐的曲线衰减"));

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("反射边缘模糊", EditorStyles.miniLabel);
       // PropertyField(fuzzyness, new GUIContent("模糊边界控制"));
        //PropertyField(contactHardening,new GUIContent("模糊距离控制"));

        PropertyField(minimumBlur,new GUIContent("最小模糊半径"));
        PropertyField(blurDownsampling,new GUIContent("模糊降采样"));
        PropertyField(blurStrength,new GUIContent("模糊强度"));

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("DebugModel", EditorStyles.miniLabel);
        PropertyField(outputMode, new GUIContent("输出模式（调试Bug用）"));
        if (outputMode.value.intValue == (int)OutputMode.SideBySideComparison)
        {
            EditorGUI.indentLevel++;
            PropertyField(separationPos);
            EditorGUI.indentLevel--;
        }

        //PropertyField(lowPrecision);
        //PropertyField(stopNaN, new GUIContent("Stop NaN"));
        

        if (reflectionsCount > 0)
        {
            EditorGUILayout.Separator();

            PropertyField(reflectionsScriptsLayerMask,
                new GUIContent("Layer Mask"));
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("场景中拥有反射组件的GameObject名称", EditorStyles.helpBox);
            for (int k = 0; k < reflectionsCount; k++)
            {
                XKTReflections refl = reflections[k];
                if (refl == null) continue;
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = refl.gameObject.activeInHierarchy;
                // if (GUILayout.Button(
                //         new GUIContent(refl.enabled ? bulbOnIcon : bulbOffIcon, "Toggle on/off this reflection"),
                //         EditorStyles.miniButton, GUILayout.Width(35)))
                // {
                //     refl.enabled = !refl.enabled;
                // }
                //
                // GUI.enabled = true;
                // if (GUILayout.Button(new GUIContent(deleteIcon, "Remove this reflection script"),
                //         EditorStyles.miniButton, GUILayout.Width(35)))
                // {
                //     if (EditorUtility.DisplayDialog("Confirmation",
                //             "Remove the reflection script on " + refl.gameObject.name + "?", "Ok", "Cancel"))
                //     {
                //         GameObject.DestroyImmediate(refl);
                //         reflections[k] = null;
                //         continue;
                //     }
                // }
                //
                // if (GUILayout.Button(new GUIContent(arrowRight, "Select this reflection script"),
                //         EditorStyles.miniButton, GUILayout.Width(35), GUILayout.Width(40)))
                // {
                //     Selection.activeObject = refl.gameObject;
                //     EditorGUIUtility.PingObject(refl.gameObject);
                //     GUIUtility.ExitGUI();
                // }

                GUI.enabled = refl.isActiveAndEnabled;
                sb.Clear();
                sb.Append(refl.name);
                if (!refl.gameObject.activeInHierarchy)
                {
                    sb.Append(" (hidden gameobject)");
                }

                if (refl.overrideGlobalSettings)
                {
                    sb.Append(" (uses custom settings)");
                }

                GUILayout.Label(sb.ToString());
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
        }
        else if (reflectionsCount == 0)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Reflections in Scene", EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "In forward rendering path, add a Reflections script to any object or group of objects that you want to get reflections.",
                MessageType.Info);
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            XKTReflections.needUpdateMaterials =
                true; // / reflections scripts that do not override global settings need to be updated as well
        }
    }
}
}