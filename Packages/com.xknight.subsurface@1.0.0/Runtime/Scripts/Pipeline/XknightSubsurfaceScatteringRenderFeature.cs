using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Sirenix.OdinInspector;

namespace XKnight.TA.SSS
{
    public enum UInjectWay
    {
        ForwardSampling,
        DeferredComposition
    }

    public class XknightSubsurfaceScatteringRenderFeature : ScriptableRendererFeature
    {
        public bool preferCompute = true;
        public UInjectWay injectWay = UInjectWay.ForwardSampling;

        [Range(0, 1)] public float SSS_Strenth = 1.0f;


        [Serializable]
        public class Settings
        {
            public UInjectWay injectWay;
            public bool preferCompute;
            public float sssStrenth;

            public Settings(UInjectWay injectWay, bool preferCompute, float sssStrenth)
            {
                this.injectWay = injectWay;
                this.preferCompute = preferCompute;
                this.sssStrenth = sssStrenth;
            }
        }
        
        private XknightSubsurfaceScatteringPass _xknightsubsurfaceScatteringPass;
        
        public override void Create()
        {
            if (!isActive)
            {
                Release();
                return;
            }
            Debug.Log("XknightSubsurfaceScatteringRenderFeature Create");
            Release();
            this.name = "XknightSubsurfaceScattering";
            _xknightsubsurfaceScatteringPass = new XknightSubsurfaceScatteringPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!isActive) return;
            Settings settings = new Settings(injectWay, preferCompute, SSS_Strenth);

            if (settings.injectWay == UInjectWay.ForwardSampling)
            {
                _xknightsubsurfaceScatteringPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            }
            else
            {
                _xknightsubsurfaceScatteringPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            }

            _xknightsubsurfaceScatteringPass.Setup(settings);

            renderer.EnqueuePass(_xknightsubsurfaceScatteringPass);
        }

        private static void SafeDispose<TDisposable>(ref TDisposable disposable) where TDisposable : class, IDisposable
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void Release()
        {
            SafeDispose(ref _xknightsubsurfaceScatteringPass);
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Log("XknightSubsurfaceScatteringRenderFeature Dispose");
            Release();
        }

        private void OnDisable()
        {
            Debug.Log("XknightSubsurfaceScatteringRenderFeature OnDisable");
            SafeDispose(ref _xknightsubsurfaceScatteringPass);
        }
    }
}