// Created By: WangYu  Date: 2024-07-31

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing.Volumes
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Toon Post-processing/" + nameof(PreObjectIdOutline), typeof(XKnightRenderPipeline))]
    public class PreObjectIdOutline : VolumeComponent
    {
        public ColorParameter outlineColor = new(value: Color.black, hdr: true, showAlpha: true, showEyeDropper: true);
        
        public ClampedFloatParameter outlineIntensityMultiplier = new(1, 0, 20);
        public FloatParameter outlineDistanceFade = new(5000);
        public ClampedIntParameter outlineMinSeparation = new(1, 1, 5);
        public ClampedFloatParameter outlineWidth = new(1, 1, 10f);
        
        public ClampedFloatParameter blurIntensity = new(1f, 0f, 1f);

        public BoolParameter enableAntiAliasing = new(false);

        public bool IsActive => outlineColor.overrideState ||
                                outlineIntensityMultiplier.overrideState || outlineDistanceFade.overrideState || outlineMinSeparation.overrideState || outlineWidth.overrideState ||
                                blurIntensity.overrideState || 
                                enableAntiAliasing.overrideState;

    }
}