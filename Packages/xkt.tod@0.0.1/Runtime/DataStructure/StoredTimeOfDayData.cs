// Created By: WangYu  Date: 2025-03-12

using System;
using System.Collections.Generic;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    [CreateAssetMenu(fileName = nameof(StoredTimeOfDayData), menuName = "TOD/创建 TOD 数据存储", order = 1)]
    public class StoredTimeOfDayData : ScriptableObject
    {
        /// <summary>
        /// 创建日期
        /// </summary>
        public string creationDate;
        
        /// <summary>
        /// 场景名
        /// </summary>
        public string sceneName;
        
        /// <summary>
        /// 阶段名
        /// </summary>
        public string phaseName;

        /// <summary>
        /// Unity 的雾设置
        /// </summary>
        public UnityFogSettings unityFogSettings;
        
        /// <summary>
        /// 太阳光源名
        /// </summary>
        public string sunSourceName;
        
        /// <summary>
        /// 天空盒设置
        /// </summary>
        public SkyboxSettings skyboxSettings;
        
        /// <summary>
        /// 环境光设置
        /// </summary>
        public EnvironmentLightingSettings environmentLightingSettings;

        /// <summary>
        /// 环境反射设置
        /// </summary>
        public EnvironmentReflectionsSettings environmentReflectionsSettings;
        
        /// <summary>
        /// 光照探针数据
        /// </summary>
        public LightProbeData lightProbeData;
        
        /// <summary>
        /// 反射探针数据
        /// </summary>
        public List<ReflectionProbeData> reflectionProbeDatas;
        
        /// <summary>
        /// 光源数据
        /// </summary>
        public List<LightSourceData> lightSourceDatas;
        
        /// <summary>
        /// 后处理配置
        /// </summary>
        public List<VolumeData> volumeDatas;

        /// <summary>
        /// 中性图调色
        /// </summary>
        public Color bakedGITint;

        /// <summary>
        /// 覆盖角色 GI 的数据
        /// </summary>
        public CharacterOverrideGIData characterOverrideGIData;

        /// <summary>
        /// 激活数据
        /// </summary>
        public List<ActiveData> activeDatas;

        /// <summary>
        /// Lightmap 烘焙数据
        /// </summary>
        public List<LightmapUniquenessData> lightmapUniquenessDatas;

        /// <summary>
        /// LightmapData 的拷贝
        /// </summary>
        public List<LightmapDataCopy> lightmapDataCopys;

        /// <summary>
        /// 角色的阴影设置
        /// </summary>
        public List<CharacterShadowSettings> characterShadowSettings;
        
    }
}