
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

namespace ToonPostProcessing
{
    public class FullScreenTransitionPass : ScriptableRenderPass
    {
        public struct RenderConst
        {
            public static readonly int _sPID_TransitionCenterPosition = Shader.PropertyToID("_TransitionCenterPosition");

            public static readonly int
                _sPID_MaxFarDepth = Shader.PropertyToID("_MaxFarDepth")
                , _sPID_MaxRadius = Shader.PropertyToID("_MaxRadius")
                , _sPID_EdgeWidth = Shader.PropertyToID("_EdgeWidth");
            public static readonly int _sPID_BlendTex = Shader.PropertyToID("_BlendTex");

            public const string c_BLEND_TEX_ON = "_BLEND_TEX_ON";
            public const string c_NEED_LINEAR_TO_SRGB = "_NEED_LINEAR_TO_SRGB";
            
            // 为了处理 Grass.shader 的穿帮问题
            public const string c_EXCLUDE_CHARACTER_ON = "_EXCLUDE_CHARACTER_ON";
            public const string c_DISABLE_DEPTHONLY = "_DISABLE_DEPTHONLY";
        }
        
        private FullScreenTransitionRendererFeature.Settings _settings;
        
        private readonly string _profilerTag, _cmdTag;
        
        public FullScreenTransitionPass(FullScreenTransitionRendererFeature.Settings setting)
        {
            _settings = setting;
            
            base.renderPassEvent = _settings.renderEvent;

            _profilerTag = nameof(FullScreenTransitionPass);
            _cmdTag = _profilerTag + "_cmd";
            
            base.profilingSampler = new ProfilingSampler(_profilerTag);
        }

        public bool Setup(FullScreenTransitionRendererFeature.Settings setting)
        {
            _settings = setting;
            if (_settings.Material == null)
            {
                return false;
            }

            base.renderPassEvent = _settings.renderEvent;
            ConfigureInput(ScriptableRenderPassInput.SceneDepthPrepass);
            
            Vector3 transitionCenterPosition = _settings.TransitionTransform != null ? _settings.TransitionTransform.position : _settings.TransitionCenterPosition;
            _settings.Material.SetVector(RenderConst._sPID_TransitionCenterPosition, transitionCenterPosition);
            
            _settings.Material.SetFloat(RenderConst._sPID_MaxFarDepth, _settings.MaxFarDepth);
            _settings.Material.SetFloat(RenderConst._sPID_MaxRadius, _settings.MaxRadius);
            _settings.Material.SetFloat(RenderConst._sPID_EdgeWidth, _settings.EdgeWidth);
            
            if (_settings.UseBlendTex && _settings.BlendRT != null)
            {
                _settings.Material.EnableKeyword(RenderConst.c_BLEND_TEX_ON);
                _settings.Material.SetTexture(RenderConst._sPID_BlendTex, _settings.BlendRT);
            }
            else
            {
                _settings.Material.DisableKeyword(RenderConst.c_BLEND_TEX_ON);
            }

            SetGammaKeyword(_settings.Material);
            
            return true;
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
                material.DisableKeyword(RenderConst.c_NEED_LINEAR_TO_SRGB);
            }
            else
            {
                material.EnableKeyword(RenderConst.c_NEED_LINEAR_TO_SRGB);
            }
#else
            material.EnableKeyword(RenderConst.c_NEED_LINEAR_TO_SRGB);
#endif
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_settings.Material == null)
            {
                return;
            }
            
            var cmd = CommandBufferPool.Get(_cmdTag);
            using (new ProfilingScope(cmd, base.profilingSampler))
            {
                if (_settings.MaxRadius > 0)
                {
                    cmd.EnableShaderKeyword(RenderConst.c_EXCLUDE_CHARACTER_ON);
                }
                else
                {
                    cmd.DisableShaderKeyword(RenderConst.c_EXCLUDE_CHARACTER_ON);
                }
                
                cmd.DrawProcedural(Matrix4x4.identity, _settings.Material, 0, MeshTopology.Triangles, 3);
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
            Shader.DisableKeyword(RenderConst.c_EXCLUDE_CHARACTER_ON);
        }
    }
}
