using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace Garena.TA.VolumetricLightingFog
{

    [CustomEditor(typeof(GlobalVolumetricLightingFogFeature))]
    public class GlobalVolumetricLightingFogFeatureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var feature = (GlobalVolumetricLightingFogFeature)target;

            var settings = feature.Setting;

            // 自定义 UI
            settings.DownsampleDepthShader = (Shader)EditorGUILayout.ObjectField("Downsample Depth Shader", settings.DownsampleDepthShader, typeof(Shader), false);
            settings.VolumetricLightingShader = (Shader)EditorGUILayout.ObjectField("Volumetric Shader", settings.VolumetricLightingShader, typeof(Shader), false);
            settings.Downsample = (DownsampleFactor)EditorGUILayout.EnumPopup("Downsample", settings.Downsample);
            
            //settings.StepSize = EditorGUILayout.Slider("Ray Marching Setp Size", settings.StepSize, 0.1f, 100);
            //settings.BlurIterations = EditorGUILayout.IntSlider(new GUIContent("Blur Iterations"), settings.BlurIterations, 0, 3);

            //settings.OpenVolumetricLight = EditorGUILayout.Toggle("Open Volumetric Lighting", settings.OpenVolumetricLight);
            //if (settings.OpenVolumetricLight)
            //{
            //    settings.LightingScale = EditorGUILayout.Slider(new GUIContent("Lighting Scale"), settings.LightingScale, 0.01f, 20);
            //    settings.LightingContrast = EditorGUILayout.Slider(new GUIContent("Lighting Contrast"), settings.LightingContrast, 0, 1);
            //    settings.LightingAnisotropy = EditorGUILayout.Slider(new GUIContent("Lighting Anisotropy"), settings.LightingAnisotropy, -1, 1);
            //    settings.VolumetricLightMinHeight = EditorGUILayout.FloatField("Volumetric Lighting Min Height", settings.VolumetricLightMinHeight);
            //    settings.VolumetricLightMaxHeight = EditorGUILayout.FloatField("Volumetric LightingFog Max Height", settings.VolumetricLightMaxHeight);
            //    settings.VolumetricLightDensity = EditorGUILayout.Slider("Volumetric Lighting Density", settings.VolumetricLightDensity, 0, 1);
            //}
            
            ////
            //settings.OpenVolumetricFog = EditorGUILayout.Toggle("Open Volumetric Fog", settings.OpenVolumetricFog);
            //if (settings.OpenVolumetricFog)
            //{
            //    settings.VolumetricFogColor = EditorGUILayout.ColorField(new GUIContent("Volumetric Fog Color"), settings.VolumetricFogColor, true, true, true);
            //    settings.VolumetricFogMinHeight = EditorGUILayout.FloatField("Volumetric Fog Min Height", settings.VolumetricFogMinHeight);
            //    settings.VolumetricFogMaxHeight = EditorGUILayout.FloatField("Volumetric Fog Max Height", settings.VolumetricFogMaxHeight);
            //    settings.VolumetricFogDensity = EditorGUILayout.Slider("Volumetric Fog Density", settings.VolumetricFogDensity, 0, 10);
            //    settings.FadeoutDistance = EditorGUILayout.FloatField("Volumetric Fog Fadeout Distance", settings.FadeoutDistance);
            //    settings.FadeoutDistance = settings.FadeoutDistance < 1 ? 1 : settings.FadeoutDistance;
            //}
            
            // 标记为已修改
            if (GUI.changed)
            {
                EditorUtility.SetDirty(feature);
            }
        }
    }
}