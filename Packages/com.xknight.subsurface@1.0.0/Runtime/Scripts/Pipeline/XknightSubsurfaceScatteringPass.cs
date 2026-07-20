using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEngine.Rendering.RendererUtils;

namespace XKnight.TA.SSS
{
    public class XknightSubsurfaceScatteringPass : ScriptableRenderPass, IDisposable
    {
        //Feature handle in 
        private XknightSubsurfaceScatteringRenderFeature.Settings m_Settings;

        // ---------------------------------内部资源------------------------------------
        private ComputeShader ScatteringCompute;
        private Material _scatterMat;
        private int _kernelScatter = -1;
        private RenderTargetIdentifier[] _splitMRT = new RenderTargetIdentifier[2];
        private readonly ShaderTagId[] _shaderTags = new ShaderTagId[1];
        private String scatterShader = "Hidden/ScreenSpaceScatter";
        private string subsurfaceDiffuseTag = "SubsurfaceDiffuse";

        // ----------------------------------用到的RT----------------------------------

        RTHandle _diffuseRT; // rgb = 漫反射辐照度, a = coverage
        RTHandle _albedoRT; // rgb = 反照率
        RTHandle _lightingRT; // 散射结果
        RTHandle _CameraDepth;


        //==============================Global Shader Parameters ======================================

        private ShaderVariableDiffusion _shaderVariableDiffusion;

        private readonly Vector4[] _sssShapeParamsAndFreePath = new Vector4[16];

        private readonly Vector4[] _sssTransmissionTintsAndFresnel0 = new Vector4[16];

        private readonly Vector4[] _sssWorldScaleAndMaxRadiusAndThicknessRemaps = new Vector4[16];

        private readonly uint[] _sssDiffusionParametersHashes = new uint[16];
        private readonly uint[] _sssShadowStrengths = new uint[16];
        private readonly uint[] _sssThicknessOffsets = new uint[16];
        private uint _sssActiveDiffusionParametersCount = 0;


        // ==============================       Shader 属性 ID     ======================================
        static class SID
        {
            // public static readonly int Diffuse = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _SSSDiffuse = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _SSSAlbedo = MemberNameHelpers.ShaderPropertyID();
            public static readonly int _SubsurfaceLightingIrradiance = MemberNameHelpers.ShaderPropertyID();

            public static readonly int _SssSampleBudget = MemberNameHelpers.ShaderPropertyID();

            //Blit
            public static readonly int _SSSScatterResult = MemberNameHelpers.ShaderPropertyID();

            public static readonly string kScreenSpaceSSSKeyword = "SCREENSPACESUBSURFACESCATTERING_ON";

            //=================================Cbuffer===========================
            public static readonly int ShaderVariableDiffusionParams = MemberNameHelpers.ShaderPropertyID();
        }

        bool Validate()
        {
            if (DiffusionParameter.AllInstances.Count == 0)
            {
                return false;
            }

            if (ScatteringCompute == null && scatterShader == null)
            {
                throw new Exception("SubsurfaceScatteringCustomPass: scatterCompute and scatterShader are both null");
                return false;
            }

            if (_scatterMat == null && scatterShader != null)
                _scatterMat = CoreUtils.CreateEngineMaterial(scatterShader);


            return _scatterMat != null;
        }

        private unsafe void SetVariable()
        {
            // set diffusion index
            _sssActiveDiffusionParametersCount = (uint)DiffusionParameter.AllInstances.Count;
            // Debug.Log("_sssActiveDiffusionParametersCount:"+_sssActiveDiffusionParametersCount);
            for (int i = 0; i < (int)_sssActiveDiffusionParametersCount; i++)
            {
                Vector3 shapeParams = DiffusionParameter.AllInstances[i].InputShape;
                Vector3 transmissionTint = DiffusionParameter.AllInstances[i].InputTransmissionTint;
                Vector3 thicknessRemap = DiffusionParameter.AllInstances[i].InputThicknessRemap;
                _sssTransmissionTintsAndFresnel0[i] =
                    new Vector4(transmissionTint.x, transmissionTint.y, transmissionTint.z,
                        DiffusionParameter.AllInstances[i].InputFresnel0);
                _sssShapeParamsAndFreePath[i] =
                    new Vector4(shapeParams.x, shapeParams.y, shapeParams.z,
                        DiffusionParameter.AllInstances[i].Input_d);
                _sssWorldScaleAndMaxRadiusAndThicknessRemaps[i] =
                    new Vector4(DiffusionParameter.AllInstances[i].InputWroldScale,
                        DiffusionParameter.AllInstances[i].InputMaxRadius, thicknessRemap.x, thicknessRemap.y);
                _sssDiffusionParametersHashes[i] = DiffusionParameter.AllInstances[i].hash;
                _sssShadowStrengths[i] =
                    (uint)System.BitConverter.SingleToInt32Bits(DiffusionParameter.AllInstances[i]
                        .InputShadowStrenthen);
                _sssThicknessOffsets[i] =
                    (uint)System.BitConverter.SingleToInt32Bits(DiffusionParameter.AllInstances[i].InputThickOffset);

                // Debug.Log("DiffusionParameter.AllInstances[i].hash:"+DiffusionParameter.AllInstances[i].hash);
            }
        }

        private unsafe void SetShaderVariableDiffusion(ref ShaderVariableDiffusion cb)
        {
            cb.DiffusionParametersCount = _sssActiveDiffusionParametersCount;

            for (int i = 0; i < _sssActiveDiffusionParametersCount; i++)
            {
                for (int c = 0; c < 4; c++)
                {
                    cb.ShapeParamsAndFreePath[i * 4 + c] = _sssShapeParamsAndFreePath[i][c];
                    cb.TransmissionTintAndFresnel[i * 4 + c] = _sssTransmissionTintsAndFresnel0[i][c];
                    cb.WorldScaleAndMaxRadiusAndThicknessRemaps[i * 4 + c] =
                        _sssWorldScaleAndMaxRadiusAndThicknessRemaps[i][c];
                }

                cb.HashAndShadowStrenthAndThickOffset[i * 4] = _sssDiffusionParametersHashes[i];
                cb.HashAndShadowStrenthAndThickOffset[i * 4 + 1] = (uint)_sssShadowStrengths[i];
                cb.HashAndShadowStrenthAndThickOffset[i * 4 + 2] = (uint)_sssThicknessOffsets[i];
                cb.HashAndShadowStrenthAndThickOffset[i * 4 + 3] = (uint)0; //占位符
            }
        }

        private unsafe void LogShaderVariableDiffusion(in ShaderVariableDiffusion cb)
        {
            var sb = new System.Text.StringBuilder(512);
            sb.Append("ShaderVariableDiffusion Count=").Append(cb.DiffusionParametersCount);

            for (int i = 0; i < (int)cb.DiffusionParametersCount; i++)
            {
                int baseIndex = i * 4;

                sb.Append("\n[").Append(i).Append("] ")
                    .Append("ShapeParamsAndFreePath=(")
                    .Append(cb.ShapeParamsAndFreePath[baseIndex + 0]).Append(", ")
                    .Append(cb.ShapeParamsAndFreePath[baseIndex + 1]).Append(", ")
                    .Append(cb.ShapeParamsAndFreePath[baseIndex + 2]).Append(", ")
                    .Append(cb.ShapeParamsAndFreePath[baseIndex + 3]).Append(") ")
                    .Append("TransmissionTintAndFresnel=(")
                    .Append(cb.TransmissionTintAndFresnel[baseIndex + 0]).Append(", ")
                    .Append(cb.TransmissionTintAndFresnel[baseIndex + 1]).Append(", ")
                    .Append(cb.TransmissionTintAndFresnel[baseIndex + 2]).Append(", ")
                    .Append(cb.TransmissionTintAndFresnel[baseIndex + 3]).Append(") ")
                    .Append("WorldScaleAndMaxRadiusAndThicknessRemaps=(")
                    .Append(cb.WorldScaleAndMaxRadiusAndThicknessRemaps[baseIndex + 0]).Append(", ")
                    .Append(cb.WorldScaleAndMaxRadiusAndThicknessRemaps[baseIndex + 1]).Append(", ")
                    .Append(cb.WorldScaleAndMaxRadiusAndThicknessRemaps[baseIndex + 2]).Append(", ")
                    .Append(cb.WorldScaleAndMaxRadiusAndThicknessRemaps[baseIndex + 3]).Append(") ")
                    .Append("Hash=").Append(cb.HashAndShadowStrenthAndThickOffset[baseIndex + 0])
                    .Append(" ShadowStrength=").Append(cb.HashAndShadowStrenthAndThickOffset[baseIndex + 1])
                    .Append(" ThickOffset=").Append(cb.HashAndShadowStrenthAndThickOffset[baseIndex + 2]);
            }

            Debug.Log(sb.ToString());
        }

        //设置全局参数
        void PushGlobals(CommandBuffer cmd)
        {
            SetVariable();
            //Set shadervarible
            SetShaderVariableDiffusion(ref _shaderVariableDiffusion);

            // LogShaderVariableDiffusion(in _shaderVariableDiffusion);
            cmd.SetGlobalTexture(SID._SubsurfaceLightingIrradiance, _lightingRT);
            if (m_Settings.injectWay == UInjectWay.ForwardSampling)
            {
                cmd.EnableShaderKeyword(SID.kScreenSpaceSSSKeyword);
            }
            else if (m_Settings.injectWay == UInjectWay.DeferredComposition)
            {
                cmd.DisableShaderKeyword(SID.kScreenSpaceSSSKeyword);
            }

            ConstantBuffer.PushGlobal(cmd, _shaderVariableDiffusion, SID.ShaderVariableDiffusionParams);
        }

        void RenderSplitLighting(ScriptableRenderContext context, CommandBuffer cmd, RenderingData renderingData)
        {
            _shaderTags[0] = new ShaderTagId(subsurfaceDiffuseTag);
            var desc = new RendererListDesc(_shaderTags, renderingData.cullResults, renderingData.cameraData.camera)
            {
                renderQueueRange = RenderQueueRange.opaque, //不透明物体
                sortingCriteria = SortingCriteria.CommonOpaque,
                rendererConfiguration = PerObjectData.LightProbe
                                        | PerObjectData.Lightmaps
                                        | PerObjectData.LightProbeProxyVolume
                                        | PerObjectData.ShadowMask
                                        | PerObjectData.OcclusionProbe,
                excludeObjectMotionVectors = false
            };
            var r1 = context.CreateRendererList(desc);
            _splitMRT[0] = _diffuseRT;
            _splitMRT[1] = _albedoRT;

            CoreUtils.SetRenderTarget(cmd, _splitMRT, renderingData.cameraData.renderer.cameraDepthTargetHandle,
                ClearFlag.Color, Color.clear);

            cmd.DrawRendererList(r1);
            context.ExecuteCommandBuffer(cmd);
        }

        void DispatchScatterCompute(ScriptableRenderContext ctx, CommandBuffer cmd, int w, int h)
        {
            cmd.SetComputeTextureParam(ScatteringCompute, _kernelScatter, SID._SSSDiffuse, _diffuseRT);
            cmd.SetComputeTextureParam(ScatteringCompute, _kernelScatter, SID._SSSAlbedo, _albedoRT);
            cmd.SetComputeTextureParam(ScatteringCompute, _kernelScatter, SID._SubsurfaceLightingIrradiance,
                _lightingRT);

            cmd.SetComputeIntParam(ScatteringCompute, SID._SssSampleBudget, 32);

            int tx = (w + 15) / 16;
            int ty = (h + 15) / 16;
            var numTilesZ = 1;
            cmd.DispatchCompute(ScatteringCompute, _kernelScatter, tx, ty, numTilesZ);
            ctx.ExecuteCommandBuffer(cmd);
        }

        void DrawScatterFallback(ScriptableRenderContext ctx, CommandBuffer cmd, int w, int h)
        {
            cmd.SetGlobalTexture(SID._SSSDiffuse, _diffuseRT);
            cmd.SetGlobalTexture(SID._SSSAlbedo, _albedoRT);
            cmd.SetGlobalInt(SID._SssSampleBudget, 32);
            //只是将pass0 ->_lightingRT
            CoreUtils.DrawFullScreen(cmd, _scatterMat, _lightingRT, null, 0);
            ctx.ExecuteCommandBuffer(cmd);
        }

        //进行后融合
        void RenderComposite(ScriptableRenderContext ctx, CommandBuffer cmd, RenderingData renderingData)
        {
            _scatterMat.SetTexture(SID._SSSDiffuse, _diffuseRT);
            _scatterMat.SetTexture(SID._SSSScatterResult, _lightingRT);

            _scatterMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _scatterMat.SetInt(("_DstBlend"), (int)BlendMode.OneMinusSrcAlpha);
            _scatterMat.SetFloat("_SSS_Strenth", m_Settings.sssStrenth);
            _scatterMat.renderQueue = (int)RenderQueue.Transparent; //设置透明序列
            CoreUtils.DrawFullScreen(cmd, _scatterMat, renderingData.cameraData.renderer.cameraColorTargetHandle, null,
                1);
            ctx.ExecuteCommandBuffer(cmd);
        }
        
        
        // ==============================       执行部分           ======================================
        public XknightSubsurfaceScatteringPass()
        {
#if UNITY_EDITOR
            if (ScatteringCompute == null)
            {
                string packagePath =
                    "Packages/com.xknight.subsurface/CommonShader/XKnight_SubsurfaceScattering.compute";
                ScatteringCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(packagePath);
                // Debug.Log("Load ComputeShader: " + packagePath);
                // Debug.Log(AssetDatabase.LoadAssetAtPath<ComputeShader>(packagePath) == null
                //     ? "Failed to load ComputeShader"
                //     : "ComputeShader loaded successfully"
                // );
            }
#endif
            if (_scatterMat == null)
                _scatterMat = CoreUtils.CreateEngineMaterial(Shader.Find(scatterShader));
            if (ScatteringCompute != null)
            {
                _kernelScatter = ScatteringCompute.FindKernel("XKNightSubsurfaceScattering");
            }
            else
            {
                throw new Exception("ScatterCompute is null");
            }
            // Debug.Log("Load ComputeShader: " + packagePath);
            // Debug.Log(_scatterMat == null
            //     ? "Failed to load scatterMat"
            //     : "scatterMat loaded successfully"
            // );
            // Debug.Log($"_kernelScatter：{_kernelScatter}");
        }

        public void Setup(XknightSubsurfaceScatteringRenderFeature.Settings settings)
        {
            m_Settings = settings;
        }

        public void Dispose()
        {
            Debug.Log("XknightSubsurfaceScatteringPass Dispose");
            _diffuseRT?.Release();
            _albedoRT?.Release();
            _lightingRT?.Release();
            Shader.DisableKeyword("SCREENSPACESUBSURFACESCATTERING_ON");
            CoreUtils.Destroy(_scatterMat);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Validate())
                return;


            //Texture设置
            XKnightRenderingUtils.ReAllocateIfNeeded(ref _diffuseRT, renderingData.cameraData.cameraTargetDescriptor,
                FilterMode.Point, TextureWrapMode.Clamp, name: nameof(SID._SSSDiffuse));
            XKnightRenderingUtils.ReAllocateIfNeeded(ref _albedoRT, renderingData.cameraData.cameraTargetDescriptor,
                FilterMode.Point, TextureWrapMode.Clamp, name: nameof(SID._SSSAlbedo));
            //打开写入但是不能使用UAV纹理
            var baseDesc = renderingData.cameraData.cameraTargetDescriptor;
            baseDesc.depthBufferBits = 0;
            baseDesc.msaaSamples = 1;
            var splitDesc = baseDesc;
            splitDesc.enableRandomWrite = m_Settings.preferCompute;
            XKnightRenderingUtils.ReAllocateIfNeeded(ref _lightingRT, splitDesc,
                FilterMode.Point, TextureWrapMode.Clamp, name: nameof(SID._SubsurfaceLightingIrradiance));

            //全局变量
            var cmdVariable = CommandBufferPool.Get("SSS Setup Global Variables");
            PushGlobals(cmdVariable);
            context.ExecuteCommandBuffer(cmdVariable);
            CommandBufferPool.Release(cmdVariable);


            //光照阶段
            var cmdLighting = CommandBufferPool.Get("SSS Lighting");
            RenderSplitLighting(context, cmdLighting, renderingData);
            CommandBufferPool.Release(cmdLighting);

            //散射阶段
            var cmdScattering = CommandBufferPool.Get("SSS Scattering");
            int w = renderingData.cameraData.cameraTargetDescriptor.width;
            int h = renderingData.cameraData.cameraTargetDescriptor.height;

            if (m_Settings.preferCompute)
            {
                //设置keyworld
                _scatterMat.DisableKeyword("SSS_FORCE_DIRECT_LOAD");
                ScatteringCompute.EnableKeyword("SSS_FORCE_DIRECT_LOAD");
                //compute
                DispatchScatterCompute(context, cmdScattering, w, h);
            }
            else
            {
                _scatterMat.EnableKeyword("SSS_FORCE_DIRECT_LOAD");
                ScatteringCompute.DisableKeyword("SSS_FORCE_DIRECT_LOAD");
                //fallback -> scatteringShader
                DrawScatterFallback(context, cmdScattering, w, h);
            }

            CommandBufferPool.Release(cmdScattering);

            //是否需要合并（看方式）
            if (m_Settings.injectWay == UInjectWay.DeferredComposition)
            {
                //合并
                var cmdComposeition = CommandBufferPool.Get("SSS Composition");
                RenderComposite(context, cmdComposeition, renderingData);
                CommandBufferPool.Release(cmdComposeition);
            }
        }
    }
}