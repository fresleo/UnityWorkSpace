using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA
{
    /// <summary>
    /// 将逐物体阴影贴图收集到屏幕空间RT上
    /// </summary>
    public class GPerObjectShadowResolvePass : ScriptableRenderPass
    {
        private GPerObjectShadowSettings settings;
        private GPerObjectShadowResolvePassSettings passSettings;

        private RTHandle screenSpaceShadowMap;
        private RTHandle sssmTextureId;

        private Mesh mesh;
        private MaterialPropertyBlock mpb;

        private Material resolveMaterial;
        private Material postResolveMaterial;

        public GPerObjectShadowResolvePass(GPerObjectShadowSettings settings)
        {
            this.settings = settings;
            this.passSettings = settings.resolvePassSettings;
            this.profilingSampler = new ProfilingSampler(nameof(GPerObjectShadowResolvePass));
            this.renderPassEvent = passSettings.Event;

            mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            mpb = new MaterialPropertyBlock();

            screenSpaceShadowMap = RTHandles.Alloc(GPerObjectShadowPropertyID.PID_GPerObjectScreenSpaceShadowMap, GPerObjectShadowPropertyID.GPerObjectScreenSpaceShadowMapName);

            //RenderingUtils.ReAllocateIfNeeded(ref screenSpaceShadowMap, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: nameof(screenSpaceShadowMap));

            if (passSettings.postResolveShader == null)
            {
                passSettings.postResolveShader = Shader.Find(GPerObjectShadowPropertyID.ResolvePostShaderName);
            }

            if (passSettings.postResolveShader != null)
            {
                postResolveMaterial = new Material(passSettings.postResolveShader);
            }

            if (passSettings.resolveShader == null)
            {
                passSettings.resolveShader = Shader.Find(GPerObjectShadowPropertyID.ResolveShaderName);
            }

            if (passSettings.resolveShader != null)
            {
                resolveMaterial = new Material(passSettings.resolveShader);
                resolveMaterial.enableInstancing = true;
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (passSettings.resolveToRenderTexture)
            {

                //sssmTextureId = new RenderTargetIdentifier(sssmHandle.id);
                // no need to execute cmd since renderer will execute it for you (and there is no profiling scope)
                // Any temporary textures that were not explicitly released will be removed after camera is done rendering.
                if (passSettings.resolveToScreenSpaceShadow)
                {
                    if (sssmTextureId == null || sssmTextureId.rt == null)
                        sssmTextureId = RTHandles.Alloc(new RenderTargetIdentifier(GPerObjectShadowPropertyID.UnityScreenSpaceShadowMapName));
                    ConfigureTarget(sssmTextureId);
                }
                else
                {

                    cmd.GetTemporaryRT(GPerObjectShadowPropertyID.PID_GPerObjectScreenSpaceShadowMap, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.R8);

                    ConfigureTarget(screenSpaceShadowMap);
                    ConfigureClear(ClearFlag.All, Color.white);
                }
            }
            else
            {
                ResetTarget();
                ConfigureClear(ClearFlag.None, Color.white);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {

                if (GPerObjectShadowMapPass.Current != null)
                {
                    resolveMaterial.SetColor(GPerObjectShadowPropertyID.PID_Color, settings.shadowColor);
                    resolveMaterial.SetFloat(GPerObjectShadowPropertyID.PID_SrcBlend, (float)passSettings.srcBlend);
                    resolveMaterial.SetFloat(GPerObjectShadowPropertyID.PID_DstBlend, (float)passSettings.dstBlend);

                    postResolveMaterial.SetColor(GPerObjectShadowPropertyID.PID_Color, settings.shadowColor);
                    postResolveMaterial.SetFloat(GPerObjectShadowPropertyID.PID_SrcBlend, (float)passSettings.srcBlend);
                    postResolveMaterial.SetFloat(GPerObjectShadowPropertyID.PID_DstBlend, (float)passSettings.dstBlend);

                    if(passSettings.useCharacterMask)
                    {
                        var maskRT = XKnightRenderPipeline.characterMaskBufferSystem.GetBackBuffer(cmd);

                        resolveMaterial.SetTexture(GPerObjectShadowPropertyID.PID_CharacterMaskTexture, maskRT);
                        postResolveMaterial.SetTexture(GPerObjectShadowPropertyID.PID_CharacterMaskTexture, maskRT);
                    }

                    if (passSettings.resolveToRenderTexture)
                    {
                        cmd.SetGlobalTexture(GPerObjectShadowPropertyID.PID_GPerObjectScreenSpaceShadowMap, screenSpaceShadowMap);
                    }

                    if (passSettings.usePostMethod)
                    {
                        Blitter.BlitTexture(cmd, new Vector4(1, 1, 0, 0), postResolveMaterial, passSettings.resolveMainLightShadow ? 1 : 0);

                        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
                        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);

                        //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, true);
                    }
                    else
                    {
                        cmd.DrawMeshInstanced(mesh, 0, resolveMaterial, 0, GPerObjectShadowMapPass.Current.data.localToWorld, GPerObjectShadowMapPass.Current.data.ValidSliceCount, mpb);
                    }
                }
            }
        }
    }
}