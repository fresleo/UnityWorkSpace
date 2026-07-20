using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DeferredLights = UnityEngine.Rendering.Universal.Internal.DeferredLights;

namespace RadiantGI.Universal
{
    /// <summary>
    /// 辐照度GI渲染特性
    /// </summary>
    public class RadiantRenderFeature : ScriptableRendererFeature
    {
        public enum ERenderingPath
        {
            Forward,
            Deferred,
            Both
        }
        
        private enum EPass
        {
            CopyExact = 0,
            Raycast = 1,
            BlurHorizontal = 2,
            BlurVertical = 3,
            Upscale = 4,
            TemporalAccum = 5,
            Albedo = 6,
            Normals = 7,
            Compose = 8,
            Compare = 9,
            FinalGIDebug = 10,
            Specular = 11,
            Copy = 12,
            WideFilter = 13,
            Depth = 14,
            CopyDepth = 15,
            RSM_Debug = 16,
            RSM = 17,
            NFO = 18,
            NFOBlur = 19,
            CopyMultiTaps = 20
        }

        private static readonly List<ReflectionProbe> s_probes = new List<ReflectionProbe>();
        private static readonly List<RadiantVirtualEmitter> s_emitters = new List<RadiantVirtualEmitter>();
        private static bool s_emittersForceRefresh;

        private const string c_shaderPath_RadiantGI = "Hidden/RadiantGI/RadiantGI_URP";
        private const string c_shaderPath_RadiantGI_OrganicLight = "Hidden/RadiantGI/RadiantGIOrganicLight";

        private const string c_path_blueNoiseGI128RGB = "RadiantGI/blueNoiseGI128RGB";
        private const string c_path_NoiseTex = "RadiantGI/NoiseTex";
        
        private static class ShaderParams
        {
            // targets
            public static int MainTex = Shader.PropertyToID("_MainTex");
            public static int DownscaledColorAndDepthRT = Shader.PropertyToID("_DownscaledColorAndDepthRT");
            public static int ResolveRT = Shader.PropertyToID("_ResolveRT");
            public static int SourceSize = Shader.PropertyToID("_SourceSize");
            public static int NoiseTex = Shader.PropertyToID("_NoiseTex");
            public static int Downscaled1RT = Shader.PropertyToID("_Downscaled1RT");
            public static int Downscaled1RTA = Shader.PropertyToID("_Downscaled1RTA");
            public static int Downscaled2RT = Shader.PropertyToID("_Downscaled2RT");
            public static int Downscaled2RTA = Shader.PropertyToID("_Downscaled2RTA");
            public static int InputRT = Shader.PropertyToID("_InputRTGI");
            public static int CompareTex = Shader.PropertyToID("_CompareTexGI");
            public static int TempAcum = Shader.PropertyToID("_TempAcum");
            public static int PrevResolve = Shader.PropertyToID("_PrevResolve");
            public static int DownscaledDepthRT = Shader.PropertyToID("_DownscaledDepthRT");
            public static int Probe1Cube = Shader.PropertyToID("_Probe1Cube");
            public static int Probe2Cube = Shader.PropertyToID("_Probe2Cube");
            public static int NFO_RT = Shader.PropertyToID("_NFO_RT");
            public static int NFOBlurRT = Shader.PropertyToID("_NFOBlurRT");

            // uniforms
            public static int IndirectData = Shader.PropertyToID("_IndirectData");
            public static int RayData = Shader.PropertyToID("_RayData");
            public static int TemporalData = Shader.PropertyToID("_TemporalData");
            public static int WorldToViewDir = Shader.PropertyToID("_WorldToViewDir");
            public static int CompareParams = Shader.PropertyToID("_CompareParams");
            public static int ExtraData = Shader.PropertyToID("_ExtraData");
            public static int ExtraData2 = Shader.PropertyToID("_ExtraData2");
            public static int ExtraData3 = Shader.PropertyToID("_ExtraData3");
            public static int ExtraData4 = Shader.PropertyToID("_ExtraData4");
            public static int ExtraData5 = Shader.PropertyToID("_ExtraData5");
            public static int EmittersPositions = Shader.PropertyToID("_EmittersPositions");
            public static int EmittersBoxMin = Shader.PropertyToID("_EmittersBoxMin");
            public static int EmittersBoxMax = Shader.PropertyToID("_EmittersBoxMax");
            public static int EmittersColors = Shader.PropertyToID("_EmittersColors");
            public static int EmittersCount = Shader.PropertyToID("_EmittersCount");
            public static int RSMIntensity = Shader.PropertyToID("_RadiantShadowMapIntensity");
            public static int StencilValue = Shader.PropertyToID("_StencilValue");
            public static int StencilCompareFunction = Shader.PropertyToID("_StencilCompareFunction");
            public static int SubstractLightingMultiplier = Shader.PropertyToID("_ExtraData4");
            public static int ProbeData = Shader.PropertyToID("_ProbeData");
            public static int Probe1HDR = Shader.PropertyToID("_Probe1HDR");
            public static int Probe2HDR = Shader.PropertyToID("_Probe2HDR");
            public static int BoundsXZ = Shader.PropertyToID("_BoundsXZ");
            public static int DebugDepthMultiplier = Shader.PropertyToID("_DebugDepthMultiplier");
            public static int NFOTint = Shader.PropertyToID("_NFOTint");

            public static int OrganicLightData = Shader.PropertyToID("_OrganicLightData");
            public static int OrganicLightTint = Shader.PropertyToID("_OrganicLightTint");
            public static int OrganicLightOffset = Shader.PropertyToID("_OrganicLightOffset");

            // keywords
            public const string SKW_FORWARD = "_FORWARD";
            public const string SKW_FORWARD_AND_DEFERRED = "_FORWARD_AND_DEFERRED";
            public const string SKW_COMPARE_MODE = "_COMPARE_MODE";
            public const string SKW_USES_BINARY_SEARCH = "_USES_BINARY_SEARCH";
            public const string SKW_USES_MULTIPLE_RAYS = "_USES_MULTIPLE_RAYS";
            public const string SKW_REUSE_RAYS = "_REUSE_RAYS";
            public const string SKW_FALLBACK_1_PROBE = "_FALLBACK_1_PROBE";
            public const string SKW_FALLBACK_2_PROBES = "_FALLBACK_2_PROBES";
            public const string SKW_VIRTUAL_EMITTERS = "_VIRTUAL_EMITTERS";
            public const string SKW_USES_NEAR_FIELD_OBSCURANCE = "_USES_NEAR_FIELD_OBSCURANCE";
            public const string SKW_ORTHO_SUPPORT = "_ORTHO_SUPPORT";
            public const string SKW_DISTANCE_BLENDING = "_DISTANCE_BLENDING";
            
            public const string IS_GL_IN_EDITOR = "_IS_GL_IN_EDITOR";
        }

        private static Mesh s_fullScreenMesh;

        private static Mesh FullscreenMesh
        {
            get
            {
                if (s_fullScreenMesh != null)
                {
                    return s_fullScreenMesh;
                }

                float num = 1f;
                float num2 = 0f;
                Mesh val = new Mesh();
                s_fullScreenMesh = val;
                s_fullScreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f, 1f, 0f),
                    new Vector3(1f, -1f, 0f),
                    new Vector3(1f, 1f, 0f)
                });
                s_fullScreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0f, num2),
                    new Vector2(0f, num),
                    new Vector2(1f, num2),
                    new Vector2(1f, num)
                });
                s_fullScreenMesh.SetIndices(new int[6] { 0, 1, 2, 2, 1, 3 }, (MeshTopology)0, 0, false);
                s_fullScreenMesh.UploadMeshData(true);
                return s_fullScreenMesh;
            }
        }
        
        internal static bool IsGLESDevice()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
        }
        
        internal static bool IsGLDevice()
        {
            return IsGLESDevice() || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
        }
        
        /// <summary>
        /// 在编辑器下使用 GL 设备，需要特别的处理
        /// </summary>
        internal static void SetMatGLKeyword(Material mat)
        {
#if UNITY_EDITOR
            bool isGLDevice = IsGLDevice();
            if (isGLDevice)
            {
                mat.EnableKeyword(ShaderParams.IS_GL_IN_EDITOR);
            }
            else
            {
                mat.DisableKeyword(ShaderParams.IS_GL_IN_EDITOR);
            }
            return;
#endif //UNITY_EDITOR
            
            mat.DisableKeyword(ShaderParams.IS_GL_IN_EDITOR);
        }

        /// <summary>
        /// 绘制 Pass
        /// </summary>
        private class RadiantPass : ScriptableRenderPass
        {
            public int computedGIRT;

            private const string RGI_CBUF_NAME = "RadiantGI";
            private const float GOLDEN_RATIO = 0.618033989f;
            private const int MAX_EMITTERS = 32;

            private class PerCameraData
            {
                public Vector3 lastCameraPosition;
                public RenderTexture rtAcum;
                public int rtAcumCreationFrame;
                public RenderTexture rtBounce;

                public int rtBounceCreationFrame;

                // emitters
                public float emittersSortTime = float.MinValue;
                public Vector3 emittersLastCameraPosition;
                public readonly List<RadiantVirtualEmitter> emittersSorted = new List<RadiantVirtualEmitter>();
            }

            private ScriptableRenderer m_renderer;
            private RadiantRenderFeature m_settings;
            private RenderTextureDescriptor m_sourceDesc, m_cameraTargetDesc;
            private readonly Dictionary<Camera, PerCameraData> m_prevs = new Dictionary<Camera, PerCameraData>();

            [NonSerialized] public RadiantGlobalIllumination radiant;
            private float m_goldenRatioAcum;
            private bool m_usesReprojection, m_usesCompareMode;
            private Vector3 m_camPos;
            private Volume[] m_volumes;
            private Material m_mat;
            private static readonly Vector4 s_unlimitedBounds = new Vector4(-1e8f, -1e8f, 1e8f, 1e8f);
            private Vector4[] m_emittersBoxMin, m_emittersBoxMax, m_emittersColors, m_emittersPositions;
            private readonly Plane[] m_cameraPlanes = new Plane[6];

            public bool Setup(RadiantGlobalIllumination radiant, ScriptableRenderer renderer, RadiantRenderFeature settings, bool isSceneView)
            {
                if (radiant == null || !radiant.IsActive()) return false;
                this.radiant = radiant;

#if UNITY_EDITOR
                if (isSceneView && !radiant.showInSceneView.value) return false;
                if (!Application.isPlaying && !radiant.showInEditMode.value) return false;
#endif //UNITY_EDITOR

                m_usesReprojection = radiant.temporalReprojection.value && (!isSceneView || Application.isPlaying);
                m_usesCompareMode = radiant.compareMode.value && !isSceneView;
                renderPassEvent = settings.renderPassEvent;
                this.m_renderer = renderer;
                this.m_settings = settings;
                if (m_mat == null)
                {
                    m_mat = CoreUtils.CreateEngineMaterial(Shader.Find(c_shaderPath_RadiantGI));
                    m_mat.SetTexture(ShaderParams.NoiseTex, Resources.Load<Texture>(c_path_blueNoiseGI128RGB));
                }

                m_mat.SetInt(ShaderParams.StencilValue, radiant.stencilValue.value);
                m_mat.SetInt(ShaderParams.StencilCompareFunction, radiant.stencilCheck.value ? (int)radiant.stencilCompareFunction.value : (int)CompareFunction.Always);
                
                return true;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                ScriptableRenderPassInput input = ScriptableRenderPassInput.Color;
                if (m_settings.renderingPath == ERenderingPath.Forward)
                {
                    input |= ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth;
                }

                if (m_usesReprojection)
                {
                    input |= ScriptableRenderPassInput.Motion;
                }

                ConfigureInput(input);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                m_sourceDesc = renderingData.cameraData.cameraTargetDescriptor;
                m_sourceDesc.colorFormat = RenderTextureFormat.ARGBHalf;
                m_sourceDesc.useMipMap = false;
                m_sourceDesc.msaaSamples = 1;
                m_sourceDesc.depthBufferBits = 0;
                m_cameraTargetDesc = m_sourceDesc;

                float downsampling = radiant.downsampling.value;
                m_sourceDesc.width = (int)(m_sourceDesc.width / downsampling);
                m_sourceDesc.height = (int)(m_sourceDesc.height / downsampling);

                Camera cam = renderingData.cameraData.camera;
                m_camPos = cam.transform.position;

                CommandBuffer cmd = CommandBufferPool.Get(RGI_CBUF_NAME);
                cmd.Clear();

                RenderGI(cmd, cam);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            private void RenderGI(CommandBuffer cmd, Camera cam)
            {
                SetMatGLKeyword(m_mat);
                
#if UNITY_2022_2_OR_NEWER
                RTHandle source = m_renderer.cameraColorTargetHandle;
#else
                RenderTargetIdentifier source = m_renderer.cameraColorTarget;
#endif //UNITY_2022_2_OR_NEWER

                int smoothing = radiant.smoothing.value;
                RadiantGlobalIllumination.EDebugView debugView = radiant.debugView.value;
                bool usesBounce = radiant.rayBounce.value;
                int frameCount = Application.isPlaying ? Time.frameCount : 0;
                bool usesForward = m_settings.renderingPath != ERenderingPath.Deferred;
                float normalMapInfluence = radiant.normalMapInfluence.value;
                float lumaInfluence = radiant.lumaInfluence.value > 0 ? radiant.lumaInfluence.value * 100f : 20000;
                float downsampling = radiant.downsampling.value;
                int currentFrame = Time.frameCount;
                bool usesRSM = RadiantShadowMap.installed && radiant.fallbackReflectiveShadowMap.value && radiant.reflectiveShadowMapIntensity.value > 0;
                bool usesEmitters = radiant.virtualEmitters.value;

                // pass radiant settings to shader
                Vector4 tempVector = new Vector4(radiant.indirectIntensity.value, radiant.indirectMaxSourceBrightness.value, radiant.indirectDistanceAttenuation.value, radiant.rayReuse.value);
                m_mat.SetVector(ShaderParams.IndirectData, tempVector);
                tempVector = new Vector4(radiant.rayCount.value, radiant.rayMaxLength.value, radiant.rayMaxSamples.value, radiant.thickness.value);
                m_mat.SetVector(ShaderParams.RayData, tempVector);

                // some uniforms required by compare render feature so declared as global vectors instead of material properties
                tempVector = new Vector4(radiant.brightnessThreshold.value, radiant.brightnessMax.value, radiant.saturation.value, radiant.reflectiveShadowMapIntensity.value);
                cmd.SetGlobalVector(ShaderParams.ExtraData2, tempVector); // global because these params are needed by the compare pass

                m_mat.DisableKeyword(ShaderParams.SKW_FORWARD_AND_DEFERRED);
                m_mat.DisableKeyword(ShaderParams.SKW_FORWARD);
                if (usesForward)
                {
                    if (m_settings.renderingPath == ERenderingPath.Both)
                    {
                        m_mat.EnableKeyword(ShaderParams.SKW_FORWARD_AND_DEFERRED);
                    }
                    else
                    {
                        m_mat.EnableKeyword(ShaderParams.SKW_FORWARD);
                    }
                }

                if (radiant.rayBinarySearch.value)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_USES_BINARY_SEARCH);
                }
                else
                {
                    m_mat.DisableKeyword(ShaderParams.SKW_USES_BINARY_SEARCH);
                }

                if (radiant.rayCount.value > 1)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_USES_MULTIPLE_RAYS);
                }
                else
                {
                    m_mat.DisableKeyword(ShaderParams.SKW_USES_MULTIPLE_RAYS);
                }

                float nearFieldObscurance = radiant.nearFieldObscurance.value;
                bool useNFO = nearFieldObscurance > 0;
                if (useNFO)
                {
                    tempVector = new Vector4(radiant.nearFieldObscuranceMaxCameraDistance.value, (1f - radiant.nearFieldObscuranceOccluderDistance.value) * 10f, 0, 0);
                    cmd.SetGlobalVector(ShaderParams.ExtraData4, tempVector);
                    cmd.SetGlobalColor(ShaderParams.NFOTint, radiant.nearFieldObscuranceTintColor.value);
                    m_mat.EnableKeyword(ShaderParams.SKW_USES_NEAR_FIELD_OBSCURANCE);
                }
                else
                {
                    m_mat.DisableKeyword(ShaderParams.SKW_USES_NEAR_FIELD_OBSCURANCE);
                }

                if (cam.orthographic)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_ORTHO_SUPPORT);
                }
                else
                {
                    m_mat.DisableKeyword(ShaderParams.SKW_ORTHO_SUPPORT);
                }

                tempVector = new Vector4(radiant.aoInfluence.value, radiant.nearFieldObscuranceSpread.value * 0.5f, 1f / (radiant.nearCameraAttenuation.value + 0.0001f), nearFieldObscurance);
                cmd.SetGlobalVector(ShaderParams.ExtraData3, tempVector); // global because these params are needed by the compare pass

                // restricts to volume bounds?
                SetupVolumeBounds(cmd);

                // pass reprojection & other raymarch data
                if (m_usesReprojection)
                {
                    m_goldenRatioAcum += GOLDEN_RATIO * radiant.rayCount.value;
                    m_goldenRatioAcum %= 5000;
                }

                cmd.SetGlobalVector(ShaderParams.SourceSize, new Vector4(m_cameraTargetDesc.width, m_cameraTargetDesc.height, m_goldenRatioAcum, frameCount));
                cmd.SetGlobalVector(ShaderParams.ExtraData, new Vector4(radiant.rayJitter.value, 1f, normalMapInfluence, lumaInfluence));
                cmd.SetGlobalVector(ShaderParams.ExtraData5, new Vector4(radiant.specularContribution.value, downsampling, radiant.sourceBrightness.value, radiant.giWeight.value));

                // pass UNITY_MATRIX_V
                cmd.SetGlobalMatrix(ShaderParams.WorldToViewDir, cam.worldToCameraMatrix);

                // create downscaled depth
                RenderTextureDescriptor downDesc = m_cameraTargetDesc;
                downDesc.width = Mathf.Min(m_sourceDesc.width, downDesc.width / 2);
                downDesc.height = Mathf.Min(m_sourceDesc.height, downDesc.height / 2);

                int downHalfDescWidth = downDesc.width;
                int downHalfDescHeight = downDesc.height;

                // copy depth into an optimized render target
                int downsamplingDepth = 9 - radiant.raytracerAccuracy.value;
                RenderTextureDescriptor rtDownDepth = m_sourceDesc;
                rtDownDepth.width = Mathf.CeilToInt((float)rtDownDepth.width / downsamplingDepth);
                rtDownDepth.height = Mathf.CeilToInt((float)rtDownDepth.height / downsamplingDepth);
#if UNITY_WEBGL
                rtDownDepth.colorFormat = RenderTextureFormat.RFloat;
#else
                rtDownDepth.colorFormat = RenderTextureFormat.RHalf;
#endif //UNITY_WEBGL
                rtDownDepth.sRGB = false;
                cmd.GetTemporaryRT(ShaderParams.DownscaledDepthRT, rtDownDepth, FilterMode.Point);
                FullScreenBlit(cmd, ShaderParams.DownscaledDepthRT, EPass.CopyDepth);

                // early debug views
                switch (debugView)
                {
                    case RadiantGlobalIllumination.EDebugView.Albedo:
                        FullScreenBlit(cmd, source, EPass.Albedo);
                        return;
                    case RadiantGlobalIllumination.EDebugView.Normals:
                        FullScreenBlit(cmd, source, EPass.Normals);
                        return;
                    case RadiantGlobalIllumination.EDebugView.Specular:
                        FullScreenBlit(cmd, source, EPass.Specular);
                        return;
                    case RadiantGlobalIllumination.EDebugView.Depth:
                        m_mat.SetFloat(ShaderParams.DebugDepthMultiplier, radiant.debugDepthMultiplier.value);
                        FullScreenBlit(cmd, source, EPass.Depth);
                        return;
                }

                // are we reusing rays?
                if (!m_prevs.TryGetValue(cam, out PerCameraData frameAcumData))
                {
                    m_prevs[cam] = frameAcumData = new PerCameraData();
                }

                RenderTexture bounceRT = frameAcumData.rtBounce;

                RenderTargetIdentifier raycastInput = source;
                if (usesBounce)
                {
                    if (bounceRT != null && (bounceRT.width != m_cameraTargetDesc.width || bounceRT.height != m_cameraTargetDesc.height))
                    {
                        bounceRT.Release();
                        bounceRT = null;
                    }

                    if (bounceRT == null)
                    {
                        bounceRT = new RenderTexture(m_cameraTargetDesc);
                        bounceRT.Create();
                        frameAcumData.rtBounce = bounceRT;
                        frameAcumData.rtBounceCreationFrame = currentFrame;
                    }
                    else
                    {
                        if (currentFrame - frameAcumData.rtBounceCreationFrame > 2)
                        {
                            raycastInput = bounceRT; // only uses bounce rt a few frames after it's created
                        }
                    }
                }
                else if (bounceRT != null)
                {
                    bounceRT.Release();
                    DestroyImmediate(bounceRT);
                }

                // virtual emitters
                if (usesEmitters)
                {
                    float now = Time.time;
                    if (s_emittersForceRefresh)
                    {
                        s_emittersForceRefresh = false;
                        foreach (PerCameraData cameraData in m_prevs.Values)
                        {
                            cameraData.emittersSortTime = float.MinValue;
                        }
                    }

                    if (now - frameAcumData.emittersSortTime > 5 || (frameAcumData.emittersLastCameraPosition - m_camPos).sqrMagnitude > 25)
                    {
                        frameAcumData.emittersSortTime = now;
                        frameAcumData.emittersLastCameraPosition = m_camPos;
                        SortEmitters(cam);
                        frameAcumData.emittersSorted.Clear();
                        frameAcumData.emittersSorted.AddRange(s_emitters);
                    }

                    usesEmitters = SetupEmitters(cam, frameAcumData.emittersSorted);
                }

                if (usesEmitters)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_VIRTUAL_EMITTERS);
                }
                else
                {
                    m_mat.DisableKeyword(ShaderParams.SKW_VIRTUAL_EMITTERS);
                }

                // set the fallback mode
                m_mat.DisableKeyword(ShaderParams.SKW_REUSE_RAYS);
                m_mat.DisableKeyword(ShaderParams.SKW_FALLBACK_1_PROBE);
                m_mat.DisableKeyword(ShaderParams.SKW_FALLBACK_2_PROBES);

                bool usingProbes = false;
                if (radiant.fallbackReflectionProbes.value)
                {
                    if (SetupProbes(cmd, out int numProbes))
                    {
                        m_mat.EnableKeyword(numProbes == 1 ? ShaderParams.SKW_FALLBACK_1_PROBE : ShaderParams.SKW_FALLBACK_2_PROBES);
                        usingProbes = true;
                    }
                }

                if (!usingProbes)
                {
                    if (radiant.fallbackReuseRays.value && currentFrame - frameAcumData.rtAcumCreationFrame > 2 && radiant.rayReuse.value > 0 && frameAcumData.rtAcum != null)
                    {
                        RenderTargetIdentifier prevRT = new RenderTargetIdentifier(frameAcumData.rtAcum, 0, CubemapFace.Unknown, -1);
                        cmd.SetGlobalTexture(ShaderParams.PrevResolve, prevRT);
                        m_mat.EnableKeyword(ShaderParams.SKW_REUSE_RAYS);
                    }
                }

                // raycast & resolve
                RenderTextureDescriptor downscaledColorAndDepthDesc = m_sourceDesc;
                cmd.GetTemporaryRT(ShaderParams.DownscaledColorAndDepthRT, downscaledColorAndDepthDesc, FilterMode.Bilinear);

                cmd.GetTemporaryRT(ShaderParams.ResolveRT, m_sourceDesc, FilterMode.Bilinear);
                FullScreenBlit(cmd, raycastInput, ShaderParams.ResolveRT, EPass.Raycast);

                cmd.GetTemporaryRT(ShaderParams.Downscaled1RT, downDesc, FilterMode.Bilinear);
                cmd.GetTemporaryRT(ShaderParams.Downscaled1RTA, downDesc, FilterMode.Bilinear);

                // Prepare NFO
                if (useNFO)
                {
                    RenderTextureDescriptor nfoDesc = downDesc;
                    nfoDesc.colorFormat = RenderTextureFormat.RHalf;
                    cmd.GetTemporaryRT(ShaderParams.NFO_RT, nfoDesc, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(ShaderParams.NFOBlurRT, nfoDesc, FilterMode.Bilinear);
                    FullScreenBlit(cmd, ShaderParams.NFOBlurRT, EPass.NFO);
                    FullScreenBlit(cmd, ShaderParams.NFOBlurRT, ShaderParams.NFO_RT, EPass.NFOBlur);
                }

                // downscale & blur
                downDesc.width /= 2;
                downDesc.height /= 2;
                cmd.GetTemporaryRT(ShaderParams.Downscaled2RT, downDesc, FilterMode.Bilinear);
                int downscaledQuarterRT = ShaderParams.Downscaled2RT;

                switch (smoothing)
                {
                    case 0:
                    {
                        if (downsampling <= 1f)
                        {
                            FullScreenBlit(cmd, ShaderParams.ResolveRT, ShaderParams.Downscaled1RT, EPass.Copy);
                            FullScreenBlit(cmd, ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RT, EPass.WideFilter);
                        }
                        else
                        {
                            cmd.SetGlobalVector(ShaderParams.ExtraData, new Vector4(radiant.rayJitter.value, 1.5f, normalMapInfluence, lumaInfluence));
                            FullScreenBlit(cmd, ShaderParams.ResolveRT, ShaderParams.Downscaled2RT, EPass.WideFilter);
                        }

                        if (usesRSM)
                        {
                            FullScreenBlit(cmd, ShaderParams.Downscaled2RT, EPass.RSM);
                        }
                    }
                        break;
                    
                    case 1:
                    {
                        cmd.GetTemporaryRT(ShaderParams.Downscaled2RTA, downDesc, FilterMode.Bilinear);
                        if (downsampling <= 1f)
                        {
                            FullScreenBlit(cmd, ShaderParams.ResolveRT, ShaderParams.Downscaled1RT, EPass.Copy);
                            FullScreenBlit(cmd, ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RTA, EPass.Copy);
                        }
                        else
                        {
                            FullScreenBlit(cmd, ShaderParams.ResolveRT, ShaderParams.Downscaled2RTA, EPass.CopyMultiTaps);
                        }

                        if (usesRSM)
                        {
                            FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, EPass.RSM);
                        }

                        FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, EPass.WideFilter);
                    }
                        break;
                    
                    case 2:
                    {
                        cmd.GetTemporaryRT(ShaderParams.Downscaled2RTA, downDesc, FilterMode.Bilinear);
                        if (downsampling <= 1f)
                        {
                            FullScreenBlit(cmd, ShaderParams.ResolveRT, ShaderParams.Downscaled1RT, EPass.Copy);
                            FullScreenBlit(cmd, ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RT, EPass.BlurHorizontal);
                            FullScreenBlit(cmd, ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, EPass.BlurVertical);
                            if (usesRSM)
                            {
                                FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, EPass.RSM);
                            }

                            FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, EPass.WideFilter);
                        }
                        else
                        {
                            FullScreenBlit(cmd, ShaderParams.ResolveRT, ShaderParams.Downscaled2RT, EPass.BlurHorizontal);
                            FullScreenBlit(cmd, ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, EPass.BlurVertical);
                            if (usesRSM)
                            {
                                FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, EPass.RSM);
                            }

                            FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, EPass.WideFilter);
                        }
                    }
                        break;

                    case 4:
                    {
                        cmd.GetTemporaryRT(ShaderParams.Downscaled2RTA, downDesc, FilterMode.Bilinear);
                        FullScreenBlit(cmd, ShaderParams.ResolveRT, ShaderParams.Downscaled1RTA, EPass.BlurHorizontal);
                        FullScreenBlit(cmd, ShaderParams.Downscaled1RTA, ShaderParams.Downscaled1RT, EPass.BlurVertical);
                        FullScreenBlit(cmd, ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RT, EPass.BlurHorizontal);
                        FullScreenBlit(cmd, ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, EPass.BlurVertical);
                        if (usesRSM)
                        {
                            FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, EPass.RSM);
                        }

                        FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, EPass.WideFilter);
                        cmd.SetGlobalVector(ShaderParams.ExtraData, new Vector4(radiant.rayJitter.value, 1.25f, normalMapInfluence, lumaInfluence));
                        FullScreenBlit(cmd, ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, EPass.WideFilter);
                        downscaledQuarterRT = ShaderParams.Downscaled2RTA;
                    }
                        break;

                    default:
                    {
                        cmd.GetTemporaryRT(ShaderParams.Downscaled2RTA, downDesc, FilterMode.Bilinear);
                        FullScreenBlit(cmd, ShaderParams.ResolveRT, ShaderParams.Downscaled1RTA, EPass.BlurHorizontal);
                        FullScreenBlit(cmd, ShaderParams.Downscaled1RTA, ShaderParams.Downscaled1RT, EPass.BlurVertical);
                        FullScreenBlit(cmd, ShaderParams.Downscaled1RT, ShaderParams.Downscaled2RT, EPass.BlurHorizontal);
                        FullScreenBlit(cmd, ShaderParams.Downscaled2RT, ShaderParams.Downscaled2RTA, EPass.BlurVertical);
                        if (usesRSM)
                        {
                            FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, EPass.RSM);
                        }

                        if (downsampling > 1)
                        {
                            cmd.SetGlobalVector(ShaderParams.ExtraData, new Vector4(radiant.rayJitter.value, 1.25f, normalMapInfluence, lumaInfluence));
                        }

                        FullScreenBlit(cmd, ShaderParams.Downscaled2RTA, ShaderParams.Downscaled2RT, EPass.WideFilter);
                    }
                        break;
                }

                // Upscale
                FullScreenBlit(cmd, downscaledQuarterRT, ShaderParams.Downscaled1RTA, EPass.Upscale);

                computedGIRT = ShaderParams.Downscaled1RTA;
                RenderTexture prev = frameAcumData?.rtAcum;

                if (m_usesReprojection)
                {
                    if (prev != null && (prev.width != downHalfDescWidth || prev.height != downHalfDescHeight))
                    {
                        prev.Release();
                        prev = null;
                    }

                    RenderTextureDescriptor acumDesc = m_sourceDesc;
                    acumDesc.width = downHalfDescWidth;
                    acumDesc.height = downHalfDescHeight;
                    float responseSpeed = radiant.temporalResponseSpeed.value;
                    EPass acumEPass = EPass.TemporalAccum;

                    if (prev == null)
                    {
                        prev = new RenderTexture(acumDesc);
                        prev.Create();
                        frameAcumData.rtAcum = prev;
                        frameAcumData.lastCameraPosition = m_camPos;
                        frameAcumData.rtAcumCreationFrame = currentFrame;
                        acumEPass = EPass.Copy;
                    }
                    else
                    {
                        float camTranslationDelta = Vector3.Distance(m_camPos, frameAcumData.lastCameraPosition);
                        frameAcumData.lastCameraPosition = m_camPos;
                        responseSpeed += camTranslationDelta * radiant.temporalCameraTranslationResponse.value;
                    }

                    tempVector = new Vector4(Mathf.Clamp01(responseSpeed * Time.unscaledDeltaTime), radiant.temporalDepthRejection.value, radiant.temporalChromaThreshold.value, 0);
                    m_mat.SetVector(ShaderParams.TemporalData, tempVector);

                    RenderTargetIdentifier prevRT = new RenderTargetIdentifier(prev, 0, CubemapFace.Unknown, -1);
                    cmd.SetGlobalTexture(ShaderParams.PrevResolve, prevRT);
                    cmd.GetTemporaryRT(ShaderParams.TempAcum, acumDesc, FilterMode.Bilinear);
                    FullScreenBlit(cmd, computedGIRT, ShaderParams.TempAcum, acumEPass);
                    FullScreenBlit(cmd, ShaderParams.TempAcum, prevRT, EPass.CopyExact);
                    computedGIRT = ShaderParams.TempAcum;
                }
                else if (prev != null)
                {
                    prev.Release();
                    DestroyImmediate(prev);
                }

                // prepare output blending
                cmd.GetTemporaryRT(ShaderParams.InputRT, m_cameraTargetDesc, FilterMode.Point);
                FullScreenBlit(cmd, source, ShaderParams.InputRT, EPass.CopyExact);

                if (m_usesCompareMode)
                {
                    cmd.GetTemporaryRT(ShaderParams.CompareTex, m_cameraTargetDesc, FilterMode.Point); // needed by the compare pass
                    if (usesBounce)
                    {
                        FullScreenBlit(cmd, computedGIRT, ShaderParams.CompareTex, EPass.Compose);
                        FullScreenBlit(cmd, ShaderParams.CompareTex, bounceRT, EPass.CopyExact);
                    }
                }
                else if (usesBounce)
                {
                    FullScreenBlit(cmd, computedGIRT, bounceRT, EPass.Compose);
                    FullScreenBlitToCamera(cmd, bounceRT, EPass.CopyExact);
                }
                else
                {
                    FullScreenBlitToCamera(cmd, computedGIRT, EPass.Compose);
                }
                
                switch (debugView)
                {
                    case RadiantGlobalIllumination.EDebugView.DownscaledHalf:
                        FullScreenBlit(cmd, ShaderParams.Downscaled1RT, source, EPass.CopyExact);
                        return;
                    case RadiantGlobalIllumination.EDebugView.DownscaledQuarter:
                        FullScreenBlit(cmd, downscaledQuarterRT, source, EPass.CopyExact);
                        return;
                    case RadiantGlobalIllumination.EDebugView.UpscaleToHalf:
                        FullScreenBlit(cmd, ShaderParams.Downscaled1RTA, source, EPass.CopyExact);
                        return;
                    case RadiantGlobalIllumination.EDebugView.Raycast:
                        FullScreenBlit(cmd, ShaderParams.ResolveRT, source, EPass.CopyExact);
                        return;
                    case RadiantGlobalIllumination.EDebugView.ReflectiveShadowMap:
                        if (usesRSM)
                        {
                            FullScreenBlit(cmd, source, EPass.RSM_Debug);
                        }
                        return;
                    case RadiantGlobalIllumination.EDebugView.TemporalAccumulationBuffer:
                        if (m_usesReprojection)
                        {
                            FullScreenBlit(cmd, ShaderParams.TempAcum, source, EPass.CopyExact);
                        }
                        return;
                    case RadiantGlobalIllumination.EDebugView.FinalGI:
                        FullScreenBlit(cmd, computedGIRT, source, EPass.FinalGIDebug);
                        return;
                }
            }
            
            private void FullScreenBlit(CommandBuffer cmd, RenderTargetIdentifier destination, EPass ePass)
            {
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
                cmd.DrawMesh(FullscreenMesh, Matrix4x4.identity, m_mat, 0, (int)ePass);
            }

            private void FullScreenBlit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, EPass ePass)
            {
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
                cmd.SetGlobalTexture(ShaderParams.MainTex, source);
                cmd.DrawMesh(FullscreenMesh, Matrix4x4.identity, m_mat, 0, (int)ePass);
            }

            private void FullScreenBlitToCamera(CommandBuffer cmd, RenderTargetIdentifier source, EPass ePass)
            {
#if UNITY_2022_2_OR_NEWER
                RTHandle destination = m_renderer.cameraColorTargetHandle;
#else
                RenderTargetIdentifier destination = m_renderer.cameraColorTarget;
#endif //UNITY_2022_2_OR_NEWER

                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
                cmd.SetGlobalTexture(ShaderParams.MainTex, source);
                cmd.DrawMesh(FullscreenMesh, Matrix4x4.identity, m_mat, 0, (int)ePass);
            }
            
            private float CalculateProbeWeight(Vector3 wpos, Vector3 probeBoxMin, Vector3 probeBoxMax, float blendDistance)
            {
                Vector3 weightDir = Vector3.Min(wpos - probeBoxMin, probeBoxMax - wpos) / blendDistance;
                return Mathf.Clamp01(Mathf.Min(weightDir.x, Mathf.Min(weightDir.y, weightDir.z)));
            }
            
            private bool SetupProbes(CommandBuffer cmd, out int numProbes)
            {
                numProbes = PickNearProbes(out ReflectionProbe probe1, out ReflectionProbe probe2);
                if (numProbes == 0) return false;
                if (!probe1.bounds.Contains(m_camPos)) return false;
                if (numProbes >= 2 && !probe2.bounds.Contains(m_camPos)) numProbes = 1;

                float probe1Weight = 0, probe2Weight = 0;
                if (numProbes >= 1)
                {
                    Shader.SetGlobalTexture(ShaderParams.Probe1Cube, probe1.texture);
                    Shader.SetGlobalVector(ShaderParams.Probe1HDR, probe1.textureHDRDecodeValues);
                    Bounds probe1Bounds = probe1.bounds;
                    probe1Weight = CalculateProbeWeight(m_camPos, probe1Bounds.min, probe1Bounds.max, probe1.blendDistance);
                }

                if (numProbes >= 2)
                {
                    Shader.SetGlobalTexture(ShaderParams.Probe2Cube, probe2.texture);
                    Shader.SetGlobalVector(ShaderParams.Probe2HDR, probe1.textureHDRDecodeValues);
                    Bounds probe2Bounds = probe2.bounds;
                    probe2Weight = CalculateProbeWeight(m_camPos, probe2Bounds.min, probe2Bounds.max, probe2.blendDistance);
                }

                float probesIntensity = radiant.probesIntensity.value;
                cmd.SetGlobalVector(ShaderParams.ProbeData, new Vector4(probe1Weight * probesIntensity, probe2Weight * probesIntensity, 0, 0));

                return true;
            }

            private int PickNearProbes(out ReflectionProbe probe1, out ReflectionProbe probe2)
            {
                int probesCount = s_probes.Count;
                probe1 = probe2 = null;
                if (probesCount == 0)
                {
                    return 0;
                }

                if (probesCount == 1)
                {
                    probe1 = s_probes[0];
                    return 1;
                }

                float probe1Value = float.MaxValue;
                float probe2Value = float.MaxValue;
                for (int k = 0; k < probesCount; k++)
                {
                    ReflectionProbe probe = s_probes[k];
                    float probeValue = ComputeProbeValue(m_camPos, probe);
                    if (probeValue < probe2Value)
                    {
                        probe2 = probe;
                        probe2Value = probeValue;
                        if (probe2Value < probe1Value)
                        {
                            // swap probe1 & probe2
                            probeValue = probe1Value;
                            probe = probe1;
                            probe1 = probe2;
                            probe1Value = probe2Value;
                            probe2 = probe;
                            probe2Value = probeValue;
                        }
                    }
                }

                return 2;
            }

            private float ComputeProbeValue(Vector3 camPos, ReflectionProbe probe)
            {
                Vector3 probePos = probe.transform.position;
                float d = (probePos - camPos).sqrMagnitude * (probe.importance + 1) * 1000;
                if (!probe.bounds.Contains(camPos)) d += 100000;
                return d;
            }

            private void SetupVolumeBounds(CommandBuffer cmd)
            {
                if (!radiant.limitToVolumeBounds.value)
                {
                    cmd.SetGlobalVector(ShaderParams.BoundsXZ, s_unlimitedBounds);
                    return;
                }

                if (m_volumes == null)
                {
                    m_volumes = VolumeManager.instance.GetVolumes(-1);
                }

                int volumeCount = m_volumes.Length;
                for (int k = 0; k < volumeCount; k++)
                {
                    Volume volume = m_volumes[k];
                    if (volume == null) continue;
                    List<Collider> colliders = volume.colliders;
                    int colliderCount = colliders.Count;
                    for (int j = 0; j < colliderCount; j++)
                    {
                        Collider collider = colliders[j];
                        if (collider == null) continue;
                        Bounds bounds = collider.bounds;
                        if (collider.bounds.Contains(m_camPos) && volume.sharedProfile.Has<RadiantGlobalIllumination>())
                        {
                            Vector4 effectBounds = new Vector4(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                            cmd.SetGlobalVector(ShaderParams.BoundsXZ, effectBounds);
                            return;
                        }
                    }
                }
            }

            private bool SetupEmitters(Camera cam, List<RadiantVirtualEmitter> emitters)
            {
                // copy emitters data
                if (m_emittersBoxMax == null || m_emittersBoxMax.Length != MAX_EMITTERS)
                {
                    m_emittersBoxMax = new Vector4[MAX_EMITTERS];
                    m_emittersBoxMin = new Vector4[MAX_EMITTERS];
                    m_emittersColors = new Vector4[MAX_EMITTERS];
                    m_emittersPositions = new Vector4[MAX_EMITTERS];
                }

                int emittersCount = 0;

                const int EMITTERS_BUDGET = 150; // max number of emitters to be processed per frame
                int emittersMax = Mathf.Min(EMITTERS_BUDGET, emitters.Count);

                GeometryUtility.CalculateFrustumPlanes(cam, m_cameraPlanes);

                for (int k = 0; k < emittersMax; k++)
                {
                    RadiantVirtualEmitter emitter = emitters[k];

                    // Cull emitters

                    // disabled emitter?
                    if (emitter == null || !emitter.isActiveAndEnabled) continue;

                    // emitter with no intensity or range?
                    if (emitter.intensity <= 0 || emitter.range <= 0) continue;

                    // emitter with black color (nothing to inject)?
                    Vector4 colorAndRange = emitter.GetGIColorAndRange();
                    if (colorAndRange.x == 0 && colorAndRange.y == 0 && colorAndRange.z == 0) continue;

                    // emitter bounds out of camera frustum
                    Bounds emitterBounds = emitter.GetBounds();
                    if (!GeometryUtility.TestPlanesAABB(m_cameraPlanes, emitterBounds)) continue;

                    // add emitter
                    Vector3 emitterPosition = emitter.transform.position;
                    m_emittersPositions[emittersCount] = emitterPosition;

                    m_emittersColors[emittersCount] = colorAndRange;

                    Vector3 boxMin = emitterBounds.min;
                    Vector3 boxMax = emitterBounds.max;

                    float lightRangeSqr = colorAndRange.w * colorAndRange.w;
                    // Commented out for future versions if needed
                    //float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                    //float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                    //float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                    //float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                    float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightRangeSqr);

                    float pointAttenX = oneOverLightRangeSqr;
                    //float pointAttenY = lightRangeSqrOverFadeRangeSqr;

                    m_emittersBoxMin[emittersCount] = new Vector4(boxMin.x, boxMin.y, boxMin.z, pointAttenX);
                    m_emittersBoxMax[emittersCount] = new Vector4(boxMax.x, boxMax.y, boxMax.z, 0); // pointAttenY

                    emittersCount++;
                    if (emittersCount >= MAX_EMITTERS) break;
                }

                if (emittersCount == 0) return false;

                Shader.SetGlobalVectorArray(ShaderParams.EmittersPositions, m_emittersPositions);
                Shader.SetGlobalVectorArray(ShaderParams.EmittersBoxMin, m_emittersBoxMin);
                Shader.SetGlobalVectorArray(ShaderParams.EmittersBoxMax, m_emittersBoxMax);
                Shader.SetGlobalVectorArray(ShaderParams.EmittersColors, m_emittersColors);
                Shader.SetGlobalInt(ShaderParams.EmittersCount, emittersCount);

                return true;
            }

            private void SortEmitters(Camera cam)
            {
                s_emitters.Sort(EmittersDistanceComparer);
            }

            private int EmittersDistanceComparer(RadiantVirtualEmitter p1, RadiantVirtualEmitter p2)
            {
                Vector3 p1Pos = p1.transform.position;
                Vector3 p2Pos = p2.transform.position;
                float d1 = (p1Pos - m_camPos).sqrMagnitude;
                float d2 = (p2Pos - m_camPos).sqrMagnitude;
                Bounds p1bounds = p1.GetBounds();
                Bounds p2bounds = p2.GetBounds();
                if (!p1bounds.Contains(m_camPos)) d1 += 100000;
                if (!p2bounds.Contains(m_camPos)) d2 += 100000;
                if (d1 < d2) return -1;
                else if (d1 > d2) return 1;
                return 0;
            }

            public void Cleanup()
            {
                CoreUtils.Destroy(m_mat);
                if (m_prevs != null)
                {
                    foreach (PerCameraData fad in m_prevs.Values)
                    {
                        if (fad.rtAcum != null)
                        {
                            fad.rtAcum.Release();
                            DestroyImmediate(fad.rtAcum);
                        }

                        if (fad.rtBounce != null)
                        {
                            fad.rtBounce.Release();
                            DestroyImmediate(fad.rtBounce);
                        }
                    }

                    m_prevs.Clear();
                }
            }
        }
        
        /// <summary>
        /// 比较 Pass
        /// </summary>
        private class RadiantComparePass : ScriptableRenderPass
        {
            private const string RGI_CBUF_NAME = "RadiantGICompare";
            
            private Material m_mat;
            private RadiantGlobalIllumination m_radiant;
            private ScriptableRenderer m_renderer;
            private RadiantPass m_radiantPass;
            private RadiantRenderFeature m_settings;

            public bool Setup(ScriptableRenderer renderer, RadiantRenderFeature settings, RadiantPass radiantPass)
            {
                m_radiant = VolumeManager.instance.stack.GetComponent<RadiantGlobalIllumination>();
                if (m_radiant == null || !m_radiant.IsActive() || m_radiant.debugView.value != RadiantGlobalIllumination.EDebugView.None) return false;

#if UNITY_EDITOR
                if (!Application.isPlaying && !m_radiant.showInEditMode.value) return false;
#endif

                if (!m_radiant.compareMode.value) return false;

                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
                this.m_settings = settings;
                this.m_renderer = renderer;
                this.m_radiantPass = radiantPass;
                if (m_mat == null)
                {
                    m_mat = CoreUtils.CreateEngineMaterial(Shader.Find(c_shaderPath_RadiantGI));
                }

                return true;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(RGI_CBUF_NAME);
                cmd.Clear();

                m_mat.DisableKeyword(ShaderParams.SKW_FORWARD_AND_DEFERRED);
                m_mat.DisableKeyword(ShaderParams.SKW_FORWARD);
                if (m_settings.renderingPath == ERenderingPath.Both)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_FORWARD_AND_DEFERRED);
                }
                else if (m_settings.renderingPath == ERenderingPath.Forward)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_FORWARD);
                }

                if (m_radiant.virtualEmitters.value)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_VIRTUAL_EMITTERS);
                }
                else
                {
                    m_mat.DisableKeyword(ShaderParams.SKW_VIRTUAL_EMITTERS);
                }

                float nearFieldObscurance = m_radiant.nearFieldObscurance.value;
                if (nearFieldObscurance > 0)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_USES_NEAR_FIELD_OBSCURANCE);
                }
                else
                {
                    m_mat.DisableKeyword(ShaderParams.SKW_USES_NEAR_FIELD_OBSCURANCE);
                }

                float angle = m_radiant.compareSameSide.value ? Mathf.PI * 0.5f : m_radiant.compareLineAngle.value;
                m_mat.SetVector(ShaderParams.CompareParams,
                    new Vector4(Mathf.Cos(angle), Mathf.Sin(angle), m_radiant.compareSameSide.value ? m_radiant.comparePanning.value : -10, m_radiant.compareLineWidth.value));
                m_mat.SetInt(ShaderParams.StencilValue, m_radiant.stencilValue.value);
                m_mat.SetInt(ShaderParams.StencilCompareFunction, m_radiant.stencilCheck.value ? (int)m_radiant.stencilCompareFunction.value : (int)CompareFunction.Always);

#if UNITY_2022_2_OR_NEWER
                RTHandle source = m_renderer.cameraColorTargetHandle;
#else
                RenderTargetIdentifier source = m_renderer.cameraColorTarget;
#endif
                FullScreenBlit(cmd, source, ShaderParams.InputRT, EPass.CopyExact); // include transparent objects in the original compare texture
                FullScreenBlit(cmd, m_radiantPass.computedGIRT, ShaderParams.CompareTex, EPass.Compose); // add gi
                FullScreenBlitToCamera(cmd, ShaderParams.InputRT, EPass.Compare); // render the split

                context.ExecuteCommandBuffer(cmd);

                CommandBufferPool.Release(cmd);
            }

            private void FullScreenBlit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, EPass ePass)
            {
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
                cmd.SetGlobalTexture(ShaderParams.MainTex, source);
                cmd.DrawMesh(FullscreenMesh, Matrix4x4.identity, m_mat, 0, (int)ePass);
            }
            
            private void FullScreenBlitToCamera(CommandBuffer cmd, RenderTargetIdentifier source, EPass ePass)
            {
#if UNITY_2022_2_OR_NEWER
                RTHandle destination = m_renderer.cameraColorTargetHandle;
                RTHandle destinationDepth = m_renderer.cameraDepthTargetHandle;
#else
                RenderTargetIdentifier destination = m_renderer.cameraColorTarget;
                RenderTargetIdentifier destinationDepth = m_renderer.cameraDepthTarget;
#endif

                cmd.SetRenderTarget(destination, destinationDepth, 0, CubemapFace.Unknown, -1);
                cmd.SetGlobalTexture(ShaderParams.MainTex, source);
                cmd.DrawMesh(FullscreenMesh, Matrix4x4.identity, m_mat, 0, (int)ePass);
            }

            public void Cleanup()
            {
                CoreUtils.Destroy(m_mat);
            }
        }
        
        /// <summary>
        /// 有机灯 Pass
        /// </summary>
        private class RadiantOrganicLightPass : ScriptableRenderPass
        {
            private enum EPass
            {
                OrganicLight = 0
            }

            private Material m_mat;
            private DeferredLights m_deferredLights;

            private Texture2D m_noiseTex;
            private Vector3 m_offset;

            public bool Setup(RadiantGlobalIllumination radiant, ScriptableRenderer renderer, bool isSceneView)
            {
                if (radiant == null || radiant.organicLight.value <= 0) return false;

#if UNITY_EDITOR
                if (isSceneView && !radiant.showInSceneView.value) return false;
                if (!Application.isPlaying && !radiant.showInEditMode.value) return false;
#endif //UNITY_EDITOR
                
                DeferredLights deferredLights = ((UniversalRenderer)renderer).deferredLights;
                if (deferredLights == null) return false;

                renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
                m_deferredLights = deferredLights;

                if (m_mat == null)
                {
                    m_mat = CoreUtils.CreateEngineMaterial(Shader.Find(c_shaderPath_RadiantGI_OrganicLight));
                }

                if (m_noiseTex == null)
                {
                    m_noiseTex = Resources.Load<Texture2D>(c_path_NoiseTex);
                }

                m_mat.SetTexture(ShaderParams.NoiseTex, m_noiseTex);
                m_mat.SetVector(ShaderParams.OrganicLightData,
                    new Vector4(1.001f - radiant.organicLightSpread.value, radiant.organicLight.value, radiant.organicLightThreshold.value, radiant.organicLightNormalsInfluence.value));
                m_mat.SetColor(ShaderParams.OrganicLightTint, radiant.organicLightTintColor.value);
                m_offset += radiant.organicLightAnimationSpeed.value * Time.deltaTime;
                m_offset.x %= 10000f;
                m_offset.y %= 10000f;
                m_offset.z %= 10000f;
                m_mat.SetVector(ShaderParams.OrganicLightOffset, m_offset);

                if (radiant.organicLightDistanceScaling.value)
                {
                    m_mat.EnableKeyword(ShaderParams.SKW_DISTANCE_BLENDING);
                }
                else
                {
                    m_mat.DisableKeyword(ShaderParams.SKW_DISTANCE_BLENDING);
                }

                return true;
            }
            
#if UNITY_2022_1_OR_NEWER
            private RTHandle GetAlbedoFromGbuffer()
            {
                return m_deferredLights.GbufferAttachments[m_deferredLights.GBufferAlbedoIndex];
            }
#else
            RenderTargetIdentifier GetAlbedoFromGbuffer()
            {
                return m_deferredLights.GbufferAttachmentIdentifiers[m_deferredLights.GBufferAlbedoIndex];
            }
#endif

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
#if UNITY_2022_1_OR_NEWER
                RTHandle m_gbufferAttachmentsHandle = GetAlbedoFromGbuffer();
                ConfigureTarget(m_gbufferAttachmentsHandle, m_deferredLights.DepthAttachmentHandle);
#else
                RenderTargetIdentifier m_gbufferAttachmentsIdentifier = GetAlbedoFromGbuffer();
                m_gbufferAttachmentsIdentifier = new RenderTargetIdentifier(m_gbufferAttachmentsIdentifier, 0, CubemapFace.Unknown, -1);
                ConfigureTarget(m_gbufferAttachmentsIdentifier, m_deferredLights.DepthAttachmentIdentifier);
#endif
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("Radiant GI Organic Light");
                cmd.DrawMesh(FullscreenMesh, Matrix4x4.identity, m_mat, 0, (int)EPass.OrganicLight);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Cleanup()
            {
                CoreUtils.Destroy(m_mat);
            }
        }

        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

        [Tooltip("Select the rendering mode according to the URP asset.")]
        public ERenderingPath renderingPath = ERenderingPath.Deferred;

        [Tooltip("Allows Radiant to be executed even if camera has Post Processing option disabled.")]
        public bool ignorePostProcessingOption = true;

        private RadiantPass m_radiantPass;
        private RadiantComparePass m_comparePass;
        private RadiantOrganicLightPass m_organicLightPass;

        private void OnDisable()
        {
            if (m_radiantPass != null)
            {
                m_radiantPass.Cleanup();
            }

            if (m_comparePass != null)
            {
                m_comparePass.Cleanup();
            }

            if (m_organicLightPass != null)
            {
                m_organicLightPass.Cleanup();
            }
        }

        public override void Create()
        {
            m_radiantPass = new RadiantPass();
            m_comparePass = new RadiantComparePass();
            m_organicLightPass = new RadiantOrganicLightPass();
            s_emittersForceRefresh = true;
        }

        public static bool needRTRefresh;
        public static bool isRenderingInDeferred;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            isRenderingInDeferred = renderingPath != ERenderingPath.Forward;
#endif
            if (!renderingData.cameraData.postProcessEnabled && !ignorePostProcessingOption) return;

            Camera cam = renderingData.cameraData.camera;
            bool isSceneView = cam.cameraType == CameraType.SceneView;
            if (cam.cameraType != CameraType.Game && !isSceneView) return;
            if (renderingData.cameraData.renderType != CameraRenderType.Base) return;

#if UNITY_EDITOR
            if (UnityEditor.ShaderUtil.anythingCompiling)
            {
                needRTRefresh = true;
            }

            if (needRTRefresh)
            {
                needRTRefresh = false;
                m_radiantPass.Cleanup();
            }
#endif //UNITY_EDITOR
            
            RadiantGlobalIllumination radiant = VolumeManager.instance.stack.GetComponent<RadiantGlobalIllumination>();

            if (m_organicLightPass.Setup(radiant, renderer, isSceneView))
            {
                renderer.EnqueuePass(m_organicLightPass);
            }

            if (m_radiantPass.Setup(radiant, renderer, this, isSceneView))
            {
                renderer.EnqueuePass(m_radiantPass);
                if (!isSceneView)
                {
                    if (m_comparePass.Setup(renderer, this, m_radiantPass))
                    {
                        renderer.EnqueuePass(m_comparePass);
                    }
                }
            }
        }

        public static void RegisterReflectionProbe(ReflectionProbe probe)
        {
            if (probe == null) return;
            if (!s_probes.Contains(probe))
            {
                s_probes.Add(probe);
            }
        }

        public static void UnregisterReflectionProbe(ReflectionProbe probe)
        {
            if (probe == null) return;
            if (s_probes.Contains(probe))
            {
                s_probes.Remove(probe);
            }
        }

        public static void RegisterVirtualEmitter(RadiantVirtualEmitter emitter)
        {
            if (emitter == null) return;
            if (!s_emitters.Contains(emitter))
            {
                s_emitters.Add(emitter);
                s_emittersForceRefresh = true;
            }
        }

        public static void UnregisterVirtualEmitter(RadiantVirtualEmitter emitter)
        {
            if (emitter == null) return;
            if (s_emitters.Contains(emitter))
            {
                s_emitters.Remove(emitter);
                s_emittersForceRefresh = true;
            }
        }
    }
}