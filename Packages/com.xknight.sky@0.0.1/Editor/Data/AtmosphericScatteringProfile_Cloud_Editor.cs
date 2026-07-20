using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public partial class AtmosphericScatteringProfileEditor
    {
        SwatchesDrawParams cloudSwatchesDrawParams = new SwatchesDrawParams();

        private void DrawCloudSettingSections(AtmosphericScatteringProfile rootProfile)
        {
            var rootObject = new SerializedObject(rootProfile.cloudProfile);

            CoreEditorUtils.DrawHeader("云通用参数");
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudMeshes"), new GUIContent("云模型"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudMaterials"), new GUIContent("云材质"));

            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudCurlSpeed"), new GUIContent("云卷曲速度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudCurlTiling"), new GUIContent("云卷曲纹理Tiling"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudCurlAmplitude"), new GUIContent("云卷曲幅度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudSunBrightenIntensity"), new GUIContent("太阳照亮云的强度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudTransparency"), new GUIContent("云透明度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudCoverage"), new GUIContent("云层覆盖范围"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudVolumeChangeSpeed"), new GUIContent("云体积变化速度"));

            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudFrontAndBackBlendFactor"), new GUIContent("云受光和背光的混合系数"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudDarkBackColor"), new GUIContent("云暗部背光颜色"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudDarkFrontColor"), new GUIContent("云暗部受光颜色"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudLightBackColor"), new GUIContent("云亮部背光颜色"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudLightFrontColor"), new GUIContent("云亮部受光颜色"));

            CoreEditorUtils.DrawHeader("流云参数");
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudLayerMesh"), new GUIContent("流云模型"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudLayerMaterial"), new GUIContent("流云材质"));

            //EditorGUILayout.PropertyField(rootObject.FindProperty("CloudDirection"), new GUIContent("云方向"));
            //EditorGUILayout.PropertyField(rootObject.FindProperty("CloudHeight"), new GUIContent("云高度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudWispsSpeed"), new GUIContent("流云旋转速度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudWispsCoverage"), new GUIContent("流云覆盖范围"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudWispsOpacity"), new GUIContent("流云不透明度"));

            CoreEditorUtils.DrawHeader("云阴影参数");
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudShadowTex"), new GUIContent("云阴影纹理"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudShadowTiling"), new GUIContent("云阴影纹理Tiling"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudShadowSpeed"), new GUIContent("云阴影移动方向和速度"));
            EditorGUILayout.PropertyField(rootObject.FindProperty("CloudShadowColor"), new GUIContent("云阴影颜色及透明度"));

            #region swatches
            if (rootProfile.cloudProfileMap == null)
            {
                FixedMapScriptable(rootProfile);
            }

            var targetData = rootProfile.cloudProfileMap;
            CommonEditorGUI.DrawScriptableDictionarySwatches<string, CloudProfile>("Swatches", cloudSwatchesDrawParams, targetData,
                (key) =>//添加
                {
                    var cloudAsset = rootProfile.cloudProfile.CopyInstance(key.ToString());
                    targetData.Add(key, cloudAsset);
                    AssetDatabase.AddObjectToAsset(cloudAsset, targetData);
                    var rootSerializedNew = new SerializedObject(rootProfile);
                    rootSerializedNew.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                },
                (option, key,value) => //添加或移除
                {
                    if (option)//apply
                    {
                        rootProfile.cloudProfile = value;
                    }
                    else//remove
                    {
                        targetData.Remove(key);
                        AssetDatabase.RemoveObjectFromAsset(value);
                        var rootSerializedNew = new SerializedObject(rootProfile);
                        rootSerializedNew.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                    }
                });
            #endregion

            rootObject.ApplyModifiedProperties();
        }

    }
}