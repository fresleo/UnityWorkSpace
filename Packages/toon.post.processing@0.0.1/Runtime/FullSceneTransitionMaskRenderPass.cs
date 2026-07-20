/*******************************************************************************
 * File: FullSceneTransitionMaskRenderPass.cs
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

namespace ToonPostProcessing
{
    public class FullSceneTransitionMaskRenderPass : ScriptableRenderPass
    {
        public class RenderConst
        {
            public static readonly int 
                _SphereWorldToObject = Shader.PropertyToID("_SphereWorldToObject")
                , _SphereCenter = Shader.PropertyToID("_SphereCenter")
                , _SphereRadius = Shader.PropertyToID("_SphereRadius");

            public static readonly int
                _PoleThresholdInner = Shader.PropertyToID("_PoleThresholdInner")
                , _PoleThresholdOuter = Shader.PropertyToID("_PoleThresholdOuter")
                , _EdgeWidth = Shader.PropertyToID("_EdgeWidth");

            public static readonly int _FullSceneTransitionMaskRT = Shader.PropertyToID("_FullSceneTransitionMaskRT");
            public static readonly int _FillFogFlowDirection = Shader.PropertyToID("_FillFogFlowDirection");
            
            public const string _BLEND_TEX_ON = "_BLEND_TEX_ON";
            public static readonly int _BlendTex = Shader.PropertyToID("_BlendTex");

            public const string _EXCLUDE_CHARACTER_ON = "_EXCLUDE_CHARACTER_ON", _NEED_LINEAR_TO_SRGB = "_NEED_LINEAR_TO_SRGB";
            
            public static readonly GraphicsFormat gf = 
                SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, FormatUsage.Linear | FormatUsage.Render) 
                    ? GraphicsFormat.R8_UNorm 
                    : GraphicsFormat.R16G16B16A16_SFloat;
        }
        
        private const string c_RTName = "_FullSceneTransitionMaskRT";
        
        private readonly string _profilerTag, _cmdTag;
        
        private FullSceneTransitionMaskSettings _settings;
        private RTHandle _maskRTHandle;

        public FullSceneTransitionMaskRenderPass()
        {
            _profilerTag = nameof(FullSceneTransitionMaskRenderPass);
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
            if (_settings.sphereTransform == null || _settings.raySphereMaskMaterial == null || _settings.transitionMaterial == null)
            {
                return false;
            }

            if (_settings.useBlendTex)
            {
                _settings.transitionMaterial.EnableKeyword(RenderConst._BLEND_TEX_ON);
                if (_settings.blendRT != null)
                {
                    _settings.transitionMaterial.SetTexture(RenderConst._BlendTex, settings.blendRT);
                }
            }
            else
            {
                _settings.transitionMaterial.DisableKeyword(RenderConst._BLEND_TEX_ON);
            }
            
            SetGammaKeyword(_settings.transitionMaterial);
            
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
                Material mat = _settings.raySphereMaskMaterial;
                
                // mask
                Transform st = _settings.sphereTransform;
                Vector3 stScale = st.lossyScale;
                float stRadius = Mathf.Max(stScale.x, Mathf.Max(stScale.y, stScale.z)) * 0.5f;
                
                mat.SetMatrix(RenderConst._SphereWorldToObject, st.worldToLocalMatrix);
                mat.SetVector(RenderConst._SphereCenter, st.position);
                mat.SetFloat(RenderConst._SphereRadius, stRadius);
                
                mat.SetFloat(RenderConst._PoleThresholdInner, _settings.poleThresholdInner);
                mat.SetFloat(RenderConst._PoleThresholdOuter, _settings.poleThresholdOuter);
                mat.SetFloat(RenderConst._EdgeWidth, _settings.irregularEdgeWidth);
                
                cmd.DrawProcedural(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, 3);

                // draw
                if (_settings.poleThresholdInner > 0 && _settings.poleThresholdOuter > 0)
                {
                    cmd.EnableShaderKeyword(RenderConst._EXCLUDE_CHARACTER_ON);
                }
                else
                {
                    cmd.DisableShaderKeyword(RenderConst._EXCLUDE_CHARACTER_ON);
                }
                
                cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
                
                _settings.transitionMaterial.SetTexture(RenderConst._FullSceneTransitionMaskRT, _maskRTHandle);
                // 因为是实验性的，所以不一定存在
                if (_settings.transitionMaterial.HasProperty(RenderConst._FillFogFlowDirection))
                {
                    _settings.transitionMaterial.SetVector(RenderConst._FillFogFlowDirection, _settings.fillFogFlowDirection);
                }
                
                cmd.DrawProcedural(Matrix4x4.identity, _settings.transitionMaterial, 0, MeshTopology.Triangles, 3);
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
        
        public static void SetGammaKeyword(Material material)
        {
            if (material == null)
            {
                return;
            }
            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                material.DisableKeyword(RenderConst._NEED_LINEAR_TO_SRGB);
            }
            else
            {
                material.EnableKeyword(RenderConst._NEED_LINEAR_TO_SRGB);
            }
#else
            material.EnableKeyword(RenderConst._NEED_LINEAR_TO_SRGB);
#endif
        }
        
        public void OnDisableRendererFeature()
        {
            Shader.DisableKeyword(RenderConst._EXCLUDE_CHARACTER_ON);
        }
    }
}
