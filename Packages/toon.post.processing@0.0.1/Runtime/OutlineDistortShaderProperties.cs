// Created by: WangYu   Date: 2025-12-19

using UnityEngine;

namespace ToonPostProcessing
{
    public class OutlineDistortShaderProperties
    {
        // 遮罩 >>>>>>>>>>>>>>>>>>>>>>
        public static readonly int _MeshPreview = Shader.PropertyToID("_MeshPreview");
        
        public static readonly int _OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        public static readonly int _OutlinePower = Shader.PropertyToID("_OutlinePower");
        
        public static readonly int _OutlineFadeStart = Shader.PropertyToID("_OutlineFadeStart");
        public static readonly int _OutlineFadeEnd = Shader.PropertyToID("_OutlineFadeEnd");

        public static readonly int _YAxisOffset = Shader.PropertyToID("_YAxisOffset");

        public static readonly int _InvertFadeDirection = Shader.PropertyToID("_InvertFadeDirection");

        public static readonly int _GradientScale = Shader.PropertyToID("_GradientScale");
        public static readonly int _GradientLeft = Shader.PropertyToID("_GradientLeft");
        public static readonly int _GradientRight = Shader.PropertyToID("_GradientRight");
        public static readonly int _GradientPower = Shader.PropertyToID("_GradientPower");
        
        // 后处理 >>>>>>>>>>>>>>>>>>>>
        public static readonly int _DistortTex = Shader.PropertyToID("_DistortTex");
        
        public static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
        public static readonly int _OutlineAlpha = Shader.PropertyToID("_OutlineAlpha");

        public static readonly int _DistortUVScrollSpeed = Shader.PropertyToID("_DistortUVScrollSpeed");
        public static readonly int _DistortScreenScale = Shader.PropertyToID("_DistortScreenScale");
        
        public static readonly int _AccumulatedUVOffset = Shader.PropertyToID("_AccumulatedUVOffset");
        public static readonly int _AccumulatedUVOffset2 = Shader.PropertyToID("_AccumulatedUVOffset2");
        public static readonly int _AccumulatedUVOffset3 = Shader.PropertyToID("_AccumulatedUVOffset3");

        public static readonly int _MultipleSampleOn = Shader.PropertyToID("_MultipleSampleOn");
        public static readonly int _OffsetSampleUV = Shader.PropertyToID("_OffsetSampleUV");
        public static readonly int _OffsetSampleTime = Shader.PropertyToID("_OffsetSampleTime");
        public static readonly int _AppendDistortStrength = Shader.PropertyToID("_AppendDistortStrength");
        
        public static readonly int _DisturbanceIntensity = Shader.PropertyToID("_DisturbanceIntensity");
        public static readonly int _YAxisStretch = Shader.PropertyToID("_YAxisStretch");

        public static readonly int _GradientMaskOn = Shader.PropertyToID("_GradientMaskOn");
        public static readonly int _GradientIntensity = Shader.PropertyToID("_GradientIntensity");
    }

    public class OutlineDistortShaderKeywords
    {
        public const string _MESH_PREVIEW_MODE = "_MESH_PREVIEW_MODE";

        public const string _MULTIPLE_SAMPLE_ON = "_MULTIPLE_SAMPLE_ON";
    }
}