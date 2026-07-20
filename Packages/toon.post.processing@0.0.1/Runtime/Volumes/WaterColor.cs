using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing.Volumes
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Toon Post-processing/" + nameof(WaterColor), typeof(XKnightRenderPipeline))]
    public class WaterColor : VolumeComponent
    {
        public ColorParameter waterColor = new ColorParameter(Color.white, true);

        public ClampedIntParameter xRadius = new ClampedIntParameter(1, 0, 8);
        public ClampedIntParameter yRadius = new ClampedIntParameter(1, 0, 8);

        public bool IsActive => waterColor.overrideState || xRadius.overrideState || yRadius.overrideState;
        
    }
}