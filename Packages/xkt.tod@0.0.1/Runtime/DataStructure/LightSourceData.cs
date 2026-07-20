// Created By: WangYu  Date: 2025-03-17

using System;
using UnityEngine;
using UnityEngine.Rendering;
using XKT.TOD.Utils;
using UnityObject = UnityEngine.Object;

namespace XKT.TOD.DataStructure
{
    /// <summary>
    /// 光源数据
    /// </summary>
    [Serializable]
    public class LightSourceData
    {
        /// <summary>
        /// 层次结构路径
        /// </summary>
        [Header("层次结构路径")]
        public string hierarchyPath;
        
        public Vector3 position;
        public Quaternion rotation;

        public LightType type;
        public Color color;
        public float colorTemperature;
        
        public float intensity;
        public float bounceIntensity;
        
        public float spotAngle;
        public float innerSpotAngle;
        public float range;
        
        public LightShape shape;
        
        public int cullingMask;
        public int renderingLayerMask;
        
        public LightShadows shadows;
        public float shadowStrength;
        public LightShadowResolution shadowResolution;

        public XKTLensFlareData lensFlareData;
        
        
        public void Collect(Light light)
        {
            if(light == null) return;
            
            this.hierarchyPath = TODUtils.GetHierarchyPath(light.transform);
            
            this.position = light.transform.position;
            this.rotation = light.transform.rotation;

            this.type = light.type;
            this.color = light.color;
            this.colorTemperature = light.colorTemperature;

            this.intensity = light.intensity;
            this.bounceIntensity = light.bounceIntensity;

            this.spotAngle = light.spotAngle;
            this.innerSpotAngle = light.innerSpotAngle;
            this.range = light.range;

            this.shape = light.shape;

            this.cullingMask = light.cullingMask;
            this.renderingLayerMask = light.renderingLayerMask;

            this.shadows = light.shadows;
            this.shadowStrength = light.shadowStrength;
            this.shadowResolution = light.shadowResolution;

            var lfc = light.GetComponent<XKTLensFlareComponentSRP>();
            if (lfc == null)
            {
                this.lensFlareData = null;
            }
            else
            {
                this.lensFlareData = new XKTLensFlareData();
                this.lensFlareData.Collect(lfc);
            }
        }

        public Light Restore(GameObject mainLightAsset)
        {
            Transform parentT = TODUtils.FindHierarchyPath(this.hierarchyPath, 1);
            string goName = TODUtils.GetHierarchyGameObjectName(this.hierarchyPath);
            
            Light light;
            // 主光源通过预设来重建
            if (this.type == LightType.Directional)
            {
                var newGo = UnityObject.Instantiate(mainLightAsset);
                newGo.name = goName;
                if (parentT != null)
                {
                    newGo.transform.SetParent(parentT);
                }
                
                light = newGo.GetComponent<Light>();
            }
            // 非主灯的话，直接按实时灯创建
            else
            {
                var newGo = new GameObject(goName);
                if (parentT != null)
                {
                    newGo.transform.SetParent(parentT);
                }

                light = newGo.AddComponent<Light>();
            }

            light.transform.position = this.position;
            light.transform.rotation = this.rotation;
            light.transform.localScale = Vector3.one;

            light.type = this.type;
            light.color = this.color;
            light.colorTemperature = this.colorTemperature;

            light.intensity = this.intensity;
            light.bounceIntensity = this.bounceIntensity;

            light.spotAngle = this.spotAngle;
            light.innerSpotAngle = this.innerSpotAngle;
            light.range = this.range;

            light.shape = this.shape;

            light.cullingMask = this.cullingMask;
            light.renderingLayerMask = this.renderingLayerMask;

            light.shadows = this.shadows;
            light.shadowStrength = this.shadowStrength;
            light.shadowResolution = this.shadowResolution;

            if (this.lensFlareData != null)
            {
                var lfc = light.gameObject.AddComponent<XKTLensFlareComponentSRP>();
                if (lfc != null)
                {
                    this.lensFlareData.Restore(lfc);
                }
            }
            
            return light;
        }

    }
}