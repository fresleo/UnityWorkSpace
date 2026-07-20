// Created By: WangYu  Date: 2025-03-17

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKT.TOD.DataStructure
{
    /// <summary>
    /// 环境光设置
    /// </summary>
    [Serializable]
    public class EnvironmentLightingSettings
    {
        /// <summary>
        /// 环境光模式
        /// </summary>
        public AmbientMode ambientMode;
        
        public float ambientIntensity;
        
        public Color skyColor;
        public Color equatorColor;
        public Color groundColor;
        
        public Color ambientColor;

        public void Collect()
        {
            this.ambientMode = RenderSettings.ambientMode;
            
            this.ambientIntensity = RenderSettings.ambientIntensity;
            
            this.skyColor = RenderSettings.ambientSkyColor;
            this.equatorColor = RenderSettings.ambientEquatorColor;
            this.groundColor = RenderSettings.ambientGroundColor;
            
            this.ambientColor = RenderSettings.ambientLight;
        }

        public void Restore()
        {
            RenderSettings.ambientMode = this.ambientMode;

            RenderSettings.ambientIntensity = this.ambientIntensity;

            RenderSettings.ambientSkyColor = this.skyColor;
            RenderSettings.ambientEquatorColor = this.equatorColor;
            RenderSettings.ambientGroundColor = this.groundColor;

            RenderSettings.ambientLight = this.ambientColor;
        }
        
    }
}