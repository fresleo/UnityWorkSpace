using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StencilDebugger
{
    [Tooltip("使模版缓冲区可视化")]
    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Stencil Debug")]
#if UNITY_6000_0_OR_NEWER
    [SupportedOnRenderer(typeof(UniversalRendererData))]
#endif
    public class StencilDebug : ScriptableRendererFeature
    {
#if UNITY_EDITOR
        
        private class StencilDebugPass : ScriptableRenderPass
        {
            private ComputeShader m_debug;
            private int m_debugKernel;
            private float m_scale, m_margin;
            private readonly ProfilingSampler m_debugSampler = new(nameof(StencilDebugPass));

            private static int DivRoundUp(int x, int y) => (x + y - 1) / y;

            public void Setup(ComputeShader debugShader, float debugScale, float debugMargin)
            {
                m_debug = debugShader;
                m_scale = debugScale;
                m_margin = debugMargin;

                m_debugKernel = m_debug.FindKernel("StencilDebug");
            }
            
#if UNITY_6000_0_OR_NEWER
            private class PassData
            {
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                
                TextureHandle colorHandle;
                TextureHandle stencilHandle;
                TextureHandle debugHandle;

                // 1. Generate.
                // -> Generate stencil texture.
                using (var builder = renderGraph.AddComputePass<PassData>(ShaderPassName.Generate, out _, profilingSampler))
                {
                    colorHandle = resourceData.activeColorTexture;
                    stencilHandle = resourceData.activeDepthTexture;

                    var desc = new TextureDesc(cameraData.cameraTargetDescriptor)
                    {
                        name = Buffer.StencilDebug,
                        colorFormat = cameraData.cameraTargetDescriptor.graphicsFormat,
                        enableRandomWrite = true
                    };
                    debugHandle = renderGraph.CreateTexture(new TextureDesc(desc));

                    builder.UseTexture(colorHandle);
                    builder.UseTexture(stencilHandle);
                    builder.UseTexture(debugHandle, AccessFlags.ReadWrite);

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(false);

                    builder.SetRenderFunc((PassData _, ComputeGraphContext context) =>
                    {
                        var cmd = context.cmd;

                        cmd.SetComputeFloatParam(m_debug, ShaderPropertyId.Scale, m_scale);
                        cmd.SetComputeFloatParam(m_debug, ShaderPropertyId.Margin, m_margin);

                        cmd.SetComputeTextureParam(m_debug, m_debugKernel, Buffer.CameraColor, colorHandle, 0);
                        cmd.SetComputeTextureParam(m_debug, m_debugKernel, Buffer.Stencil, stencilHandle, 0, RenderTextureSubElement.Stencil);
                        cmd.SetComputeTextureParam(m_debug, m_debugKernel, Buffer.StencilDebug, debugHandle);

                        cmd.DispatchCompute(m_debug, m_debugKernel, DivRoundUp(cameraData.scaledWidth, 8), DivRoundUp(cameraData.scaledHeight, 8), 1);
                    });
                }

                // 2. Compose.
                // -> Compose stencil texture with scene.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(ShaderPassName.Compose, out _, profilingSampler))
                {
                    builder.UseTexture(debugHandle);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(false);

                    builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, debugHandle, new Vector4(1, 1, 0, 0), 0, false);
                    });
                }
            }
#endif // UNITY_6000_0_OR_NEWER
            
            private RTHandle m_cameraDepthRTHandle;
            private RTHandle m_debugRTHandle;

            #pragma warning disable 618, 672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthStencilFormat = GraphicsFormat.None;
                desc.enableRandomWrite = true;
                
                XKnightRenderingUtils.ReAllocateIfNeeded(ref m_debugRTHandle, desc);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_debugSampler))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    
                    var colorHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    var stencilHandle = m_cameraDepthRTHandle;
                    var debugHandle = m_debugRTHandle;

                    cmd.SetComputeFloatParam(m_debug, ShaderPropertyId.Scale, m_scale);
                    cmd.SetComputeFloatParam(m_debug, ShaderPropertyId.Margin, m_margin);

                    cmd.SetComputeTextureParam(m_debug, m_debugKernel, Buffer.CameraColor, colorHandle, 0);
                    cmd.SetComputeTextureParam(m_debug, m_debugKernel, Buffer.Stencil, stencilHandle, 0, RenderTextureSubElement.Stencil);
                    cmd.SetComputeTextureParam(m_debug, m_debugKernel, Buffer.StencilDebug, debugHandle);

                    cmd.DispatchCompute(m_debug, m_debugKernel, DivRoundUp(renderingData.cameraData.cameraTargetDescriptor.width, 8), DivRoundUp(renderingData.cameraData.cameraTargetDescriptor.height, 8), 1);

                    Blitter.BlitTexture(cmd, debugHandle, new Vector4(1, 1, 0, 0), 0, false);
                }
                
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            #pragma warning restore 618, 672
            
            public void SetTarget(RTHandle depth)
            {
                m_cameraDepthRTHandle = depth;
            }
            
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException(nameof(cmd));
                }

                m_cameraDepthRTHandle = null;
            }

            public void Dispose()
            {
                m_debugRTHandle?.Release();
            }
        }
        
        // 序列化设置
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
        public bool showInSceneView = true;
        [Range(0.0f, 100.0f)] public float scale = 50;
        [Range(0.0f, 1.0f)] public float margin = 1;
        
        private StencilDebugPass m_stencilDebugPass;
        private ComputeShader m_shader;
        
        protected override void Dispose(bool disposing)
        {
            m_stencilDebugPass?.Dispose();
            m_stencilDebugPass = null;
        }

        private void OnDestroy()
        {
            m_stencilDebugPass?.Dispose();
        }
        
#endif // UNITY_EDITOR
        
        public override void Create()
        {
#if UNITY_EDITOR
            
            var shaderPath = AssetDatabase.GUIDToAssetPath(ShaderPath.DebugGuid);
            m_shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(shaderPath);
            
            m_stencilDebugPass ??= new StencilDebugPass();
            
#endif // UNITY_EDITOR
        }
        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            
            // 不为某些视图渲染。
            if (renderingData.cameraData.cameraType == CameraType.Preview
                || renderingData.cameraData.cameraType == CameraType.Reflection
                || renderingData.cameraData.cameraType == CameraType.SceneView && !showInSceneView
#if UNITY_6000_0_OR_NEWER
                || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData)
#endif
                )
            {
                return;
            }

            if (m_shader == null)
            {
                Debug.LogWarning("无法加载所需的 compute shader ，模版调试不会渲染。");
                return;
            }

            m_stencilDebugPass.Setup(m_shader, scale, margin);
            m_stencilDebugPass.renderPassEvent = injectionPoint;
            renderer.EnqueuePass(m_stencilDebugPass);
            
#endif // UNITY_EDITOR
        }
        
#if UNITY_EDITOR
        
        #pragma warning disable 618, 672
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            m_stencilDebugPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_stencilDebugPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            m_stencilDebugPass.SetTarget(renderer.cameraDepthTargetHandle);
        }
        #pragma warning restore 618, 672
        
#endif // UNITY_EDITOR
        
    }
}