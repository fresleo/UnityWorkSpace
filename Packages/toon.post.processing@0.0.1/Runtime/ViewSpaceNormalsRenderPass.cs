// Created By: WangYu  Date: 2024-08-05

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace ToonPostProcessing
{
    public class ViewSpaceNormalsRenderPass : ScriptableRenderPass
    {
        private List<ShaderTagId> m_shaderTagIdList = new List<ShaderTagId>()
        {
            // new ShaderTagId("UniversalForward"),
            new ShaderTagId("ViewSpaceNormals"),
        };

        private readonly string _profilerTag, _cmdTag;
        
        // private Material m_viewSpaceNormalsMat;
        
        private ViewSpaceNormalsRendererFeature.Settings m_settings;
        
        private RTHandle m_maskRT, m_maskDepthRT;
        private string m_bufferName, m_bufferDepthName; // 申请的 Buffer 名字

        
        public ViewSpaceNormalsRenderPass()
        {
            _profilerTag = nameof(ViewSpaceNormalsRenderPass);
            _cmdTag = _profilerTag + "_cmd";
            
            base.profilingSampler = new ProfilingSampler(_profilerTag);
        }

        public void Dispose()
        {
            m_maskRT?.Release();
            m_maskRT = null;
            m_maskDepthRT?.Release();
            m_maskDepthRT = null;
            
            // CoreUtils.Destroy(m_viewSpaceNormalsMat);
            // m_viewSpaceNormalsMat = null;
        }

        public void CreateAssets()
        {
            // AssetUtils.CreateEngineMaterial(ref m_viewSpaceNormalsMat, "ToonPostProcessingShader/ViewSpaceNormals");
        }

        public void Setup(ViewSpaceNormalsRendererFeature.Settings settings)
        {
            m_settings = settings;
            m_bufferName = "_ViewSpaceNormalsTexture";
            m_bufferDepthName = $"{m_bufferName}_Depth";
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor desc = cameraTextureDescriptor;
            
            GraphicsFormatUtils.SetColorDescriptor(ref desc, false, 4);
            XKnightRenderingUtils.ReAllocateIfNeeded(ref m_maskRT, desc, FilterMode.Point, TextureWrapMode.Clamp, name: m_bufferName);

            GraphicsFormatUtils.SetDepthDescriptor(ref desc, true);
            XKnightRenderingUtils.ReAllocateIfNeeded(ref m_maskDepthRT, desc, name: m_bufferDepthName);
            
            // 切换渲染目标
            ConfigureTarget(m_maskRT, m_maskDepthRT);
            ConfigureClear(ClearFlag.All, Color.clear);
            
            cmd.SetGlobalTexture(m_bufferName, m_maskRT);
            cmd.SetGlobalTexture(m_bufferDepthName, m_maskDepthRT);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            base.renderPassEvent = m_settings.renderPassEvent + m_settings.renderPassEventOffset;
            
            // 执行 cmd 指令
            CommandBuffer cmd = CommandBufferPool.Get(_cmdTag);
            using (new ProfilingScope(cmd, this.profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawDs = XKnightRenderingUtils.CreateDrawingSettings(m_shaderTagIdList, ref renderingData, sortingCriteria);
                // drawDs.overrideMaterial = m_viewSpaceNormalsMat;
                FilteringSettings drawFs = new FilteringSettings(RenderQueueRangeExt.opaque, m_settings.drawLayerMask);
                
                context.DrawRenderers(renderingData.cullResults, ref drawDs, ref drawFs);
            }
            // 不要写在 ProfilingScope 里，对 FrameDebugger 有影响
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}