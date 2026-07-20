// Created By: WangYu  Date: 2024-07-16

using ToonPostProcessing.Volumes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace ToonPostProcessing
{
    public class ToonPostProcessingRenderPass : ScriptableRenderPass
    {
        private static class WaterColorPid
        {
            public static readonly int _WaterColor = Shader.PropertyToID("_WaterColor");
            public static readonly int _XRadius = Shader.PropertyToID("_XRadius");
            public static readonly int _YRadius = Shader.PropertyToID("_YRadius");
        }
        
        private static class SobelOutlinePid
        {
            public static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
            public static readonly int _OutlineData = Shader.PropertyToID("_OutlineData");
        }
        
        private static class PreObjectIdOutlinePid
        {
            public static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
            public static readonly int _OutlineData = Shader.PropertyToID("_OutlineData");
            public static readonly int _BlurScale = Shader.PropertyToID("_BlurScale");
            public static readonly int _OutlineTexture = Shader.PropertyToID("_OutlineTexture");

            public const string OUTLINE_MIN_SEPARATION_ON = "OUTLINE_MIN_SEPARATION_ON";
        }
        
        private static class ViewSpaceNormalsOutlinePid
        {
            public static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
            public static readonly int _OutlineData_0 = Shader.PropertyToID("_OutlineData_0");
            public static readonly int _OutlineData_1 = Shader.PropertyToID("_OutlineData_1");
            public static readonly int _OutlineData_2 = Shader.PropertyToID("_OutlineData_2");
            
            public static readonly int _OutlineTexture = Shader.PropertyToID("_OutlineTexture");

            public const string _DIRECT_BLEND = "_DIRECT_BLEND";
        }

        private readonly string _profilerTag, _cmdTag;
        
        private ToonPostProcessingRendererFeature.Settings m_settings;
        
#if UNITY_EDITOR
        private RTHandle m_editorCameraColorTargetHandle;
#endif
        private BufferWheel m_bw_targetRT, m_bw_tempRT;

        // 材质球
        private Material m_wcMat, m_soMat, m_poioMat, m_vsnoMat;
        
        // Volume 组件
        // 水彩的控制
        private WaterColor m_wcVolume;
        // 不同的描边控制
        private SobelOutline m_soVolume;
        private PreObjectIdOutline m_poioVolume;
        private ViewSpaceNormalsOutline m_vsnoVolume;
        
        
        public ToonPostProcessingRenderPass()
        {
            _profilerTag = nameof(ToonPostProcessingRenderPass);
            _cmdTag = _profilerTag + "_cmd";
            
            base.profilingSampler = new ProfilingSampler(_profilerTag);
            
            m_bw_targetRT = new BufferWheel();
            m_bw_tempRT = new BufferWheel();
        }

        public void Dispose()
        {
            m_bw_targetRT?.Dispose();
            m_bw_targetRT = null;
            m_bw_tempRT?.Dispose();
            m_bw_tempRT = null;
            
            CoreUtils.Destroy(m_wcMat);
            m_wcMat = null;
            CoreUtils.Destroy(m_soMat);
            m_soMat = null;
            CoreUtils.Destroy(m_poioMat);
            m_poioMat = null;
            CoreUtils.Destroy(m_vsnoMat);
            m_vsnoMat = null;
        }
        
#if UNITY_EDITOR
        public void SetEditorCameraColorTarget(RTHandle cameraColorTargetRTHandle)
        {
            m_editorCameraColorTargetHandle = cameraColorTargetRTHandle;
        }
#endif

        /// <summary>
        /// 执行前设置
        /// </summary>
        public bool ExecuteSetup(
            ToonPostProcessingRendererFeature.Settings settings,
            WaterColor wcV, 
            SobelOutline soV, PreObjectIdOutline poioV, ViewSpaceNormalsOutline vsnoV)
        {
            m_settings = settings;
            
            m_wcVolume = wcV;
            m_soVolume = soV;
            m_poioVolume = poioV;
            m_vsnoVolume = vsnoV;

            if (m_settings?.waterColorGroupRendererData != null)
            {
                m_wcMat = CoreUtils.CreateEngineMaterial(m_settings.waterColorGroupRendererData.renderResources.waterColorV2Shader);
                m_soMat = CoreUtils.CreateEngineMaterial(m_settings.waterColorGroupRendererData.renderResources.sobelOutlineShader);
                m_poioMat = CoreUtils.CreateEngineMaterial(m_settings.waterColorGroupRendererData.renderResources.preObjectIdOutlineShader);
                m_vsnoMat = CoreUtils.CreateEngineMaterial(m_settings.waterColorGroupRendererData.renderResources.viewSpaceNormalsOutlineShader);
            }
            
            bool result = m_settings != null && (m_wcVolume != null || m_soVolume != null || m_poioVolume != null || m_vsnoVolume != null);
            return result;
        }
        
        private void CheckVolume(
            out bool wcVb, 
            out bool soVb, out bool poioVb, out bool vsnoVb)
        {
            wcVb = m_wcVolume != null && m_wcVolume.IsActive;
            soVb = m_soVolume != null && m_soVolume.IsActive;
            poioVb = m_poioVolume != null && m_poioVolume.IsActive;
            vsnoVb = m_vsnoVolume != null && m_vsnoVolume.IsActive;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor desc = cameraTextureDescriptor;
            
            GraphicsFormatUtils.SetColorDescriptor(ref desc, false, 4);
            
            m_bw_targetRT.ReAllocateIfNeeded(desc, FilterMode.Point, TextureWrapMode.Clamp, "_ToonPostProcessing_BW_0");
            m_bw_tempRT.ReAllocateIfNeeded(desc, FilterMode.Point, TextureWrapMode.Clamp, "_ToonPostProcessing_BW_1");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 改变 Pass 的执行时机
            int rpe = (int)RenderPassEvent.BeforeRenderingSkybox;
            // MRT 时，后处理夹在场景和角色的绘制 Pass 之间，以达到排除角色的目的
            if (XKnightRenderPipeline.asset.MRTBuffer)
            {
                rpe = (int)RenderPassEvent.BeforeRenderingOpaques + 1;
            }
            base.renderPassEvent = (RenderPassEvent)rpe;
            
            // 执行 cmd 指令
            CommandBuffer cmd = CommandBufferPool.Get(_cmdTag);
            using (new ProfilingScope(cmd, this.profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SetMaterialProperties(cmd, ref renderingData);
                ExecuteRender(cmd, ref renderingData);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        
        private float GetCameraFarClipPlane(ref RenderingData renderingData)
        {
            float cameraFarClipPlane = renderingData.cameraData.camera.farClipPlane + 1E-10f;
            return cameraFarClipPlane;
        }
        
        private float GetRenderScale(ref RenderingData renderingData)
        {
            // SceneView 相机不受 renderScale 影响
            float renderScale = renderingData.cameraData.isSceneViewCamera ? 1 : XKnightRenderPipeline.asset.renderScale;
            return renderScale;
        }
        
        private void SetMaterialProperties(CommandBuffer cmd, ref RenderingData renderingData)
        {
            float cameraFarClipPlane = GetCameraFarClipPlane(ref renderingData);
            float renderScale = GetRenderScale(ref renderingData);
            
            if (m_wcVolume != null && m_wcMat != null)
            {
                if (m_wcMat.HasProperty(WaterColorPid._WaterColor))
                {
                    m_wcMat.SetColor(WaterColorPid._WaterColor, m_wcVolume.waterColor.value);
                }
                
                if (m_wcMat.HasProperty(WaterColorPid._XRadius))
                {
                    m_wcMat.SetFloat(WaterColorPid._XRadius, m_wcVolume.xRadius.value);
                }
                
                if (m_wcMat.HasProperty(WaterColorPid._YRadius))
                {
                    m_wcMat.SetFloat(WaterColorPid._YRadius, m_wcVolume.yRadius.value);
                }
            }

            if (m_soVolume != null && m_soMat != null)
            {
                if (m_soMat.HasProperty(SobelOutlinePid._OutlineColor))
                {
                    m_soMat.SetColor(SobelOutlinePid._OutlineColor, m_soVolume.outlineColor.value);
                }

                if (m_soMat.HasProperty(SobelOutlinePid._OutlineData))
                {
                    float outlineDistanceFade = Mathf.Clamp(m_soVolume.outlineDistanceFade.value, 0, cameraFarClipPlane)  / cameraFarClipPlane;
                    
                    Vector4 outlineData = new Vector4(
                        m_soVolume.outlineThickness.value,
                        outlineDistanceFade,
                        m_soVolume.outlineEdgeMultiplier.value,
                        m_soVolume.outlineEdgeBias.value
                        );
                    m_soMat.SetVector(SobelOutlinePid._OutlineData, outlineData);
                }
            }

            if (m_poioVolume != null && m_poioMat != null)
            {
                if (m_poioMat.HasProperty(PreObjectIdOutlinePid._OutlineColor))
                {
                    m_poioMat.SetColor(PreObjectIdOutlinePid._OutlineColor, m_poioVolume.outlineColor.value);
                }
                
                if (m_poioMat.HasProperty(PreObjectIdOutlinePid._OutlineData))
                {
                    float outlineDistanceFade = Mathf.Clamp(m_poioVolume.outlineDistanceFade.value, 0, cameraFarClipPlane) / cameraFarClipPlane;
                    
                    Vector4 outlineData = new Vector4(
                        m_poioVolume.outlineIntensityMultiplier.value, 
                        outlineDistanceFade,
                        m_poioVolume.outlineMinSeparation.value, 
                        m_poioVolume.outlineWidth.value * renderScale
                        );
                    m_poioMat.SetVector(PreObjectIdOutlinePid._OutlineData, outlineData);
                }
            }

            if (m_vsnoVolume != null && m_vsnoMat != null)
            {
                if (m_vsnoMat.HasProperty(ViewSpaceNormalsOutlinePid._OutlineColor))
                {
                    m_vsnoMat.SetColor(ViewSpaceNormalsOutlinePid._OutlineColor, m_vsnoVolume.outlineColor.value);
                }

                if (m_vsnoMat.HasProperty(ViewSpaceNormalsOutlinePid._OutlineData_0))
                {
                    float outlineDistanceFade = Mathf.Clamp(m_vsnoVolume.outlineDistanceFade.value, 0, cameraFarClipPlane) / cameraFarClipPlane;
                    
                    var outlineData0 = new Vector4(
                        outlineDistanceFade,
                        m_vsnoVolume.outlineScale.value,
                        m_vsnoVolume.depthThreshold.value,
                        m_vsnoVolume.depthDiffMultiplier.value
                    );
                    m_vsnoMat.SetVector(ViewSpaceNormalsOutlinePid._OutlineData_0, outlineData0);
                }
                
                if (m_vsnoMat.HasProperty(ViewSpaceNormalsOutlinePid._OutlineData_1))
                {
                    var outlineData0 = new Vector4(
                        m_vsnoVolume.normalThreshold.value,
                        m_vsnoVolume.steepAngleThreshold.value,
                        m_vsnoVolume.steepAngleMultiplier.value,
                        0
                        );
                    m_vsnoMat.SetVector(ViewSpaceNormalsOutlinePid._OutlineData_1, outlineData0);
                }
            }
        }
        
        private void ExecuteRender(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 水彩 blit
            RTHandle cameraColorTargetRTHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
#if UNITY_EDITOR
            if (renderingData.cameraData.cameraType != CameraType.Game)
            {
                cameraColorTargetRTHandle = m_editorCameraColorTargetHandle;
            }
#endif
            
            // 旧的单后期 Blit 方式
            //cmd.Blit(null, cameraColorTargetRTHandle, m_waterColorMat, 0);
            
            // 拷贝颜色
            RTHandle targetRT = m_bw_targetRT.GetLeftBuffer();
            CoreUtils.SetRenderTarget(cmd, targetRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            ExecuteCopyColorPass(cmd, cameraColorTargetRTHandle);

            ExecuteOutlinePass(cmd, ref renderingData, cameraColorTargetRTHandle); // 描边
            ExecuteWaterColorPass(cmd, cameraColorTargetRTHandle); // 水彩
        }
        
        private void ExecuteOutlinePass(CommandBuffer cmd, ref RenderingData renderingData, RTHandle cameraColorTargetRTHandle)
        {
            CheckVolume(
                out bool wcVb, 
                out bool soVb, out bool poioVb, out bool vsnoVb);
            
            RTHandle sourceRT, targetRT;
            RTHandle leftRT, rightRT;
            
            // ViewSpaceNormals 描边
            if (vsnoVb)
            {
                bool enableAntiAliasing = m_vsnoVolume.enableAntiAliasing.value;
                CoreUtils.SetKeyword(m_vsnoMat, ViewSpaceNormalsOutlinePid._DIRECT_BLEND, !enableAntiAliasing);
                
                // 开启抗锯齿
                if (enableAntiAliasing)
                {
                    leftRT = m_bw_tempRT.GetLeftBuffer();
                    rightRT = m_bw_tempRT.GetRightBuffer();
                    Blitter.BlitCameraTexture(cmd, leftRT, rightRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_vsnoMat, 2);
                    m_bw_tempRT.SwapBuffer();
                    
                    leftRT = m_bw_tempRT.GetLeftBuffer();
                    rightRT = m_bw_tempRT.GetRightBuffer();
                    Blitter.BlitCameraTexture(cmd, leftRT, rightRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_vsnoMat, 1);
                    
                    m_vsnoMat.SetTexture(PreObjectIdOutlinePid._OutlineTexture, rightRT); // 设置到材质上
                    
                    sourceRT = m_bw_targetRT.GetLeftBuffer();
                    if (poioVb || soVb || wcVb)
                    {
                        targetRT = m_bw_targetRT.GetRightBuffer();
                    }
                    else
                    {
                        targetRT = cameraColorTargetRTHandle;
                    }
                    
                    CoreUtils.SetRenderTarget(cmd, targetRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    ExecuteMainPass(cmd, sourceRT, m_vsnoMat, 0);
                    m_bw_targetRT.SwapBuffer();
                }
                else
                {
                    sourceRT = m_bw_targetRT.GetLeftBuffer();
                    if (poioVb || soVb || wcVb)
                    {
                        targetRT = m_bw_targetRT.GetRightBuffer();
                    }
                    else
                    {
                        targetRT = cameraColorTargetRTHandle;
                    }
                    
                    CoreUtils.SetRenderTarget(cmd, targetRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    ExecuteMainPass(cmd, sourceRT, m_vsnoMat, 3);
                    m_bw_targetRT.SwapBuffer();
                }
            }
            
            // PreObjectId 描边
            if (poioVb)
            {
                // Detection Pass
                CoreUtils.SetKeyword(m_poioMat, PreObjectIdOutlinePid.OUTLINE_MIN_SEPARATION_ON, m_poioVolume.outlineMinSeparation.value > 1);
                
                leftRT = m_bw_tempRT.GetLeftBuffer();
                rightRT = m_bw_tempRT.GetRightBuffer();
                Blitter.BlitCameraTexture(cmd, leftRT, rightRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_poioMat, 0);
                m_bw_tempRT.SwapBuffer();

                // Diffusion Pass
                if (m_poioVolume.enableAntiAliasing.value)
                {
                    leftRT = m_bw_tempRT.GetLeftBuffer();
                    rightRT = m_bw_tempRT.GetRightBuffer();
                    Blitter.BlitCameraTexture(cmd, leftRT, rightRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_poioMat, 5);
                    m_bw_tempRT.SwapBuffer();
                }
                
                // Blur Pass
                int size;
                RenderTextureDescriptor sourceDesc = cameraColorTargetRTHandle.rt.descriptor;

                float renderScale = GetRenderScale(ref renderingData);
                int baseSize = (int)(512 * renderScale);
                
                float blurIntensity = m_poioVolume.blurIntensity.value;
                if (blurIntensity < 1f)
                {
                    size = (int)Mathf.Lerp(sourceDesc.width, baseSize, blurIntensity);
                }
                else
                {
                    size = (int)(baseSize / blurIntensity);
                }

                float blurScale = blurIntensity > 1f ? 1f : blurIntensity;
                float ratio = (float)sourceDesc.width / size;
                //Debug.LogError($"blurScale = {blurScale} | ratio = {ratio}");
                
                // Horizontally
                m_poioMat.SetFloat(PreObjectIdOutlinePid._BlurScale, blurScale * ratio);
                
                leftRT = m_bw_tempRT.GetLeftBuffer();
                rightRT = m_bw_tempRT.GetRightBuffer();
                Blitter.BlitCameraTexture(cmd, leftRT, rightRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_poioMat, 1);
                m_bw_tempRT.SwapBuffer();
                
                // Vertically
                leftRT = m_bw_tempRT.GetLeftBuffer();
                rightRT = m_bw_tempRT.GetRightBuffer();
                m_poioMat.SetFloat(PreObjectIdOutlinePid._BlurScale, blurScale);
                Blitter.BlitCameraTexture(cmd, leftRT, rightRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_poioMat, 2);
                m_bw_tempRT.SwapBuffer();
                
                // Blend Pass
                leftRT = m_bw_tempRT.GetLeftBuffer();
                rightRT = m_bw_tempRT.GetRightBuffer();
                Blitter.BlitCameraTexture(cmd, leftRT, rightRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_poioMat, 3);
                
                m_poioMat.SetTexture(PreObjectIdOutlinePid._OutlineTexture, rightRT); // 设置到材质上
                
                // Combine Pass
                sourceRT = m_bw_targetRT.GetLeftBuffer();
                if (soVb || wcVb)
                {
                    targetRT = m_bw_targetRT.GetRightBuffer();
                }
                else
                {
                    targetRT = cameraColorTargetRTHandle;
                }
                
                CoreUtils.SetRenderTarget(cmd, targetRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                ExecuteMainPass(cmd, sourceRT, m_poioMat, 4);
                m_bw_targetRT.SwapBuffer();
            }
            
            // Sobel 描边
            if (soVb)
            {
                sourceRT = m_bw_targetRT.GetLeftBuffer();
                if (wcVb)
                {
                    targetRT = m_bw_targetRT.GetRightBuffer();
                }
                else
                {
                    targetRT = cameraColorTargetRTHandle;
                }
                
                CoreUtils.SetRenderTarget(cmd, targetRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                ExecuteMainPass(cmd, sourceRT, m_soMat, 0);
                m_bw_targetRT.SwapBuffer();
            }
        }
        
        private void ExecuteWaterColorPass(CommandBuffer cmd, RTHandle cameraColorTargetRTHandle)
        {
            CheckVolume(
                out bool wcVb, 
                out bool soVb, out bool poioVb, out bool vsnoVb);
            if (!wcVb)
            {
                return;
            }
            
            RTHandle sourceRT, targetRT;

            sourceRT = m_bw_targetRT.GetLeftBuffer();
            targetRT = cameraColorTargetRTHandle;
            
            CoreUtils.SetRenderTarget(cmd, targetRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            ExecuteMainPass(cmd, sourceRT, m_wcMat, 0);
        }
        
        
        // 核心渲染逻辑 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private static MaterialPropertyBlock s_SharedPropertyBlock = new();
        
        private static void ExecuteCopyColorPass(CommandBuffer cmd, RTHandle sourceTexture)
        {
            Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
        }

        private static void ExecuteMainPass(CommandBuffer cmd, RTHandle sourceTexture, Material material, int passIndex)
        {
            s_SharedPropertyBlock.Clear();
            if (sourceTexture != null)
            {
                s_SharedPropertyBlock.SetTexture(ShaderPropertyId.blitTexture, sourceTexture);
            }

            // We need to set the "_BlitScaleBias" uniform for user materials with shaders relying on core Blit.hlsl to work
            s_SharedPropertyBlock.SetVector(ShaderPropertyId.blitScaleBias, new Vector4(1, 1, 0, 0));

            cmd.DrawProcedural(Matrix4x4.identity, material, passIndex, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }
        
    }
}