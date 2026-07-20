// Created By: WangYu  Date: 2025-02-21

using System;
using AirSticker.Runtime.Logic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AirSticker.Runtime.Render
{
    public class KnifeMarkDecalRenderer : BaseDecalRenderer
    {
        private static readonly int _AlphaControl = Shader.PropertyToID("_AlphaControl");
        private static readonly int _UVTilingOffset = Shader.PropertyToID("_UVTilingOffset");

        private static readonly int _HighTempColor_1 = Shader.PropertyToID("_HighTempColor_1");
        private static readonly int _HighTempStrength_1 = Shader.PropertyToID("_HighTempStrength_1");

        private static readonly int _HighTempColor_2 = Shader.PropertyToID("_HighTempColor_2");
        private static readonly int _HighTempStrength_2 = Shader.PropertyToID("_HighTempStrength_2");

        private static readonly int _HighTempSmoothingFactor = Shader.PropertyToID("_HighTempSmoothingFactor");
        
        
        public override void SetDisplayResource(Material cloneMaterial, Mesh mesh)
        {
            if (cloneMaterial == null || mesh == null)
            {
                Debug.LogError($"{nameof(Material)} 或 {nameof(Mesh)} 为空： {nameof(KnifeMarkDecalRenderer)}");
                return;
            }

            m_cloneMaterial = cloneMaterial;
            
            // 设置渲染器
            if (m_meshRenderer != null)
            {
                m_meshRenderer.material = m_cloneMaterial;
                m_meshFilter.mesh = mesh;
            }
            else if (m_skinnedMeshRenderer != null)
            {
                m_skinnedMeshRenderer.material = m_cloneMaterial;
                m_skinnedMeshRenderer.sharedMesh = mesh;
            }
            
            // 初始化材质参数
            RenderUtil.SetFloat(m_cloneMaterial, _AlphaControl, 0);
            
            RenderUtil.SetVector(m_cloneMaterial, _UVTilingOffset, new Vector4(1, 1, 0, 0));
            
            RenderUtil.SetColor(m_cloneMaterial, _HighTempColor_1, Color.white);
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempStrength_1, 0);
            
            RenderUtil.SetColor(m_cloneMaterial, _HighTempColor_2, Color.white);
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempStrength_2, 0);
            
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempSmoothingFactor, 0);
        }
        
        public override void CreateLifecycle(long uniqueKey, AbsDecalConfig lifeConfig, Action<long> callback)
        {
            base.CreateLifecycle(uniqueKey, lifeConfig, callback);
            
            var realLifeConfig = m_lifeConfig as KnifeMarkDecalConfig;
            if (realLifeConfig == null)
            {
                return;
            }
            
            m_waitForCallbacks.Add(uniqueKey, callback);

            var newTaskData = new RendererTaskData
            {
                uniqueKey = uniqueKey,
                
                fadeinTime = realLifeConfig.fadeinTime,
                fadeinCurve = realLifeConfig.fadeinCurve,
                
                duration = realLifeConfig.duration,
                
                fadeoutTime = realLifeConfig.fadeoutTime,
                fadeoutCurve = realLifeConfig.fadeoutCurve,
            };
            m_taskDatas.Add(newTaskData);

            var newTask = new RendererTask
            {
                uniqueKey = uniqueKey,
            };
            m_runningTasks.Add(newTask);
        }

        protected override void OnFadein(float progress)
        {
            var realLifeConfig = m_lifeConfig as KnifeMarkDecalConfig;
            if (realLifeConfig == null)
            {
                return;
            }
            
            RenderUtil.SetFloat(m_cloneMaterial, _AlphaControl, 1);
            
            // 拉伸刀痕
            Vector4 stretch = Vector4.Lerp(realLifeConfig.stretchLeft, realLifeConfig.stretchRight, progress);
            RenderUtil.SetVector(m_cloneMaterial, _UVTilingOffset, stretch);

            // 升温过程
            // 低温区
            Color lowTempColor = realLifeConfig.warmingLowTempGradient.Evaluate(progress);
            RenderUtil.SetColor(m_cloneMaterial, _HighTempColor_1, lowTempColor);
            
            float lowTempStrength = realLifeConfig.warmingLowTempStrengthCurve.Evaluate(progress);
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempStrength_1, lowTempStrength);
            
            // 高温区
            Color highTempColor = realLifeConfig.warmingHighTempGradient.Evaluate(progress);
            RenderUtil.SetColor(m_cloneMaterial, _HighTempColor_2, highTempColor);
            
            float heightTempStrength = realLifeConfig.warmingHighTempStrengthCurve.Evaluate(progress);
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempStrength_2, heightTempStrength);
            
            float highTempSmoothingFactor = realLifeConfig.warmingHighTempSmoothingFactorCurve.Evaluate(progress);
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempSmoothingFactor, highTempSmoothingFactor);
        }

        protected override void OnDuration(float progress)
        {
            var realLifeConfig = m_lifeConfig as KnifeMarkDecalConfig;
            if (realLifeConfig == null)
            {
                return;
            }

            // 透明度控制
            Color alphaControl = realLifeConfig.durationAlphaGradient.Evaluate(progress);
            RenderUtil.SetFloat(m_cloneMaterial, _AlphaControl, alphaControl.a);
            
            // 拉伸过程已结束
            RenderUtil.SetVector(m_cloneMaterial, _UVTilingOffset, realLifeConfig.stretchRight);

            // 降温过程
            // 低温区
            Color lowTempColor = realLifeConfig.coolingLowTempGradient.Evaluate(progress);
            RenderUtil.SetColor(m_cloneMaterial, _HighTempColor_1, lowTempColor);
            
            float lowTempStrength = realLifeConfig.coolingLowTempStrengthCurve.Evaluate(progress);
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempStrength_1, lowTempStrength);
            
            // 高温区
            Color highTempColor = realLifeConfig.coolingHighTempGradient.Evaluate(progress);
            RenderUtil.SetColor(m_cloneMaterial, _HighTempColor_2, highTempColor);
            
            float heightTempStrength = realLifeConfig.coolingHighTempStrengthCurve.Evaluate(progress);
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempStrength_2, heightTempStrength);
            
            float highTempSmoothingFactor = realLifeConfig.coolingHighTempSmoothingFactorCurve.Evaluate(progress);
            RenderUtil.SetFloat(m_cloneMaterial, _HighTempSmoothingFactor, highTempSmoothingFactor);
        }

        protected override void OnFadeout(float progress)
        {
            // 淡出的过程就是慢慢的变透明了
            RenderUtil.SetFloat(m_cloneMaterial, _AlphaControl, progress);
        }
        
    }
}
