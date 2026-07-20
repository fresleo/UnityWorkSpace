using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA.VolumetricLightingFog
{

    public class GlobalVolumetricLightingFogSettings : VolumeComponent, IPostProcessComponent
    {
        //
        [Range(0.5f, 100)]
        public FloatParameter StepSize = new FloatParameter(1);

        [Range(0, 4)]
        public ClampedIntParameter BlurIterations = new ClampedIntParameter(0, 0, 4);
        //体积光
        public BoolParameter OpenVolumetricLight = new BoolParameter(false);
        public FloatParameter VolumetricLightMinHeight = new FloatParameter(0);
        public FloatParameter VolumetricLightMaxHeight = new FloatParameter(100);
        [Range(0, 1)]
        public FloatParameter VolumetricLightDensity = new FloatParameter(0.1f);
        [Range(0.01f, 2)]
        public FloatParameter LightingScale = new FloatParameter(2); //

        [Range(0, 1)]
        public FloatParameter LightingContrast = new FloatParameter(0);
        [Range(-1, 1)]
        public FloatParameter LightingAnisotropy = new FloatParameter(0);
        //体积雾
        public BoolParameter OpenVolumetricFog = new BoolParameter(false);
        public ColorParameter VolumetricFogColor = new ColorParameter(Color.white);
        public FloatParameter VolumetricFogMinHeight = new FloatParameter(0);
        public FloatParameter VolumetricFogMaxHeight = new FloatParameter(200);
        public FloatParameter FadeoutDistance = new FloatParameter(100);
        [Range(0, 1)]
        public FloatParameter VolumetricFogDensity = new FloatParameter(0.5f);
        public BoolParameter OpenLocalVolumetricFog = new BoolParameter(false);
        public bool IsActive()
        {
            return OpenVolumetricFog.value || OpenVolumetricLight.value || OpenLocalVolumetricFog.value;
        }
        public bool IsTileCompatible()
        {
            return true;
        }
    }
}