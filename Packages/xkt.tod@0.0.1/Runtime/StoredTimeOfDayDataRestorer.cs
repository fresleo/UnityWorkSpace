// Created By: WangYu  Date: 2025-03-20

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using XKT.TOD.DataStructure;
using XKT.TOD.Lightmap;
using XKT.TOD.Tag;
using XKT.TOD.Utils;
using RenderSettings = UnityEngine.RenderSettings;

namespace XKT.TOD
{
    public class StoredTimeOfDayDataRestorer
    {
        /// <summary>
        /// 新建的对象列表
        /// </summary>
        public List<GameObject> createdGOL;
        /// <summary>
        /// 主光源资源
        /// </summary>
        public GameObject mainLightAsset;

        public static readonly int SPID_TODTimeIndex = Shader.PropertyToID("_TODTimeIndex");

        public void Exit()
        {
            Shader.SetGlobalFloat(SPID_TODTimeIndex, 0);
        }
        
        public void Execute(StoredTimeOfDayData todData, int index)
        {
            if (todData == null)
            {
                Debug.LogError("TOD 数据不能为空，流程无法继续");
                return;
            }

            if (this.mainLightAsset == null)
            {
                Debug.LogError("主灯资源配置丢失，流程无法继续");
                return;
            }

            RestoreLightSource(todData);
            RestoreVolume(todData);
            
            RestoreSkybox(todData);
            RestoreRenderSettings(todData);
            
            RestoreLightProbe(todData);
            RestoreReflectionProbe(todData);
            RestoreCharacterOverrideGI(todData);
            RestoreActiveData(todData);
            RestoreLightmap(todData);
            RestoreCharacterShadowSettings(todData);
            
            // 全局广播 TOD 的时间索引
            Shader.SetGlobalFloat(SPID_TODTimeIndex, index);
        }

        private void RestoreLightSource(StoredTimeOfDayData todData)
        {
            if(todData.lightSourceDatas == null || todData.lightSourceDatas.Count == 0) return;
            
            foreach (LightSourceData lsd in todData.lightSourceDatas)
            {
                Light light = lsd.Restore(this.mainLightAsset);
                createdGOL.Add(light.gameObject);
                
                // 设置全局环境参数
                if (light.type == LightType.Directional)
                {
                    // 避免反复设置
                    if (RenderSettings.sun != light)
                    {
                        RenderSettings.sun = light;
                    }
                }
            }
        }

        private void RestoreVolume(StoredTimeOfDayData todData)
        {
            if(todData.volumeDatas == null || todData.volumeDatas.Count == 0) return;
            
            foreach (VolumeData volumeData in todData.volumeDatas)
            {
                Volume volume = volumeData.Restore();
                createdGOL.Add(volume.gameObject);
            }
        }
        
        private void RestoreSkybox(StoredTimeOfDayData todData)
        {
            todData.skyboxSettings.Restore();
        }

        private void RestoreRenderSettings(StoredTimeOfDayData todData)
        {
            todData.unityFogSettings.Restore();
            todData.environmentLightingSettings.Restore();
            todData.environmentReflectionsSettings.Restore();
        }

        private void RestoreLightProbe(StoredTimeOfDayData todData)
        {
            todData.lightProbeData.Restore();
        }

        private void RestoreReflectionProbe(StoredTimeOfDayData todData)
        {
            if(todData.reflectionProbeDatas == null || todData.reflectionProbeDatas.Count == 0) return;
            
            foreach (ReflectionProbeData rpd in todData.reflectionProbeDatas)
            {
                ReflectionProbe rp = rpd.Restore();
                createdGOL.Add(rp.gameObject);
            }
        }

        private void RestoreCharacterOverrideGI(StoredTimeOfDayData todData)
        {
            if(todData.characterOverrideGIData == null) return;

            var component = todData.characterOverrideGIData.Restore();
            createdGOL.Add(component.gameObject);
        }
        
        private void RestoreActiveData(StoredTimeOfDayData todData)
        {
            var ats = TODUtils.FindObjectsOfTypeInActiveScene<ActiveTag>(true);
            
            foreach (var at in ats)
            {
                at.ResetState();
            }

            foreach (var at in ats)
            {
                ActiveData ad = todData.activeDatas.Find(item => item.scriptId == at.scriptId);
                if (ad == null)
                {
                    // Debug.LogError($"没有找到 {nameof(ActiveTag)} 的数据: {at.scriptId}");
                    continue;
                }

                at.SetState(ad.state);
            }
        }

        /// <summary>
        /// 当前场景的 Lightmap 计数
        /// </summary>
        public int rawLightmapCount;

        private List<LightmapData> m_lightmapDataList = new();
        
        private void RestoreLightmap(StoredTimeOfDayData todData)
        {
            m_lightmapDataList.Clear();
            
            // 只保留原始的部分
            for (int i = 0; i < this.rawLightmapCount; i++)
            {
                var item = LightmapSettings.lightmaps[i];
                m_lightmapDataList.Add(item);
            }
            
            // 添加新的 Lightmap 纹理
            for (int i = 0; i < todData.lightmapDataCopys.Count; i++)
            {
                var itemC = todData.lightmapDataCopys[i];
                
                LightmapData itemLD = new LightmapData();
                itemLD.lightmapColor = itemC.lightmapColor;
                itemLD.lightmapDir = itemC.lightmapDir;
                itemLD.shadowMask = itemC.shadowMask;
                
                m_lightmapDataList.Add(itemLD);
            }

            LightmapSettings.lightmaps = m_lightmapDataList.ToArray();
            m_lightmapDataList.Clear();
            
            // 找到所有的 LightmapTag 组件，包括隐藏的
            var lts = TODUtils.FindObjectsOfTypeInActiveScene<LightmapTag>(true);
            foreach (var lt in lts)
            {
                // 没激活的除外
                if (!lt.enabled)
                {
                    continue;
                }
                
                // 重置 Lightmap 数据
                lt.ResetLightmapData();
            }
            
            // 设置新的 Lightmap 数据
            foreach (var lt in lts)
            {
                LightmapUniquenessData lud = todData.lightmapUniquenessDatas.Find(item => item.scriptId == lt.scriptId);
                if (lud == null)
                {
                    // Debug.LogError($"没有找到 {nameof(LightmapTag)} 的数据: {lt.scriptId}");
                    continue;
                }

                lt.SetLightmapData(this.rawLightmapCount, lud);
            }
        }

        private void RestoreCharacterShadowSettings(StoredTimeOfDayData todData)
        {
            // 暂时没有需求，因为无法在这里设置角色，只是预留一下
        }
        
    }
}