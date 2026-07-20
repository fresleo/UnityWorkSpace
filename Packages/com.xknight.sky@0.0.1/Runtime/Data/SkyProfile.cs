using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    public class SkyProfileParameter : VolumeParameter<AtmosphericScatteringProfile>
    {
        public SkyProfileParameter(AtmosphericScatteringProfile value, bool overrideState = false) : base(value, overrideState) { }
    }
}