// Created By: WangYu  Date: 2025-03-17

using System;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class UnityFogSettings
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool enabled;
        
        /// <summary>
        /// 雾的颜色
        /// </summary>
        public Color fogColor;
        
        /// <summary>
        /// 雾的密度
        /// </summary>
        public float fogDensity;


        public void Collect()
        {
            this.enabled = RenderSettings.fog;
            this.fogColor = RenderSettings.fogColor;
            this.fogDensity = RenderSettings.fogDensity;
        }

        public void Restore()
        {
            RenderSettings.fog = this.enabled;
            RenderSettings.fogColor = this.fogColor;
            RenderSettings.fogDensity = this.fogDensity;
        }
        
    }
}