// Created By: WangYu  Date: 2025-03-20

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class EnvironmentReflectionsSettings
    {
        public DefaultReflectionMode defaultReflectionMode;

        public int defaultReflectionResolution;
        public Cubemap customReflectionTexture;

        public float reflectionIntensity;
        public int reflectionBounces;

        public void Collect()
        {
            this.defaultReflectionMode = RenderSettings.defaultReflectionMode;
            
            this.defaultReflectionResolution = RenderSettings.defaultReflectionResolution;
            this.customReflectionTexture = RenderSettings.customReflectionTexture as Cubemap;
            
            this.reflectionIntensity = RenderSettings.reflectionIntensity;
            this.reflectionBounces = RenderSettings.reflectionBounces;
        }

        public void Restore()
        {
            RenderSettings.defaultReflectionMode = this.defaultReflectionMode;

            RenderSettings.defaultReflectionResolution = this.defaultReflectionResolution;
            RenderSettings.customReflectionTexture = this.customReflectionTexture;

            RenderSettings.reflectionIntensity = this.reflectionIntensity;
            RenderSettings.reflectionBounces = this.reflectionBounces;
        }
        
    }
}