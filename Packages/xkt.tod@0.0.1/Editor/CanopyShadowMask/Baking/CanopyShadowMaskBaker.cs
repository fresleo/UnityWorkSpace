/*******************************************************************************
 * File: CanopyShadowMaskBaker.cs
 * Author: WangYu
 * Date: 2026-06-30
 * Description: 把第2次 bake 出来的阴影合并到官方的 shadowmask 纹理里。
 *******************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using XKT.TOD.Utils;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 收集接收器/代理，并将树冠遮挡合成到 shadowmask 缓冲区中。
    /// </summary>
    public static class CanopyShadowMaskBaker
    {
        /// <summary>
        /// Unity 标签用于收集树冠的 _ShadowOnly 代理 mesh 对象。
        /// </summary>
        public const string C_CANOPY_SHADOW_ONLY_TAG = "CanopyShadowOnly";

        private const float C_BARY_EPSILON = 1e-6f;

        /// <summary>
        /// 烘焙过程的结果总结
        /// </summary>
        public sealed class BakeResult
        {
            /// <summary>
            /// 烘焙完成而没有严重错误。
            /// </summary>
            public bool success;

            /// <summary>
            /// 烘焙结果总结。
            /// </summary>
            public string message;

            /// <summary>
            /// 由2次烘焙变暗的图集像素数量。
            /// </summary>
            public int modifiedTexels;

            /// <summary>
            /// 实际写入树冠 shadowmask 的 lightmap 数量。
            /// </summary>
            public int modifiedLightmapCount;

            /// <summary>
            /// 收集到的地面接收器数量。
            /// </summary>
            public int groundRendererCount;

            /// <summary>
            /// 收集到的树冠代理 MeshRenderer 的数量。
            /// </summary>
            public int canopyProxyCount;

            /// <summary>
            /// 测试 shadow map 的接收器图集像素数量。
            /// </summary>
            public int testedTexels;

            /// <summary>
            /// 被标记树冠代理阻挡的测试纹理数量。
            /// </summary>
            public int blockedTexels;

            /// <summary>
            /// 被阻挡的像素跳过，因为目标通道已经更暗了。
            /// </summary>
            public int alreadyDarkerTexels;

            /// <summary>
            /// 接收器 lightmap UV 三角形访问的数量。
            /// </summary>
            public int receiverTriangles;

            /// <summary>
            /// 产生至少一个图集样本的接收器三角形数量。
            /// </summary>
            public int sampledTriangles;

            /// <summary>
            /// 因 lightmap UV 区域无效而跳过的接收器三角形数量。
            /// </summary>
            public int invalidUvTriangles;

            /// <summary>
            /// 因为 uv2 不可用，所以使用 uv0 的接收器数量。
            /// </summary>
            public int uv0FallbackReceivers;

            /// <summary>
            /// 因为既没有 uv2 也没有 uv0 而跳过的接收器数量。
            /// </summary>
            public int missingUvReceivers;

            /// <summary>
            /// 此烘焙使用的临时 shadow map 分辨率。
            /// </summary>
            public int shadowMapResolution;

            /// <summary>
            /// 绘制到 shadow map 中的标记树冠渲染器数量。
            /// </summary>
            public int drawnCanopyRenderers;

            /// <summary>
            /// 调试为这次烘焙写的遮罩路径。
            /// </summary>
            public string debugMaskPaths;

            /// <summary>
            /// 为这次烘焙写的灯光空间 shadow map 调试路径。
            /// </summary>
            public string shadowMapDebugPath;

            /// <summary>
            /// 用于结果诊断的短接收器渲染器列表。
            /// </summary>
            public string receiverSummary;
        }

        /// <summary>
        /// 运行完整的合成与回写流程。
        /// </summary>
        public static BakeResult Bake(CanopyShadowMaskBakeParams bakeParams)
        {
            BakeResult result = new BakeResult();
            if (bakeParams == null)
            {
                result.message = "Bake 参数是 null。";
                return result;
            }

            if (bakeParams.mainLight == null)
            {
                result.message = "主灯光未分配。";
                Debug.LogError(result.message);
                return result;
            }

            if (bakeParams.autoUseLightShadowmaskChannel)
            {
                EShadowMaskChannel channel;
                if (TryGetShadowMaskChannel(bakeParams.mainLight, out channel))
                {
                    bakeParams.channel = channel;
                }
                else
                {
                    Debug.LogWarning($"主灯光没有有效的 Shadowmask 通道。使用手动通道 {bakeParams.channel}。");
                }
            }

            if (LightmapSettings.lightmaps == null || LightmapSettings.lightmaps.Length == 0)
            {
                result.message = "场景没有烘焙 lightmap。请先运行 Unity lightmap 烘焙。";
                Debug.LogError(result.message);
                return result;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                result.message = "当前没有有效的活动场景。";
                Debug.LogError(result.message);
                return result;
            }

            if (bakeParams.mainLight.gameObject.scene != activeScene)
            {
                result.message = "主灯光不属于当前活动场景。";
                Debug.LogError(result.message);
                return result;
            }

            List<MeshRenderer> canopyProxyRenderers = CollectCanopyProxyRenderers(result);
            if (!string.IsNullOrEmpty(result.message))
            {
                Debug.LogError(result.message);
                return result;
            }

            List<MeshRenderer> grounds = CollectGroundRenderers(bakeParams);
            result.canopyProxyCount = canopyProxyRenderers.Count;
            result.groundRendererCount = grounds.Count;
            result.receiverSummary = BuildRendererSummary(grounds);

            if (canopyProxyRenderers.Count == 0)
            {
                result.message = $"未找到带 tag 的树冠代理 MeshRenderer。将 _ShadowOnly 对象标记为 {C_CANOPY_SHADOW_ONLY_TAG} 。";
                Debug.LogError(result.message);
                return result;
            }

            if (grounds.Count == 0)
            {
                result.message = "未找到 lightmap 地面渲染器。";
                Debug.LogError(result.message);
                return result;
            }

            HashSet<int> usedLightmapIndices = CollectUsedLightmapIndices(grounds);
            if (usedLightmapIndices.Count == 0)
            {
                result.message = "地面接收器没有有效的 lightmap 索引。";
                Debug.LogError(result.message);
                return result;
            }

            using (var progress = new CanopyShadowMaskBakeProgress())
            {
                return BakeInternal(
                    bakeParams,
                    result,
                    canopyProxyRenderers,
                    grounds,
                    usedLightmapIndices,
                    progress);
            }
        }

        // 内部 bake 方法
        private static BakeResult BakeInternal(
            CanopyShadowMaskBakeParams bakeParams,
            BakeResult result,
            List<MeshRenderer> canopyProxyRenderers,
            List<MeshRenderer> grounds,
            HashSet<int> usedLightmapIndices,
            CanopyShadowMaskBakeProgress progress)
        {
            if (!progress.Report(0.05f, "加载 shadowmask 纹理..."))
            {
                return CreateCancelledResult(result);
            }

            List<CanopyShadowMaskWriteback.ShadowMaskBuffer> buffers;
            try
            {
                buffers = CanopyShadowMaskWriteback.LoadShadowMaskBuffersForLightmapIndices(usedLightmapIndices);
            }
            catch (Exception ex)
            {
                result.message = "加载 shadowmask 纹理失败: " + ex.Message;
                Debug.LogError(result.message);
                return result;
            }

            if (buffers.Count == 0)
            {
                result.message = "没有加载 shadowmask 纹理。";
                Debug.LogError(result.message);
                return result;
            }
            
            Dictionary<int, Color32[]> debugMasks = null;
            if (bakeParams.writeDebugTextures)
            {
                debugMasks = new Dictionary<int, Color32[]>();
            }

            for (int i = 0; i < buffers.Count; i++)
            {
                CanopyShadowMaskWriteback.ShadowMaskBuffer buffer = buffers[i];
                if (debugMasks != null)
                {
                    debugMasks[buffer.lightmapIndex] = new Color32[buffer.width * buffer.height];
                }
            }

            Dictionary<int, List<MeshRenderer>> groundsByLightmap = GroupGroundsByLightmap(grounds);
            List<string> shadowMapDebugPaths = new List<string>();

            int modified = 0;
            var modifiedBuffers = new List<CanopyShadowMaskWriteback.ShadowMaskBuffer>();
            try
            {
                for (int i = 0; i < buffers.Count; i++)
                {
                    CanopyShadowMaskWriteback.ShadowMaskBuffer buffer = buffers[i];
                    if (buffer == null)
                    {
                        continue;
                    }

                    List<MeshRenderer> lightmapGrounds;
                    if (!groundsByLightmap.TryGetValue(buffer.lightmapIndex, out lightmapGrounds)
                        || lightmapGrounds == null
                        || lightmapGrounds.Count == 0)
                    {
                        continue;
                    }

                    string infoTxt = "渲染并合成 lightmap {0} ({1}/{2})";
                    if (!progress.ReportStep(
                            0.15f, 0.8f, i, buffers.Count, 
                            string.Format(infoTxt, buffer.lightmapIndex, i + 1, buffers.Count))
                        )
                    {
                        return CreateCancelledResult(result);
                    }

                    if (!CanopyShadowMaskShadowMap.HasRelevantCanopyRenderers(
                            canopyProxyRenderers, lightmapGrounds, bakeParams.mainLight))
                    {
                        continue;
                    }

                    CanopyShadowMaskShadowMap shadowMap = CanopyShadowMaskShadowMap.Render(
                        canopyProxyRenderers, lightmapGrounds, bakeParams.mainLight);
                    if (shadowMap == null)
                    {
                        result.message = "无法渲染临时的树冠 shadowmap。";
                        Debug.LogError(result.message);
                        return result;
                    }

                    try
                    {
                        if (result.shadowMapResolution == 0)
                        {
                            result.shadowMapResolution = shadowMap.Resolution;
                        }

                        result.drawnCanopyRenderers += shadowMap.DrawnRendererCount;
                        
                        if (bakeParams.writeDebugTextures)
                        {
                            string debugPath = shadowMap.WriteDebugTexture(buffer.canopyAssetPath);
                            if (!string.IsNullOrEmpty(debugPath))
                            {
                                shadowMapDebugPaths.Add(debugPath);
                            }
                        }

                        for (int g = 0; g < lightmapGrounds.Count; g++)
                        {
                            MeshRenderer renderer = lightmapGrounds[g];
                            if (renderer == null)
                            {
                                continue;
                            }

                            MeshFilter filter = renderer.GetComponent<MeshFilter>();
                            if (filter == null || filter.sharedMesh == null)
                            {
                                continue;
                            }

                            Color32[] debugPixels = null;
                            if (debugMasks != null)
                            {
                                debugPixels = debugMasks[buffer.lightmapIndex];
                            }

                            modified += ProjectOntoRenderer(
                                renderer,
                                filter.sharedMesh,
                                buffer,
                                bakeParams,
                                shadowMap,
                                debugPixels,
                                result);
                        }
                    }
                    finally
                    {
                        shadowMap.Dispose();
                    }
                }

                result.shadowMapDebugPath = string.Join(", ", shadowMapDebugPaths.ToArray());
                
                modifiedBuffers = CollectModifiedBuffers(buffers);
                result.modifiedLightmapCount = modifiedBuffers.Count;
                if (modifiedBuffers.Count > 0)
                {
                    if (!progress.Report(0.85f, "写回树冠 shadowmask 纹理..."))
                    {
                        return CreateCancelledResult(result);
                    }

                    if (!CanopyShadowMaskWriteback.WriteShadowMaskBuffers(modifiedBuffers))
                    {
                        result.message = "写回失败，一个或多个树冠 shadowmask 资源未保存。";
                        Debug.LogError(result.message);
                        return result;
                    }
                }

                if (!progress.Report(0.92f, "更新 shadowmask 映射资源..."))
                {
                    return CreateCancelledResult(result);
                }

                CanopyShadowMaskBindingAssetUtility.SyncBindingAsset(modifiedBuffers, usedLightmapIndices);

                if (bakeParams.writeDebugTextures)
                {
                    if (!progress.Report(0.96f, "写入 debug 纹理..."))
                    {
                        return CreateCancelledResult(result);
                    }

                    result.debugMaskPaths = WriteDebugMasks(modifiedBuffers, debugMasks);
                }

                if (!progress.Report(1, "完成"))
                {
                    return CreateCancelledResult(result);
                }
            }
            catch (Exception ex)
            {
                result.message = $"Bake 失败: \n{ex.Message}";
                Debug.LogError(result.message);
                return result;
            }
            
            result.success = true;
            result.modifiedTexels = modified;
            result.message = BuildBakeSummaryMessage(result, modified, grounds.Count, canopyProxyRenderers.Count);
            
            if (modified == 0)
            {
                Debug.LogWarning(result.message);
            }
            else
            {
                Debug.Log(result.message);
            }

            return result;
        }

        // 创建取消结果
        private static BakeResult CreateCancelledResult(BakeResult result)
        {
            result.message = "烘焙已取消。";
            Debug.LogWarning(result.message);
            return result;
        }

        // 收集已使用的光照贴图索引
        private static HashSet<int> CollectUsedLightmapIndices(List<MeshRenderer> grounds)
        {
            var indices = new HashSet<int>();
            if (grounds == null)
            {
                return indices;
            }

            for (int i = 0; i < grounds.Count; i++)
            {
                MeshRenderer renderer = grounds[i];
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.lightmapIndex >= 0)
                {
                    indices.Add(renderer.lightmapIndex);
                }
            }

            return indices;
        }

        // 按 Lightmap 分组地面
        private static Dictionary<int, List<MeshRenderer>> GroupGroundsByLightmap(
            List<MeshRenderer> grounds)
        {
            var result = new Dictionary<int, List<MeshRenderer>>();
            if (grounds == null)
            {
                return result;
            }

            for (int i = 0; i < grounds.Count; i++)
            {
                MeshRenderer renderer = grounds[i];
                if (renderer == null || renderer.lightmapIndex < 0)
                {
                    continue;
                }

                List<MeshRenderer> group;
                if (!result.TryGetValue(renderer.lightmapIndex, out group))
                {
                    group = new List<MeshRenderer>();
                    result.Add(renderer.lightmapIndex, group);
                }

                group.Add(renderer);
            }

            return result;
        }
        
        // 收集已修改的 shadowmask
        private static List<CanopyShadowMaskWriteback.ShadowMaskBuffer> CollectModifiedBuffers(
            List<CanopyShadowMaskWriteback.ShadowMaskBuffer> buffers)
        {
            var modifiedBuffers = new List<CanopyShadowMaskWriteback.ShadowMaskBuffer>();
            if (buffers == null)
            {
                return modifiedBuffers;
            }

            for (int i = 0; i < buffers.Count; i++)
            {
                CanopyShadowMaskWriteback.ShadowMaskBuffer buffer = buffers[i];
                if (buffer != null && buffer.modifiedTexelCount > 0)
                {
                    modifiedBuffers.Add(buffer);
                }
            }

            return modifiedBuffers;
        }

        private static string BuildBakeSummaryMessage(
            BakeResult result,
            int modified,
            int groundCount,
            int canopyProxyCount)
        {
            string headline = modified == 0
                ? "Bake 完成，但修改过的纹理单元是 0。"
                : "Bake 完成。 修改过的纹理单元: " + modified;

            string summary =
                headline
                + " 受影响 lightmap: " + result.modifiedLightmapCount
                + ", 测试过的纹理单元: " + result.testedTexels
                + ", 被遮挡的纹理单元: " + result.blockedTexels
                + ", 已经变暗的: " + result.alreadyDarkerTexels
                + ", 采样三角形: " + result.sampledTriangles + "/" + result.receiverTriangles
                + ", 无效的UV三角形: " + result.invalidUvTriangles
                + ", uv0 备用接收器: " + result.uv0FallbackReceivers
                + ", 丢失的 uv 接收器: " + result.missingUvReceivers
                + ", 地面: " + groundCount
                + ", 树冠代理: " + canopyProxyCount
                + ", shadow map: " + result.shadowMapResolution
                + ", 绘制渲染器: " + result.drawnCanopyRenderers
                + ", 接收器: " + result.receiverSummary;

            if (!string.IsNullOrEmpty(result.debugMaskPaths))
            {
                summary += "\n, debug masks: " + result.debugMaskPaths;
            }

            if (!string.IsNullOrEmpty(result.shadowMapDebugPath))
            {
                summary += "\n, shadowmap debug: " + result.shadowMapDebugPath;
            }

            if (modified == 0)
            {
                summary += "\n检查 tag, 代理材质 alpha, 光照方向, 接收器 UV, 或通道。";
            }

            return summary;
        }

        /// <summary>
        /// 解析 Unity 光照烘焙器分配的 shadowmask 通道。
        /// </summary>
        public static bool TryGetShadowMaskChannel(Light light, out EShadowMaskChannel channel)
        {
            channel = EShadowMaskChannel.R;
            if (light == null)
            {
                return false;
            }

            LightBakingOutput output = light.bakingOutput;
            if (output.lightmapBakeType != LightmapBakeType.Mixed)
            {
                return false;
            }

            if (output.occlusionMaskChannel < 0 || output.occlusionMaskChannel > 3)
            {
                return false;
            }

            channel = (EShadowMaskChannel)output.occlusionMaskChannel;
            return true;
        }

        // 收集树冠代理渲染器
        private static List<MeshRenderer> CollectCanopyProxyRenderers(BakeResult result)
        {
            var list = new List<MeshRenderer>();
            var usedRenderers = new HashSet<MeshRenderer>();

            if (!IsCanopyShadowOnlyTagDefined())
            {
                result.message =
                    $"丢失 Unity tag: {C_CANOPY_SHADOW_ONLY_TAG} 。"
                    + "把它加到 ProjectSettings/TagManager.asset 里。";
                return list;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                result.message = "当前没有有效的活动场景。";
                return list;
            }

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject taggedObject = allObjects[i];
                if (taggedObject == null || taggedObject.scene != activeScene)
                {
                    continue;
                }

                if (!HasCanopyShadowOnlyTag(taggedObject))
                {
                    continue;
                }

                MeshRenderer[] renderers = taggedObject.GetComponentsInChildren<MeshRenderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    MeshRenderer renderer = renderers[r];
                    if (renderer == null)
                    {
                        continue;
                    }

                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        continue;
                    }

                    if (!usedRenderers.Add(renderer))
                    {
                        continue;
                    }

                    list.Add(renderer);
                }
            }

            return list;
        }

        // 收集地面渲染器
        private static List<MeshRenderer> CollectGroundRenderers(CanopyShadowMaskBakeParams bakeParams)
        {
            var list = new List<MeshRenderer>();
            if (bakeParams.manualGroundRenderers != null && bakeParams.manualGroundRenderers.Length > 0)
            {
                for (int i = 0; i < bakeParams.manualGroundRenderers.Length; i++)
                {
                    MeshRenderer mr = bakeParams.manualGroundRenderers[i];
                    if (mr != null && mr.lightmapIndex >= 0 && !IsUnderCanopyShadowOnlyTag(mr.transform))
                    {
                        list.Add(mr);
                    }
                }

                return list;
            }

            List<MeshRenderer> all = TODUtils.FindObjectsOfTypeInActiveScene<MeshRenderer>(true);
            for (int i = 0; i < all.Count; i++)
            {
                MeshRenderer renderer = all[i];
                if (renderer == null || renderer.lightmapIndex < 0)
                {
                    continue;
                }

                if (IsUnderCanopyShadowOnlyTag(renderer.transform))
                {
                    continue;
                }

                if (bakeParams.groundLayerMask != 0)
                {
                    int layerBit = 1 << renderer.gameObject.layer;
                    if ((bakeParams.groundLayerMask.value & layerBit) == 0)
                    {
                        continue;
                    }
                }

                if (IsExcludedShader(renderer, bakeParams.excludedShaderNames))
                {
                    continue;
                }

                list.Add(renderer);
            }

            return list;
        }

        // 检查 transform 是否有树冠阴影 tag 或在有这个 tag 的对象之下
        private static bool IsUnderCanopyShadowOnlyTag(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (HasCanopyShadowOnlyTag(current.gameObject))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        // 构建渲染器摘要
        private static string BuildRendererSummary(List<MeshRenderer> renderers)
        {
            if (renderers == null || renderers.Count == 0)
            {
                return "none";
            }

            var names = new List<string>();
            int count = Mathf.Min(renderers.Count, 8);
            for (int i = 0; i < count; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                names.Add(renderer.name + "@LM" + renderer.lightmapIndex);
            }

            if (renderers.Count > count)
            {
                names.Add("..." + (renderers.Count - count) + " more");
            }

            string summary = string.Join("|", names.ToArray());
            return summary;
        }

        // 对象有树冠阴影 tag
        private static bool HasCanopyShadowOnlyTag(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            bool result = obj.CompareTag(C_CANOPY_SHADOW_ONLY_TAG);
            return result;
        }

        // 编辑器里是否定义了树冠阴影的 tag
        private static bool IsCanopyShadowOnlyTagDefined()
        {
            string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == C_CANOPY_SHADOW_ONLY_TAG)
                {
                    return true;
                }
            }

            return false;
        }

        // 被排除的着色器
        private static bool IsExcludedShader(MeshRenderer renderer, string[] excludedShaderNames)
        {
            if (excludedShaderNames == null || excludedShaderNames.Length == 0)
            {
                return false;
            }

            Material[] mats = renderer.sharedMaterials;
            if (mats == null)
            {
                return false;
            }

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null || mat.shader == null)
                {
                    continue;
                }

                string shaderName = mat.shader.name;
                for (int e = 0; e < excludedShaderNames.Length; e++)
                {
                    if (string.IsNullOrEmpty(excludedShaderNames[e]))
                    {
                        continue;
                    }

                    if (shaderName == excludedShaderNames[e])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 投影到渲染器
        private static int ProjectOntoRenderer(
            MeshRenderer renderer,
            Mesh mesh,
            CanopyShadowMaskWriteback.ShadowMaskBuffer buffer,
            CanopyShadowMaskBakeParams bakeParams,
            CanopyShadowMaskShadowMap shadowMap,
            Color32[] debugPixels,
            BakeResult result)
        {
            bool usedUv0Fallback;
            Vector2[] lightmapUvs = ResolveReceiverLightmapUvs(mesh, out usedUv0Fallback);
            if (lightmapUvs == null || lightmapUvs.Length == 0)
            {
                result.missingUvReceivers++;
                Debug.LogWarning($"mesh 缺少有效的 lightmap UV 和 UV0: {mesh.name}");
                return 0;
            }

            if (usedUv0Fallback)
            {
                result.uv0FallbackReceivers++;
                Debug.LogWarning($"mesh 缺少有效的 uv2，回退使用 uv0: {mesh.name}");
            }

            Vector4 scaleOffset = renderer.lightmapScaleOffset;
            int[] tris = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Transform tr = renderer.transform;
            int step = Mathf.Clamp(bakeParams.atlasTexelStep, 1, 8);

            int modified = 0;
            for (int t = 0; t < tris.Length; t += 3)
            {
                int i0 = tris[t];
                int i1 = tris[t + 1];
                int i2 = tris[t + 2];

                Vector3 w0 = tr.TransformPoint(vertices[i0]);
                Vector3 w1 = tr.TransformPoint(vertices[i1]);
                Vector3 w2 = tr.TransformPoint(vertices[i2]);

                Vector2 uvA = MapLightmapUv(lightmapUvs[i0], scaleOffset);
                Vector2 uvB = MapLightmapUv(lightmapUvs[i1], scaleOffset);
                Vector2 uvC = MapLightmapUv(lightmapUvs[i2], scaleOffset);
                
                result.receiverTriangles++;
                if (!IsValidUvTriangle(uvA, uvB, uvC))
                {
                    result.invalidUvTriangles++;
                    continue;
                }

                float minU = Mathf.Min(uvA.x, Mathf.Min(uvB.x, uvC.x));
                float maxU = Mathf.Max(uvA.x, Mathf.Max(uvB.x, uvC.x));
                float minV = Mathf.Min(uvA.y, Mathf.Min(uvB.y, uvC.y));
                float maxV = Mathf.Max(uvA.y, Mathf.Max(uvB.y, uvC.y));

                int minX = UvToMinPixel(minU, buffer.width);
                int maxX = UvToMaxPixel(maxU, buffer.width);
                int minY = UvToMinPixel(minV, buffer.height);
                int maxY = UvToMaxPixel(maxV, buffer.height);
                
                bool sampledTriangle = false;
                for (int py = minY; py <= maxY; py += step)
                {
                    for (int px = minX; px <= maxX; px += step)
                    {
                        Vector2 uvLm = PixelToLightmapUv(px, py, buffer.width, buffer.height);
                        Vector3 bary = ComputeBarycentricUv(uvLm, uvA, uvB, uvC);
                        if (!IsValidBarycentric(bary))
                        {
                            continue;
                        }

                        if (ProjectSample(
                                px, py,
                                bary,
                                w0, w1, w2,
                                buffer,
                                bakeParams,
                                shadowMap,
                                debugPixels,
                                result,
                                ref modified))
                        {
                            sampledTriangle = true;
                        }
                    }
                }

                if (!sampledTriangle)
                {
                    Vector2 centerUv = (uvA + uvB + uvC) / 3f;
                    int px = UvToNearestPixel(centerUv.x, buffer.width);
                    int py = UvToNearestPixel(centerUv.y, buffer.height);
                    Vector3 bary = new Vector3(1f / 3f, 1f / 3f, 1f / 3f);
                    sampledTriangle = ProjectSample(
                        px, py,
                        bary,
                        w0, w1, w2,
                        buffer,
                        bakeParams,
                        shadowMap,
                        debugPixels,
                        result,
                        ref modified);
                }

                if (sampledTriangle)
                {
                    result.sampledTriangles++;
                }
            }

            return modified;
        }

        // 解析接收器 Lightmap UV
        private static Vector2[] ResolveReceiverLightmapUvs(Mesh mesh, out bool usedUv0Fallback)
        {
            usedUv0Fallback = false;
            if (mesh == null)
            {
                return null;
            }

            Vector2[] uv2 = mesh.uv2;
            if (uv2 != null && uv2.Length == mesh.vertexCount)
            {
                return uv2;
            }

            Vector2[] uv0 = mesh.uv;
            if (uv0 != null && uv0.Length == mesh.vertexCount)
            {
                usedUv0Fallback = true;
                return uv0;
            }

            return null;
        }

        // 投影采样
        private static bool ProjectSample(
            int px, int py,
            Vector3 bary,
            Vector3 w0, Vector3 w1, Vector3 w2,
            CanopyShadowMaskWriteback.ShadowMaskBuffer buffer,
            CanopyShadowMaskBakeParams bakeParams,
            CanopyShadowMaskShadowMap shadowMap,
            Color32[] debugPixels,
            BakeResult result,
            ref int modified)
        {
            if (px < 0 || px >= buffer.width || py < 0 || py >= buffer.height)
            {
                return false;
            }

            result.testedTexels++;
            if (debugPixels != null)
            {
                int debugIndex = py * buffer.width + px;
                if (debugPixels[debugIndex].a == 0)
                {
                    debugPixels[debugIndex] = new Color32(0, 64, 255, 255);
                }
            }

            Vector3 worldPos = w0 * bary.x + w1 * bary.y + w2 * bary.z;
            float occlusion = ComputeShadowMapOcclusion(worldPos, bakeParams, shadowMap);
            if (occlusion >= 0.999f)
            {
                return true;
            }

            result.blockedTexels++;
            if (debugPixels != null)
            {
                debugPixels[py * buffer.width + px] = new Color32(255, 0, 0, 255);
            }

            float prev = buffer.GetChannel(px, py, bakeParams.channel);
            if (occlusion < prev - 0.001f)
            {
                buffer.ApplyMinChannel(px, py, bakeParams.channel, occlusion);
                modified++;
                buffer.modifiedTexelCount++;
            }
            else
            {
                result.alreadyDarkerTexels++;
            }

            return true;
        }

        // 像素 -> Lightmap UV
        private static Vector2 PixelToLightmapUv(int px, int py, int width, int height)
        {
            float u = (px + 0.5f) / Mathf.Max(1, width);
            float v = (py + 0.5f) / Mathf.Max(1, height);
            var uv = new Vector2(u, v);
            return uv;
        }

        // UV -> 最小像素
        private static int UvToMinPixel(float uv, int size)
        {
            return Mathf.Clamp(Mathf.FloorToInt(uv * size), 0, size - 1);
        }

        // UV -> 最大像素
        private static int UvToMaxPixel(float uv, int size)
        {
            return Mathf.Clamp(Mathf.CeilToInt(uv * size), 0, size - 1);
        }

        // UV -> 最近的像素
        private static int UvToNearestPixel(float uv, int size)
        {
            return Mathf.Clamp(Mathf.FloorToInt(uv * size), 0, size - 1);
        }
        
        // 是否是有效的 UV 三角形
        private static bool IsValidUvTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            return Mathf.Abs(area) > C_BARY_EPSILON;
        }

        // 计算重心 UV
        private static Vector3 ComputeBarycentricUv(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 v0 = b - a;
            Vector2 v1 = c - a;
            Vector2 v2 = p - a;
            
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            
            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) < C_BARY_EPSILON)
            {
                return new Vector3(-1f, -1f, -1f);
            }

            float inv = 1f / denom;
            float v = (d11 * d20 - d01 * d21) * inv;
            float w = (d00 * d21 - d01 * d20) * inv;
            float u = 1f - v - w;
            return new Vector3(u, v, w);
        }

        // 是否是有效的重心坐标
        private static bool IsValidBarycentric(Vector3 bary)
        {
            if (bary.x < -0.001f || bary.y < -0.001f || bary.z < -0.001f)
            {
                return false;
            }

            return true;
        }

        // 计算 ShadowMap 遮挡
        private static float ComputeShadowMapOcclusion(
            Vector3 worldPos,
            CanopyShadowMaskBakeParams bakeParams,
            CanopyShadowMaskShadowMap shadowMap)
        {
            int samples = bakeParams.shadowMapSoftShadow ? 4 : 1;
            float blocked = 0;
            for (int i = 0; i < samples; i++)
            {
                Vector3 origin = worldPos + GetJitter(i, samples) + Vector3.up * 0.02f;
                if (shadowMap.SampleOcclusion(origin) < 0.5f)
                {
                    blocked += 1;
                }
            }

            float strength = Mathf.Clamp01(bakeParams.globalShadowStrength);
            return 1 - strength * (blocked / samples);
        }

        // 映射 Lightmap UV
        private static Vector2 MapLightmapUv(Vector2 uv, Vector4 scaleOffset)
        {
            return new Vector2(
                uv.x * scaleOffset.x + scaleOffset.z,
                uv.y * scaleOffset.y + scaleOffset.w);
        }

        // 获取抖动
        private static Vector3 GetJitter(int index, int count)
        {
            if (count <= 1)
            {
                return Vector3.zero;
            }

            float s = 0.15f;
            switch (index % 4)
            {
                case 0:
                    return new Vector3(s, 0, s);
                case 1:
                    return new Vector3(-s, 0, s);
                case 2:
                    return new Vector3(s, 0, -s);
                default:
                    return new Vector3(-s, 0, -s);
            }
        }

        // 写入 debug 遮罩
        private static string WriteDebugMasks(
            List<CanopyShadowMaskWriteback.ShadowMaskBuffer> buffers, Dictionary<int, Color32[]> debugMasks)
        {
            if (buffers == null || debugMasks == null)
            {
                return string.Empty;
            }

            List<string> paths = new List<string>();
            for (int i = 0; i < buffers.Count; i++)
            {
                CanopyShadowMaskWriteback.ShadowMaskBuffer buffer = buffers[i];
                Color32[] pixels;
                if (buffer == null || !debugMasks.TryGetValue(buffer.lightmapIndex, out pixels))
                {
                    continue;
                }

                string path = GetDebugMaskPath(buffer.canopyAssetPath);
                
                Texture2D texture = new Texture2D(buffer.width, buffer.height, TextureFormat.RGBA32, false, true);
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                
                byte[] bytes = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                File.WriteAllBytes(path, bytes);
                
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                paths.Add(path);
            }

            string result = string.Join(", ", paths.ToArray());
            return result;
        }

        // 获取 debug 遮罩的路径
        private static string GetDebugMaskPath(string canopyAssetPath)
        {
            string dir = Path.GetDirectoryName(canopyAssetPath);
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }

            string name = Path.GetFileNameWithoutExtension(canopyAssetPath) + "_debug.png";
            if (string.IsNullOrEmpty(dir))
            {
                return name;
            }

            return dir + "/" + name;
        }

        /// <summary>
        /// 删除当前场景 lightmap 关联的 debug 纹理
        /// </summary>
        public static string DeleteDebugTextures()
        {
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            if (lightmaps == null || lightmaps.Length == 0)
            {
                return "场景没有 lightmap，无需清理 debug 纹理。";
            }

            // 候选路径队列
            var candidatePaths = new HashSet<string>();
            
            for (int i = 0; i < lightmaps.Length; i++)
            {
                Texture2D shadowMask = lightmaps[i].shadowMask;
                if (shadowMask == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(shadowMask);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                string officialPath = CanopyShadowMaskWriteback.ResolveOfficialAssetPath(assetPath);
                string canopyPath = CanopyShadowMaskWriteback.GetCanopyAssetPath(officialPath);

                string debugMaskPath = GetDebugMaskPath(canopyPath);
                if (!string.IsNullOrEmpty(debugMaskPath))
                {
                    candidatePaths.Add(debugMaskPath);
                }

                string shadowMapDebugPath = CanopyShadowMaskShadowMap.GetShadowMapDebugAssetPath(canopyPath);
                if (!string.IsNullOrEmpty(shadowMapDebugPath))
                {
                    candidatePaths.Add(shadowMapDebugPath);
                }
            }

            if (candidatePaths.Count == 0)
            {
                return "未找到可清理的 debug 纹理路径。";
            }

            var deletedPaths = new List<string>();
            var failedPaths = new List<string>();
            foreach (string path in candidatePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null)
                {
                    continue;
                }

                if (AssetDatabase.DeleteAsset(path))
                {
                    deletedPaths.Add(path);
                }
                else
                {
                    failedPaths.Add(path);
                }
            }

            if (deletedPaths.Count > 0)
            {
                AssetDatabase.Refresh();
            }

            if (deletedPaths.Count == 0 && failedPaths.Count == 0)
            {
                return "未找到可删除的 debug 纹理。";
            }

            var messageParts = new List<string>();
            if (deletedPaths.Count > 0)
            {
                string msg = $"已删除 {deletedPaths.Count} 个 debug 纹理: ";
                msg += "\n" + string.Join(", ", deletedPaths.ToArray());
                
                messageParts.Add(msg);
            }

            if (failedPaths.Count > 0)
            {
                string msg = $"删除失败 {failedPaths.Count} 个: ";
                msg += "\n" + string.Join(", ", failedPaths.ToArray());
                
                messageParts.Add(msg);
            }

            string message = string.Join("\n\n", messageParts.ToArray());
            if (failedPaths.Count > 0)
            {
                Debug.LogError($"[{nameof(CanopyShadowMaskBaker)}] {message}");
            }
            else
            {
                Debug.Log($"[{nameof(CanopyShadowMaskBaker)}] {message}");
            }

            return message;
        }

        /// <summary>
        /// 当用户没有指定时，找到活动场景里最合适的混合方向主光。
        /// </summary>
        public static Light FindDefaultMainLight()
        {
            List<Light> lights = TODUtils.FindObjectsOfTypeInActiveScene<Light>(true);
            Light bestMixed = null;
            float bestMixedIntensity = -1f;
            Light bestBaked = null;
            float bestBakedIntensity = -1f;

            for (int i = 0; i < lights.Count; i++)
            {
                Light light = lights[i];
                if (light == null || light.type != LightType.Directional)
                {
                    continue;
                }

                if (light.lightmapBakeType == LightmapBakeType.Realtime)
                {
                    continue;
                }

                EShadowMaskChannel channel;
                if (light.lightmapBakeType == LightmapBakeType.Mixed
                    && TryGetShadowMaskChannel(light, out channel))
                {
                    if (light.intensity > bestMixedIntensity)
                    {
                        bestMixedIntensity = light.intensity;
                        bestMixed = light;
                    }

                    continue;
                }

                if (light.intensity > bestBakedIntensity)
                {
                    bestBakedIntensity = light.intensity;
                    bestBaked = light;
                }
            }

            if (bestMixed != null)
            {
                return bestMixed;
            }

            return bestBaked;
        }
        
    }
}