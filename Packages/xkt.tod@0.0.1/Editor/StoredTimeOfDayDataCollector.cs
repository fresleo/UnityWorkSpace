// Created By: WangYu  Date: 2025-03-17

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using XKT.TOD.DataStructure;
using XKT.TOD.Lightmap;
using XKT.TOD.Tag;
using XKT.TOD.Utils;

namespace XKT.TOD
{
    public class StoredTimeOfDayDataCollector
    {
        public IEnumerator Execute(StoredTimeOfDayData todData)
        {
            if (todData == null)
            {
                Debug.LogError("TOD 数据容器不能为空");
                yield break;
            }
            
            CollectLightSource(todData);
            CollectVolume(todData);
            
            CollectSkybox(todData);
            CollectRenderSettings(todData);
            
            CollectLightProbe(todData);
            yield return CollectReflectionProbe(todData);
            CollectEmission(todData);
            CollectCharacterOverrideGI(todData);
            CollectActiveTag(todData);
            CollectLightmapTag(todData);
            CollectCharacterShadowSettings(todData);
            
            EditorUtility.SetDirty(todData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CollectLightSource(StoredTimeOfDayData todData)
        {
            todData.lightSourceDatas = new List<LightSourceData>();

            var lightSources = TODUtils.FindObjectsOfTypeInActiveScene<Light>();
            foreach (var light in lightSources)
            {
                // 不收集没启用的
                if(!light.enabled) continue;
                // 烘焙灯在运行时就不存在了
                if (light.lightmapBakeType == LightmapBakeType.Baked) continue;
                
                var lsd = new LightSourceData();
                lsd.Collect(light);
                todData.lightSourceDatas.Add(lsd);

                if (light.type == LightType.Directional)
                {
                    todData.bakedGITint = light.color;
                }
            }
        }

        private void CollectVolume(StoredTimeOfDayData todData)
        {
            todData.volumeDatas = new List<VolumeData>();
            
            var volumes = TODUtils.FindObjectsOfTypeInActiveScene<Volume>();
            foreach (var volume in volumes)
            {
                if(!volume.enabled) continue;
                if(volume.sharedProfile == null) continue;

                var volumeData = new VolumeData();
                volumeData.Collect(volume);
                
                todData.volumeDatas.Add(volumeData);
            }
        }
        
        private void CollectSkybox(StoredTimeOfDayData todData)
        {
            var ss = new SkyboxSettings();
            ss.Collect();
            todData.skyboxSettings = ss;
        }
        
        private void CollectRenderSettings(StoredTimeOfDayData todData)
        {
            // Unity 的雾设置
            var ufs = new UnityFogSettings();
            ufs.Collect();
            todData.unityFogSettings = ufs;
            
            // 环境光设置
            var els = new EnvironmentLightingSettings();
            els.Collect();
            todData.environmentLightingSettings = els;
            
            // 环境反射设置
            var ers = new EnvironmentReflectionsSettings();
            ers.Collect();
            todData.environmentReflectionsSettings = ers;
            
            // 太阳光源的名字
            todData.sunSourceName = RenderSettings.sun.name;
        }
        
        private void CollectLightProbe(StoredTimeOfDayData todData)
        {
            var lpd = new LightProbeData();
            lpd.Collect();
            todData.lightProbeData = lpd;
        }

        private IEnumerator CollectReflectionProbe(StoredTimeOfDayData todData)
        {
            string dataAssetPath = AssetDatabase.GetAssetPath(todData);
            string storeDirectory = Path.GetDirectoryName(dataAssetPath);
            
            var textureImporterSettings = new TextureImporterSettings();
            textureImporterSettings.npotScale = TextureImporterNPOTScale.ToNearest;
            textureImporterSettings.textureShape = TextureImporterShape.TextureCube;
            textureImporterSettings.alphaSource = TextureImporterAlphaSource.FromInput;
            textureImporterSettings.filterMode = FilterMode.Bilinear;
            textureImporterSettings.mipmapEnabled = true;
            //textureImporterSettings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            //textureImporterSettings.sRGBTexture = true;

            yield return null;

            todData.reflectionProbeDatas = new List<ReflectionProbeData>();
            
            var sceneReflectionProbes = TODUtils.FindObjectsOfTypeInActiveScene<ReflectionProbe>();
            int probeLength = sceneReflectionProbes.Count;
            
            string storeFullPath;
            for (int i = 0; i < probeLength; i++)
            {
                ReflectionProbe reflectionProbe = sceneReflectionProbes[i];
                // 不收集没启用的
                if(!reflectionProbe.enabled) continue;
                
                storeFullPath = $"{storeDirectory}/ReflectionProbe-{todData.phaseName}_{i}.exr";
                
                ReflectionProbeData reflectionProbeData = new ReflectionProbeData();
                reflectionProbeData.Collect(textureImporterSettings, storeFullPath, reflectionProbe);
                
                todData.reflectionProbeDatas.Add(reflectionProbeData);
            }
        }

        private void CollectEmission(StoredTimeOfDayData todData)
        {
            // 用全局 shader 变量来实现了，所以不用收集
        }

        private void CollectCharacterOverrideGI(StoredTimeOfDayData todData)
        {
            var data = new CharacterOverrideGIData();
            data.Collect();
            todData.characterOverrideGIData = data;
        }

        private void CollectActiveTag(StoredTimeOfDayData todData)
        {
            todData.activeDatas = new List<ActiveData>();
            
            var ats = TODUtils.FindObjectsOfTypeInActiveScene<ActiveTag>(); // 触发器的话，开着和关着的都得要
            foreach (var at in ats)
            {
                var itemData = new ActiveData();
                itemData.scriptId = at.scriptId;
                
                // 记录当前的激活状态
                itemData.state = at.gameObject.activeSelf;
                
                todData.activeDatas.Add(itemData);
            }
        }

        private void CollectLightmapTag(StoredTimeOfDayData todData)
        {
            todData.lightmapUniquenessDatas = new List<LightmapUniquenessData>();
            
            var lts = TODUtils.FindObjectsOfTypeInActiveScene<LightmapTag>();
            foreach (var lt in lts)
            {
                var itemData = new LightmapUniquenessData();
                itemData.scriptId = lt.scriptId;
                
                // Lightmap 的索引，偏移数据都用 LightmapVolumeBakeWindow 来写入配置
                
                todData.lightmapUniquenessDatas.Add(itemData);
            }
        }

        private void CollectCharacterShadowSettings(StoredTimeOfDayData todData)
        {
            // 暂时没有收集的需求，先手动填写数值
        }
        
    }
}