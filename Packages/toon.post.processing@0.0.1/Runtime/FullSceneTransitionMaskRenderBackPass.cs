/*******************************************************************************
 * File: FullSceneTransitionMaskRenderBackPass.cs
 * Author: WangYu
 * Date: 2026-01-23
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderConst = ToonPostProcessing.FullSceneTransitionMaskRenderPass.RenderConst;

namespace ToonPostProcessing
{
    public class FullSceneTransitionMaskRenderBackPass : ScriptableRenderPass
    {
        private const string c_RTName = "_FullSceneTransitionBackMaskRT";
        
        private readonly string _profilerTag, _cmdTag;
        
        private FullSceneTransitionMaskSettings _settings;
        private RTHandle _maskRTHandle;

        public FullSceneTransitionMaskRenderBackPass()
        {
            _profilerTag = nameof(FullSceneTransitionMaskRenderBackPass);
            _cmdTag = _profilerTag + "_cmd";
            
            base.profilingSampler = new ProfilingSampler(_profilerTag);
        }

        public bool ExecuteSetup(FullSceneTransitionMaskSettings settings)
        {
            if (settings == null)
            {
                return false;
            }
            _settings = settings;
            if (_settings.backSphereTransform == null || _settings.backRaySphereMaskMaterial == null || _settings.backTransitionMaterial == null)
            {
                return false;
            }

            if (_settings.backUseBlendTex)
            {
                _settings.backTransitionMaterial.EnableKeyword(RenderConst._BLEND_TEX_ON);
                if (_settings.backBlendRT != null)
                {
                    _settings.backTransitionMaterial.SetTexture(RenderConst._BlendTex, settings.backBlendRT);
                }
            }
            else
            {
                _settings.backTransitionMaterial.DisableKeyword(RenderConst._BLEND_TEX_ON);
            }
            
            FullSceneTransitionMaskRenderPass.SetGammaKeyword(_settings.backTransitionMaterial);
            
            base.renderPassEvent = _settings.renderPassEvent;
            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.msaaSamples = (int)MSAASamples.None;
            desc.graphicsFormat = RenderConst.gf;
            desc.depthStencilFormat = GraphicsFormat.None;
            desc.depthBufferBits = (int)DepthBits.None;
            
            XKnightRenderingUtils.ReAllocateIfNeeded(ref _maskRTHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: c_RTName);
            
            ConfigureTarget(_maskRTHandle);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_cmdTag);
            using (new ProfilingScope(cmd, this.profilingSampler))
            {
                Material mat = _settings.backRaySphereMaskMaterial;
                
                // mask
                Transform st = _settings.backSphereTransform;
                Vector3 stScale = st.lossyScale;
                float stRadius = Mathf.Max(stScale.x, Mathf.Max(stScale.y, stScale.z)) * 0.5f;

                mat.SetMatrix(RenderConst._SphereWorldToObject, st.worldToLocalMatrix);
                mat.SetVector(RenderConst._SphereCenter, st.position);
                mat.SetFloat(RenderConst._SphereRadius, stRadius);
                
                mat.SetFloat(RenderConst._PoleThresholdInner, _settings.backPoleThresholdInner);
                mat.SetFloat(RenderConst._PoleThresholdOuter, _settings.backPoleThresholdOuter);
                
                cmd.DrawProcedural(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, 3);
                
                // draw
                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                _settings.backTransitionMaterial.SetTexture(RenderConst._FullSceneTransitionMaskRT, _maskRTHandle);
                cmd.DrawProcedural(Matrix4x4.identity, _settings.backTransitionMaterial, 0, MeshTopology.Triangles, 3);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _maskRTHandle?.Release();
            _maskRTHandle = null;

            _settings = null;
        }
        
        public void OnDisableRendererFeature()
        {
            Shader.DisableKeyword(RenderConst._EXCLUDE_CHARACTER_ON);
        }
    }
}
