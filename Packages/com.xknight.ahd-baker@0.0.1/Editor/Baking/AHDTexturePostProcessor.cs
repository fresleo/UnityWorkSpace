/*******************************************************************************
 * File: AHDTexturePostProcessor.cs
 * Author: WangYu
 * Date: 2026-05-13
 * Description: AHD 贴图后处理。
 * Notice: 仅用于 Unity Editor 烘焙流程。
 *******************************************************************************/

using Unity.Mathematics;
using UnityEngine;

namespace XKnight.AHDBaker.Editor
{
    internal static class AHDTexturePostProcessor
    {
        private const float 
            C_DENOISE_PROGRESS_END = 0.3f // 后处理总进度中 降噪阶段 结束位置。
            , C_FEATHER_PROGRESS_START = 0.3f // 后处理总进度中 过渡带羽化阶段 开始位置。
            , C_FEATHER_PROGRESS_END = 0.75f; // 后处理总进度中 过渡带羽化阶段 结束位置，同时也是 扩边阶段 开始位置。
        
        private const int 
            C_PROGRESS_ROW_INTERVAL_MASK = 15 // y 行刷新进度的位掩码，15 表示每 16 行刷新一次进度。
            , C_PROGRESS_DENOMINATOR_MIN = 1; // 进度比例计算使用的最小分母，避免高度或轮数为 0 时除零。
        
        // 默认世界空间上方向
        private static readonly float3 s_DefaultUpDirection = new(0, 1, 0);

        // 判断方向累积向量是否可归一化的最小长度平方。
        private const float C_DIRECTION_LENGTH_SQ_EPSILON = 0.000001f;

        // 判断 AHD 强度是否属于有效 texel 的最小阈值。
        private const float C_VALID_STRENGTH_EPSILON = 0.0001f;

        // 判断方向差权重是否需要启用的最小阈值。
        private const float C_DIRECTION_WEIGHT_EPSILON = 0.0001f;

        // 检测半径必须比小半径模糊窗口至少大 1，才能形成大小窗口差异。
        private const int C_TRANSITION_DETECT_RADIUS_MIN_OFFSET = 1;

        // 羽化半径最小值，0 半径不会产生有效模糊。
        private const int C_MIN_TRANSITION_FEATHER_RADIUS = 1;

        // 羽化迭代次数最小值，保证启用羽化时至少执行一轮。
        private const int C_MIN_TRANSITION_FEATHER_ITERATIONS = 1;

        // 过渡带阈值上限相对阈值的倍数，用于 SmoothStep 的过渡区间。
        private const float C_TRANSITION_THRESHOLD_END_MULTIPLIER = 2;

        // 过渡带阈值上限的最小增量，避免 threshold 为 0 或过小时区间退化。
        private const float C_TRANSITION_THRESHOLD_END_MIN_OFFSET = 0.001f;

        // 将 dot 差值从 [-1, 1] 映射到 [0, 1] 的缩放系数。
        private const float C_DIRECTION_DIFF_TO_UNIT_SCALE = 0.5f;

        // 认为两个方向足够接近、可退化为 lerp 的 dot 阈值。
        private const float C_SLERP_LINEAR_DOT_THRESHOLD = 0.9995f;
        
        private const float 
            C_DEBUG_ENABLED_VALUE = 1  // 过渡带权重或调试 mask 中表示 启用 的值。
            , C_DEBUG_DISABLED_VALUE = 0; // 过渡带权重或调试 mask 中表示 关闭 的值。

        // 调试灰度图的不透明 alpha。
        private const float C_DEBUG_ALPHA = 1;

        // dilate 每一轮只从 1 像素邻域向外扩展，避免跨 chart 过度扩散。
        private const int C_DILATE_NEIGHBOR_RADIUS = 1;

        // chart 归属选择时给强度的偏置，保证全黑但 valid 的邻居也能参与归属。
        private const float C_DOMINANT_CHART_STRENGTH_BIAS = 0.001f;

        // dominantStrength 的未初始化哨兵值，低于任何合法偏置后强度。
        private const float C_DOMINANT_STRENGTH_INVALID = -1;

        // 小半径模糊窗口，用于过渡带检测时的局部基准。
        private const int C_TRANSITION_SMALL_BLUR_RADIUS = 3;

        // occlusionRatio 差异进入过渡带评分时的权重，避免遮挡项压过强度项。
        private const float C_TRANSITION_OCCLUSION_WEIGHT = 0.75f;

        // 过渡带检测半径的最大保护值，防止 Editor 参数导致过大窗口。
        private const int C_MAX_TRANSITION_DETECT_RADIUS = 64;

        // 过渡带羽化半径的最大保护值，防止后处理成本过高。
        private const int C_MAX_TRANSITION_FEATHER_RADIUS = 64;

        // 过渡带羽化迭代次数的最大保护值。
        private const int C_MAX_TRANSITION_FEATHER_ITERATIONS = 4;

        
        public static bool Apply(
            AHDLightmapWorkset workset, AHDBakeSettings settings, AHDBakeContext context,
            string progressTitle, float progressStart, float progressEnd)
        {
            if (settings.denoiseRadius > 0)
            {
                float denoiseEnd = Mathf.Lerp(progressStart, progressEnd, C_DENOISE_PROGRESS_END);
                if (!Denoise(
                        workset,
                        settings.denoiseRadius,
                        context,
                        progressTitle,
                        progressStart,
                        denoiseEnd))
                {
                    return false;
                }
            }

            if (settings.useTransitionFeather)
            {
                float featherStart = Mathf.Lerp(progressStart, progressEnd, C_FEATHER_PROGRESS_START);
                float featherEnd = Mathf.Lerp(progressStart, progressEnd, C_FEATHER_PROGRESS_END);
                if (!ApplyTransitionFeather(
                        workset,
                        settings,
                        context,
                        progressTitle,
                        featherStart,
                        featherEnd))
                {
                    return false;
                }
            }

            if (settings.chartDilateRadius > 0)
            {
                float dilateStart = Mathf.Lerp(progressStart, progressEnd, C_FEATHER_PROGRESS_END);
                if (!Dilate(
                        workset,
                        settings.chartDilateRadius,
                        context,
                        progressTitle,
                        dilateStart,
                        progressEnd))
                {
                    return false;
                }
            }

            return ReportProgress(context, progressTitle, progressEnd);
        }

        private static bool Denoise(
            AHDLightmapWorkset workset,
            int radius,
            AHDBakeContext context,
            string progressTitle, float progressStart, float progressEnd)
        {
            AHDTexelResult[] source = (AHDTexelResult[])workset.results.Clone();
            for (int y = 0; y < workset.height; y++)
            {
                if ((y & C_PROGRESS_ROW_INTERVAL_MASK) == 0)
                {
                    float t = (float)y / Mathf.Max(workset.height, C_PROGRESS_DENOMINATOR_MIN);
                    float progress = Mathf.Lerp(progressStart, progressEnd, t);
                    if (!ReportProgress(context, progressTitle, progress))
                    {
                        return false;
                    }
                }

                for (int x = 0; x < workset.width; x++)
                {
                    int index = y * workset.width + x;
                    if (!workset.validMask[index])
                    {
                        continue;
                    }

                    NeighborAccumulation acc = AccumulateChartNeighbors(
                        workset,
                        source,
                        workset.validMask,
                        x,
                        y,
                        radius,
                        workset.texels[index].chartId);

                    if (acc.count <= 0)
                    {
                        continue;
                    }

                    AHDTexelResult result = workset.results[index];
                    result.directionWS = math.lengthsq(acc.directionAccum) > C_DIRECTION_LENGTH_SQ_EPSILON
                        ? math.normalize(acc.directionAccum)
                        : result.directionWS;
                    result.strength = Mathf.Clamp01(acc.strengthAccum / acc.count);
                    result.visibleWeight = Mathf.Clamp01(acc.visibleAccum / acc.count);
                    result.confidence = Mathf.Clamp01(acc.confidenceAccum / acc.count);
                    workset.results[index] = result;
                }
            }

            return true;
        }

        private static bool ApplyTransitionFeather(
            AHDLightmapWorkset workset, AHDBakeSettings settings, AHDBakeContext context,
            string progressTitle, float progressStart, float progressEnd)
        {
            if (workset == null || workset.results == null || workset.validMask == null)
            {
                return true;
            }

            int pixelCount = workset.width * workset.height;
            if (pixelCount <= 0)
            {
                return true;
            }

            bool[] validMask = BuildTransitionValidMask(workset, pixelCount);
            int[] chartIds = BuildTransitionChartIds(workset, pixelCount);
            float[] strengths = new float[pixelCount];
            float[] occlusions = new float[pixelCount];
            float3[] directions = new float3[pixelCount];
            for (int i = 0; i < pixelCount; i++)
            {
                AHDTexelResult result = workset.results[i];
                strengths[i] = Mathf.Clamp01(result.strength);
                occlusions[i] = Mathf.Clamp01(result.occlusionRatio);
                directions[i] = math.normalizesafe(result.directionWS, s_DefaultUpDirection);
            }

            float threshold = Mathf.Clamp01(settings.transitionFeatherThreshold);
            int detectRadius = Mathf.Clamp(settings.transitionDetectRadius, C_TRANSITION_SMALL_BLUR_RADIUS + C_TRANSITION_DETECT_RADIUS_MIN_OFFSET, C_MAX_TRANSITION_DETECT_RADIUS);
            float directionWeight = Mathf.Max(settings.transitionDirectionWeight, 0f);
            float[] featherWeights = BuildTransitionWeights(
                workset.width,
                workset.height,
                validMask,
                chartIds,
                strengths,
                occlusions,
                directions,
                directionWeight,
                threshold,
                detectRadius,
                out float[] transitionScores,
                out float[] directionDiffScores,
                out int transitionPixelCount);

            FillDebugMap(workset.transitionScoreDebug, transitionScores);
            FillDebugMap(workset.directionDiffDebug, directionDiffScores);
            FillDebugMap(workset.featherWeightDebug, featherWeights);

            if (transitionPixelCount <= 0)
            {
                return ReportProgress(context, progressTitle, progressEnd);
            }

            int featherRadius = Mathf.Clamp(settings.transitionFeatherRadius, C_MIN_TRANSITION_FEATHER_RADIUS, C_MAX_TRANSITION_FEATHER_RADIUS);
            float featherStrength = Mathf.Clamp01(settings.transitionFeatherStrength);
            int iterations = Mathf.Clamp(settings.transitionFeatherIterations, C_MIN_TRANSITION_FEATHER_ITERATIONS, C_MAX_TRANSITION_FEATHER_ITERATIONS);
            float[] currentStrengths = (float[])strengths.Clone();
            float3[] currentDirections = (float3[])directions.Clone();

            for (int pass = 0; pass < iterations; pass++)
            {
                float progress = Mathf.Lerp(progressStart, progressEnd, (float)pass / Mathf.Max(iterations, C_PROGRESS_DENOMINATOR_MIN));
                if (!ReportProgress(context, progressTitle, progress))
                {
                    return false;
                }

                float[] blurredStrengths = BlurScalar(
                    currentStrengths,
                    validMask,
                    chartIds,
                    workset.width,
                    workset.height,
                    featherRadius);
                float3[] blurredDirections = BlurVector(
                    currentDirections,
                    validMask,
                    chartIds,
                    workset.width,
                    workset.height,
                    featherRadius);

                for (int i = 0; i < pixelCount; i++)
                {
                    if (!validMask[i] || featherWeights[i] <= 0)
                    {
                        continue;
                    }

                    float blend = Mathf.Clamp01(featherWeights[i] * featherStrength);
                    float targetStrength = Mathf.Lerp(
                        currentStrengths[i],
                        blurredStrengths[i],
                        blend);
                    if (settings.transitionFeatherMode == EAHDTransitionFeatherMode.ReduceHighOnly)
                    {
                        targetStrength = Mathf.Min(currentStrengths[i], targetStrength);
                    }

                    currentStrengths[i] = Mathf.Clamp01(targetStrength);
                    currentDirections[i] = SlerpDirection(
                        currentDirections[i],
                        blurredDirections[i],
                        blend);
                }
            }

            for (int i = 0; i < pixelCount; i++)
            {
                if (!validMask[i])
                {
                    continue;
                }

                AHDTexelResult result = workset.results[i];
                float delta = Mathf.Abs(result.strength - currentStrengths[i]);
                result.strength = currentStrengths[i];
                result.directionWS = currentDirections[i];
                workset.results[i] = result;

                if (workset.featherMaskDebug != null)
                {
                    workset.featherMaskDebug[i] = EncodeDebugValue(featherWeights[i] > 0 
                        ? C_DEBUG_ENABLED_VALUE
                        : C_DEBUG_DISABLED_VALUE);
                }

                if (workset.featherDeltaDebug != null)
                {
                    workset.featherDeltaDebug[i] = EncodeDebugValue(delta);
                }
            }

            return ReportProgress(context, progressTitle, progressEnd);
        }

        private static bool[] BuildTransitionValidMask(
            AHDLightmapWorkset workset,
            int pixelCount)
        {
            bool[] validMask = new bool[pixelCount];
            for (int i = 0; i < pixelCount; i++)
            {
                validMask[i] = workset.validMask[i] && workset.results[i].strength > C_VALID_STRENGTH_EPSILON;
            }

            return validMask;
        }

        private static int[] BuildTransitionChartIds(
            AHDLightmapWorkset workset,
            int pixelCount)
        {
            int[] chartIds = new int[pixelCount];
            for (int i = 0; i < pixelCount; i++)
            {
                chartIds[i] = workset.texels[i].chartId;
            }

            return chartIds;
        }

        private static float[] BuildTransitionWeights(
            int width, int height,
            bool[] validMask, int[] chartIds, float[] strengths, float[] occlusions,
            float3[] directions, float directionWeight,
            float threshold,
            int detectRadius,
            out float[] transitionScores, out float[] directionDiffScores, out int transitionPixelCount)
        {
            int pixelCount = width * height;
            transitionScores = new float[pixelCount];
            directionDiffScores = new float[pixelCount];
            float[] transitionWeights = new float[pixelCount];
            transitionPixelCount = 0;

            float[] smallStrengths = BlurScalar(
                strengths, validMask, chartIds,
                width, height, C_TRANSITION_SMALL_BLUR_RADIUS);
            float[] largeStrengths = BlurScalar(
                strengths, validMask, chartIds,
                width, height, detectRadius);
            float[] smallOcclusions = BlurScalar(
                occlusions, validMask, chartIds,
                width, height, C_TRANSITION_SMALL_BLUR_RADIUS);
            float[] largeOcclusions = BlurScalar(
                occlusions, validMask, chartIds,
                width, height, detectRadius);

            float3[] smallDirections = null;
            float3[] largeDirections = null;
            bool useDirectionDiff = directionWeight > C_DIRECTION_WEIGHT_EPSILON && directions != null;
            if (useDirectionDiff)
            {
                smallDirections = BlurVector(
                    directions, validMask, chartIds,
                    width, height, C_TRANSITION_SMALL_BLUR_RADIUS);
                largeDirections = BlurVector(
                    directions, validMask, chartIds,
                    width, height, detectRadius);
            }

            float thresholdEnd = Mathf.Max(threshold * C_TRANSITION_THRESHOLD_END_MULTIPLIER, threshold + C_TRANSITION_THRESHOLD_END_MIN_OFFSET);
            for (int i = 0; i < pixelCount; i++)
            {
                if (!validMask[i])
                {
                    continue;
                }

                float strengthDiff = Mathf.Abs(smallStrengths[i] - largeStrengths[i]);
                float occlusionDiff = Mathf.Abs(smallOcclusions[i] - largeOcclusions[i]) * C_TRANSITION_OCCLUSION_WEIGHT;
                float bandScore = Mathf.Max(strengthDiff, occlusionDiff);

                if (useDirectionDiff)
                {
                    // BlurVector 返回未归一化的平均向量，需要在使用方归一化才能做角度差。
                    float3 smallUnit = math.normalizesafe(smallDirections[i], s_DefaultUpDirection);
                    float3 largeUnit = math.normalizesafe(largeDirections[i], smallUnit);
                    float dot = math.dot(smallUnit, largeUnit);
                    float directionDiff = Mathf.Clamp01((1 - dot) * C_DIRECTION_DIFF_TO_UNIT_SCALE);
                    directionDiffScores[i] = directionDiff;
                    bandScore = Mathf.Max(bandScore, directionDiff * directionWeight);
                }

                transitionScores[i] = bandScore;
                if (bandScore < threshold)
                {
                    continue;
                }

                transitionPixelCount++;
                float bandT = Mathf.InverseLerp(threshold, thresholdEnd, bandScore);
                transitionWeights[i] = Mathf.SmoothStep(0, 1, bandT);
            }

            return transitionWeights;
        }

        private static float[] BlurScalar(
            float[] values, bool[] validMask, int[] chartIds,
            int width, int height, int radius)
        {
            int pixelCount = width * height;
            float[] horizontal = new float[pixelCount];
            float[] result = new float[pixelCount];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!validMask[index])
                    {
                        continue;
                    }

                    float sum = 0;
                    int count = 0;
                    int chartId = chartIds != null ? chartIds[index] : 0;
                    for (int xx = x - radius; xx <= x + radius; xx++)
                    {
                        if (xx < 0 || xx >= width)
                        {
                            continue;
                        }

                        int sampleIndex = y * width + xx;
                        if (!IsSameTransitionChart(validMask, chartIds, sampleIndex, chartId))
                        {
                            continue;
                        }

                        sum += values[sampleIndex];
                        count++;
                    }

                    horizontal[index] = count > 0 ? sum / count : values[index];
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!validMask[index])
                    {
                        continue;
                    }

                    float sum = 0;
                    int count = 0;
                    int chartId = chartIds != null ? chartIds[index] : 0;
                    for (int yy = y - radius; yy <= y + radius; yy++)
                    {
                        if (yy < 0 || yy >= height)
                        {
                            continue;
                        }

                        int sampleIndex = yy * width + x;
                        if (!IsSameTransitionChart(validMask, chartIds, sampleIndex, chartId))
                        {
                            continue;
                        }

                        sum += horizontal[sampleIndex];
                        count++;
                    }

                    result[index] = count > 0 ? sum / count : values[index];
                }
            }

            return result;
        }

        private static float3[] BlurVector(
            float3[] values, bool[] validMask, int[] chartIds,
            int width, int height, int radius)
        {
            int pixelCount = width * height;
            float3[] horizontal = new float3[pixelCount];
            float3[] result = new float3[pixelCount];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!validMask[index])
                    {
                        continue;
                    }

                    float3 sum = new float3(0);
                    int count = 0;
                    int chartId = chartIds != null ? chartIds[index] : 0;
                    for (int xx = x - radius; xx <= x + radius; xx++)
                    {
                        if (xx < 0 || xx >= width)
                        {
                            continue;
                        }

                        int sampleIndex = y * width + xx;
                        if (!IsSameTransitionChart(validMask, chartIds, sampleIndex, chartId))
                        {
                            continue;
                        }

                        sum += values[sampleIndex];
                        count++;
                    }

                    // 分离卷积：水平 pass 不能归一化，否则 vertical pass 拿到的不是 2D box 均值。
                    // 真正的单位方向由 caller 在最后用 normalize 处理。
                    horizontal[index] = count > 0
                        ? sum / count
                        : values[index];
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!validMask[index])
                    {
                        continue;
                    }

                    float3 sum = new float3(0);
                    int count = 0;
                    int chartId = chartIds != null ? chartIds[index] : 0;
                    for (int yy = y - radius; yy <= y + radius; yy++)
                    {
                        if (yy < 0 || yy >= height)
                        {
                            continue;
                        }

                        int sampleIndex = yy * width + x;
                        if (!IsSameTransitionChart(validMask, chartIds, sampleIndex, chartId))
                        {
                            continue;
                        }

                        sum += horizontal[sampleIndex];
                        count++;
                    }

                    // 仅在最终 pass 归一化，得到单位方向；length 信息保留在 SlerpDirection 的 caller 中
                    // 通过 normalizesafe 的 fallback 体现。
                    result[index] = count > 0
                        ? math.normalizesafe(sum / count, values[index])
                        : values[index];
                }
            }

            return result;
        }

        private static bool IsSameTransitionChart(
            bool[] validMask, int[] chartIds, int sampleIndex, int chartId)
        {
            if (!validMask[sampleIndex])
            {
                return false;
            }

            return chartIds == null || chartIds[sampleIndex] == chartId;
        }

        private static float3 SlerpDirection(float3 from, float3 to, float t)
        {
            float3 fromDir = math.normalizesafe(from, s_DefaultUpDirection);
            float3 toDir = math.normalizesafe(to, fromDir);
            float dot = Mathf.Clamp(math.dot(fromDir, toDir), -1, 1);
            if (dot > C_SLERP_LINEAR_DOT_THRESHOLD)
            {
                return math.normalizesafe(math.lerp(fromDir, toDir, t), fromDir);
            }

            float theta = Mathf.Acos(dot) * Mathf.Clamp01(t);
            float3 relative = math.normalizesafe(toDir - fromDir * dot, fromDir);
            return math.normalizesafe(
                fromDir * Mathf.Cos(theta) + relative * Mathf.Sin(theta),
                fromDir);
        }

        private static void FillDebugMap(Color[] debugPixels, float[] values)
        {
            if (debugPixels == null || values == null)
            {
                return;
            }

            int count = Mathf.Min(debugPixels.Length, values.Length);
            for (int i = 0; i < count; i++)
            {
                debugPixels[i] = EncodeDebugValue(values[i]);
            }
        }

        private static Color EncodeDebugValue(float value)
        {
            value = Mathf.Clamp01(value);
            return new Color(value, value, value, C_DEBUG_ALPHA);
        }

        private static bool Dilate(
            AHDLightmapWorkset workset,
            int radius,
            AHDBakeContext context,
            string progressTitle, float progressStart, float progressEnd)
        {
            bool[] valid = (bool[])workset.validMask.Clone();
            for (int pass = 0; pass < radius; pass++)
            {
                AHDTexelResult[] source = (AHDTexelResult[])workset.results.Clone();
                bool[] nextValid = (bool[])valid.Clone();
                int changed = 0;
                for (int y = 0; y < workset.height; y++)
                {
                    if ((y & C_PROGRESS_ROW_INTERVAL_MASK) == 0)
                    {
                        float passProgress = (pass + (float)y / Mathf.Max(workset.height, C_PROGRESS_DENOMINATOR_MIN)) / Mathf.Max(radius, C_PROGRESS_DENOMINATOR_MIN);
                        float progress = Mathf.Lerp(progressStart, progressEnd, passProgress);
                        if (!ReportProgress(context, progressTitle, progress))
                        {
                            return false;
                        }
                    }

                    for (int x = 0; x < workset.width; x++)
                    {
                        int index = y * workset.width + x;
                        if (valid[index])
                        {
                            continue;
                        }

                        // chart-外的 dilate 目标 texel 自身没有 chartId（chartId 仅在三角形
                        // 覆盖的 texel 上有值）。先扫一遍邻居，取 strength 最大者的 chartId 作
                        // 为本 texel 的"归属 chart"，再只接受该 chart 的邻居参与累加，避免相邻
                        // chart 的高强度数据被复制到当前 chart 边缘外，造成 GPU bilinear 后的
                        // 明显亮块。归属 chartId 同时写回 workset.texels[index]，下个 dilate
                        // pass 自然按同样规则继续隔离扩张。
                        if (!TryFindDominantChartIdAmongNeighbors(
                                workset,
                                source, valid,
                                x, y, C_DILATE_NEIGHBOR_RADIUS,
                                out int dominantChartId))
                        {
                            continue;
                        }

                        NeighborAccumulation acc = AccumulateChartNeighbors(
                            workset,
                            source, valid,
                            x, y, C_DILATE_NEIGHBOR_RADIUS,
                            dominantChartId);

                        if (acc.count <= 0)
                        {
                            continue;
                        }

                        AHDTexelData texelData = workset.texels[index];
                        texelData.chartId = dominantChartId;
                        workset.texels[index] = texelData;

                        workset.results[index] = new AHDTexelResult
                        {
                            directionWS = math.lengthsq(acc.directionAccum) > C_DIRECTION_LENGTH_SQ_EPSILON
                                ? math.normalize(acc.directionAccum)
                                : s_DefaultUpDirection,
                            strength = Mathf.Clamp01(acc.strengthAccum / acc.count),
                            visibleWeight = Mathf.Clamp01(acc.visibleAccum / acc.count),
                            confidence = Mathf.Clamp01(acc.confidenceAccum / acc.count),
                            dominantLightIndex = source[index].dominantLightIndex
                        };
                        nextValid[index] = true;
                        changed++;
                    }
                }

                valid = nextValid;
                if (changed == 0)
                {
                    break;
                }
            }

            return true;
        }

        private static bool ReportProgress(AHDBakeContext context, string title, float progress)
        {
            if (context == null || context.progress == null)
            {
                return true;
            }

            return context.progress(title, Mathf.Clamp01(progress));
        }

        /// <summary>
        /// Denoise / Dilate 的邻居加权累加结果。directionAccum 按 strength 加权，
        /// 上层根据语义自行 normalize 或保留 fallback。
        /// </summary>
        private struct NeighborAccumulation
        {
            public float3 directionAccum;
            public float strengthAccum;
            public float visibleAccum;
            public float confidenceAccum;
            public int count;
        }

        /// <summary>
        /// 在 (x, y) 周围 [±radius] 范围内累加同一 chartId 的邻居 texel 数据。
        /// validMask 由调用方传入：Denoise 用原始 workset.validMask，
        /// Dilate 用 working copy（已包含本 pass 之前的扩张结果）。
        /// chartIdFilter 必须与邻居 chartId 严格相等才计入，避免跨 chart 串扰。
        /// </summary>
        private static NeighborAccumulation AccumulateChartNeighbors(
            AHDLightmapWorkset workset,
            AHDTexelResult[] source, bool[] validMask,
            int x, int y, int radius,
            int chartIdFilter)
        {
            NeighborAccumulation acc = default;
            for (int yy = y - radius; yy <= y + radius; yy++)
            {
                if (yy < 0 || yy >= workset.height)
                {
                    continue;
                }

                for (int xx = x - radius; xx <= x + radius; xx++)
                {
                    if (xx < 0 || xx >= workset.width)
                    {
                        continue;
                    }

                    int sampleIndex = yy * workset.width + xx;
                    if (!validMask[sampleIndex] || workset.texels[sampleIndex].chartId != chartIdFilter)
                    {
                        continue;
                    }

                    AHDTexelResult sample = source[sampleIndex];
                    acc.directionAccum += sample.directionWS * sample.strength;
                    acc.strengthAccum += sample.strength;
                    acc.visibleAccum += sample.visibleWeight;
                    acc.confidenceAccum += sample.confidence;
                    acc.count++;
                }
            }

            return acc;
        }

        /// <summary>
        /// 在 (x, y) 周围 [±radius] 范围内扫描 valid 邻居，
        /// 取 strength 最大者的 chartId 作为"本 texel 应归属哪个 chart"的判定。
        /// strength 上加 C_DOMINANT_CHART_STRENGTH_BIAS 微小偏置，保证 strength=0 的 valid 邻居也能被选中，
        /// 避免完全暗区无法决出归属。
        /// </summary>
        /// <param name="dominantChartId">输出主导 chartId（无 valid 邻居时未定义）。</param>
        /// <returns>true 表示找到至少一个 valid 邻居；false 表示该 texel 无任何可参考邻居。</returns>
        private static bool TryFindDominantChartIdAmongNeighbors(
            AHDLightmapWorkset workset,
            AHDTexelResult[] source, bool[] validMask,
            int x, int y, int radius,
            out int dominantChartId)
        {
            dominantChartId = 0;
            float dominantStrength = C_DOMINANT_STRENGTH_INVALID;
            for (int yy = y - radius; yy <= y + radius; yy++)
            {
                if (yy < 0 || yy >= workset.height)
                {
                    continue;
                }

                for (int xx = x - radius; xx <= x + radius; xx++)
                {
                    if (xx < 0 || xx >= workset.width)
                    {
                        continue;
                    }

                    int sampleIndex = yy * workset.width + xx;
                    if (!validMask[sampleIndex])
                    {
                        continue;
                    }

                    float neighborStrength = source[sampleIndex].strength + C_DOMINANT_CHART_STRENGTH_BIAS;
                    if (neighborStrength > dominantStrength)
                    {
                        dominantStrength = neighborStrength;
                        dominantChartId = workset.texels[sampleIndex].chartId;
                    }
                }
            }

            return dominantStrength >= 0;
        }
        
    }
}
