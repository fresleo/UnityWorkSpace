using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RadiantGI.Universal
{
    /// <summary>
    /// 辐照度全局照明
    /// </summary>
    [ExecuteInEditMode, VolumeComponentMenu("Post-processing/Radiant Global Illumination")]
    public class RadiantGlobalIllumination : VolumeComponent, IPostProcessComponent
    {
        public enum EDebugView
        {
            None,
            Albedo,
            Normals,
            Specular,
            Depth,
            Raycast = 20,
            DownscaledHalf = 30,
            DownscaledQuarter = 40,
            ReflectiveShadowMap = 41,
            UpscaleToHalf = 50,
            TemporalAccumulationBuffer = 60,
            FinalGI = 70
        }
        
        
        public FloatParameter indirectIntensity = new FloatParameter(0);
        public ClampedFloatParameter indirectDistanceAttenuation = new ClampedFloatParameter(0, 0, 1);
        public BoolParameter rayBounce = new BoolParameter(false);
        public FloatParameter indirectMaxSourceBrightness = new FloatParameter(8);
        public ClampedFloatParameter normalMapInfluence = new ClampedFloatParameter(1f, 0, 1);
        public FloatParameter lumaInfluence = new FloatParameter(0f);
        
        public FloatParameter nearFieldObscurance = new FloatParameter(0);
        public ClampedFloatParameter nearFieldObscuranceSpread = new ClampedFloatParameter(0.2f, 0.01f, 1f);
        public ClampedFloatParameter nearFieldObscuranceOccluderDistance = new ClampedFloatParameter(0.825f, 0, 1f);
        public FloatParameter nearFieldObscuranceMaxCameraDistance = new FloatParameter(125f);
        [ColorUsage(showAlpha: false)]
        public ColorParameter nearFieldObscuranceTintColor = new ColorParameter(Color.black);
        
        public BoolParameter virtualEmitters = new BoolParameter(false);
        
        public ClampedFloatParameter organicLight = new ClampedFloatParameter(0, 0, 1);
        public ClampedFloatParameter organicLightSpread = new ClampedFloatParameter(0.98f, 0.9f, 1f);
        public ClampedFloatParameter organicLightThreshold = new ClampedFloatParameter(0.5f, 0, 1);
        public ClampedFloatParameter organicLightNormalsInfluence = new ClampedFloatParameter(0.95f, 0f, 1f);
        public ColorParameter organicLightTintColor = new ColorParameter(Color.white);
        public Vector3Parameter organicLightAnimationSpeed = new Vector3Parameter(Vector3.zero);
        public BoolParameter organicLightDistanceScaling = new BoolParameter(false);

        public ClampedIntParameter rayCount = new ClampedIntParameter(1, 1, 4);
        public FloatParameter rayMaxLength = new FloatParameter(8);
        public IntParameter rayMaxSamples = new IntParameter(32);
        public FloatParameter rayJitter = new FloatParameter(0);
        public FloatParameter thickness = new FloatParameter(1f);
        public BoolParameter rayBinarySearch = new BoolParameter(true);
        
        public ClampedIntParameter smoothing = new ClampedIntParameter(3, 0, 4);
        public BoolParameter temporalReprojection = new BoolParameter(true);
        
        public FloatParameter temporalResponseSpeed = new FloatParameter(12);
        public ClampedFloatParameter temporalChromaThreshold = new ClampedFloatParameter(0.2f, 0, 2f);
        public FloatParameter temporalCameraTranslationResponse = new FloatParameter(100);
        
        public BoolParameter fallbackReuseRays = new BoolParameter(false);
        public ClampedFloatParameter rayReuse = new ClampedFloatParameter(0, 0, 1);
        public FloatParameter temporalDepthRejection = new FloatParameter(1f);
        
        public BoolParameter fallbackReflectionProbes = new BoolParameter(false);
        public FloatParameter probesIntensity = new FloatParameter(1f);
        public BoolParameter fallbackReflectiveShadowMap = new BoolParameter(false);
        public ClampedFloatParameter reflectiveShadowMapIntensity = new ClampedFloatParameter(0.8f, 0, 1);
        
        public ClampedIntParameter raytracerAccuracy = new ClampedIntParameter(8, 1, 8);
        public ClampedFloatParameter downsampling = new ClampedFloatParameter(1, 1, 4);
        
        public FloatParameter brightnessThreshold = new FloatParameter(0f);
        public FloatParameter brightnessMax = new FloatParameter(8f);
        public ClampedFloatParameter specularContribution = new ClampedFloatParameter(0.75f, 0, 1f);
        public ClampedFloatParameter sourceBrightness = new ClampedFloatParameter(1f, 0, 2f);
        public FloatParameter giWeight = new FloatParameter(0f);
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1, 0, 2);
        public FloatParameter nearCameraAttenuation = new FloatParameter(0);
        public BoolParameter limitToVolumeBounds = new BoolParameter(false);
        
        public BoolParameter stencilCheck = new BoolParameter(false);
        public IntParameter stencilValue = new IntParameter(1);
        [Serializable]
        public sealed class CompareFunctionParameter : VolumeParameter<CompareFunction>
        {
        }
        public CompareFunctionParameter stencilCompareFunction = new CompareFunctionParameter { value = CompareFunction.NotEqual };
        
        public ClampedFloatParameter aoInfluence = new ClampedFloatParameter(0f, 0, 1f);
        
        public BoolParameter showInEditMode = new BoolParameter(true);
        public BoolParameter showInSceneView = new BoolParameter(true);
        
        [Serializable]
        public sealed class DebugViewParameter : VolumeParameter<EDebugView>
        {
        }
        public DebugViewParameter debugView = new DebugViewParameter { value = EDebugView.None };
        
        public FloatParameter debugDepthMultiplier = new FloatParameter(10);

        public BoolParameter compareMode = new BoolParameter(false);

        public BoolParameter compareSameSide = new BoolParameter(false);

        public ClampedFloatParameter comparePanning = new ClampedFloatParameter(0.25f, 0, 0.5f);

        public ClampedFloatParameter compareLineAngle = new ClampedFloatParameter(1.4f, -Mathf.PI, Mathf.PI);

        public ClampedFloatParameter compareLineWidth = new ClampedFloatParameter(0.002f, 0.0001f, 0.05f);


        public bool IsActive() => indirectIntensity.value > 0 || compareMode.value;

        public bool IsTileCompatible() => true;

        private void OnValidate()
        {
            indirectIntensity.value = Mathf.Max(0, indirectIntensity.value);
            indirectMaxSourceBrightness.value = Mathf.Max(0, indirectMaxSourceBrightness.value);
            temporalResponseSpeed.value = Mathf.Max(0, temporalResponseSpeed.value);
            temporalDepthRejection.value = Mathf.Max(0, temporalDepthRejection.value);
            rayMaxLength.value = Mathf.Max(0.1f, rayMaxLength.value);
            rayMaxSamples.value = Mathf.Max(2, rayMaxSamples.value);
            rayJitter.value = Mathf.Max(0, rayJitter.value);
            lumaInfluence.value = Mathf.Max(0, lumaInfluence.value);
            thickness.value = Mathf.Max(0.1f, thickness.value);
            brightnessThreshold.value = Mathf.Max(0, brightnessThreshold.value);
            brightnessMax.value = Mathf.Max(0, brightnessMax.value);
            nearCameraAttenuation.value = Mathf.Max(0, nearCameraAttenuation.value);
            nearFieldObscurance.value = Mathf.Max(0, nearFieldObscurance.value);
            nearFieldObscuranceMaxCameraDistance.value = Mathf.Max(0, nearFieldObscuranceMaxCameraDistance.value);
            debugDepthMultiplier.value = Mathf.Max(0, debugDepthMultiplier.value);
            giWeight.value = Mathf.Max(0, giWeight.value);
        }

        private void Reset()
        {
            RadiantRenderFeature.needRTRefresh = true;
        }
    }
}