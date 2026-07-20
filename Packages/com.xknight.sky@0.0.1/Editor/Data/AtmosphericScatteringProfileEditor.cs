using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(AtmosphericScatteringProfile))]
    public partial class AtmosphericScatteringProfileEditor : Editor
    {
        private bool cloudFolder = false;
        private bool timeFolder = false;
        private bool atmosphereFolder = false;
        private bool volumetricLightFolder = false;

        public override void OnInspectorGUI()
        {
            AtmosphericScatteringProfile rootProfile = target as AtmosphericScatteringProfile;
            if(rootProfile.timeProfile == null || 
                rootProfile.cloudProfile == null ||
                rootProfile.atmosphereProfile == null ||
                rootProfile.volumetricLightProfile == null)
            {
                FixProfileRefrence(rootProfile);
            }

            #region 时间
            timeFolder = CommonEditorGUI.DrawSubFolder(new GUIContent("时间参数"), timeFolder);
            CoreEditorUtils.DrawSplitter(false);
            if (timeFolder)
            {
                CommonEditorGUI.BeginBoxContent();
                DrawTimeSettingSections(rootProfile);
                CommonEditorGUI.EndBoxContent();
            }
            CommonEditorGUI.EndSubFolder();
            #endregion

            #region 云
            cloudFolder = CommonEditorGUI.DrawSubFolder(new GUIContent("云"), cloudFolder);
            CoreEditorUtils.DrawSplitter(false);
            if (cloudFolder)
            {
                CommonEditorGUI.BeginBoxContent();
                DrawCloudSettingSections(rootProfile);
                CommonEditorGUI.EndBoxContent();

            }
            CommonEditorGUI.EndSubFolder();
            #endregion

            #region Atmosphere
            atmosphereFolder = CommonEditorGUI.DrawSubFolder(new GUIContent("大气散射"), atmosphereFolder);
            CoreEditorUtils.DrawSplitter(false);
            if (atmosphereFolder)
            {
                CommonEditorGUI.BeginBoxContent();
                DrawAtmosphereSections(rootProfile);
                CommonEditorGUI.EndBoxContent();

            }
            CommonEditorGUI.EndSubFolder();
            #endregion

            #region VolumetricLight
            volumetricLightFolder = CommonEditorGUI.DrawSubFolder(new GUIContent("体积光"), volumetricLightFolder);
            CoreEditorUtils.DrawSplitter(false);
            if (volumetricLightFolder)
            {
                CommonEditorGUI.BeginBoxContent();
                DrawVolumetricLightSections(rootProfile);
                CommonEditorGUI.EndBoxContent();

            }
            CommonEditorGUI.EndSubFolder();
            #endregion

            serializedObject.ApplyModifiedProperties();
            
        }

        //Scriptable 嵌套层会丢引用 ，需要另外维护一份引用清单或在手动规则修复引用
        private void FixProfileRefrence(AtmosphericScatteringProfile rootProfile)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetOrScenePath(target));

            bool cloudFixed = false;
            bool timeFixed = false;
            bool atmosphereFixed = false;
            bool volumetricLightFixed = false;
            foreach (var item in allAssets)
            {
                if (item is CloudProfile)
                {
                    rootProfile.cloudProfile = item as CloudProfile;
                    cloudFixed = true;
                }
                if (item is TimeProfile)
                {
                    rootProfile.timeProfile = item as TimeProfile;
                    timeFixed = true;
                }
                if (item is AtmosphereProfile)
                {
                    rootProfile.atmosphereProfile = item as AtmosphereProfile;
                    atmosphereFixed = true;
                }
                if (item is VolumetricLightProfile)
                {
                    rootProfile.volumetricLightProfile = item as VolumetricLightProfile;
                    volumetricLightFixed = true;
                }
            }
            if (!timeFixed)
            {
                rootProfile.timeProfile = CreateInstance<TimeProfile>();
                rootProfile.timeProfile.name = "TimeProfile";
                AssetDatabase.AddObjectToAsset(rootProfile.timeProfile, rootProfile);
                AssetDatabase.SaveAssets();
            }
            if (!cloudFixed)
            {
                rootProfile.cloudProfile = CreateInstance<CloudProfile>();
                rootProfile.cloudProfile.name = "CloudProfile";
                AssetDatabase.AddObjectToAsset(rootProfile.cloudProfile, rootProfile);
                AssetDatabase.SaveAssets();
            }
            if (!atmosphereFixed)
            {
                rootProfile.atmosphereProfile = CreateInstance<AtmosphereProfile>();
                rootProfile.atmosphereProfile.name = "AtmosphereProfile";
                AssetDatabase.AddObjectToAsset(rootProfile.atmosphereProfile, rootProfile);
                AssetDatabase.SaveAssets();
            }
            if (!volumetricLightFixed)
            {
                rootProfile.volumetricLightProfile = CreateInstance<VolumetricLightProfile>();
                rootProfile.volumetricLightProfile.name = "VolumetricLightProfile";
                AssetDatabase.AddObjectToAsset(rootProfile.volumetricLightProfile, rootProfile);
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.SaveAssets();
        }

        private void FixedMapScriptable(AtmosphericScatteringProfile rootProfile)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetOrScenePath(rootProfile));
            bool cloudMapFixed = false;
            foreach (var item in allAssets)
            {
                if (item is CloudProfileMap)
                {
                    rootProfile.cloudProfileMap = item as CloudProfileMap;
                    cloudMapFixed = true;
                }
            }

            if (!cloudMapFixed)
            {
                rootProfile.cloudProfileMap = CreateInstance<CloudProfileMap>();
                rootProfile.cloudProfileMap.name = "cloudProfileMap";
                AssetDatabase.AddObjectToAsset(rootProfile.cloudProfileMap, rootProfile);
                AssetDatabase.SaveAssets();
            }
        }
    }
}