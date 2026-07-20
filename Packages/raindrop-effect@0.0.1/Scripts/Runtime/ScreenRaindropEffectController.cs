// Created By: WangYu  Date: 2024-11-19

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RaindropEffect
{
    public class ScreenRaindropEffectController : MonoBehaviour
    {
        private static ScreenRaindropEffectController s_instance;
        /// <summary>
        /// 单例
        /// </summary>
        public static ScreenRaindropEffectController Instance => s_instance;
        
        /// <summary>
        /// 预设数据列表
        /// </summary>
        public List<RainfallData> presetDatas = new();
        /// <summary>
        /// 启动值列表
        /// </summary>
        public List<float> startValues = new();
        
        [NonSerialized] // 不用序列化，因为我们不用保存它
        public bool orderChanged;
        
        [SerializeField] // 需要能序列化，为了能在 Timeline 里被 K 帧
        [Range(0, 1)]
        public float globalBlendFactor;
        
        [SerializeField] // 需要能序列化，为了能在 Timeline 里被 K 帧
        public float timeScale = 1;
        
        // 目标渲染特性
        private ScreenRaindropEffectRendererFeature m_targetRF;
        // 避免重操作的变量
        private float m_lastGlobalBlendFactor;
        private float m_lastTimeScale;

        
        private void Awake()
        {
            s_instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDisable()
        {
            // 重置缩放值
            timeScale = 1;
            SetTimeScale(timeScale);
            
            m_targetRF?.ClearRaindropEffect();
        }

        private void Update()
        {
            SetTimeScale(timeScale);
            PlayRainfallData(globalBlendFactor);
        }
        
        
        // 确保这个渲染特性
        private bool EnsureTargetRF()
        {
            if (m_targetRF == null)
            {
                m_targetRF = FindScreenRaindropEffectRendererFeature();
            }

            return m_targetRF != null;
        }
        
        // 查找渲染特性
        private static ScreenRaindropEffectRendererFeature FindScreenRaindropEffectRendererFeature()
        {
            if (XKnightRenderPipeline.asset == null)
            {
                return null;
            }

            // 这里其实是在找 DefaultRendererData 这个配置
            int targetIndex = 0;
            XKnightRendererData rendererData = XKnightRenderPipeline.asset.GetRendererData(targetIndex) as XKnightRendererData;
            if (rendererData == null)
            {
                return null;
            }

            for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
            {
                ScreenRaindropEffectRendererFeature feature = rendererData.rendererFeatures[i] as ScreenRaindropEffectRendererFeature;
                if (feature != null)
                {
                    return feature;
                }
            }

            return null;
        }
        
        
        /// <summary>
        /// 播放目标雨量的配置数据
        /// </summary>
        public void PlayRainfallData(float gbf)
        {
            // 确保这个渲染特性
            if (!EnsureTargetRF())
            {
                return;
            }
            
            if (Mathf.Approximately(m_lastGlobalBlendFactor, gbf))
            {
                return;
            }
            m_lastGlobalBlendFactor = gbf;
            this.globalBlendFactor = gbf;
            
            // 计算读取配置的参数
            int fromIndex = -1;
            int toIndex = -1;
            float localBlendFactor = 0;
            
            for (int i = 0; i < presetDatas.Count; i++)
            {
                if (i < presetDatas.Count - 2)
                {
                    if (gbf >= startValues[i] && gbf <= startValues[i + 1])
                    {
                        fromIndex = i;
                        toIndex = i + 1;
                        localBlendFactor = MixBlendFactor(gbf, startValues[i], startValues[i + 1]);
                        break;
                    }
                }
                else
                {
                    if (gbf >= startValues[i])
                    {
                        fromIndex = i;
                        toIndex = presetDatas.Count - 1;
                        localBlendFactor = MixBlendFactor(gbf, startValues[i], 1);
                        break;
                    }
                }
            }

            PlayRainfallData(fromIndex, toIndex, localBlendFactor);
        }

        private static float MixBlendFactor(float blendFactor, float beginStartValue, float endStartValue)
        {
            float result = Mathf.Clamp((blendFactor - beginStartValue) / (endStartValue - beginStartValue), 0, 1);
            return result;
        }
        
        /// <summary>
        /// 播放目标雨量的配置数据
        /// </summary>
        public void PlayRainfallData(int fromIndex, int toIndex, float blendFactor)
        {
            // 确保这个渲染特性
            if (!EnsureTargetRF())
            {
                return;
            }
            
            // 参数没有问题
            int total = presetDatas.Count;
            if (fromIndex < 0 || fromIndex >= total)
            {
                return;
            }
            if (toIndex < 0 || toIndex >= total)
            {
                return;
            }
            if (blendFactor < 0 || blendFactor > 1)
            {
                return;
            }
            
            // 找得到目标配置
            var fromData = presetDatas[fromIndex];
            var toData = presetDatas[toIndex];
            if (fromData == null || toData == null)
            {
                return;
            }
            
            // 设置模拟数据
            SetSimuParas(fromData, toData, blendFactor);
            // 设置渲染数据
            SetRendParas(fromData, toData, blendFactor);
            
            // 开关效果
            if (blendFactor > 0)
            {
                m_targetRF.StartRaindropEffect();
            }
            else
            {
                m_targetRF.StopRaindropEffect();
            }
        }

        private void SetSimuParas(RainfallData fromData, RainfallData toData, float blendFactor)
        {
            Vector2 blend_spawnInterval = Vector2.Lerp(fromData.simuParas.spawnInterval, toData.simuParas.spawnInterval, blendFactor);
            m_targetRF.simuParas.spawnInterval = blend_spawnInterval;
            
            Vector2 blend_raindropSizeRange = Vector2.Lerp(fromData.simuParas.raindropSizeRange, toData.simuParas.raindropSizeRange, blendFactor);
            m_targetRF.simuParas.raindropSizeRange = blend_raindropSizeRange;
            
            float blend_gravity = Mathf.Lerp(fromData.simuParas.gravity, toData.simuParas.gravity, blendFactor);
            m_targetRF.simuParas.gravity = blend_gravity;

            float blend_frictionForceCoefficient = Mathf.Lerp(fromData.simuParas.frictionForceCoefficient, toData.simuParas.frictionForceCoefficient, blendFactor);
            m_targetRF.simuParas.frictionForceCoefficient = blend_frictionForceCoefficient;

            Vector2 blend_xVelocityCoefficientRange = Vector2.Lerp(fromData.simuParas.xVelocityCoefficientRange, toData.simuParas.xVelocityCoefficientRange, blendFactor);
            m_targetRF.simuParas.xVelocityCoefficientRange = blend_xVelocityCoefficientRange;

            float blend_trailRaindropSizeSpread = Mathf.Lerp(fromData.simuParas.trailRaindropSizeSpread, toData.simuParas.trailRaindropSizeSpread, blendFactor);
            m_targetRF.simuParas.trailRaindropSizeSpread = blend_trailRaindropSizeSpread;
        }

        private void SetRendParas(RainfallData fromData, RainfallData toData, float blendFactor)
        {
            float blend_dropletsSpawnRate = Mathf.Lerp(fromData.rendParas.dropletsSpawnRate, toData.rendParas.dropletsSpawnRate, blendFactor);
            m_targetRF.rendParas.dropletsSpawnRate = blend_dropletsSpawnRate;
        }

        
        /// <summary>
        /// 设置时间缩放
        /// </summary>
        public void SetTimeScale(float ts)
        {
            // 确保这个渲染特性
            if (!EnsureTargetRF())
            {
                return;
            }

            if (Mathf.Approximately(m_lastTimeScale, ts))
            {
                return;
            }
            m_lastTimeScale = ts;
            this.timeScale = ts;
            
            m_targetRF.SetTimeScale(ts);
        }
        
    }
}