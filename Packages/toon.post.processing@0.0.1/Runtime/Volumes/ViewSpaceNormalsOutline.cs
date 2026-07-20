// Created By: WangYu  Date: 2024-08-05

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing.Volumes
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Toon Post-processing/" + nameof(ViewSpaceNormalsOutline), typeof(XKnightRenderPipeline))]
    public class ViewSpaceNormalsOutline : VolumeComponent
    {
        public ColorParameter outlineColor = new(value: Color.black, hdr: true, showAlpha: true, showEyeDropper: true);

        public FloatParameter outlineDistanceFade = new(5000);
        public ClampedFloatParameter outlineScale = new(1f, 0f, 5f);

        // 描边过滤设置
        public ClampedFloatParameter depthThreshold = new(1.5f, 0f, 100f);
        public ClampedFloatParameter depthDiffMultiplier = new(100f, 0f, 500f);
        public ClampedFloatParameter normalThreshold = new(0.4f, 0f, 1f);

        // 深度法线关系设置
        public ClampedFloatParameter steepAngleThreshold = new(0.2f, 0f, 2f);
        public ClampedFloatParameter steepAngleMultiplier = new(25f, 0f, 500f);

        // 其它设置
        public BoolParameter enableAntiAliasing = new(false);

        public bool IsActive
        {
            get
            {
                bool result = false;
                result |= outlineColor.overrideState;
                
                result |= outlineDistanceFade.overrideState;
                result |= outlineScale.overrideState;
                
                result |= depthThreshold.overrideState;
                result |= depthDiffMultiplier.overrideState;
                result |= normalThreshold.overrideState;
                
                result |= steepAngleThreshold.overrideState;
                result |= steepAngleMultiplier.overrideState;
                
                result |= enableAntiAliasing.overrideState;
                return result;
            }
            set
            {
                bool result = value;
                if (!result)
                {
                    outlineColor.overrideState = false;
                    
                    outlineDistanceFade.overrideState = false;
                    outlineScale.overrideState = false;

                    depthThreshold.overrideState = false;
                    depthDiffMultiplier.overrideState = false;
                    normalThreshold.overrideState = false;

                    steepAngleThreshold.overrideState = false;
                    steepAngleMultiplier.overrideState = false;

                    enableAntiAliasing.overrideState = false;
                }
            }
        }
        
    }
}