// Created By: WangYu  Date: 2024-07-16

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing.Volumes
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Toon Post-processing/" + nameof(SobelOutline), typeof(XKnightRenderPipeline))]
    public class SobelOutline : VolumeComponent
    {
        public ColorParameter outlineColor = new ColorParameter(value: Color.black, hdr: true, showAlpha: true, showEyeDropper: true);
        
        public FloatParameter outlineThickness = new FloatParameter(1);
        public FloatParameter outlineDistanceFade = new FloatParameter(5000);

        public ClampedFloatParameter outlineEdgeMultiplier = new ClampedFloatParameter(1, 0, 10);
        public ClampedFloatParameter outlineEdgeBias = new ClampedFloatParameter(10, 1E-10f, 30);


        public bool IsActive => outlineColor.overrideState || outlineThickness.overrideState || outlineDistanceFade.overrideState || outlineEdgeMultiplier.overrideState ||
                                outlineEdgeBias.overrideState;

    }
}