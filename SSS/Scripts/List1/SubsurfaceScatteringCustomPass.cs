using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace Garena.TA.SSS
{
    [Serializable]
    public class SubsurfaceScatteringCustomPass : CustomPass
    {
        // ---------------- Inspector 可调 ----------------
        [Header("Resources")] public ComputeShader scatterCompute; // SSS_ScreenSpaceScatter.compute
        private String scatterShader = "Hidden/ScreenSpaceScatter"; // SSS_ScreenSpaceScatter.shader (回退 + 合成)

        //SSS 材质用于输出漫反射辐照度+反照率的 Pass 的 LightMode
        private string subsurfaceDiffuseTag = "SubsurfaceDiffuse";

        [Header("Scattering")] public bool preferCompute = true; // 平台支持时优先 Compute
        public bool remultiplyAlbedo = true; // 散射后乘回反照率(= 最终漫反射颜色)

        public DiffusionProfileParam Profile;

        public Light mainLight; // 不填则用 RenderSettings.sun

        // ---------------- 内部资源 ----------------
        RTHandle _diffuseRT; // rgb = 漫反射辐照度, a = coverage
        RTHandle _albedoRT; // rgb = 反照率
        RTHandle _lightingRT; // 散射结果（compute 需 randomWrite）

        Material _scatterMat;
        Material blitMaterial;
        int _kernelScatter = -1;

        RenderTargetIdentifier[] _splitMRT;
        readonly ShaderTagId[] _shaderTags = new ShaderTagId[1];

        const int kPassScatterFallback = 0;
        const int kPassComposite = 1;

        // ---------------- Shader 属性 ID ----------------
        static class SID
        {
            public static readonly int Diffuse = Shader.PropertyToID("_SSSDiffuse");
            public static readonly int Albedo = Shader.PropertyToID("_SSSAlbedo");
            public static readonly int DiscKernel = Shader.PropertyToID("_SSSDiscKernel");
            public static readonly int Output = Shader.PropertyToID("_SubsurfaceLighting");
            public static readonly int Shape = Shader.PropertyToID("_ShapeParams");
            public static readonly int MaxRadius = Shader.PropertyToID("_MaxRadius");
            public static readonly int WorldScale = Shader.PropertyToID("_WorldScale");
            public static readonly int KernelCount = Shader.PropertyToID("_DiscKernelCount");


            public static readonly int MainLightDir = Shader.PropertyToID("_SSSMainLightDir");
            public static readonly int MainLightColor = Shader.PropertyToID("_SSSMainLightColor");
            public static readonly int ScatterResult = Shader.PropertyToID("_SSSScatterResult");
        }

        const string kKeywordRemultiply = "SSS_REMULTIPLY_ALBEDO";

        // =========================================================================
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (scatterShader != null)
                _scatterMat = CoreUtils.CreateEngineMaterial(Shader.Find(scatterShader));

            if (scatterCompute != null)
                _kernelScatter = scatterCompute.FindKernel("XKNightSubsurfaceScattering");

            // diffuse(辐照度) + albedo：half 浮点，Point，全分辨率
            _diffuseRT = RTHandles.Alloc(
                Vector2.one, slices: TextureXR.slices, dimension: TextureDimension.Tex2D,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                useDynamicScale: true, name: "_SSSDiffuse");

            _albedoRT = RTHandles.Alloc(
                Vector2.one, slices: TextureXR.slices, dimension: TextureDimension.Tex2D,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                useDynamicScale: true, name: "_SSSAlbedo");

            // lighting：compute 需要 randomWrite
            _lightingRT = RTHandles.Alloc(
                Vector2.one, slices: TextureXR.slices, dimension: TextureDimension.Tex2D,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
                enableRandomWrite: true, useDynamicScale: true, name: "_SSSLighting");

            _splitMRT = new RenderTargetIdentifier[2];

            PushGlobals(cmd);
        }

        // =========================================================================
        protected override void Execute(CustomPassContext ctx)
        {
            if (!Validate())
                return;
            // Debug.Log("_diffuseRT.rt.dimension:"+_diffuseRT.rt.dimension);
            var hd = ctx.hdCamera;
            var cmd = ctx.cmd;
            int w = ctx.cameraDepthBuffer.rt.width;
            int h = ctx.cameraDepthBuffer.rt.height;
            // int w = hd.actualWidth;
            // int h = hd.actualHeight;

            //动态贴图的设置
            // ReallocIfNeeded(ref _diffuseRT, w, h, enableRandomWrite: true);
            // ReallocIfNeeded(ref _albedoRT, w, h, enableRandomWrite: true);
            // ReallocIfNeeded(ref _lightingRT, w, h, enableRandomWrite: true);

            // 主光源全局量
            PushGlobals(cmd);

            // ---- Stage 1: albedo----
            RenderSplitLighting(ctx);

            // ---- 公共散射参数 ----
            bool useCompute = preferCompute
                              && scatterCompute != null
                              && _kernelScatter >= 0
                              && SystemInfo.supportsComputeShaders; //支持计算着色器
            // Debug.Log("preferCompute:"+preferCompute + ", useCompute:"+useCompute + ", scatterCompute:"+scatterCompute + ", _kernelScatter:"+ _kernelScatter + ", SystemInfo.supportsComputeShaders:"+ SystemInfo.supportsComputeShaders);
            Vector4 zParams = GetZBufferParams(hd.camera);
            // Debug.Log("ctx.cameraDepthBuffer:"+ctx.cameraDepthBuffer.rt.dimension);
            // ---- Stage 2: Scatter (结果存入 _lightingRT) ----
            if (useCompute)
            {
                //ctx.cameraDepthBuffer,
                DispatchScatterCompute(cmd, w, h);
            }
            // else
            // {
            //     DrawScatterFallback(ctx, w, h, kernelN, zParams, pixelScale);
            // }

            // ---- Stage 3: Composite (加法叠加回相机颜色) ----
            RenderComposite(ctx, w, h);
        }

        // -------------------------------------------------------------------------
        bool Validate()
        {
            if (Profile == null) return false;
            if (_scatterMat == null && scatterShader != null)
                _scatterMat = CoreUtils.CreateEngineMaterial(scatterShader);
            return _scatterMat != null;
        }

        void ReallocIfNeeded(ref RTHandle handle, int w, int h, bool enableRandomWrite = false)
        {
            if (handle != null && handle.rt.width == w && handle.rt.height == h) return;

            handle?.Release();

            // RTHandles.Alloc(
            //     Vector2.one, slices: TextureXR.slices, dimension: TextureDimension.Tex2D,
            //     colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
            //     filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
            //     enableRandomWrite: true, useDynamicScale: true, name: "_SSSLighting");

            handle = RTHandles.Alloc(
                w, h,
                slices: TextureXR.slices,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Point,
                wrapMode: TextureWrapMode.Clamp,
                enableRandomWrite: enableRandomWrite,
                useDynamicScale: false,
                name: handle?.name ?? "_SSSTemp");
        }

        void PushGlobals(CommandBuffer cmd)
        {
            Light l = mainLight != null ? mainLight : RenderSettings.sun;
            Vector4 rtHandleScale = _albedoRT.rtHandleProperties.rtHandleScale;
            // Debug.Log("_albedoRT x:" + rtHandleScale.x + ", y:" + rtHandleScale.y);
            if (l != null && l.type == LightType.Directional)
            {
                Vector3 dir = -l.transform.forward; // 指向光源
                Color c = l.color * l.intensity;
                cmd.SetGlobalVector(SID.MainLightDir, new Vector4(dir.x, dir.y, dir.z, 0f));
                cmd.SetGlobalVector(SID.MainLightColor, new Vector4(c.r, c.g, c.b, 0f));
            }
            else
            {
                cmd.SetGlobalVector(SID.MainLightDir, new Vector4(0f, 1f, 0f, 0f));
                cmd.SetGlobalVector(SID.MainLightColor, Vector4.zero);
            }
        }

        // -------------------------------------------------------------------------
        void RenderSplitLighting(CustomPassContext ctx)
        {
            _shaderTags[0] = new ShaderTagId(subsurfaceDiffuseTag);
            //渲染List,不透明，Tag为 SubsurfaceDiffuse也会进入
            var desc = new RendererListDesc(_shaderTags, ctx.cullingResults, ctx.hdCamera.camera)
            {
                renderQueueRange = RenderQueueRange.opaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                // 让 HDRP 把光照贴图/光照探针/阴影遮罩等逐物体数据喂给 SubsurfaceDiffuse Pass
                rendererConfiguration = PerObjectData.LightProbe
                                        | PerObjectData.Lightmaps
                                        | PerObjectData.LightProbeProxyVolume
                                        | PerObjectData.ShadowMask
                                        | PerObjectData.OcclusionProbe,
                excludeObjectMotionVectors = false,
            };

            var rl = ctx.renderContext.CreateRendererList(desc);

            _splitMRT[0] = _diffuseRT; //第 0 个颜色附件指向
            _splitMRT[1] = _albedoRT; //第 1 个颜色附件指向
            // 用相机深度做 ZTest（SubsurfaceDiffuse Pass 内 ZTest Equal / ZWrite Off）
            // 两张 MRT 清成 (0,0,0,0)，coverage=0 → 非 SSS 区域散射不产生贡献
            CoreUtils.SetRenderTarget(ctx.cmd, _splitMRT, ctx.cameraDepthBuffer,
                ClearFlag.Color, Color.clear);

            ctx.cmd.DrawRendererList(rl);
        }

        // -------------------------------------------------------------------------
        void DispatchScatterCompute(CommandBuffer cmd, int w, int h)
        {
            CoreUtils.SetKeyword(scatterCompute, kKeywordRemultiply, remultiplyAlbedo);

            cmd.SetComputeTextureParam(scatterCompute, _kernelScatter, SID.Diffuse, _diffuseRT);
            cmd.SetComputeTextureParam(scatterCompute, _kernelScatter, SID.Albedo, _albedoRT);
            cmd.SetComputeTextureParam(scatterCompute, _kernelScatter, SID.DiscKernel, Profile.discKernelTex);
            cmd.SetComputeTextureParam(scatterCompute, _kernelScatter, SID.Output, _lightingRT);


            Debug.Log("Shape:" + Profile.InputShape.ToString());
            Debug.Log("MaxRadius:" + Profile.InputMaxRadius.ToString());
            Debug.Log("WorldScale:" + Profile.InputWroldScale.ToString());
            Debug.Log(("DiscSampleCount:" + Profile.InputDiscSampleCount.ToString()));

            cmd.SetComputeFloatParam(scatterCompute, SID.WorldScale, Profile.InputWroldScale);
            cmd.SetComputeIntParam(scatterCompute, SID.KernelCount, Profile.InputDiscSampleCount);
            cmd.SetComputeVectorParam(scatterCompute, SID.Shape, Profile.InputShape);
            cmd.SetComputeFloatParam(scatterCompute, SID.MaxRadius, Profile.InputMaxRadius);
            int tx = (w + 15) / 16;
            int ty = (h + 15) / 16;
            var numTilesZ = 1;
            cmd.DispatchCompute(scatterCompute, _kernelScatter, tx, ty, numTilesZ);
        }

        // -------------------------------------------------------------------------
        // void DrawScatterFallback(CustomPassContext ctx, int w, int h, int kernelN,
        //     Vector4 zParams, float pixelScale)
        // {
        //     CoreUtils.SetKeyword(_scatterMat, kKeywordRemultiply, remultiplyAlbedo);
        //
        //     _scatterMat.SetTexture(SID.Diffuse, _diffuseRT);
        //     _scatterMat.SetTexture(SID.Albedo, _albedoRT);
        //     _scatterMat.SetTexture(SID.DiscKernel, discKernel);
        //     _scatterMat.SetTexture(SID.DepthTexture, ctx.cameraDepthBuffer);
        //
        //     _scatterMat.SetVector(SID.ScreenSize, new Vector4(w, h, 1f / w, 1f / h));
        //     _scatterMat.SetInt(SID.KernelCount, kernelN);
        //     _scatterMat.SetVector(SID.ZBufferParams, zParams);
        //     _scatterMat.SetFloat(SID.PixelScale, pixelScale);
        //
        //
        //     CoreUtils.DrawFullScreen(ctx.cmd, _scatterMat, _lightingRT, ctx.propertyBlock, kPassScatterFallback);
        // }
        // -------------------------------------------------------------------------
        void RenderComposite(CustomPassContext ctx, int w, int h)
        {
            _scatterMat.SetTexture(SID.Albedo, _albedoRT);
            _scatterMat.SetTexture(SID.ScatterResult, _diffuseRT);

            CoreUtils.DrawFullScreen(ctx.cmd, _scatterMat, ctx.cameraColorBuffer, ctx.propertyBlock, kPassComposite);
        }


        // -------------------------------------------------------------------------
        // 复刻 Unity _ZBufferParams（含 reverse-Z 处理）。LinearEyeDepth = 1/(z*d + w)。
        static Vector4 GetZBufferParams(Camera cam)
        {
            double n = cam.nearClipPlane;
            double f = cam.farClipPlane;
            double x = 1.0 - f / n;
            double y = f / n;
            if (SystemInfo.usesReversedZBuffer)
            {
                x = -1.0 + f / n;
                y = 1.0;
            }

            return new Vector4((float)x, (float)y, (float)(x / f), (float)(y / f));
        }

        // =========================================================================
        protected override void Cleanup()
        {
            _diffuseRT?.Release();
            _albedoRT?.Release();
            _lightingRT?.Release();
            CoreUtils.Destroy(_scatterMat);
        }
    }
}