// <summary>
//<author>calvin</author>
//date: 2026-05-27
//desc: 预积分SSS贴图生成工具
// </summary>
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
namespace Garena.TA.SSS
{

    public class SSS_PreIntegrated
    {

        //衰减参数
        [SerializeField] public float _minCurvature = 0.0f;
        //最大曲度 1/mm
        [SerializeField] public float _maxCurvature = 2.0f;



        [SerializeField] public BurleyParameters _burleyParameters = new BurleyParameters();


        // ------------------------------- 多高斯方法-------------------------------------------------
        private static readonly float SqrtTwoPi = Mathf.Sqrt(2f * Mathf.PI);

        private Vector3 ComputeDiffuse(float theta, float radius, int _integrationSamples = 64, float _indexOfRefraction = 1.4f)
        {
            Vector3 num = Vector3.zero;
            Vector3 den = Vector3.zero;

            float dx = 2f * Mathf.PI / _integrationSamples; //角度增量
                                                            //卷积
            for (int i = 0; i < _integrationSamples; i++)
            {
                float xParam = -Mathf.PI + (i + 0.5f) * dx;   // [-pi, pi] 取中间点 +0.5f
                float cosNeighbor = Mathf.Cos(theta + xParam);     //临近点的cos值
                float d = 2f * radius * Mathf.Abs(Mathf.Sin(xParam * 0.5f)); // 弦长公式

                Vector3 R =EvaluateBurley(d);

                if (cosNeighbor > 0f)
                {
                    float fresnelIn = FresnelTransmittance(cosNeighbor, _indexOfRefraction);
                    num += cosNeighbor * fresnelIn * R * dx;
                }


                //分母的归一化参数
                den += R * dx;
            }

            return new Vector3(
                den.x > 1e-10f ? num.x / den.x : 0f,
                den.y > 1e-10f ? num.y / den.y : 0f,
                den.z > 1e-10f ? num.z / den.z : 0f);
        }

        // === Fresnel 边界项（光从空气进入皮肤的反射损失）===
        private float FresnelTransmittance(float cosTheta, float ior)
        {
            float r0 = (1.0f - ior) / (1.0f + ior);
            r0 = r0 * r0;
            float fresnel = r0 + (1.0f - r0) * Mathf.Pow(1.0f - cosTheta, 5.0f);
            return 1.0f - fresnel;  // 透射 = 1 - 反射
        }



        private float Gaussian(float d, float sigma)
        {
            float x = d / sigma;
            return Mathf.Exp(-0.5f * x * x) / (sigma * SqrtTwoPi);
        }


        // === Burley profile(迪士尼sss效果) ===
        //公式：R(r) = A * (exp(-s*r/d) + exp(-s*r/(3*d))) / (8*pi*d*r)
        //d: 自由程，A: 散射颜色乘以散射强度，s: 经验参数，保证能量守恒 r: 距离(弦长)
        private Vector3 EvaluateBurley(float r)
        {
            Vector3 A = new Vector3(
                _burleyParameters._scatteringColor.r * _burleyParameters._scatteringMultiplier,
                _burleyParameters._scatteringColor.g * _burleyParameters._scatteringMultiplier,
                _burleyParameters._scatteringColor.b * _burleyParameters._scatteringMultiplier);

            // 自由程
            Vector3 d = new Vector3(_burleyParameters._maxRadius, _burleyParameters._maxRadius, _burleyParameters._maxRadius) / 3.0f;

            // s参数：经验参数，保证光辐射能量永远不会大于A
            Vector3 s = new Vector3(
                ShapeParam(A.x),
                ShapeParam(A.y),
                ShapeParam(A.z));

            if (r < 1e-5f) r = 1e-5f;  // 避免除零


            Vector3 result;
            //每个通道的衰减值
            result.x = A.x * (Mathf.Exp(-s.x * r / d.x) + Mathf.Exp(-s.x * r / (3.0f * d.x)))
                       / (8.0f * Mathf.PI * d.x * r);
            result.y = A.y * (Mathf.Exp(-s.y * r / d.y) + Mathf.Exp(-s.y * r / (3.0f * d.y)))
                       / (8.0f * Mathf.PI * d.y * r);
            result.z = A.z * (Mathf.Exp(-s.z * r / d.z) + Mathf.Exp(-s.z * r / (3.0f * d.z)))
                       / (8.0f * Mathf.PI * d.z * r);
            return result;
        }

        private float ShapeParam(float A)
        {
            float diff = A - 0.8f;
            return 1.85f - A + 7.0f * diff * diff * diff;
        }

        public float GetEffectiveMaxRadius()
        {
            return _burleyParameters._maxRadius;
        }


        // ---------- Disc preview ----------
        public void RegenerateDiscPreview(ref Texture2D _discPreviewTexture, int _discPreviewSize, float _previewExposure)
        {
            int size = _discPreviewSize;
            if (_discPreviewTexture == null || _discPreviewTexture.width != size)
            {
                if (_discPreviewTexture != null) Object.DestroyImmediate(_discPreviewTexture); ;
                _discPreviewTexture = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "DiffusionProfile_Disc",
                };
            }

            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float effectiveR = GetEffectiveMaxRadius();
            float scale = effectiveR / center;  // pixels → mm

            for (int y = 0; y < size; y++)
            {
                float dy = y - center;
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float distancePixels = Mathf.Sqrt(dx * dx + dy * dy);
                    float distance = distancePixels * scale;

                    Vector3 R = (distance > effectiveR) ? Vector3.zero : EvaluateProfile(distance);
                    R *= _previewExposure;
                    pixels[y * size + x] = new Color(R.x, R.y, R.z, 1f);
                }
            }
            _discPreviewTexture.SetPixels(pixels);
            _discPreviewTexture.Apply(false, false);
        }

        private Vector3 EvaluateProfile(float d)
        {
            return EvaluateBurley(d);
        }

        public void RegenerateLUT(ref Texture2D _lutTexture, int _resolution, bool _remapNdotL, EditorWindow editorWindow)
        {
            if (_lutTexture != null) Object.DestroyImmediate(_lutTexture);

            _lutTexture = new Texture2D(_resolution, _resolution, TextureFormat.RGBAFloat, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "PreIntegratedSkinLUT",
            };

            var pixels = new Color[_resolution * _resolution];
            try
            {
                for (int y = 0; y < _resolution; y++)
                {
                    if ((y & 7) == 0)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(
                                "Generating Pre-Integrated LUT",
                                $"Row {y + 1}/{_resolution}",
                                (float)y / _resolution))
                        {
                            EditorUtility.ClearProgressBar();
                            return;
                        }
                    }

                    float curvature = _resolution > 1
                        ? Mathf.Lerp(_minCurvature, _maxCurvature, (float)y / (_resolution - 1))
                        : _minCurvature;
                    float radius = curvature > 1e-6f ? 1f / curvature : 1e6f;

                    for (int x = 0; x < _resolution; x++)
                    {
                        float u = _resolution > 1 ? (float)x / (_resolution - 1) : 0f;
                        float nDotL = _remapNdotL ? Mathf.Lerp(-1f, 1f, u) : u;
                        float theta = Mathf.Acos(Mathf.Clamp(nDotL, -1f, 1f));

                        Vector3 result = ComputeDiffuse(theta, radius);
                        pixels[y * _resolution + x] = new Color(result.x, result.y, result.z, 1f);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _lutTexture.SetPixels(pixels);
            _lutTexture.Apply(false, false);
            editorWindow.Repaint();
        }

        public void RegenerateCurvePreview(ref Texture2D _curvePreviewTexture, int _discPreviewSize, float _previewExposure, bool _previewLogScale)
        {
            const int width = 384;
            int height = _discPreviewSize;

            if (_curvePreviewTexture == null
                || _curvePreviewTexture.width != width || _curvePreviewTexture.height != height)
            {
                if (_curvePreviewTexture != null) Object.DestroyImmediate(_curvePreviewTexture);
                _curvePreviewTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "DiffusionProfile_Curve",
                };
            }

            var pixels = new Color[width * height];
            float effectiveR = GetEffectiveMaxRadius();

            // Pass 1: compute R(d) along the X axis, track max for normalization
            var profileValues = new Vector3[width];
            float maxVal = 0f;
            for (int x = 0; x < width; x++)
            {
                float distance = (x / (float)(width - 1)) * effectiveR;
                Vector3 R = EvaluateProfile(distance) * _previewExposure;
                profileValues[x] = R;
                maxVal = Mathf.Max(maxVal, R.x, R.y, R.z);
            }
            if (maxVal < 1e-6f) maxVal = 1f;

            float logMin = -4f;          // log10 floor for log scale
            float logMax = Mathf.Log10(maxVal);

            // Pass 2: rasterize bars per channel
            for (int x = 0; x < width; x++)
            {
                Vector3 R = profileValues[x];

                Vector3 Rn;
                if (_previewLogScale)
                {
                    Rn = new Vector3(
                        Mathf.Clamp01((Mathf.Log10(Mathf.Max(R.x, 1e-6f)) - logMin) / (logMax - logMin)),
                        Mathf.Clamp01((Mathf.Log10(Mathf.Max(R.y, 1e-6f)) - logMin) / (logMax - logMin)),
                        Mathf.Clamp01((Mathf.Log10(Mathf.Max(R.z, 1e-6f)) - logMin) / (logMax - logMin)));
                }
                else
                {
                    Rn = new Vector3(R.x / maxVal, R.y / maxVal, R.z / maxVal);
                }

                for (int y = 0; y < height; y++)
                {
                    float yNorm = y / (float)(height - 1);  // 0 at bottom (SetPixels y=0 is bottom)
                    const float bg = 0.08f;

                    float r = bg, g = bg, b = bg;
                    if (yNorm <= Rn.x) r += 0.95f;
                    if (yNorm <= Rn.y) g += 0.85f;
                    if (yNorm <= Rn.z) b += 0.95f;

                    pixels[y * width + x] = new Color(r, g, b, 1f);
                }
            }

            // Grid lines at 25%, 50%, 75%
            for (int frac = 1; frac <= 3; frac++)
            {
                int yLine = (height - 1) * frac / 4;
                for (int x = 0; x < width; x++)
                {
                    var c = pixels[yLine * width + x];
                    pixels[yLine * width + x] = new Color(
                        c.r * 0.7f + 0.15f, c.g * 0.7f + 0.15f, c.b * 0.7f + 0.15f, 1f);
                }
            }

            _curvePreviewTexture.SetPixels(pixels);
            _curvePreviewTexture.Apply(false, false);
        }

        public Vector3 EvaluateProfilePublic(float d) => EvaluateProfile(d);
        //皮肤1
        public void LoadPresetBurleySkin()
        {
            _burleyParameters._scatteringColor = new Color(0.75f, 0.40f, 0.28f);
            _burleyParameters._scatteringMultiplier = 1.0f;
            _burleyParameters._maxRadius = 5.0f;
            _burleyParameters._indexOfRefraction = 1.4f;
        }

        public void LoadPresetMarble()
        {
            _burleyParameters._scatteringColor = new Color(0.85f, 0.78f, 0.70f);
            _burleyParameters._scatteringMultiplier = 1.0f;
            _burleyParameters._maxRadius = 8.0f;
            _burleyParameters._indexOfRefraction = 1.5f;
        }

        public void LoadPaperPreset(SSS_MaterialPreset preset)
        {
            SSS_DipoleConverter.ApplyPresetToBurley(preset, _burleyParameters);
        }
        public void ExportBothLUTs(
    int resolution, bool remapNdotL,
    int discSampleCount, OutputFormat format, string basePath, EditorWindow editorWindow)
        {
            // ---- 1. Penner LUT ----
            Texture2D pennerLut = null;
            RegenerateLUT(ref pennerLut, resolution, remapNdotL, editorWindow);
            if (pennerLut != null)
                SSS_ImageExportTools.SaveToAsset(format, ref pennerLut, basePath + "_PennerLUT");

            // ---- 2. Burley disc 采样表 ----
            float maxR = GetEffectiveMaxRadius();
            Texture2D discKernel = SSS_DiscSampling.GenerateDiscKernel(_burleyParameters, discSampleCount, maxR);
            if (discKernel != null)
                SSS_DiscSampling.SaveDiscKernel(format, discKernel, basePath + "_BurleyDisc");

            Debug.Log($"[SSS] 已导出 Penner LUT + Burley disc 采样表 ({discSampleCount} samples) → {basePath}_*");
        }

    }
}