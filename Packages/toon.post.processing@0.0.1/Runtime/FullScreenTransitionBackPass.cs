
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using RenderConst = ToonPostProcessing.FullScreenTransitionPass.RenderConst;

namespace ToonPostProcessing
{
    public class FullScreenTransitionBackPass : ScriptableRenderPass
    {
        private FullScreenTransitionRendererFeature.Settings _settings;

        private readonly string _profilerTag, _cmdTag;
        
        public FullScreenTransitionBackPass(FullScreenTransitionRendererFeature.Settings setting)
        {
            _settings = setting;
            
            base.renderPassEvent = _settings.renderEvent;

            _profilerTag = nameof(FullScreenTransitionBackPass);
            _cmdTag = _profilerTag + "_cmd";
            
            base.profilingSampler = new ProfilingSampler(_profilerTag);
        }

        public bool Setup(FullScreenTransitionRendererFeature.Settings setting)
        {
            _settings = setting;
            if (_settings.BackMaterial == null || _settings.BackBlendRT == null)
            {
                return false;
            }
            
            base.renderPassEvent = _settings.renderEvent;
            ConfigureInput(ScriptableRenderPassInput.SceneDepthPrepass);
            
            Vector3 transitionCenterPosition = _settings.BackTransitionTF != null ? _settings.BackTransitionTF.position : _settings.BackTransitionCenterPosition;
            _settings.BackMaterial.SetVector(RenderConst._sPID_TransitionCenterPosition, transitionCenterPosition);
            
            _settings.BackMaterial.SetFloat(RenderConst._sPID_MaxRadius, _settings.BackMaxRadius);

            if (_settings.BackUseBlendTex && _settings.BackBlendRT != null)
            {
                _settings.BackMaterial.EnableKeyword(RenderConst.c_BLEND_TEX_ON);
                _settings.BackMaterial.SetTexture(RenderConst._sPID_BlendTex, _settings.BackBlendRT);
            }
            else
            {
                _settings.BackMaterial.DisableKeyword(RenderConst.c_BLEND_TEX_ON);
            }
            
            FullScreenTransitionPass.SetGammaKeyword(_settings.BackMaterial);

            return true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_settings.BackMaterial == null)
            {
                return;
            }
            
            var cmd = CommandBufferPool.Get(_cmdTag);
            using (new ProfilingScope(cmd, base.profilingSampler))
            {
                if (_settings.BackMaxRadius > 0)
                {
                    cmd.EnableShaderKeyword(RenderConst.c_DISABLE_DEPTHONLY);
                }
                else
                {
                    cmd.DisableShaderKeyword(RenderConst.c_DISABLE_DEPTHONLY);
                }
                
                cmd.DrawProcedural(Matrix4x4.identity, _settings.BackMaterial, 0, MeshTopology.Triangles, 3);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        
        public void Dispose()
        {
            _settings = null;
        }

        public void DisableRendererFeature()
        {
            Shader.DisableKeyword(RenderConst.c_DISABLE_DEPTHONLY);
        }
    }
}
