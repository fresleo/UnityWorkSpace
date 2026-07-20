using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XKnight.Reflection.Runtime
{
    public enum OutputMode
    {
        Final,
        OnlyReflections,
        SideBySideComparison,
        DebugRayCast = 9,
        DebugDepth = 10,
        DebugDeferredNormals = 11
    }

    public enum SkyboxResolution
    {
        [InspectorName("16")] _16,
        [InspectorName("32")] _32,
        [InspectorName("64")] _64,
        [InspectorName("128")] _128,
        [InspectorName("256")] _256,
        [InspectorName("512")] _512,
        [InspectorName("1024")] _1024,
        [InspectorName("2048")] _2048,
        [InspectorName("4096")] _4096,
        [InspectorName("8192")] _8192
    }

    public enum SkyboxUpdateMode
    {
        OnStart,
        Interval,
        CustomCubemap
    }

    public enum SkyboxContributionPass
    {
        Deferred,
        Forward,
        Both
    }

    [ExecuteInEditMode, VolumeComponentMenu("XKTSSR")]
    public class XKTSSR : VolumeComponent, IPostProcessComponent
    {
        //TODO:改为Intensity
        public ClampedFloatParameter reflectionsMultiplier = new ClampedFloatParameter(0f, 0, 2f);

        /// <summary>
        /// 输出模式
        /// </summary>
        [Serializable]
        public sealed class OutputModeParameter : VolumeParameter<OutputMode>
        {
        }

        public OutputModeParameter outputMode = new OutputModeParameter { value = OutputMode.Final };

        public Texture2DParameter noiseTexture = new Texture2DParameter(null);

        /// <summary>
        /// 采样次数
        /// </summary>
        public ClampedIntParameter sampleCount = new ClampedIntParameter(16, 4, 256);

        public FloatParameter maxRayLength = new FloatParameter(24f);

        public FloatParameter thickness = new FloatParameter(0.8f);

        public ClampedIntParameter binarySearchIterations = new ClampedIntParameter(6, 0, 16);

        public ClampedFloatParameter jitter = new ClampedFloatParameter(0.3f, 0, 1f);

        //public BoolParameter computeBackFaces = new BoolParameter(false);

        [Serializable]
        public sealed class SkyboxResolutionParameter : VolumeParameter<SkyboxResolution>
        {
        }


        public SkyboxResolutionParameter skyboxResolution = new SkyboxResolutionParameter
            { value = SkyboxResolution._1024 };

        [Serializable]
        public sealed class SkyboxUpdateModeParameter : VolumeParameter<SkyboxUpdateMode>
        {
        }

        public SkyboxUpdateModeParameter skyboxUpdateMode = new SkyboxUpdateModeParameter
            { value = SkyboxUpdateMode.OnStart };

        public FloatParameter skyboxUpdateInterval = new FloatParameter(3f);

        [Serializable]
        public sealed class SkyboxContributionPassParameter : VolumeParameter<SkyboxContributionPass>
        {
        }
        
        public BoolParameter refineThickness = new BoolParameter(false);

        public ClampedFloatParameter thicknessFine = new ClampedFloatParameter(0.05f, 0.005f, 1f);

        public SkyboxContributionPassParameter skyboxContributionPass = new SkyboxContributionPassParameter
            { value = SkyboxContributionPass.Forward };

        public TextureParameter skyboxCustomCubemap = new TextureParameter(null);

        public ClampedFloatParameter thicknessMinimum = new ClampedFloatParameter(0.16f, 0.01f, 1f);

        public LayerMaskParameter computeBackFacesLayerMask = new LayerMaskParameter(-1);

        public ClampedIntParameter downsampling = new ClampedIntParameter(1, 1, 4);

        public FloatParameter depthBias = new FloatParameter(0.03f);

        /// <summary>
        /// 反射光滑度最低阈值（即物体反射的清晰程度）
        /// </summary>
        public ClampedFloatParameter smoothnessThreshold = new ClampedFloatParameter(0, 0, 1f);

        public ClampedFloatParameter reflectionsMinIntensity = new ClampedFloatParameter(0, 0, 1f);

        public ClampedFloatParameter reflectionsMaxIntensity = new ClampedFloatParameter(1f, 0, 1f);

        public ClampedFloatParameter fresnel = new ClampedFloatParameter(0.75f, 0, 1f);

        public FloatParameter decay = new FloatParameter(2f);

        public ClampedFloatParameter metallicBoost = new ClampedFloatParameter(0f, 0, 0.3f);

        public ClampedFloatParameter metallicBoostThreshold = new ClampedFloatParameter(0f, 0, 1f);

        public BoolParameter specularControl = new BoolParameter(true);

        public FloatParameter specularSoftenPower = new FloatParameter(15f);

        public ClampedFloatParameter skyboxIntensity = new ClampedFloatParameter(0f, 0f, 1f);

        public FloatParameter nearCameraAttenuationStart = new FloatParameter(0);

        public FloatParameter nearCameraAttenuationRange = new FloatParameter(1);

        public ClampedFloatParameter vignetteSize = new ClampedFloatParameter(1.1f, 0.5f, 2f);

        public ClampedFloatParameter vignettePower = new ClampedFloatParameter(1.5f, 0.1f, 10f);

        [Header("Reflection Sharpness")] public FloatParameter fuzzyness = new FloatParameter(0);

        public FloatParameter contactHardening = new FloatParameter(0);

        public ClampedFloatParameter minimumBlur = new ClampedFloatParameter(0.25f, 0, 4f);

        public ClampedIntParameter blurDownsampling = new ClampedIntParameter(1, 1, 8);

        public Vector2Parameter blurStrength = new Vector2Parameter(Vector2.one);

        public BoolParameter stopNaN = new BoolParameter(false);

        public LayerMaskParameter reflectionsScriptsLayerMask = new LayerMaskParameter(-1);
        public ClampedFloatParameter separationPos = new ClampedFloatParameter(0.5f, -0.01f, 1.01f);

        public bool IsActive() => reflectionsMultiplier.value > 0 && reflectionsMaxIntensity.value > 0;

        public bool IsTileCompatible() => true;

        private void OnValidate()
        {
            decay.value = Mathf.Max(1f, decay.value);
            maxRayLength.value = Mathf.Max(0.1f, maxRayLength.value);

            fuzzyness.value = Mathf.Max(0, fuzzyness.value);
            thickness.value = Mathf.Max(0.01f, thickness.value);
            contactHardening.value = Mathf.Max(0, contactHardening.value);
            reflectionsMaxIntensity.value = Mathf.Max(reflectionsMinIntensity.value, reflectionsMaxIntensity.value);
            nearCameraAttenuationStart.value = Mathf.Max(0, nearCameraAttenuationStart.value);
            //nearCameraAttenuationRange.value = Mathf.Max(0.001f, nearCameraAttenuationRange.value);
            Vector2 blurStrength = this.blurStrength.value;
            blurStrength.x = Mathf.Max(blurStrength.x, 0f);
            blurStrength.y = Mathf.Max(blurStrength.y, 0f);
            this.blurStrength.value = blurStrength;
        }
    }
}