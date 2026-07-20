// Created by: WangYu   Date: 2025-12-15

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ToonPostProcessing
{
    public class OutlineDistortRenderPass : ScriptableRenderPass
    {
        private List<ShaderTagId> m_shaderTagIdList = new()
        {
            new ShaderTagId("UniversalForward"),
        };

        private const string c_key_RT = "_OutlineSourceRT";
        
        private readonly string m_profilingTag, m_cmdTag;
        
        private OutlineDistortSettings m_settings;
        private Material m_outlineDistortMaskMat, m_outlineDistortPSMat;
        
        private RTHandle m_sourceTarget;
        private OutlineDistortVolume m_volume;

        private GraphicsFormat m_gfxf;
        private readonly RenderTargetHandle m_outlineSourceRT = RenderTargetHandle.CameraTarget;
        private bool m_hasOutlineSourceRT;
        
        // 多相机调用时的状态管理 >>>>>>>>>>>>>>>>>>>>>>
        private struct CameraTimerState
        {
            public float lastAccessTime;
            public float startTime;
            public Vector2 accumulatedUVOffset;
            public float lastTimer;
        }
        private Dictionary<int, CameraTimerState> m_cameraTimerStates = new();
        private const float CLEANUP_INTERVAL = 5f;
        private float m_lastCleanupTime;
        private List<int> m_needRemoveCameraTimerStates = new();

        private float m_applyAlpha = 1;

        // targetEffectId > 0 时的缓存
        // 收集结果缓存，同帧同 effectId 只遍历一次
        private readonly List<Renderer> m_filteredRenderers = new List<Renderer>();
        private int m_lastCollectFrame = -1;
        private int m_lastCollectEffectId = -1;
        private readonly Dictionary<Renderer, int> m_subMeshCountCache = new Dictionary<Renderer, int>();
        private readonly List<Renderer> m_needRemoveFromSubMeshCache = new List<Renderer>();
        
        
        public OutlineDistortRenderPass()
        {
            m_profilingTag = nameof(OutlineDistortRenderPass);
            m_cmdTag = m_profilingTag + "_cmd";
            base.profilingSampler = new ProfilingSampler(m_profilingTag);
            
            const FormatUsage usage = FormatUsage.Linear | FormatUsage.Render;
            m_gfxf = SystemInfo.IsFormatSupported(GraphicsFormat.R8G8_UNorm, usage) ? GraphicsFormat.R8G8_UNorm : GraphicsFormat.R16G16B16A16_SFloat; // HDR fallback
            m_outlineSourceRT.Init(c_key_RT);
        }
        
        public void Dispose()
        {
            m_settings = null;
            m_outlineDistortMaskMat = null;
            m_outlineDistortPSMat = null;

            m_sourceTarget = null;
            m_volume = null;
            
            m_cameraTimerStates.Clear();
            m_lastCleanupTime = 0;
            m_needRemoveCameraTimerStates.Clear();

            m_filteredRenderers.Clear();
            m_lastCollectFrame = -1;
            m_lastCollectEffectId = -1;
            m_subMeshCountCache.Clear();
            m_needRemoveFromSubMeshCache.Clear();
        }
        
        /// <summary>
        /// 设置合成目标
        /// </summary>
        public void SetBlitTarget(RTHandle source)
        {
            m_sourceTarget = source;
        }
        
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // base.OnCameraCleanup(cmd);

            if (m_hasOutlineSourceRT)
            {
                m_hasOutlineSourceRT = false;
                cmd.ReleaseTemporaryRT(m_outlineSourceRT.id);
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // base.OnCameraSetup(cmd, ref renderingData);
            
            if (m_settings != null && m_settings.drawMask)
            {
                RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = m_gfxf;
                // desc.graphicsFormat = GraphicsFormat.RGBA_ASTC4X4_UFloat;

                cmd.GetTemporaryRT(m_outlineSourceRT.id, desc, FilterMode.Bilinear);

                m_hasOutlineSourceRT = true;
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // base.Configure(cmd, cameraTextureDescriptor);

            if (m_hasOutlineSourceRT)
            {
                ConfigureTarget(m_outlineSourceRT.Identifier());
            }
        }

        public bool ExecuteSetup(OutlineDistortSettings settings)
        {
            m_settings = settings;
            if (m_settings == null)
            {
                return false;
            }
            
            base.renderPassEvent = m_settings.renderPassEvent;

            var vis = VolumeManager.instance.stack;
            m_volume = vis.GetComponent<OutlineDistortVolume>();
            if (m_volume == null || !m_volume.IsActive())
            {
                return false;
            }

            var renderData = m_volume.renderData.value as OutlineDistortRenderData;
            m_outlineDistortMaskMat = renderData?.renderResources?.outlineDistortMaskMat;
            m_outlineDistortPSMat = renderData?.renderResources?.outlineDistortPSMat;
            
            bool result = true;
            result &= m_outlineDistortMaskMat != null;
            result &= m_outlineDistortPSMat != null;
            
            return result;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            bool needDrawMask = m_hasOutlineSourceRT && m_settings.drawMask;
            
            int cameraInstanceId = renderingData.cameraData.camera.GetInstanceID();
            SetupPSData(cameraInstanceId);
            
            CommandBuffer cmd = CommandBufferPool.Get(m_cmdTag);
            using (new ProfilingScope(cmd, this.profilingSampler))
            {
                // 绘制遮罩
                if (needDrawMask)
                {
                    cmd.ClearRenderTarget(true, true, Color.clear, 0);

                    if (m_settings.targetEffectId > 0)
                    {
                        int currentFrame = Time.frameCount;
                        if (m_lastCollectFrame != currentFrame || m_lastCollectEffectId != m_settings.targetEffectId)
                        {
                            m_lastCollectFrame = currentFrame;
                            m_lastCollectEffectId = m_settings.targetEffectId;
                            XKnightScreenEffectIdMaskPass.CollectActiveRenderers(m_filteredRenderers, m_settings.targetEffectId);
                        }

                        for (int i = 0; i < m_filteredRenderers.Count; i++)
                        {
                            Renderer r = m_filteredRenderers[i];
                            if (r == null)
                            {
                                continue;
                            }
                            int subMeshCount = GetSubMeshCountCached(r);
                            for (int sub = 0; sub < subMeshCount; sub++)
                            {
                                cmd.DrawRenderer(r, m_outlineDistortMaskMat, sub, 0);
                            }
                        }
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                    }
                    //else
                    //{
                    //    // 原有逻辑：按 LayerMask 批量绘制
                    //    FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, m_settings.targetLayerMask);

                    //    DrawingSettings drawingSettings = XKnightRenderingUtils.CreateDrawingSettings(m_shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                    //    drawingSettings.enableDynamicBatching = false;
                    //    drawingSettings.enableInstancing = false;
                    //    drawingSettings.overrideMaterial = m_outlineDistortMaskMat;

                    //    RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);
                    //    RendererList rendererList = context.CreateRendererList(ref rendererListParams);
                    //    cmd.DrawRendererList(rendererList);
                    //    context.ExecuteCommandBuffer(cmd);
                    //    cmd.Clear();
                    //}
                }

                // 绘制轮廓
                RenderTargetIdentifier sourceTarget = default;
                bool hasSourceTarget = false;
                if (needDrawMask)
                {
                    sourceTarget = m_outlineSourceRT.Identifier();
                    hasSourceTarget = true;
                }
                else if(m_sourceTarget != null)
                {
                    sourceTarget = m_sourceTarget.nameID;
                    hasSourceTarget = true;
                }
                
                if (hasSourceTarget)
                {
                    RTHandle destTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    cmd.Blit(sourceTarget, destTarget.nameID, m_outlineDistortPSMat);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        
        private void SetupPSData(int cameraInstanceId)
        {
            float nowTime = Time.time;
            
            if (!m_cameraTimerStates.TryGetValue(cameraInstanceId, out CameraTimerState timerState))
            {
                timerState = new CameraTimerState
                {
                    lastAccessTime = nowTime,
                    startTime = nowTime,
                    accumulatedUVOffset = Vector2.zero,
                    lastTimer = 0,
                };
                m_cameraTimerStates[cameraInstanceId] = timerState;
            }
            else
            {
                timerState.lastAccessTime = nowTime;
            }
            
            float existTimer = nowTime - timerState.startTime;
            float deltaTime = existTimer - timerState.lastTimer;
            
            Vector2 distortUVScrollSpeed = m_volume.distortUVScrollSpeed.value;
            
            float offsetTime1 = 0;
            float offsetTime2 = 0;
            bool multipleSampleOn = m_volume.multipleSampleOn.value;
            if (multipleSampleOn)
            {
                offsetTime1 = m_volume.offsetSampleTime1.value;
                offsetTime2 = m_volume.offsetSampleTime2.value;
            }
            
            Vector2 uvOffset = Vector2.zero;
            Vector2 uvOffset2 = Vector2.zero;
            Vector2 uvOffset3 = Vector2.zero;
            
            // =0 就是第1次做计算
            if (timerState.lastTimer == 0)
            {
                uvOffset = Vector2.zero;
            }
            else
            {
                // 累积偏移: 当前偏移 = 上次偏移 + 当前速度 * 时间差
                uvOffset = timerState.accumulatedUVOffset + distortUVScrollSpeed * deltaTime;
            }
            
            // 多次采样
            if (multipleSampleOn)
            {
                uvOffset2 = uvOffset + distortUVScrollSpeed * offsetTime1;
                uvOffset3 = uvOffset + distortUVScrollSpeed * offsetTime2;
            }
            else
            {
                uvOffset2 = uvOffset;
                uvOffset3 = uvOffset;
            }
            
            timerState.accumulatedUVOffset = uvOffset;
            timerState.lastTimer = existTimer;
            m_cameraTimerStates[cameraInstanceId] = timerState; // 把状态写回去
            
            // 定时清理
            float timeDiff = nowTime - m_lastCleanupTime;
            if (timeDiff > CLEANUP_INTERVAL)
            {
                m_lastCleanupTime = nowTime;
                CleanupUnusedCameraStates(nowTime);
            }
            
            // 这些是需要累积的参数
            if (m_outlineDistortPSMat.HasProperty(OutlineDistortShaderProperties._AccumulatedUVOffset))
            {
                m_outlineDistortPSMat.SetVector(OutlineDistortShaderProperties._AccumulatedUVOffset, new Vector4(uvOffset.x, uvOffset.y, 0, 0));
            }
            if (m_outlineDistortPSMat.HasProperty(OutlineDistortShaderProperties._AccumulatedUVOffset2))
            {
                m_outlineDistortPSMat.SetVector(OutlineDistortShaderProperties._AccumulatedUVOffset2, new Vector4(uvOffset2.x, uvOffset2.y, 0, 0));
            }
            if (m_outlineDistortPSMat.HasProperty(OutlineDistortShaderProperties._AccumulatedUVOffset3))
            {
                m_outlineDistortPSMat.SetVector(OutlineDistortShaderProperties._AccumulatedUVOffset3, new Vector4(uvOffset3.x, uvOffset3.y, 0, 0));
            }
            
            // 这个变量跟着 setting 里的开关状态走
            if (m_outlineDistortPSMat.HasProperty(OutlineDistortShaderProperties._GradientMaskOn))
            {
                int val = m_settings.drawMask ? 1 : 0;
                m_outlineDistortPSMat.SetFloat(OutlineDistortShaderProperties._GradientMaskOn, val);
            }

            if (m_outlineDistortPSMat.HasProperty(OutlineDistortShaderProperties._OutlineAlpha))
            {
                m_outlineDistortPSMat.SetFloat(OutlineDistortShaderProperties._OutlineAlpha, m_applyAlpha);
            }
        }
        
        // 清除长时间未使用的摄像机状态
        private void CleanupUnusedCameraStates(float nowTime)
        {
            m_needRemoveCameraTimerStates.Clear();
            foreach (var iter in m_cameraTimerStates)
            {
                int cid = iter.Key;
                CameraTimerState cts = iter.Value;
                
                float diffTime = nowTime - cts.lastAccessTime;
                if (diffTime > CLEANUP_INTERVAL)
                {
                    m_needRemoveCameraTimerStates.Add(cid);
                }
            }

            foreach (int key in m_needRemoveCameraTimerStates)
            {
                m_cameraTimerStates.Remove(key);
            }

            // 清理targetEffectId > 0 时的缓存
            if (m_subMeshCountCache.Count > 0)
            {
                m_needRemoveFromSubMeshCache.Clear();
                foreach (var iter in m_subMeshCountCache)
                {
                    if (iter.Key == null)
                    {
                        m_needRemoveFromSubMeshCache.Add(iter.Key);
                    }
                }
                foreach (Renderer key in m_needRemoveFromSubMeshCache)
                {
                    m_subMeshCountCache.Remove(key);
                }
            }
        }

        public void ApplyAlpha(float alpha)
        {
            m_applyAlpha = alpha;
        }

        private int GetSubMeshCountCached(Renderer renderer)
        {
            if (!m_subMeshCountCache.TryGetValue(renderer, out int count))
            {
                count = QuerySubMeshCount(renderer);
                m_subMeshCountCache[renderer] = count;
            }
            return count;
        }


        private static int QuerySubMeshCount(Renderer renderer)
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                mesh = skinnedMeshRenderer.sharedMesh;
            }
            else if (renderer is MeshRenderer)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    mesh = meshFilter.sharedMesh;
                }
            }
            return mesh != null ? Mathf.Max(1, mesh.subMeshCount) : 1;
        }

    }
}