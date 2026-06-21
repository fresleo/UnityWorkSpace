/// <summary>
/// author : calvin
/// date   : 2026-05-28
/// desc   : Burley disc 采样表生成器（屏幕空间 SSSS 用）
///          对 Burley profile 做重要性采样，生成 disc kernel 查找表。
///          cdf:从入射点开始-最大半径为maxradius的圆盘上，光的能量变化
/// </summary>

using System.IO;
using UnityEngine;
using UnityEditor;

namespace Garena.TA.SSS
{
    public static class SSS_DiscSampling
    {
        //黄金角 
        private const float GoldenAngle = 2.39996323f; // π(3-√5)

        /// <summary>
        /// 生成 disc 采样核纹理。
        /// </summary>
        /// <param name="burleyParameters">Burley 采样参数</param>
        /// <param name="sampleCount">采样点数）</param>
        /// <param name="importanceCdfResolution">构建逆 CDF 的精度，采样时累计分布的精度</param>
        public static Texture2D GenerateDiscKernel(
            BurleyParameters burleyParameters,
            int sampleCount,
            int importanceCdfResolution = 1024)
        {
            float maxRadius = burleyParameters._maxRadius;
            if (burleyParameters == null)
            {
                Debug.LogError("[SSS Disc] BurleyParameters 为空");
                return null;
            }

            // 2D 圆盘上的面积权重是 R(r)·r（极坐标面积元 r·dr·dθ）
            int M = Mathf.Max(64, importanceCdfResolution);
            var cdf = new float[M + 1];
            cdf[0] = 0f; //因为从采样区间的最左端开始采样

            //这里的maxRadius为采样圆盘的最大半径，如果对应shader中，实际采样的弦长不会大于这个半径
            float dr = maxRadius / M;

            for (int i = 0; i < M; i++)
            {
                float r = (i + 0.5f) * dr; //采样中间点
                Vector3 R = EvaluateBurley(burleyParameters, r); //获取该半径下的衰减值
                float lum = (R.x + R.y + R.z) / 3f; // 平均亮度
                float pdfUnnorm = Mathf.Max(lum * r, 0f); //防止亮度为负值：打个比方，光找到物体上，没有任何光子会减掉能量，所以衰减值不应该为负数
                cdf[i + 1] = cdf[i] + pdfUnnorm * dr;
            }

            float total = cdf[M]; //这个点是采样区间的最右端，理论上是能量的总和，也就是1，因为Burley返回的衰减值已经是归一化的
            if (total < 1e-12f) total = 1f; //如果衰减值过小，说明几乎没有光子会到达这个半径，直接把总量设为1，避免后续除以total时数值不稳定
            for (int i = 0; i <= M; i++) cdf[i] /= total; // 归一化到 [0,1]，反应从0到r的累计能量占比（CDF）

            // ---------- 2. 逆 CDF 重要性采样 N 个半径 ----------
            var radii = new float[sampleCount];
            var weights = new Vector3[sampleCount];
            Vector3 weightSum = Vector3.zero;

            for (int s = 0; s < sampleCount; s++)
            {
                // 分层采样：把 [0,1] 均匀切 N 段，取段中点
                float xi = (s + 0.5f) / sampleCount;
                //假设 sampleCount =32. 那么需要知道的是xi从0-1进行线性排列，每个xi对应一个半径r，
                //cdf越陡峭的地方，r的分布应该越密集，反之越稀疏。通过逆cdf函数，可以根据xi找到对应的r，使得r的分布满足重要性采样的要求。
                float r = InverseCdf(cdf, xi, maxRadius);
                radii[s] = r;

                // 衰减值采样
                Vector3 R = EvaluateBurley(burleyParameters, r);

                // 重要性采样的 pdf(r) ∝ lum(r)·r / total，权重 = R / pdf
                //  平均亮度 
                float lum = (R.x + R.y + R.z) / 3f;
                float pdf = Mathf.Max(lum * r / total, 1e-8f); //防止pdf过小导致权重过大

                Vector3 w = new Vector3(R.x / pdf, R.y / pdf, R.z / pdf);
                weights[s] = w;
                weightSum += w;
            }

            // ---------- 3. 归一化权重，使每通道 Σ = 1（能量守恒）----------
            for (int s = 0; s < sampleCount; s++)
            {
                weights[s] = new Vector3(
                    weightSum.x > 1e-8f ? weights[s].x / weightSum.x : 0f,
                    weightSum.y > 1e-8f ? weights[s].y / weightSum.y : 0f,
                    weightSum.z > 1e-8f ? weights[s].z / weightSum.z : 0f);
            }

            // ---------- 4. 写入 N×1 纹理 ----------
            var tex = new Texture2D(sampleCount, 1, TextureFormat.RGBAFloat, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point, // 采样表必须 Point，禁止插值
                name = "BurleyDiscKernel_" + maxRadius.ToString("F1") + "mm_" + sampleCount + "samples",
            };

            var pixels = new Color[sampleCount];
            for (int s = 0; s < sampleCount; s++)
                pixels[s] = new Color(weights[s].x, weights[s].y, weights[s].z, radii[s]); //不是被“归一化”为 [0,1]

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private static Vector3 EvaluateBurley(BurleyParameters burleyParameters, float r)
        {
            Vector3 A = new Vector3(
                burleyParameters._scatteringColor.r * burleyParameters._scatteringMultiplier,
                burleyParameters._scatteringColor.g * burleyParameters._scatteringMultiplier,
                burleyParameters._scatteringColor.b * burleyParameters._scatteringMultiplier);

            Vector3 d = burleyParameters.GetMeanFreePath();

            Vector3 s = new Vector3(
                BurleyFunction.ShapeParam(A.x),
                BurleyFunction.ShapeParam(A.y),
                BurleyFunction.ShapeParam(A.z));

            r = Mathf.Max(r, 1e-5f);

            return new Vector3(
                A.x * (Mathf.Exp(-s.x * r / d.x) + Mathf.Exp(-s.x * r / (3.0f * d.x))) / (8.0f * Mathf.PI * d.x * r),
                A.y * (Mathf.Exp(-s.y * r / d.y) + Mathf.Exp(-s.y * r / (3.0f * d.y))) / (8.0f * Mathf.PI * d.y * r),
                A.z * (Mathf.Exp(-s.z * r / d.z) + Mathf.Exp(-s.z * r / (3.0f * d.z))) / (8.0f * Mathf.PI * d.z * r));
        }



        /// <summary>逆 CDF 查找：给定 ξ∈[0,1] 返回对应半径（线性插值）。</summary>
        private static float InverseCdf(float[] cdf, float xi, float maxRadius)
        {
            int M = cdf.Length - 1;
            // 二分找 cdf[i] <= xi < cdf[i+1]
            int lo = 0, hi = M;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                //如果cdf[mid] < xi，说明xi在mid的右边，所以lo=mid+1；否则说明xi在mid的左边或者等于mid，所以hi=mid
                if (cdf[mid] < xi) lo = mid + 1;
                else hi = mid;
            }

            int idx = Mathf.Clamp(lo - 1, 0, M - 1);

            float c0 = cdf[idx], c1 = cdf[idx + 1];
            float t = (c1 - c0) > 1e-8f ? (xi - c0) / (c1 - c0) : 0f;
            float rNorm = (idx + t) / M;
            return rNorm * maxRadius;
        }

        // ============================================================
        // 导出 disc kernel 到磁盘
        // ============================================================
        public static bool SaveDiscKernel(
            OutputFormat format, Texture2D discKernel, string path)
        {
            if (discKernel == null)
            {
                Debug.LogError("[SSS Disc] disc kernel 为空");
                return false;
            }

            // disc kernel 必须无损浮点，强制 EXR 或 Asset，PNG 会丢精度
            if (format == OutputFormat.PNG)
            {
                Debug.LogWarning("[SSS Disc] disc kernel 含 HDR 权重和半径，PNG 会丢精度，已强制改用 EXR。");
                format = OutputFormat.EXR;
            }

            return SSS_ImageExportTools.SaveToAsset(format, ref discKernel, path);
        }

        /// <summary>
        /// 导出 Burley 参数 + 生成纹理 为可编辑的 ScriptableObject 资产。
        /// </summary>
        public static bool SaveResolveProfileParamsAsset(
            BurleyParameters burleyParameters,
            float worldScale,
            Texture2D discKernelTex,
            RenderTexture discPreviewTexture, 
            int discSampleCount,
            float discKernelMaxRadius,
            string path)
        {
            if (burleyParameters == null)
            {
                Debug.LogError("[SSS Params] BurleyParameters 为空，无法导出参数资产。");
                return false;
            }

            string assetPath = path;
            if (string.IsNullOrEmpty(assetPath))
                assetPath = "Assets/SSSResolveProfileParams.asset";
            if (!assetPath.StartsWith("Assets"))
                assetPath = "Assets/" + assetPath;
            if (!assetPath.EndsWith(".asset"))
                assetPath = Path.ChangeExtension(assetPath, ".asset");
            assetPath = assetPath.Replace('\\', '/');

            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir))
            {
                string absDir = SSS_ImageExportTools.SystemPath(dir);
                if (!Directory.Exists(absDir))
                    Directory.CreateDirectory(absDir);
            }

            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var asset = ScriptableObject.CreateInstance<DiffusionProfileParam>();
            asset.name = "SSSResolveProfileParams";
            asset.scatteringColor = burleyParameters._scatteringColor;
            asset.scatteringMultiplier = burleyParameters._scatteringMultiplier;
            asset.maxRadius = burleyParameters._maxRadius;
            asset.indexOfRefraction = burleyParameters._indexOfRefraction;
            asset.worldScale = worldScale;
            asset.kernelSampleCount = discSampleCount;
            asset.updateKernel();

            AssetDatabase.CreateAsset(asset, assetPath);

            var kernelCopy = DuplicateTextureForAsset(discKernelTex, "DiscKernel");
            if (kernelCopy != null)
            {
                AssetDatabase.AddObjectToAsset(kernelCopy, asset);
                asset.discKernelTex = kernelCopy;
            }

            // 如果预览是 RenderTexture，则先读回为 Texture2D，然后把内容复制到一个新的 RenderTexture 资产中
            Texture2D previewSource = null;
            if (discPreviewTexture != null)
            {
                // 读回到临时 Texture2D（CPU 可访问）
                var prev = RenderTexture.active;
                RenderTexture.active = discPreviewTexture;
                previewSource = new Texture2D(discPreviewTexture.width, discPreviewTexture.height,
                    TextureFormat.RGBAFloat, false, true);
                previewSource.ReadPixels(new Rect(0, 0, discPreviewTexture.width, discPreviewTexture.height), 0, 0,
                    false);
                previewSource.Apply(false, false);
                RenderTexture.active = prev;

                // 创建一个新的 RenderTexture 作为可序列化的子资产，并把预览内容拷贝到其中
                var previewRT = new RenderTexture(previewSource.width, previewSource.height, 0, RenderTextureFormat.ARGBFloat)
                {
                    name = "DiscPreview",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    useMipMap = false,
                    autoGenerateMips = false
                };

                // 将临时 Texture2D 内容拷贝到 RenderTexture
                var prevActive = RenderTexture.active;
                Graphics.Blit(previewSource, previewRT);
                RenderTexture.active = prevActive;

                AssetDatabase.AddObjectToAsset(previewRT, asset);
                asset.discPreviewTexture = previewRT;
            }

            // 清理临时读回的 Texture2D（不属于 asset）
            if (previewSource != null)
                Object.DestroyImmediate(previewSource);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"[SSS Params] ResolveProfileParams 资产已导出: {assetPath}");
            return true;
        }

        private static Texture2D DuplicateTextureForAsset(Texture2D source, string name)
        {
            if (source == null)
                return null;

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true)
            {
                name = name,
                wrapMode = source.wrapMode,
                filterMode = source.filterMode
            };
            copy.SetPixels(source.GetPixels());
            copy.Apply(false, false);
            return copy;
        }

        public static void RegenerateKernelPreview(
            Texture2D discKernel,
            ref Texture2D previewTex,
            int size,
            float maxRadius,
            float dotScale = 6f)
        {
            if (discKernel == null)
            {
                Debug.LogWarning("[SSS Disc Preview] discKernel 为空，先调用 GenerateDiscKernel。");
                return;
            }

            // ---- 重建预览纹理 ----
            if (previewTex == null || previewTex.width != size)
            {
                if (previewTex != null) Object.DestroyImmediate(previewTex);
                previewTex = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "DiscKernel_Preview",
                };
            }

            // ---- 背景：深灰 + 圆盘边界 ----
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float bgFill = 0.10f;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(bgFill, bgFill, bgFill, 1f);

            // 画圆盘边界（细环）
            DrawCircleOutline(pixels, size, center, center, center - 2f, new Color(0.3f, 0.3f, 0.3f, 1f));

            // ---- 读取 kernel 数据 ----
            int n = discKernel.width;
            Color[] kernel = discKernel.GetPixels(); // 每个 texel: rgb=weight, a=radius(mm)

            // 找权重最大值用于归一化点亮度
            float maxW = 1e-6f;
            for (int s = 0; s < n; s++)
                maxW = Mathf.Max(maxW, kernel[s].r, kernel[s].g, kernel[s].b);

            // mm → 像素缩放
            float mmToPixel = (center - 4f) / Mathf.Max(maxRadius, 1e-4f);

            // ---- 逐采样点：Vogel 黄金角重建位置 + 画点 ----
            for (int s = 0; s < n; s++)
            {
                float rMM = kernel[s].a;
                float theta = s * GoldenAngle; // 与 shader 一致
                float rPixel = rMM * mmToPixel;

                float px = center + Mathf.Cos(theta) * rPixel;
                float py = center + Mathf.Sin(theta) * rPixel;

                // 点颜色 = 该样本权重（归一化提亮，方便看）
                Color w = kernel[s];
                Color dotColor = new Color(w.r / maxW, w.g / maxW, w.b / maxW, 1f);

                // 点半径：权重越大点越大（亮度均值映射）
                float wLum = (w.r + w.g + w.b) / 3f / maxW;
                float radius = dotScale * (0.5f + wLum);

                DrawFilledDot(pixels, size, px, py, radius, dotColor);
            }

            previewTex.SetPixels(pixels);
            previewTex.Apply(false, false);
        }

        // ---------- 画实心圆点（带简单抗锯齿）----------
        private static void DrawFilledDot(Color[] px, int size, float cx, float cy, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - 1));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(cx + radius + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - 1));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(cy + radius + 1));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // 1px 边缘软过渡
                    float a = Mathf.Clamp01(radius - dist);
                    if (a <= 0f) continue;

                    int idx = y * size + x;
                    Color bg = px[idx];
                    px[idx] = new Color(
                        Mathf.Lerp(bg.r, color.r, a),
                        Mathf.Lerp(bg.g, color.g, a),
                        Mathf.Lerp(bg.b, color.b, a),
                        1f);
                }
            }
        }

        // ---------- 画圆环边界 ----------
        private static void DrawCircleOutline(Color[] px, int size, float cx, float cy, float radius, Color color)
        {
            int steps = Mathf.CeilToInt(2f * Mathf.PI * radius);
            for (int i = 0; i < steps; i++)
            {
                float t = (i / (float)steps) * 2f * Mathf.PI;
                int x = Mathf.RoundToInt(cx + Mathf.Cos(t) * radius);
                int y = Mathf.RoundToInt(cy + Mathf.Sin(t) * radius);
                if (x >= 0 && x < size && y >= 0 && y < size)
                    px[y * size + x] = color;
            }
        }
        // ============================================================
        // Shader 端用法示例（注释，复制到你的 .shader / .hlsl）
        // ============================================================
        /*
        // _DiscKernel        : 上面生成的 N×1 采样表 (Point filter)
        // _DiscKernelCount   : 采样点数 N
        // _SSSWorldScale     : 1 unit = ? mm 的倒数换算（把 mm 半径转世界/屏幕）
        // _MaxRadius         : 生成表时的 maxRadius (mm)

        static const float GOLDEN_ANGLE = 2.39996323;

        float3 ScreenSpaceSSS(float2 uv, float3 normalVS, float depth)
        {
            float3 sum = 0;
            for (int i = 0; i < _DiscKernelCount; i++)
            {
                // 读权重 + 半径
                float4 k = _DiscKernel.Load(int3(i, 0, 0));  // rgb=weight, a=radius(mm)
                float3 weight = k.rgb;
                float  rMM    = k.a;

                // Vogel disk 重建采样方向（确定性，不必存表）
                float theta = i * GOLDEN_ANGLE;
                float2 dir  = float2(cos(theta), sin(theta));

                // mm → 屏幕 UV 偏移（按深度和 worldScale 缩放）
                float2 offset = dir * rMM * _SSSWorldScale / depth;

                float3 sampleIrradiance = SAMPLE_TEXTURE2D(_IrradianceBuffer, sampler_, uv + offset).rgb;
                sum += sampleIrradiance * weight;
            }
            return sum;  // 权重已归一化，Σ=1，无需再除
        }
        */
    }
}