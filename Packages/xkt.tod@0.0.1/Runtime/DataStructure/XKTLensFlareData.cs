/*******************************************************************************
 * File: XKTLensFlareData.cs
 * Author: WangYu
 * Date: 2026-01-07
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class XKTLensFlareData
    {
        public bool enabled;
        
        public XKTLensFlareDataSRP dataAsset;

        public float intensity;
        public float maxAttenuationDistance;
        public float maxAttenuationScale;
        public AnimationCurve distanceAttenuationCurve;
        public AnimationCurve scaleByDistanceCurve;
        public bool attenuationByLightShape;
        public AnimationCurve radialScreenAttenuationCurve;

        public bool useOcclusion;
        public float occlusionRadius;
        public bool useBackgroundCloudOcclusion;
        public uint sampleCount;
        public float occlusionOffset;
        public float scale;
        public bool allowOffScreen;
        public bool volumetricCloudOcclusion;

        public TextureCurve occlusionRemapCurve;
        
        public bool regardSelfAsSunlight;

        public void Collect(XKTLensFlareComponentSRP component)
        {
            if (component == null)
            {
                return;
            }
            
            this.enabled = component.enabled;
            
            this.dataAsset = component.lensFlareData;

            this.intensity = component.intensity;
            this.maxAttenuationDistance = component.maxAttenuationDistance;
            this.maxAttenuationScale = component.maxAttenuationScale;
            
            // 深拷贝，避免引用同1个曲线
            this.distanceAttenuationCurve = component.distanceAttenuationCurve != null 
                ? new AnimationCurve(component.distanceAttenuationCurve.keys) : null;
            this.scaleByDistanceCurve = component.scaleByDistanceCurve != null 
                ? new AnimationCurve(component.scaleByDistanceCurve.keys) : null;
            this.attenuationByLightShape = component.attenuationByLightShape;
            this.radialScreenAttenuationCurve = component.radialScreenAttenuationCurve != null 
                ? new AnimationCurve(component.radialScreenAttenuationCurve.keys) : null;

            this.useOcclusion = component.useOcclusion;
            this.occlusionRadius = component.occlusionRadius;
            this.useBackgroundCloudOcclusion = component.useBackgroundCloudOcclusion;
            this.sampleCount = component.sampleCount;
            this.occlusionOffset = component.occlusionOffset;
            this.scale = component.scale;
            this.allowOffScreen = component.allowOffScreen;
            this.volumetricCloudOcclusion = component.volumetricCloudOcclusion;

            this.occlusionRemapCurve = DeepCopy(component.occlusionRemapCurve);
            
            this.regardSelfAsSunlight = component.regardSelfAsSunlight;
        }

        public void Restore(XKTLensFlareComponentSRP component)
        {
            if (component == null)
            {
                return;
            }
            
            component.enabled = this.enabled;

            component.lensFlareData = this.dataAsset;

            component.intensity = this.intensity;
            component.maxAttenuationDistance = this.maxAttenuationDistance;
            component.maxAttenuationScale = this.maxAttenuationScale;
            
            component.distanceAttenuationCurve = this.distanceAttenuationCurve != null 
                ? new AnimationCurve(this.distanceAttenuationCurve.keys) 
                : new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
            component.scaleByDistanceCurve = this.scaleByDistanceCurve != null
                ? new AnimationCurve(this.scaleByDistanceCurve.keys)
                : new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
            component.attenuationByLightShape = this.attenuationByLightShape;
            component.radialScreenAttenuationCurve = this.radialScreenAttenuationCurve != null
                ? new AnimationCurve(this.radialScreenAttenuationCurve.keys)
                : new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

            component.useOcclusion = this.useOcclusion;
            component.occlusionRadius = this.occlusionRadius;
            component.useBackgroundCloudOcclusion = this.useBackgroundCloudOcclusion;
            component.sampleCount = this.sampleCount;
            component.occlusionOffset = this.occlusionOffset;
            component.scale = this.scale;
            component.allowOffScreen = this.allowOffScreen;
            component.volumetricCloudOcclusion = this.volumetricCloudOcclusion;
            
            component.occlusionRemapCurve = DeepCopy(this.occlusionRemapCurve);

            component.regardSelfAsSunlight = this.regardSelfAsSunlight;
        }
        
        private TextureCurve DeepCopy(TextureCurve source)
        {
            var keys = new Keyframe[source.length];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = source[i];
            }

            // 使用反射获取私有字段
            var type = typeof(TextureCurve);
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            
            var zeroValueField = type.GetField("m_ZeroValue", bindingFlags);
            var loopField = type.GetField("m_Loop", bindingFlags);
            var rangeField = type.GetField("m_Range", bindingFlags);

            float zeroValue = (float)zeroValueField.GetValue(source);
            bool loop = (bool)loopField.GetValue(source);
            float range = (float)rangeField.GetValue(source);
            var bounds = new Vector2(0, range);

            var newSource = new TextureCurve(keys, zeroValue, loop, bounds);
            return newSource;
        }
        
    }
}