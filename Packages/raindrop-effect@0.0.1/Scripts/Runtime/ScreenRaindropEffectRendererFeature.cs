// Created By: WangYu  Date: 2024-11-18

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RaindropEffect
{
    public class ScreenRaindropEffectRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        [Reload("RaindropRendererData.asset")]
        public RaindropRendererData rendererData;
        public SimulateParameters simuParas;
        public RenderParameters rendParas;

        [SerializeField]
        private bool m_isActive;
        [SerializeField]
        private float m_timeScale = 1;

        private RaindropSimulator m_simulator = new();
        private RaindropRenderer m_renderer = new();
        
        private ScreenRaindropEffectRenderPass m_renderPass;
        
        private int m_lastRaindropCount;

        protected override void Dispose(bool disposing)
        {
            m_simulator.Clear();
            m_renderer.Destroy();
            
            base.Dispose(disposing);
        }

        public override void Create()
        {
            // 设置一下当前屏幕的尺寸
            GetScreenSize(out int screenWidth, out int screenHeight);
            simuParas.screenWidth = screenWidth;
            simuParas.screenHeight = screenHeight;
            rendParas.renderTextureWidth = screenWidth;
            rendParas.renderTextureHeight = screenHeight;
            
            // 绑定参数，并进行初始化
            m_simulator.simuParas = this.simuParas;
            m_simulator.Resize();

            m_renderer.rendererData = this.rendererData;
            m_renderer.rendParas = this.rendParas;
            m_renderer.Init();
            
            m_renderPass = new ScreenRaindropEffectRenderPass();
            m_renderPass.renderPassEvent = this.renderPassEvent;
            m_renderPass.Setup(m_simulator, m_renderer);
            
            m_lastRaindropCount = 0;
        }
        
        /// <summary>
        /// 获取当前屏幕的尺寸
        /// </summary>
        public static void GetScreenSize(out int width, out int height)
        {
            // 有个问题，Screen.orientation 一直返回的是竖屏，且屏幕尺寸有颠倒的情况，成因暂时不明
            // 综上，这里只能做一下特殊处理了，强行认为一直是横屏来处理
            
            width = Mathf.Max(Screen.width, Screen.height);
            if (width <= 0)
            {
                width = 1920;
            }

            height = Mathf.Min(Screen.width, Screen.height);
            if (height <= 0)
            {
                height = 1080;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // 场景摄像机不用渲染这个
            if (renderingData.cameraData.isSceneViewCamera) return;
            // 非运行时没必要，因为雨滴也不会动
            if (!Application.isPlaying) return;

            // 处理时停的影响
            float deltaTime = m_timeScale * Time.deltaTime;
            m_renderPass.deltaTime = deltaTime;
            
            if (m_isActive)
            {
                // 绘制当前帧
                renderer.EnqueuePass(m_renderPass);
                
                // 当雨停了时，流动的雨珠会慢慢被回收，等到全部回收完了，就停止整个系统，顺便达到清除屏幕小液滴的效果
                int raindropCount = m_simulator.raindropList.Count;
                if (m_lastRaindropCount != raindropCount && raindropCount == 0)
                {
                    ClearRaindropEffect();
                }
                m_lastRaindropCount = raindropCount;
            }
        }

        /// <summary>
        /// 启动雨滴效果
        /// </summary>
        public void StartRaindropEffect()
        {
            m_isActive = true;
            
            m_simulator.enableSpawning = true;
        }

        /// <summary>
        /// 停止雨滴效果
        /// </summary>
        public void StopRaindropEffect()
        {
            m_simulator.enableSpawning = false;
            rendParas.dropletsSpawnRate = 0;
        }

        /// <summary>
        /// 清除雨滴效果
        /// </summary>
        public void ClearRaindropEffect()
        {
            m_isActive = false;
            
            StopRaindropEffect();
            
            m_simulator.Clear();
            m_renderer.Clear();
        }

        /// <summary>
        /// 设置时间缩放值
        /// </summary>
        public void SetTimeScale(float timeScale)
        {
            m_timeScale = timeScale;
        }
        
    }
}