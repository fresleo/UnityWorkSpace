using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Sky")]
    public sealed class SkyVolume : VolumeComponent
    {
        public SkyProfileParameter SkySetting = new SkyProfileParameter(null, true);
        public BoolParameter RenderClouds = new BoolParameter(false);
    }
}