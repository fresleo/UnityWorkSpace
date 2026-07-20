// Created by: WangYu   Date: 2025-12-16

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    public class OutlineDistortVolume : VolumeComponent, IPostProcessComponent
    {
        public ScriptableObjectParameter renderData = new(null);
        
        // 绘制遮罩参数 >>>>>>>>>>>>>>>>>>>>
        public BoolParameter meshPreview = new(false);
        
        public FloatParameter outlineWidth = new(5);
        public ClampedFloatParameter outlinePower = new(0.6f, 0.1f, 1.5f);
        
        public FloatParameter outlineFadeStart = new(0);
        public FloatParameter outlineFadeEnd = new(50);
        
        public FloatParameter yAxisOffset = new(0);
        
        public BoolParameter invertFadeDirection = new(true);
        
        public FloatParameter gradientScale = new(1);
        public FloatParameter gradientLeft = new(0);
        public FloatParameter gradientRight = new(1);
        public FloatParameter gradientPower = new(1);
        
        // 后处理参数 >>>>>>>>>>>>>>>>>>>>>>
        public Vector2Parameter distortTextureTiling = new(new Vector2(1, 1));
        public Vector2Parameter distortTextureOffset = new(new Vector2(0, 0));
        
        public ColorParameter outlineColor = new(Color.white, true, false, true);
        // public FloatParameter outlineAlpha = new(1);
        
        public Vector2Parameter distortUVScrollSpeed = new(new Vector2(0, -1));
        public Vector2Parameter distortScreenScale = new(new Vector2(0, -1));
        
        public BoolParameter multipleSampleOn = new(false);
        public Vector2Parameter offsetSampleUV1 = new(new Vector2(0.2f, 0.2f));
        public FloatParameter offsetSampleTime1 = new(0.3f);
        public Vector2Parameter offsetSampleUV2 = new(new Vector2(-0.2f, -0.2f));
        public FloatParameter offsetSampleTime2 = new(0.7f);
        public FloatParameter appendDistortStrength = new(1);

        public FloatParameter disturbanceIntensity = new(1);
        public FloatParameter yAxisStretch = new(1);
        
        public FloatParameter gradientIntensity = new(1);
        
        
        public bool IsActive()
        {
            bool result = false;

            result |= renderData.overrideState;
            
            // 绘制遮罩参数 >>>>>>>>>>>>>>>>>>>>
            result |= meshPreview.overrideState;
            result |= outlineWidth.overrideState;
            result |= outlinePower.overrideState;
            result |= outlineFadeStart.overrideState;
            result |= outlineFadeEnd.overrideState;
            
            result |= yAxisOffset.overrideState;
            result |= invertFadeDirection.overrideState;

            result |= gradientScale.overrideState;
            result |= gradientLeft.overrideState;
            result |= gradientRight.overrideState;
            result |= gradientPower.overrideState;
            
            // 后处理参数 >>>>>>>>>>>>>>>>>>>>>>
            result |= distortTextureTiling.overrideState;
            result |= distortTextureOffset.overrideState;
            
            result |= outlineColor.overrideState;
            // result |= outlineAlpha.overrideState;

            result |= distortUVScrollSpeed.overrideState;
            result |= distortScreenScale.overrideState;

            result |= multipleSampleOn.overrideState;
            result |= offsetSampleUV1.overrideState;
            result |= offsetSampleTime1.overrideState;
            result |= offsetSampleUV2.overrideState;
            result |= offsetSampleTime2.overrideState;
            result |= appendDistortStrength.overrideState;
            
            result |= disturbanceIntensity.overrideState;
            result |= yAxisStretch.overrideState;
            result |= gradientIntensity.overrideState;
            
            if (!result) return false;

            result = outlineWidth.value > 0;
            
            return result;
        }
        
        public bool IsTileCompatible() => false;
        
    }
}