/*******************************************************************************
 * File: SpiralFluidTransitionShaderProperties.cs
 * Author: fan.shi
 * Date: 2026/05/20
 * Description: SpiralFluidTransition的Shader 属性缓存。
 *
 * Notice:
 *******************************************************************************/

using UnityEngine;

namespace ToonPostProcessing
{
    /// <summary>
    /// 旋涡流体转场的 Shader 属性与关键字缓存。
    /// </summary>
    public static class SpiralFluidTransitionShaderProperties
    {
        /// <summary>
        /// 低质量路径关键字。
        /// </summary>
        public const string CLOW_QUALITY = "_SPIRAL_FLUID_LOW_QUALITY";

        /// <summary>
        /// 输出到后处理之后时需要执行 Linear 转 sRGB 的关键字。
        /// </summary>
        public const string CNEED_LINEAR_TO_SRGB = "_NEED_LINEAR_TO_SRGB";

        /// <summary>
        /// 外部传入 / Camera.Render 捕获的 FromRT，采样时需要 sRGB 转 Linear。
        /// </summary>
        public const string CFROM_TEX_DISPLAY_SRGB = "_FROM_TEX_DISPLAY_SRGB";

        /// <summary>
        /// Camera.Render 捕获的 ToRT，采样时需要 sRGB 转 Linear。
        /// </summary>
        public const string CTO_TEX_DISPLAY_SRGB = "_TO_TEX_DISPLAY_SRGB";

        private static readonly int _fromTex = Shader.PropertyToID("_FromTex");
        private static readonly int _toTex = Shader.PropertyToID("_ToTex");
        private static readonly int _distortionTex = Shader.PropertyToID("_DistortionTex");
        private static readonly int _distortionTilingFlow = Shader.PropertyToID("_DistortionTilingFlow");
        private static readonly int _textureDistortionParams = Shader.PropertyToID("_TextureDistortionParams");
        private static readonly int _warmBrightLut = Shader.PropertyToID("_WarmBrightLut");
        private static readonly int _warmBrightLutParams = Shader.PropertyToID("_WarmBrightLutParams");
        private static readonly int _center = Shader.PropertyToID("_Center");
        private static readonly int _transitionParams = Shader.PropertyToID("_TransitionParams");
        private static readonly int _visualParams = Shader.PropertyToID("_VisualParams");
        private static readonly int _toFinishParams = Shader.PropertyToID("_ToFinishParams");
        private static readonly int _radiusParams = Shader.PropertyToID("_RadiusParams");
        private static readonly int _swirlParams = Shader.PropertyToID("_SwirlParams");
        private static readonly int _noiseParams = Shader.PropertyToID("_NoiseParams");
        private static readonly int _edgeParams = Shader.PropertyToID("_EdgeParams");
        private static readonly int _foldParams = Shader.PropertyToID("_FoldParams");
        private static readonly int _exposureParams = Shader.PropertyToID("_ExposureParams");
        private static readonly int _layerParams = Shader.PropertyToID("_LayerParams");

        /// <summary>
        /// FromRT Shader 属性 ID。
        /// </summary>
        public static int FromTex => _fromTex;

        /// <summary>
        /// ToRT Shader 属性 ID。
        /// </summary>
        public static int ToTex => _toTex;

        /// <summary>
        /// Texture distortion Shader 属性 ID。
        /// </summary>
        public static int DistortionTex => _distortionTex;

        /// <summary>
        /// Texture distortion tiling and flow Shader 属性 ID。
        /// </summary>
        public static int DistortionTilingFlow => _distortionTilingFlow;

        /// <summary>
        /// Texture distortion strength Shader 属性 ID。
        /// </summary>
        public static int TextureDistortionParams => _textureDistortionParams;

        /// <summary>
        /// Warm Bright LUT Shader property ID.
        /// </summary>
        public static int WarmBrightLut => _warmBrightLut;

        /// <summary>
        /// Warm Bright LUT parameter Shader property ID.
        /// </summary>
        public static int WarmBrightLutParams => _warmBrightLutParams;

        /// <summary>
        /// 转场中心 Shader 属性 ID。
        /// </summary>
        public static int Center => _center;

        /// <summary>
        /// 进度、宽高比、时间和预提亮阶段参数 Shader 属性 ID。
        /// </summary>
        public static int TransitionParams => _transitionParams;

        /// <summary>
        /// Visual progress Shader property ID.
        /// </summary>
        public static int VisualParams => _visualParams;

        /// <summary>
        /// ToTex finish timing and debug mode Shader property ID.
        /// </summary>
        public static int ToFinishParams => _toFinishParams;

        /// <summary>
        /// 半径与边缘宽度参数 Shader 属性 ID。
        /// </summary>
        public static int RadiusParams => _radiusParams;

        /// <summary>
        /// 旋涡和扭曲参数 Shader 属性 ID。
        /// </summary>
        public static int SwirlParams => _swirlParams;

        /// <summary>
        /// 噪声参数 Shader 属性 ID。
        /// </summary>
        public static int NoiseParams => _noiseParams;

        /// <summary>
        /// 边缘效果参数 Shader 属性 ID。
        /// </summary>
        public static int EdgeParams => _edgeParams;

        /// <summary>
        /// 翻卷参数 Shader 属性 ID。
        /// </summary>
        public static int FoldParams => _foldParams;

        /// <summary>
        /// 曝光参数 Shader 属性 ID。
        /// </summary>
        public static int ExposureParams => _exposureParams;

        /// <summary>
        /// 分层扭曲参数 Shader 属性 ID。
        /// </summary>
        public static int LayerParams => _layerParams;

    }
}
