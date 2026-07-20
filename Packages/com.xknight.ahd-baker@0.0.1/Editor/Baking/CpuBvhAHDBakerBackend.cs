/*******************************************************************************
 * File: CpuBvhAHDBakerBackend.cs
 * Author: WangYu
 * Date: 2026-05-13
 * Description: C# CPU/BVH AHD 烘焙后端。
 * Notice: 仅用于 Unity Editor 烘焙流程。
 *******************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XKnight.AHDBaker.Editor
{
    /// <summary>
    /// C# CPU/BVH AHD 烘焙后端。
    /// </summary>
    public sealed class CpuBvhAHDBakerBackend : IAHDBakerBackend
    {
        // 亮度参考直方图桶数量，用于近似 percentile。
        private const int C_LUMINANCE_HISTOGRAM_BINS = 256;

        // lightmap 亮度参考值取正亮度分布的该百分位，避免少量极亮点主导归一化。
        private const float C_LUMINANCE_REFERENCE_PERCENTILE = 0.95f;

        // 亮度、权重和影响力的统一有效阈值。
        private const float C_LUMINANCE_EPSILON = 0.0001f;

        // 单次 Job 调度处理的 texel 数量。
        private const int C_JOB_TEXEL_CHUNK_SIZE = 512;

        // Bake 开始时收集场景数据的进度位置。
        private const float C_PROGRESS_COLLECT_SCENE = 0.02f;

        // 构建 BVH 的进度位置。
        private const float C_PROGRESS_BUILD_BVH = 0.08f;

        // 每张 lightmap 烘焙段在总进度中的起点。
        private const float C_LIGHTMAP_PROGRESS_START = 0.1f;

        // 所有 lightmap 烘焙段在总进度中的跨度。
        private const float C_LIGHTMAP_PROGRESS_SPAN = 0.85f;

        // 同一 lightmap 内栅格化结束到正式烘焙开始的进度偏移。
        private const float C_RASTERIZE_PROGRESS_OFFSET = 0.02f;

        // 同一 lightmap 内 CPU bake 阶段结束比例。
        private const float C_BAKE_PROGRESS_END_RATIO = 0.75f;

        // 同一 lightmap 内后处理阶段结束比例。
        private const float C_POST_PROGRESS_END_RATIO = 0.88f;

        // 写入 Binder 或整理结果前的最终进度位置。
        private const float C_FINAL_PROGRESS = 0.98f;

        // Unity lightmapIndex 从 0 开始，小于该值表示无效。
        private const int C_MIN_VALID_LIGHTMAP_INDEX = 0;

        // 像素坐标的最小合法索引。
        private const int C_MIN_PIXEL_INDEX = 0;

        // 初始化 direction map 时写入的中性方向通道值。
        private const float C_ENCODED_DIRECTION_NEUTRAL = 0.5f;

        // 完全透明 alpha，用于无效 texel。
        private const float C_ALPHA_TRANSPARENT = 0;

        // 完全不透明 alpha，用于 debug 图。
        private const float C_ALPHA_OPAQUE = 1;

        // 完整权重值，用于 mask、coherence 和可见性等非颜色语义。
        private const float C_FULL_WEIGHT = 1;

        // 坐标轴向量中的 0 分量。
        private const float C_AXIS_ZERO = 0;

        // 坐标轴向量中的正向单位分量。
        private const float C_AXIS_POSITIVE = 1;

        // 坐标轴向量中的反向单位分量。
        private const float C_AXIS_NEGATIVE = -1;

        // 向量长度平方低于该值时视为退化。
        private const float C_VECTOR_LENGTH_SQ_EPSILON = 0.000001f;

        // UV 三角形面积低于该值时视为退化。
        private const float C_UV_TRIANGLE_AREA_EPSILON = 0.0000001f;

        // 栅格化重心权重允许的负向容差，避免边界 texel 被浮点误差丢弃。
        private const float C_BARYCENTRIC_EDGE_EPSILON = 0.0001f;

        // 单 texel 被至少两个三角形覆盖时才启用 cross-coherence 抑制。
        private const float C_CROSS_COHERENCE_MIN_WEIGHT = 2;

        // valid 标志写入 AHDTexelData 的有效值。
        private const int C_VALID_TEXEL_FLAG = 1;

        // 亮度遮罩 softness 的最小保护值。
        private const float C_MIN_LUMINANCE_MASK_SOFTNESS = 0.001f;

        // RenderTexture depth buffer 位数，读回 lightmap 颜色不需要深度。
        private const int C_READBACK_DEPTH_BITS = 0;

        // ReadPixels 从 render target 左下角开始读。
        private const int C_READBACK_START_PIXEL = 0;

        // 输出 direction texture 不使用各向异性过滤。
        private const int C_DIRECTION_TEXTURE_ANISO_LEVEL = 0;

        // 采样像素中心的 UV 偏移。
        private const float C_TEXEL_CENTER_OFFSET = 0.5f;

        // 缺少 lightmap 亮度遮罩时默认不压低强度。
        private const float C_DEFAULT_LUMINANCE_MASK = 1;

        // Rec.709 线性亮度 R 权重。
        private const float C_LUMINANCE_R_WEIGHT = 0.2126f;

        // Rec.709 线性亮度 G 权重。
        private const float C_LUMINANCE_G_WEIGHT = 0.7152f;

        // Rec.709 线性亮度 B 权重。
        private const float C_LUMINANCE_B_WEIGHT = 0.0722f;

        // Job 和采样循环的最小样本数。
        private const int C_MIN_SAMPLE_COUNT = 1;

        // 最后一个索引相对集合长度的偏移。
        private const int C_LAST_INDEX_OFFSET = 1;

        // Job 调度批大小。
        private const int C_JOB_SCHEDULE_BATCH_SIZE = 64;

        // rayBias 的最小保护值，避免射线起点贴面导致自相交。
        private const float C_MIN_RAY_BIAS = 0.0001f;

        // directional light 射线距离的最小保护值。
        private const float C_MIN_DIRECTIONAL_RAY_DISTANCE = 1;

        // 方向编码时从 [-1, 1] 映射到 [0, 1] 的缩放。
        private const float C_DIRECTION_ENCODE_SCALE = 0.5f;

        // 方向编码时从 [-1, 1] 映射到 [0, 1] 的偏移。
        private const float C_DIRECTION_ENCODE_BIAS = 0.5f;

        // dominant light debug 色相步进。
        private const float C_DOMINANT_LIGHT_HUE_STEP = 0.173f;

        // dominant light debug 颜色饱和度。
        private const float C_DOMINANT_LIGHT_DEBUG_SATURATION = 0.8f;

        // dominant light debug 颜色明度。
        private const float C_DOMINANT_LIGHT_DEBUG_VALUE = 1;

        // 没有 dominant light 时使用的索引哨兵。
        private const int C_INVALID_LIGHT_INDEX = -1;

        // AHDLightData 中 directional light 的类型编码。
        private const int C_DIRECTIONAL_LIGHT_TYPE = 0;

        // BVH 根节点索引。
        private const int C_BVH_ROOT_NODE_INDEX = 0;

        // BVH 子节点索引小于该值表示不存在。
        private const int C_MIN_VALID_BVH_NODE_INDEX = 0;

        // AHDLightData 中 spot light 的类型编码。
        private const int C_SPOT_LIGHT_TYPE = 2;

        // AHDLightData 中 area light 的类型编码。
        private const int C_AREA_LIGHT_TYPE = 3;

        // AHDLightData 中 disc area light 的形状编码。
        private const int C_DISC_AREA_SHAPE = 1;

        // 光照距离和命中距离的最小保护值。
        private const float C_DISTANCE_EPSILON = 0.0001f;

        // light range 的最小保护值。
        private const float C_MIN_LIGHT_RANGE = 0.001f;

        // 点光/聚光距离参考点取 light.range 的一半。
        private const float C_REFERENCE_DISTANCE_RANGE_SCALE = 0.5f;

        // spot inner/outer cos 差值的最小保护值。
        private const float C_MIN_SPOT_COS_DENOMINATOR = 0.001f;

        // visible coherence 生成强度 mask 的下限。
        private const float C_COHERENCE_MASK_MIN = 0.15f;

        // visible coherence 生成强度 mask 的上限。
        private const float C_COHERENCE_MASK_MAX = 0.55f;

        // visible direction 长度和方向置信度的有效阈值。
        private const float C_DIRECTION_CONFIDENCE_EPSILON = 0.0001f;

        // 方向置信度低于该值时回退到法线方向。
        private const float C_MIN_VISIBLE_COHERENCE_FOR_DIRECTION = 0.05f;

        // directional light 软遮挡的接收点抖动半径。
        private const float C_DIRECTIONAL_OCCLUSION_DISC_RADIUS = 0.02f;

        // 抖动半径或光源半径低于该值时不做额外 jitter。
        private const float C_JITTER_EPSILON = 0.0001f;

        // area light 尺寸的最小保护值。
        private const float C_MIN_AREA_LIGHT_SIZE = 0.001f;

        // Ray / AABB 相交时避免方向分量为 0 的最小绝对值。
        private const float C_RAY_AABB_DIRECTION_EPSILON = 0.000001f;

        // Ray / Triangle 行列式绝对值低于该值时视为平行或退化。
        private const float C_RAY_TRIANGLE_DET_EPSILON = 0.0000001f;

        // 构建切线基时，法线过于接近世界 Y 轴则改用 X 轴辅助向量。
        private const float C_BASIS_UP_DOT_THRESHOLD = 0.9f;

        // Fibonacci 采样使用的黄金角。
        private const float C_GOLDEN_ANGLE = 2.3999632f;

        // 单位球 z 分量从 1 到 -1，总跨度为 2。
        private const float C_UNIT_SPHERE_Z_SPAN = 2;

        // RadicalInverseBase2 把 uint 映射到 [0, 1) 的缩放系数。
        private const float C_RADICAL_INVERSE_UINT_SCALE = 2.3283064365386963e-10f;

        public const string c_TextureNamePrefix = "AHDBakedDirection", c_TextureNameSuffix = "_comp_dir.png";
        
        private static readonly float3 
            s_WorldUp = new(C_AXIS_ZERO, C_AXIS_POSITIVE, C_AXIS_ZERO), 
            s_WorldRight = new(C_AXIS_POSITIVE, C_AXIS_ZERO, C_AXIS_ZERO),
            s_WorldForward = new(C_AXIS_ZERO, C_AXIS_ZERO, C_AXIS_POSITIVE),
            s_WorldBack = new(C_AXIS_ZERO, C_AXIS_ZERO, C_AXIS_NEGATIVE);

        /// <summary>
        /// 后端显示名称。
        /// </summary>
        public string DisplayName => "CPU BVH（C# 多线程）";
        
        /// <summary>
        /// 执行 CPU BVH AHD 烘焙
        /// </summary>
        /// <param name="context">烘焙上下文</param>
        /// <param name="settings">烘焙设置</param>
        /// <returns>烘焙结果</returns>
        public AHDBakeResult Bake(AHDBakeContext context, AHDBakeSettings settings)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            AHDBakeSettings resolvedSettings = settings.Clone();
            AHDBakeResult result = new AHDBakeResult();

            if (!ReportProgress(context, "收集场景数据", C_PROGRESS_COLLECT_SCENE))
            {
                result.cancelled = true;
                return result;
            }

            AHDSceneData sceneData = AHDSceneCollector.Collect(resolvedSettings);
            
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            if (lightmaps == null || lightmaps.Length == 0)
            {
                result.summary = "未找到可用 lightmap。";
                return result;
            }

            if (!AHDBakeRuntimeBridge.TryGetDefaultOutputFolder(out string outputFolder))
            {
                result.summary = "未找到已保存的场景路径，不能生成 AHD direction maps。";
                return result;
            }

            if (sceneData.lights.Count == 0)
            {
                Debug.LogWarning("[AHD 烘焙器] 未找到支持的 Baked/Mixed 灯光。");
            }

            if (!ReportProgress(context, "构建 BVH", C_PROGRESS_BUILD_BVH))
            {
                result.cancelled = true;
                return result;
            }

            AHDBvhBuildResult bvh = AHDBvh.Build(sceneData.occluders);
            Texture2D[] directionMaps = new Texture2D[lightmaps.Length];
            
            int totalValidTexels = 0, bakedCount = 0;

            NativeArray<AHDOccluderTriangle> nativeTriangles = new NativeArray<AHDOccluderTriangle>(sceneData.occluders.ToArray(), Allocator.TempJob);
            NativeArray<AHDBvhNode> nativeNodes = new NativeArray<AHDBvhNode>(bvh.nodes, Allocator.TempJob);
            NativeArray<int> nativeTriangleIndices = new NativeArray<int>(bvh.triangleIndices, Allocator.TempJob);
            NativeArray<AHDLightData> nativeLights = new NativeArray<AHDLightData>(sceneData.lights.ToArray(), Allocator.TempJob);

            try
            {
                for (int lightmapIndex = 0; lightmapIndex < lightmaps.Length; lightmapIndex++)
                {
                    float baseProgress = C_LIGHTMAP_PROGRESS_START + (C_LIGHTMAP_PROGRESS_SPAN * lightmapIndex / lightmaps.Length);
                    
                    if (!IsValidLightmapIndex(lightmaps, lightmapIndex))
                    {
                        continue;
                    }

                    if (!ReportProgress(context, "光栅化 Lightmap " + lightmapIndex, baseProgress))
                    {
                        result.cancelled = true;
                        return result;
                    }

                    AHDLightmapWorkset workset = BuildLightmapWorkset(
                        sceneData,
                        lightmaps[lightmapIndex],
                        lightmapIndex,
                        resolvedSettings);
                    if (workset.validTexelCount == 0)
                    {
                        Debug.LogWarning("[AHD 烘焙器] Lightmap " + lightmapIndex + " 没有找到有效 AHD 接收 texel。");
                        continue;
                    }

                    if (!ReportProgress(context, "烘焙 Lightmap " + lightmapIndex, baseProgress + C_RASTERIZE_PROGRESS_OFFSET))
                    {
                        result.cancelled = true;
                        return result;
                    }

                    float nextLightmapProgress = C_LIGHTMAP_PROGRESS_START + (C_LIGHTMAP_PROGRESS_SPAN * (lightmapIndex + 1) / lightmaps.Length);
                    float lightmapProgressSpan = nextLightmapProgress - baseProgress;
                    float bakeEndProgress = baseProgress + lightmapProgressSpan * C_BAKE_PROGRESS_END_RATIO;
                    float postEndProgress = baseProgress + lightmapProgressSpan * C_POST_PROGRESS_END_RATIO;
                    if (!BakeWorkset(
                            workset,
                            nativeTriangles, nativeNodes, nativeTriangleIndices, nativeLights,
                            resolvedSettings, context,
                            lightmapIndex,
                            baseProgress + C_RASTERIZE_PROGRESS_OFFSET, bakeEndProgress))
                    {
                        result.cancelled = true;
                        return result;
                    }

                    if (!AHDTexturePostProcessor.Apply(
                            workset,
                            resolvedSettings,
                            context,
                            "后处理 Lightmap " + lightmapIndex,
                            bakeEndProgress, postEndProgress))
                    {
                        result.cancelled = true;
                        return result;
                    }

                    Texture2D generatedMap = CreateDirectionTexture(workset);
                    string assetPath = outputFolder + "/" + c_TextureNamePrefix + "-" + lightmapIndex + c_TextureNameSuffix;
                    Texture2D savedMap = AHDBakeRuntimeBridge.SaveDirectionMap(generatedMap, assetPath);
                    if (savedMap != null)
                    {
                        directionMaps[lightmapIndex] = savedMap;
                        bakedCount++;
                    }

                    if (resolvedSettings.writeDebugMaps)
                    {
                        SaveDebugMaps(workset, outputFolder);
                    }

                    totalValidTexels += workset.validTexelCount;
                }
            }
            finally
            {
                if (nativeTriangles.IsCreated)
                {
                    nativeTriangles.Dispose();
                }

                if (nativeNodes.IsCreated)
                {
                    nativeNodes.Dispose();
                }

                if (nativeTriangleIndices.IsCreated)
                {
                    nativeTriangleIndices.Dispose();
                }

                if (nativeLights.IsCreated)
                {
                    nativeLights.Dispose();
                }
            }

            string finalProgressTitle = resolvedSettings.assignToSceneBinder
                ? "写入 Binder"
                : "整理烘焙结果";
            if (!ReportProgress(context, finalProgressTitle, C_FINAL_PROGRESS))
            {
                result.cancelled = true;
                return result;
            }

            if (resolvedSettings.assignToSceneBinder && bakedCount > 0)
            {
                AHDBakeRuntimeBridge.ApplyDirectionMaps(directionMaps);
            }
            
            stopwatch.Stop();
            
            result.succeeded = bakedCount > 0;
            result.directionMaps = directionMaps;
            
            result.summary =
                "已烘焙 lightmap 数: " + bakedCount
                + ", 接收三角形: " + sceneData.receivers.Count
                + ", 遮挡三角形: " + sceneData.occluders.Count
                + ", 灯光: " + sceneData.lights.Count
                + ", 有效 texel: " + totalValidTexels
                + ", 输出目录: " + outputFolder
                + ", 耗时: " + stopwatch.Elapsed.TotalSeconds.ToString("F2") + "s";
            
            string binderSummary = !resolvedSettings.assignToSceneBinder
                ? ", 未写入 Binder"
                : bakedCount > 0
                    ? ", 已写入 Binder"
                    : ", 未写入 Binder（无生成贴图）";
            result.summary += binderSummary;
            
            Debug.Log("[AHD 烘焙器] " + result.summary);
            
            return result;
        }

        private static bool ReportProgress(AHDBakeContext context, string title, float progress)
        {
            if (context == null || context.progress == null)
            {
                return true;
            }

            return context.progress(title, Mathf.Clamp01(progress));
        }

        private static bool IsValidLightmapIndex(LightmapData[] lightmaps, int lightmapIndex)
        {
            return lightmaps != null
                && lightmapIndex >= C_MIN_VALID_LIGHTMAP_INDEX
                && lightmapIndex < lightmaps.Length
                && lightmaps[lightmapIndex] != null
                && lightmaps[lightmapIndex].lightmapColor != null;
        }

        private static AHDLightmapWorkset BuildLightmapWorkset(AHDSceneData sceneData, LightmapData lightmapData, int lightmapIndex, AHDBakeSettings settings)
        {
            Texture2D lightmapColor = lightmapData.lightmapColor;
            int width = lightmapColor.width;
            int height = lightmapColor.height;
            
            AHDLightmapWorkset workset = new AHDLightmapWorkset
            {
                lightmapIndex = lightmapIndex,
                width = width,
                height = height,
                texels = new AHDTexelData[width * height],
                results = new AHDTexelResult[width * height],
                validMask = new bool[width * height],
                pixels = new Color[width * height],
                strengthDebug = new Color[width * height],
                visibilityDebug = new Color[width * height],
                confidenceDebug = new Color[width * height],
                luminanceDebug = new Color[width * height],
                dominantDebug = new Color[width * height],
                transitionScoreDebug = new Color[width * height],
                directionDiffDebug = new Color[width * height],
                featherWeightDebug = new Color[width * height],
                featherMaskDebug = new Color[width * height],
                featherDeltaDebug = new Color[width * height]
            };

            for (int i = 0; i < workset.pixels.Length; i++)
            {
                workset.pixels[i] = new Color(
                    C_ENCODED_DIRECTION_NEUTRAL,
                    C_ENCODED_DIRECTION_NEUTRAL,
                    C_ENCODED_DIRECTION_NEUTRAL,
                    C_ALPHA_TRANSPARENT);
            }

            float[] lightmapLuminanceMasks = BuildLightmapLuminanceMasks(lightmapColor, settings);
            for (int i = 0; i < sceneData.receivers.Count; i++)
            {
                AHDMeshTriangle triangle = sceneData.receivers[i];
                if (triangle.lightmapIndex != lightmapIndex)
                {
                    continue;
                }

                RasterizeTriangle(workset, triangle, lightmapLuminanceMasks);
            }

            ResolveTexels(workset);
            EnsureValidTexelLuminanceMask(workset, settings);
            
            return workset;
        }

        /// <summary>
        /// 把 RasterizeTriangle 阶段的累加缓冲解析为最终的 texel 状态。
        /// position/normal/luminanceMask 取加权平均；crossCoherence = |sum(unitNormal)| / weight。
        /// </summary>
        private static void ResolveTexels(AHDLightmapWorkset workset)
        {
            if (workset == null || workset.texels == null)
            {
                return;
            }

            int validCount = 0;
            for (int i = 0; i < workset.texels.Length; i++)
            {
                AHDTexelData texel = workset.texels[i];
                if (texel.weightAccum <= 0)
                {
                    workset.validMask[i] = false;
                    continue;
                }

                float invWeight = 1 / texel.weightAccum;
                texel.positionWS = texel.positionAccum * invWeight;
                
                float3 avgNormal = texel.normalAccum * invWeight;
                float normalLen = math.length(avgNormal);
                texel.normalWS = normalLen > C_VECTOR_LENGTH_SQ_EPSILON
                    ? avgNormal / normalLen
                    : s_WorldUp;
                
                texel.luminanceMask = texel.luminanceAccum * invWeight;
                
                // 单三角形覆盖时 normalLen == 1（unit normal 的累加），crossCoherence 会被压成 1；
                // 但低面数曲面单 texel 内法线插值变化也可能让 normalLen < 1 → 误压 strength。
                // 只在跨三角形覆盖（weightAccum >= 2）时才输出 < 1 的 cross-coherence，
                // 单覆盖统一给 1，把"几何接缝抑制"严格限定在多覆盖情形。
                texel.crossCoherence = texel.weightAccum >= C_CROSS_COHERENCE_MIN_WEIGHT
                    ? math.saturate(normalLen)
                    : C_FULL_WEIGHT;
                texel.valid = C_VALID_TEXEL_FLAG;
                
                workset.texels[i] = texel;
                workset.validMask[i] = true;
                
                validCount++;
            }

            workset.validTexelCount = validCount;
        }

        private static Color[] TryReadPixels(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            try
            {
                return texture.GetPixels();
            }
            catch (Exception)
            {
                return TryReadPixelsByRenderTexture(texture);
            }
        }

        private static float[] BuildLightmapLuminanceMasks(Texture2D lightmapColor, AHDBakeSettings settings)
        {
            if (!settings.useLightmapLuminanceMask)
            {
                return null;
            }

            Color[] lightmapPixels = TryReadPixels(lightmapColor);
            if (lightmapPixels == null || lightmapPixels.Length == 0)
            {
                return null;
            }

            float[] luminanceValues = new float[lightmapPixels.Length];
            float maxLuminance = 0;
            for (int i = 0; i < lightmapPixels.Length; i++)
            {
                float luminance = CalculateLuminance(lightmapPixels[i]);
                luminanceValues[i] = luminance;
                maxLuminance = Mathf.Max(maxLuminance, luminance);
            }

            float referenceLuminance = CalculateLuminanceReference(luminanceValues, maxLuminance);
            if (referenceLuminance <= C_LUMINANCE_EPSILON)
            {
                // 与 V1 行为一致
                Debug.LogWarning("[AHD Baker] Lightmap 亮度参考值太低。亮度 mask 全为 0。");
                return new float[lightmapPixels.Length];
            }

            float cutoff = Mathf.Clamp01(settings.lightmapLuminanceMaskCutoff);
            float softness = Mathf.Max(settings.lightmapLuminanceMaskSoftness, C_MIN_LUMINANCE_MASK_SOFTNESS);
            float cutoffEnd = cutoff + softness;
            
            float[] masks = new float[luminanceValues.Length];
            float maxMask = 0;
            for (int i = 0; i < luminanceValues.Length; i++)
            {
                float normalizedLuminance = luminanceValues[i] / referenceLuminance;
                float maskT = Mathf.InverseLerp(cutoff, cutoffEnd, normalizedLuminance);
                float mask = Mathf.SmoothStep(0, 1, maskT);
                masks[i] = mask;
                maxMask = Mathf.Max(maxMask, mask);
            }

            if (maxMask <= C_LUMINANCE_EPSILON)
            {
                // 与 V1 行为一致
                Debug.LogWarning("[AHD Baker] Lightmap 亮度 mask 全为 0。Strength 也将被压制为 0。");
                return masks;
            }

            return masks;
        }

        private static void EnsureValidTexelLuminanceMask(AHDLightmapWorkset workset, AHDBakeSettings settings)
        {
            if (!settings.useLightmapLuminanceMask)
            {
                return;
            }

            if (workset.validTexelCount == 0)
            {
                return;
            }

            float maxValidMask = 0;
            for (int i = 0; i < workset.validMask.Length; i++)
            {
                if (!workset.validMask[i])
                {
                    continue;
                }

                maxValidMask = Mathf.Max(maxValidMask, workset.texels[i].luminanceMask);
            }

            if (maxValidMask > C_LUMINANCE_EPSILON)
            {
                return;
            }

            // 与 V1 行为一致：
            // lightmap 亮度 mask 全为 0 时，把所有有效 texel 的亮度 mask 全为 0。strength 也为 0。
            // 而不是反向回写成 1 把暗区"开亮"。
            Debug.LogWarning("[AHD Baker] lightmap 亮度 mask 全为 0 时，把所有有效 texel 的亮度 mask 全为 0。strength 也为 0。");
            
            for (int i = 0; i < workset.validMask.Length; i++)
            {
                if (!workset.validMask[i])
                {
                    continue;
                }

                AHDTexelData texel = workset.texels[i];
                texel.luminanceMask = 0;
                workset.texels[i] = texel;
            }
        }

        private static Color[] TryReadPixelsByRenderTexture(Texture2D texture)
        {
            RenderTexture previous = RenderTexture.active;
            
            RenderTexture temporary = RenderTexture.GetTemporary(
                texture.width, texture.height, C_READBACK_DEPTH_BITS,
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            
            Texture2D readable = null;
            try
            {
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;
                
                readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBAHalf, false, true);
                readable.ReadPixels(
                    new Rect(0f, 0f, texture.width, texture.height),
                    C_READBACK_START_PIXEL, C_READBACK_START_PIXEL,
                    false);
                readable.Apply(false, false);
                
                return readable.GetPixels();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[AHD Baker] Lightmap 不可读取，亮度遮罩回退到 1。"
                    + " Texture:" + texture.name
                    + ", Error:" + exception.Message);
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                
                if (readable != null)
                {
                    UnityEngine.Object.DestroyImmediate(readable);
                }
            }
        }

        private static void RasterizeTriangle(AHDLightmapWorkset workset, AHDMeshTriangle triangle, float[] lightmapLuminanceMasks)
        {
            float2 uv0 = triangle.uv0;
            float2 uv1 = triangle.uv1;
            float2 uv2 = triangle.uv2;
            
            int minX = Mathf.Clamp(Mathf.FloorToInt(math.min(math.min(uv0.x, uv1.x), uv2.x) * workset.width), C_MIN_PIXEL_INDEX, workset.width - C_LAST_INDEX_OFFSET);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(math.max(math.max(uv0.x, uv1.x), uv2.x) * workset.width), C_MIN_PIXEL_INDEX, workset.width - C_LAST_INDEX_OFFSET);
            int minY = Mathf.Clamp(Mathf.FloorToInt(math.min(math.min(uv0.y, uv1.y), uv2.y) * workset.height), C_MIN_PIXEL_INDEX, workset.height - C_LAST_INDEX_OFFSET);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(math.max(math.max(uv0.y, uv1.y), uv2.y) * workset.height), C_MIN_PIXEL_INDEX, workset.height - C_LAST_INDEX_OFFSET);
            
            float area = Edge(uv0, uv1, uv2);
            
            if (math.abs(area) <= C_UV_TRIANGLE_AREA_EPSILON)
            {
                return;
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float2 uv = new float2((x + C_TEXEL_CENTER_OFFSET) / workset.width, (y + C_TEXEL_CENTER_OFFSET) / workset.height);
                    float w0 = Edge(uv1, uv2, uv) / area;
                    float w1 = Edge(uv2, uv0, uv) / area;
                    float w2 = Edge(uv0, uv1, uv) / area;
                    
                    if (w0 < -C_BARYCENTRIC_EDGE_EPSILON
                        || w1 < -C_BARYCENTRIC_EDGE_EPSILON
                        || w2 < -C_BARYCENTRIC_EDGE_EPSILON)
                    {
                        continue;
                    }

                    int index = y * workset.width + x;
                    float3 position = triangle.world0 * w0 + triangle.world1 * w1 + triangle.world2 * w2;
                    float3 normalRaw = triangle.normal0 * w0 + triangle.normal1 * w1 + triangle.normal2 * w2;
                    float3 unitNormal = math.normalizesafe(normalRaw, s_WorldUp);
                    float luminanceMask = EvaluateLuminanceMask(lightmapLuminanceMasks, index);

                    // 跨三角形累加：同一 texel 被多个三角形覆盖（chart 接缝、UV 重叠等）时
                    // 不再"最后写入获胜"，而是按权重累加，由 ResolveTexels 统一解析。
                    AHDTexelData accum = workset.texels[index];
                    if (accum.weightAccum <= 0f)
                    {
                        // 首次写入：记录 chartId / ownerId；后续覆盖的三角形沿用首个写入的 chartId。
                        // ownerId 用于 ray 自遮挡剔除，单 texel 不便加权融合，沿用首个写入。
                        accum.chartId = triangle.chartId;
                        accum.ownerId = triangle.ownerId;
                    }

                    accum.weightAccum += C_FULL_WEIGHT;
                    accum.positionAccum += position;
                    accum.normalAccum += unitNormal;
                    accum.luminanceAccum += luminanceMask;
                    
                    workset.texels[index] = accum;
                }
            }
        }

        private static float EvaluateLuminanceMask(float[] lightmapLuminanceMasks, int index)
        {
            if (lightmapLuminanceMasks == null
                || index < 0
                || index >= lightmapLuminanceMasks.Length)
            {
                return C_DEFAULT_LUMINANCE_MASK;
            }

            return lightmapLuminanceMasks[index];
        }

        private static float CalculateLuminance(Color color)
        {
            float luminance = color.r * C_LUMINANCE_R_WEIGHT + color.g * C_LUMINANCE_G_WEIGHT + color.b * C_LUMINANCE_B_WEIGHT;
            return Mathf.Max(0, luminance);
        }

        private static float CalculateLuminanceReference(float[] luminanceValues, float maxLuminance)
        {
            if (luminanceValues == null
                || luminanceValues.Length == 0
                || maxLuminance <= C_LUMINANCE_EPSILON)
            {
                return 0;
            }

            int positiveCount = 0;
            int[] histogram = new int[C_LUMINANCE_HISTOGRAM_BINS];
            float histogramScale = (C_LUMINANCE_HISTOGRAM_BINS - C_LAST_INDEX_OFFSET) / maxLuminance;
            
            for (int i = 0; i < luminanceValues.Length; i++)
            {
                if (luminanceValues[i] <= C_LUMINANCE_EPSILON)
                {
                    continue;
                }

                int bin = Mathf.Clamp(Mathf.FloorToInt(luminanceValues[i] * histogramScale), 0, C_LUMINANCE_HISTOGRAM_BINS - C_LAST_INDEX_OFFSET);
                histogram[bin]++;
                positiveCount++;
            }

            if (positiveCount == 0)
            {
                return 0;
            }

            int targetCount = Mathf.CeilToInt(positiveCount * C_LUMINANCE_REFERENCE_PERCENTILE);
            int cumulativeCount = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                cumulativeCount += histogram[i];
                if (cumulativeCount >= targetCount)
                {
                    return maxLuminance * (i + C_TEXEL_CENTER_OFFSET) / C_LUMINANCE_HISTOGRAM_BINS;
                }
            }

            return maxLuminance;
        }

        private static float Edge(float2 a, float2 b, float2 c)
        {
            return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
        }

        private static bool BakeWorkset(
            AHDLightmapWorkset workset,
            NativeArray<AHDOccluderTriangle> nativeTriangles,
            NativeArray<AHDBvhNode> nativeNodes,
            NativeArray<int> nativeTriangleIndices,
            NativeArray<AHDLightData> nativeLights,
            AHDBakeSettings settings,
            AHDBakeContext context,
            int lightmapIndex,
            float progressStart,
            float progressEnd)
        {
            NativeArray<AHDTexelData> texels = new NativeArray<AHDTexelData>(workset.texels, Allocator.TempJob);
            NativeArray<AHDTexelResult> results = new NativeArray<AHDTexelResult>(workset.results.Length, Allocator.TempJob);
            
            try
            {
                int texelCount = workset.texels.Length;
                int chunkSize = Mathf.Max(C_JOB_TEXEL_CHUNK_SIZE, C_MIN_SAMPLE_COUNT);
                for (int startIndex = 0; startIndex < texelCount; startIndex += chunkSize)
                {
                    float t = (float)startIndex / Mathf.Max(texelCount, C_MIN_SAMPLE_COUNT);
                    float progress = Mathf.Lerp(progressStart, progressEnd, t);
                    if (!ReportProgress(context, "烘焙 Lightmap " + lightmapIndex, progress))
                    {
                        return false;
                    }

                    int count = Mathf.Min(chunkSize, texelCount - startIndex);
                    AHDBakeTexelJob job = new AHDBakeTexelJob
                    {
                        texels = texels,
                        triangles = nativeTriangles,
                        nodes = nativeNodes,
                        triangleIndices = nativeTriangleIndices,
                        lights = nativeLights,
                        results = results,
                        startIndex = startIndex,
                        samplesPerLight = Mathf.Max(settings.samplesPerLight, C_MIN_SAMPLE_COUNT),
                        rayBias = Mathf.Max(settings.rayBias, C_MIN_RAY_BIAS),
                        directionalRayDistance = Mathf.Max(settings.directionalRayDistance, C_MIN_DIRECTIONAL_RAY_DISTANCE),
                        softOcclusionRadius = Mathf.Max(settings.softOcclusionRadius, 0)
                    };
                    JobHandle handle = job.Schedule(count, C_JOB_SCHEDULE_BATCH_SIZE);
                    handle.Complete();
                }

                if (!ReportProgress(context, "烘焙 Lightmap " + lightmapIndex, progressEnd))
                {
                    return false;
                }

                results.CopyTo(workset.results);
                return true;
            }
            finally
            {
                if (texels.IsCreated)
                {
                    texels.Dispose();
                }

                if (results.IsCreated)
                {
                    results.Dispose();
                }
            }
        }

        private static Texture2D CreateDirectionTexture(AHDLightmapWorkset workset)
        {
            for (int i = 0; i < workset.results.Length; i++)
            {
                AHDTexelResult result = workset.results[i];
                
                workset.pixels[i] = EncodeDirection(result.directionWS, result.strength);
                workset.strengthDebug[i] = EncodeDebugValue(result.strength);
                workset.visibilityDebug[i] = EncodeDebugValue(result.visibleWeight);
                workset.confidenceDebug[i] = EncodeDebugValue(result.confidence);
                workset.luminanceDebug[i] = EncodeDebugValue(workset.texels[i].luminanceMask);
                workset.dominantDebug[i] = EncodeDominantLight(result.dominantLightIndex);
            }

            Texture2D texture = new Texture2D(workset.width, workset.height, TextureFormat.RGBA32, false, true)
            {
                name = c_TextureNamePrefix + "-" + workset.lightmapIndex,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = C_DIRECTION_TEXTURE_ANISO_LEVEL
            };
            texture.SetPixels(workset.pixels);
            texture.Apply(false, false);
            
            return texture;
        }

        private static Color EncodeDirection(float3 directionWS, float strength)
        {
            strength = math.saturate(strength);
            if (strength <= C_LUMINANCE_EPSILON
                || math.lengthsq(directionWS) <= C_VECTOR_LENGTH_SQ_EPSILON)
            {
                return new Color(
                    C_ENCODED_DIRECTION_NEUTRAL,
                    C_ENCODED_DIRECTION_NEUTRAL,
                    C_ENCODED_DIRECTION_NEUTRAL,
                    C_ALPHA_TRANSPARENT);
            }

            float3 encoded = math.normalize(directionWS) * C_DIRECTION_ENCODE_SCALE + new float3(C_DIRECTION_ENCODE_BIAS);
            return new Color(encoded.x, encoded.y, encoded.z, strength);
        }

        private static Color EncodeDebugValue(float value)
        {
            value = Mathf.Clamp01(value);
            return new Color(value, value, value, C_ALPHA_OPAQUE);
        }

        private static Color EncodeDominantLight(int lightIndex)
        {
            if (lightIndex < C_MIN_VALID_LIGHTMAP_INDEX)
            {
                return Color.black;
            }

            float hue = Mathf.Repeat(lightIndex * C_DOMINANT_LIGHT_HUE_STEP, C_FULL_WEIGHT);
            Color resultColor = Color.HSVToRGB(hue, C_DOMINANT_LIGHT_DEBUG_SATURATION, C_DOMINANT_LIGHT_DEBUG_VALUE);
            return resultColor;
        }

        private static void SaveDebugMaps(AHDLightmapWorkset workset, string outputFolder)
        {
            SaveDebugMap(workset.strengthDebug, workset, outputFolder, "debug_strength");
            SaveDebugMap(workset.visibilityDebug, workset, outputFolder, "debug_visibility");
            SaveDebugMap(workset.confidenceDebug, workset, outputFolder, "debug_confidence");
            SaveDebugMap(workset.luminanceDebug, workset, outputFolder, "debug_luminance_mask");
            SaveDebugMap(workset.dominantDebug, workset, outputFolder, "debug_dominant");
            SaveDebugMap(workset.transitionScoreDebug, workset, outputFolder, "debug_transition_score");
            SaveDebugMap(workset.directionDiffDebug, workset, outputFolder, "debug_direction_diff");
            SaveDebugMap(workset.featherWeightDebug, workset, outputFolder, "debug_feather_weight");
            SaveDebugMap(workset.featherMaskDebug, workset, outputFolder, "debug_feather_mask");
            SaveDebugMap(workset.featherDeltaDebug, workset, outputFolder, "debug_feather_delta");
        }

        private static void SaveDebugMap(Color[] pixels, AHDLightmapWorkset workset, string outputFolder, string suffix)
        {
            if (pixels == null || workset == null || string.IsNullOrEmpty(outputFolder))
            {
                return;
            }

            Texture2D texture = new Texture2D(workset.width, workset.height, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            
            string path = outputFolder + "/" + c_TextureNamePrefix + "-" + workset.lightmapIndex + "_" + suffix + ".png";
            AHDBakeRuntimeBridge.SaveDebugMap(texture, path);
        }

        [BurstCompile]
        private struct AHDBakeTexelJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<AHDTexelData> texels;

            [ReadOnly]
            public NativeArray<AHDOccluderTriangle> triangles;

            [ReadOnly]
            public NativeArray<AHDBvhNode> nodes;

            [ReadOnly]
            public NativeArray<int> triangleIndices;

            [ReadOnly]
            public NativeArray<AHDLightData> lights;

            [NativeDisableParallelForRestriction]
            public NativeArray<AHDTexelResult> results;
            
            public int startIndex;
            public int samplesPerLight;
            public float rayBias;
            public float directionalRayDistance;
            public float softOcclusionRadius;

            public void Execute(int index)
            {
                int texelIndex = startIndex + index;
                AHDTexelData texel = texels[texelIndex];
                if (texel.valid != C_VALID_TEXEL_FLAG || lights.Length == 0)
                {
                    results[texelIndex] = default;
                    return;
                }

                float3 normalWS = math.normalizesafe(texel.normalWS, s_WorldUp);
                float3 origin = texel.positionWS + normalWS * rayBias;
                
                float3 weightedDirection = new float3(0);
                float3 rawWeightedDirection = new float3(0);
                float totalWeight = 0;
                float visibleWeight = 0;
                float dominantWeight = 0;
                
                int dominantLightIndex = C_INVALID_LIGHT_INDEX;

                for (int lightIndex = 0; lightIndex < lights.Length; lightIndex++)
                {
                    AHDLightData light = lights[lightIndex];
                    if (light.type == C_AREA_LIGHT_TYPE)
                    {
                        if (!EvaluateAreaLight(
                                origin,
                                texel.positionWS,
                                normalWS,
                                light,
                                out float areaTotalWeight,
                                out float areaVisibleWeight,
                                out float3 areaWeightedDirection,
                                out float3 areaRawWeightedDirection))
                        {
                            continue;
                        }

                        totalWeight += areaTotalWeight;
                        visibleWeight += areaVisibleWeight;
                        weightedDirection += areaWeightedDirection;
                        rawWeightedDirection += areaRawWeightedDirection;
                        if (areaVisibleWeight > dominantWeight)
                        {
                            dominantWeight = areaVisibleWeight;
                            dominantLightIndex = lightIndex;
                        }

                        continue;
                    }

                    if (!TryEvaluateLight(
                            light,
                            texel.positionWS,
                            normalWS,
                            out float3 lightDirection,
                            out float maxDistance,
                            out float influence))
                    {
                        continue;
                    }

                    float visibility = EvaluateVisibility(
                        origin,
                        normalWS,
                        light,
                        lightDirection,
                        maxDistance);
                    float weight = influence;
                    float visible = weight * visibility;
                    totalWeight += weight;
                    visibleWeight += visible;
                    weightedDirection += lightDirection * visible;
                    rawWeightedDirection += lightDirection * weight;
                    if (visible > dominantWeight)
                    {
                        dominantWeight = visible;
                        dominantLightIndex = lightIndex;
                    }
                }

                if (totalWeight <= C_LUMINANCE_EPSILON
                    || visibleWeight <= C_LUMINANCE_EPSILON)
                {
                    results[texelIndex] = default;
                    return;
                }

                // V1 等价公式：用 visibleWeight 的绝对量截断，不要用 ratio 替代物理强度。
                float visibleDirectionLen = math.length(weightedDirection);
                float visibleCoherence = math.saturate(visibleDirectionLen / math.max(visibleWeight, C_LUMINANCE_EPSILON));
                float rawDirectionLen = math.length(rawWeightedDirection);
                float rawCoherence = math.saturate(rawDirectionLen / math.max(totalWeight, C_LUMINANCE_EPSILON));
                float visibilityMask = math.saturate(visibleWeight / math.max(totalWeight, C_LUMINANCE_EPSILON));
                float visibleStrength = visibleCoherence * math.saturate(visibleWeight);
                float rawStrength = rawCoherence * math.saturate(totalWeight);
                float coherenceMask = math.smoothstep(C_COHERENCE_MASK_MIN, C_COHERENCE_MASK_MAX, visibleCoherence);
                float crossCoherence = math.saturate(texel.crossCoherence);
                if (crossCoherence <= 0)
                {
                    crossCoherence = C_FULL_WEIGHT;
                }

                float3 directionWS = visibleDirectionLen > C_DIRECTION_CONFIDENCE_EPSILON && visibleCoherence > C_MIN_VISIBLE_COHERENCE_FOR_DIRECTION
                    ? weightedDirection / visibleDirectionLen
                    : normalWS;
                float strength = math.min(visibleStrength, rawStrength * visibilityMask) * coherenceMask * crossCoherence * math.saturate(texel.luminanceMask);

                results[texelIndex] = new AHDTexelResult
                {
                    directionWS = directionWS,
                    strength = math.saturate(strength),
                    visibleWeight = visibilityMask,
                    occlusionRatio = math.saturate(C_FULL_WEIGHT - visibilityMask),
                    confidence = visibleCoherence,
                    dominantLightIndex = dominantLightIndex
                };
            }

            private bool TryEvaluateLight(
                AHDLightData light, float3 positionWS, float3 normalWS,
                out float3 lightDirection, out float maxDistance, out float influence)
            {
                lightDirection = s_WorldUp;
                maxDistance = directionalRayDistance;
                influence = 0;

                if (light.type == C_DIRECTIONAL_LIGHT_TYPE)
                {
                    lightDirection = math.normalizesafe(light.directionToLightWS, s_WorldUp);
                    float normalWeight = math.saturate(math.dot(normalWS, lightDirection));
                    influence = normalWeight * light.intensity;
                    return influence > C_LUMINANCE_EPSILON;
                }

                float3 toLight = light.positionWS - positionWS;
                float distSq = math.max(math.lengthsq(toLight), C_DISTANCE_EPSILON);
                float distance = math.sqrt(distSq);
                if (distance >= light.range)
                {
                    return false;
                }

                lightDirection = toLight / distance;
                maxDistance = distance;
                float normalDot = math.saturate(math.dot(normalWS, lightDirection));
                if (normalDot <= 0)
                {
                    return false;
                }

                float ratio = distance / math.max(light.range, C_MIN_LIGHT_RANGE);
                float ratioSq = ratio * ratio;
                float rangeFade = math.saturate(C_FULL_WEIGHT - ratioSq * ratioSq);
                float referenceDistance = light.range * C_REFERENCE_DISTANCE_RANGE_SCALE;
                float referenceDistSq = referenceDistance * referenceDistance;
                float distanceWeight = math.saturate((C_FULL_WEIGHT / distSq) * referenceDistSq * rangeFade * rangeFade);
                float spotWeight = C_FULL_WEIGHT;
                if (light.type == C_SPOT_LIGHT_TYPE)
                {
                    float cosAngle = math.dot(light.directionToLightWS, lightDirection);
                    float denominator = math.max(light.spotInnerCos - light.spotOuterCos, C_MIN_SPOT_COS_DENOMINATOR);
                    spotWeight = math.saturate((cosAngle - light.spotOuterCos) / denominator);
                }

                influence = normalDot * distanceWeight * spotWeight * light.intensity;
                return influence > C_LUMINANCE_EPSILON;
            }

            private float EvaluateVisibility(
                float3 origin,
                float3 normalWS,
                AHDLightData light,
                float3 lightDirection,
                float maxDistance)
            {
                int sampleCount = math.max(samplesPerLight, C_MIN_SAMPLE_COUNT);
                BuildBasis(normalWS, out float3 tangentWS, out float3 bitangentWS);

                // 软遮挡两端同时抖动：接收点切平面 disc + 光源端球面（仅 punctual）。
                // 这样阴影硬转折→半影→全亮之间是平滑过渡，而不是 0/1 二值。
                float discRadius = light.type == C_DIRECTIONAL_LIGHT_TYPE
                    ? C_DIRECTIONAL_OCCLUSION_DISC_RADIUS
                    : math.max(softOcclusionRadius, 0);
                bool jitterOrigin = sampleCount > C_MIN_SAMPLE_COUNT && discRadius > C_JITTER_EPSILON;
                bool jitterLightSphere = sampleCount > C_MIN_SAMPLE_COUNT && light.type != C_DIRECTIONAL_LIGHT_TYPE && light.sourceRadius > C_JITTER_EPSILON;

                int visibleSamples = 0;
                for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    float3 rayOrigin = origin;
                    if (jitterOrigin)
                    {
                        float2 disk = GetFibonacciDiscOffset(sampleIndex, sampleCount, discRadius);
                        rayOrigin = origin + tangentWS * disk.x + bitangentWS * disk.y;
                    }

                    float3 direction = lightDirection;
                    float distance = maxDistance;
                    if (jitterLightSphere)
                    {
                        float3 sphereOffset = GetFibonacciSphereOffset(sampleIndex, sampleCount) * light.sourceRadius;
                        float3 rayVector = light.positionWS + sphereOffset - rayOrigin;
                        float jitteredDistance = math.length(rayVector);
                        if (jitteredDistance > C_DISTANCE_EPSILON)
                        {
                            distance = jitteredDistance;
                            direction = rayVector / jitteredDistance;
                        }
                    }

                    if (!IsOccluded(rayOrigin, direction, distance))
                    {
                        visibleSamples++;
                    }
                }

                return (float)visibleSamples / sampleCount;
            }

            private bool EvaluateAreaLight(
                float3 origin,
                float3 positionWS,
                float3 normalWS,
                AHDLightData light,
                out float totalWeight,
                out float visibleWeight,
                out float3 weightedDirection,
                out float3 rawWeightedDirection)
            {
                totalWeight = 0;
                visibleWeight = 0;
                weightedDirection = new float3(0);
                rawWeightedDirection = new float3(0);

                int sampleCount = math.max(samplesPerLight, C_MIN_SAMPLE_COUNT);
                float2 areaSize = math.max(light.areaSize, new float2(C_MIN_AREA_LIGHT_SIZE));
                float3 areaRight = math.normalizesafe(light.rightWS, s_WorldRight);
                float3 areaUp = math.normalizesafe(light.upWS, s_WorldUp);
                float3 areaNormal = math.normalizesafe(light.directionToLightWS, s_WorldBack);
                for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    float2 areaOffset = GetAreaLightSampleOffset(sampleIndex, sampleCount, areaSize, light.areaShape);
                    float3 samplePosition = light.positionWS + areaRight * areaOffset.x + areaUp * areaOffset.y;
                    float3 toSample = samplePosition - positionWS;
                    float distSq = math.max(math.lengthsq(toSample), C_DISTANCE_EPSILON);
                    float distance = math.sqrt(distSq);
                    if (distance >= light.range)
                    {
                        continue;
                    }

                    float3 sampleDirection = toSample / distance;
                    float surfaceWeight = math.saturate(math.dot(normalWS, sampleDirection));
                    float facingWeight = math.saturate(math.dot(areaNormal, sampleDirection));
                    if (surfaceWeight <= 0 || facingWeight <= 0)
                    {
                        continue;
                    }

                    float ratio = distance / math.max(light.range, C_MIN_LIGHT_RANGE);
                    float ratioSq = ratio * ratio;
                    float rangeFade = math.saturate(C_FULL_WEIGHT - ratioSq * ratioSq);
                    float referenceDistance = light.range * C_REFERENCE_DISTANCE_RANGE_SCALE;
                    float referenceDistSq = referenceDistance * referenceDistance;
                    float distanceWeight = math.saturate((C_FULL_WEIGHT / distSq) * referenceDistSq * rangeFade * rangeFade);
                    float sampleWeight = surfaceWeight * facingWeight * distanceWeight * light.intensity / sampleCount;
                    if (sampleWeight <= C_LUMINANCE_EPSILON)
                    {
                        continue;
                    }

                    totalWeight += sampleWeight;
                    rawWeightedDirection += sampleDirection * sampleWeight;
                    if (IsOccluded(origin, sampleDirection, distance))
                    {
                        continue;
                    }

                    visibleWeight += sampleWeight;
                    weightedDirection += sampleDirection * sampleWeight;
                }

                return totalWeight > C_LUMINANCE_EPSILON;
            }

            private bool IsOccluded(float3 origin, float3 direction, float maxDistance)
            {
                if (nodes.Length == 0)
                {
                    return false;
                }

                // FixedList128Bytes 实际仅能放约 30 个 int，深 BVH 会溢出抛异常，
                // 在 Burst Job 里会导致整个 chunk 失败、对应 texel 拿 default 值。
                // 用 FixedList512Bytes（≈128 int）容纳 ~2^128 三角形深度，留足余量。
                FixedList512Bytes<int> stack = default;
                stack.Add(C_BVH_ROOT_NODE_INDEX);
                while (stack.Length > 0)
                {
                    int stackIndex = stack.Length - C_LAST_INDEX_OFFSET;
                    int nodeIndex = stack[stackIndex];
                    stack.RemoveAt(stackIndex);
                    
                    AHDBvhNode node = nodes[nodeIndex];
                    if (!RayAabb(origin, direction, maxDistance, node.boundsMin, node.boundsMax))
                    {
                        continue;
                    }

                    if (node.left < 0 && node.right < 0)
                    {
                        for (int i = 0; i < node.count; i++)
                        {
                            int triangleIndex = triangleIndices[node.start + i];
                            AHDOccluderTriangle triangle = triangles[triangleIndex];
                            // 自遮挡通过两层防护：origin 已沿法线偏移 rayBias，
                            // RayTriangle 内部还有 C_DISTANCE_EPSILON 下限。
                            // 不再按 ownerId 整体豁免，凹面/折叠面才能正确自投影。
                            if (RayTriangle(origin, direction, triangle.world0, triangle.world1, triangle.world2, maxDistance))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (node.left >= C_MIN_VALID_BVH_NODE_INDEX)
                        {
                            stack.Add(node.left);
                        }

                        if (node.right >= C_MIN_VALID_BVH_NODE_INDEX)
                        {
                            stack.Add(node.right);
                        }
                    }
                }

                return false;
            }

            private static bool RayAabb(
                float3 origin, float3 direction,
                float maxDistance,
                float3 boundsMin, float3 boundsMax)
            {
                float3 safeDirection = math.select(
                    direction,
                    math.select(
                        new float3(-C_RAY_AABB_DIRECTION_EPSILON),
                        new float3(C_RAY_AABB_DIRECTION_EPSILON),
                        new bool3(direction.x >= 0f, direction.y >= 0f, direction.z >= 0)),
                    math.abs(direction) < C_RAY_AABB_DIRECTION_EPSILON);
                float3 inv = C_FULL_WEIGHT / safeDirection;
                float3 t0 = (boundsMin - origin) * inv;
                float3 t1 = (boundsMax - origin) * inv;
                float3 tMin3 = math.min(t0, t1);
                float3 tMax3 = math.max(t0, t1);
                float tMin = math.max(math.max(tMin3.x, tMin3.y), tMin3.z);
                float tMax = math.min(math.min(tMax3.x, tMax3.y), tMax3.z);
                return tMax >= math.max(tMin, 0f) && tMin <= maxDistance;
            }

            private static bool RayTriangle(
                float3 origin, float3 direction,
                float3 v0, float3 v1, float3 v2,
                float maxDistance)
            {
                float3 edge1 = v1 - v0;
                float3 edge2 = v2 - v0;
                float3 p = math.cross(direction, edge2);
                float det = math.dot(edge1, p);
                if (math.abs(det) < C_RAY_TRIANGLE_DET_EPSILON)
                {
                    return false;
                }

                float invDet = C_FULL_WEIGHT / det;
                float3 t = origin - v0;
                float u = math.dot(t, p) * invDet;
                if (u < 0 || u > 1)
                {
                    return false;
                }

                float3 q = math.cross(t, edge1);
                float v = math.dot(direction, q) * invDet;
                if (v < 0 || u + v > 1)
                {
                    return false;
                }

                float hitDistance = math.dot(edge2, q) * invDet;
                bool result = hitDistance > C_DISTANCE_EPSILON && hitDistance < maxDistance;
                return result;
            }

            private static void BuildBasis(float3 normalWS, out float3 tangentWS, out float3 bitangentWS)
            {
                float3 helper = math.abs(math.dot(normalWS, s_WorldUp)) > C_BASIS_UP_DOT_THRESHOLD
                    ? s_WorldRight
                    : s_WorldUp;
                
                tangentWS = math.normalizesafe(math.cross(helper, normalWS), s_WorldRight);
                bitangentWS = math.normalizesafe(math.cross(normalWS, tangentWS), s_WorldForward);
            }

            private static float2 GetFibonacciDiscOffset(int sampleIndex, int sampleCount, float radius)
            {
                if (sampleIndex <= 0)
                {
                    return new float2(0);
                }

                float sampleT = (sampleIndex - C_TEXEL_CENTER_OFFSET) / math.max(sampleCount - C_LAST_INDEX_OFFSET, C_MIN_SAMPLE_COUNT);
                float r = math.sqrt(sampleT) * radius;
                float angle = sampleIndex * C_GOLDEN_ANGLE;
                
                float2 result = new float2(math.cos(angle), math.sin(angle)) * r;
                return result;
            }

            private static float3 GetFibonacciSphereOffset(int sampleIndex, int sampleCount)
            {
                float t = (sampleIndex + C_TEXEL_CENTER_OFFSET) / math.max(sampleCount, C_MIN_SAMPLE_COUNT);
                float z = C_FULL_WEIGHT - C_UNIT_SPHERE_Z_SPAN * t;
                float r = math.sqrt(math.max(0, C_FULL_WEIGHT - z * z));
                float phi = sampleIndex * C_GOLDEN_ANGLE;
                
                float3 result = new float3(math.cos(phi) * r, math.sin(phi) * r, z);
                return result;
            }

            private static float2 GetAreaLightSampleOffset(int sampleIndex, int sampleCount, float2 areaSize, int areaShape)
            {
                float2 halfSize = areaSize * C_TEXEL_CENTER_OFFSET;
                if (areaShape == C_DISC_AREA_SHAPE)
                {
                    float discRadius = math.max(halfSize.x, C_MIN_AREA_LIGHT_SIZE);
                    return GetFibonacciDiscOffset(sampleIndex, sampleCount, discRadius);
                }

                float u = ((float)sampleIndex + C_TEXEL_CENTER_OFFSET) / math.max(sampleCount, C_MIN_SAMPLE_COUNT);
                float v = RadicalInverseBase2((uint)sampleIndex);

                float2 result = new float2((u - C_TEXEL_CENTER_OFFSET) * areaSize.x, (v - C_TEXEL_CENTER_OFFSET) * areaSize.y);
                return result;
            }

            private static float RadicalInverseBase2(uint bits)
            {
                bits = (bits << 16) | (bits >> 16);
                bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
                bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
                bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
                bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
                return bits * C_RADICAL_INVERSE_UINT_SCALE;
            }
            
        }
    }
}
