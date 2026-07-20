// Created By: WangYu  Date: 2025-03-17

using System;
using UnityEngine;
using XKT.TOD.Utils;
using UnityObject = UnityEngine.Object;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class SkyboxSettings
    {
        public bool enabled;
        
        public Color tint;
        public float exposure;
        public float rotation;
        
        public Cubemap skyboxTexture;
        
        public void Collect()
        {
            Material skyboxMat = RenderSettings.skybox;
            if (skyboxMat == null) return;
            
            this.enabled = true;
            
            if (skyboxMat.HasProperty(TODUtils.sp_Tint))
            {
                this.tint = skyboxMat.GetColor(TODUtils.sp_Tint);
            }

            if (skyboxMat.HasProperty(TODUtils.sp_Exposure))
            {
                this.exposure = skyboxMat.GetFloat(TODUtils.sp_Exposure);
            }

            if (skyboxMat.HasProperty(TODUtils.sp_Rotation))
            {
                this.rotation = skyboxMat.GetFloat(TODUtils.sp_Rotation);
            }

            if (skyboxMat.HasProperty(TODUtils.sp_Tex))
            {
                this.skyboxTexture = skyboxMat.GetTexture(TODUtils.sp_Tex) as Cubemap;
            }
        }

        public void Restore()
        {
            Material skyboxMat = RenderSettings.skybox;
            if (skyboxMat == null) return;
            
            if(!this.enabled) return;

            // 克隆天空球材质
            Material newSkyboxMat = UnityObject.Instantiate(skyboxMat);
            newSkyboxMat.name = newSkyboxMat.name.Replace("(Clone)", "") + "(Clone)";
            RenderSettings.skybox = newSkyboxMat;
            
            // 只能销毁克隆的材质球
            if (skyboxMat.name.EndsWith("(Clone)"))
            {
                UnityObject.Destroy(skyboxMat);
            }
            
            if (newSkyboxMat.HasProperty(TODUtils.sp_Tint))
            {
                newSkyboxMat.SetColor(TODUtils.sp_Tint, this.tint);
            }

            if (newSkyboxMat.HasProperty(TODUtils.sp_Exposure))
            {
                newSkyboxMat.SetFloat(TODUtils.sp_Exposure, this.exposure);
            }

            if (newSkyboxMat.HasProperty(TODUtils.sp_Rotation))
            {
                newSkyboxMat.SetFloat(TODUtils.sp_Rotation, this.rotation);
            }

            if (newSkyboxMat.HasProperty(TODUtils.sp_Tex))
            {
                newSkyboxMat.SetTexture(TODUtils.sp_Tex, this.skyboxTexture);
            }
        }
        
    }
}