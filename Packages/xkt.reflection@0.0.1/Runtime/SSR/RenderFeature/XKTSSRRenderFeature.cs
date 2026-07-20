using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace XKnight.Reflection.Runtime
{
    public class XKTSSRRenderFeature : ScriptableRendererFeature
    {
        class SSRRenderPass : ScriptableRenderPass
        {
            const string SSRTag = "SSR";
            const float GOLDEN_RATIO = 0.618033989f;
            static public XKTSSR settings;

            /// <summary>
            /// 模糊用到的临时纹理ID
            /// </summary>
            static int[] rtPyramid;

            /// <summary>
            /// 降采样模糊次数
            /// </summary>
            const int MIP_COUNT = 5;

            enum Pass
            {
                CopyDepth = 0,
                CopyExact = 1,
                SSRSurf = 2,
                Resolve = 3,
                BlurHoriz = 4,
                BlurVert = 5,
                Combine = 6,
                CombineWithCompare = 7,
                Debug = 8,
                DebugRayCast = 9,
                // 
                // GBuffPass = 8,
                // Copy = 9,
                // TemporalAccum = 10,
            }

            //需要剔除反射的部分
            //static readonly List<ExcludeReflections> excludedReflections = new List<ExcludeReflections>();

            // public static void RegisterExcludeReflections(ExcludeReflections o) {
            //     if (!excludedReflections.Contains(o)) {
            //         excludedReflections.Add(o);
            //     }
            // }
            //
            // public static void UnregisterExcludeReflections(ExcludeReflections o) {
            //     if (excludedReflections.Contains(o)) {
            //         excludedReflections.Remove(o);
            //     }
            // }

            private static Material mat;

            /// <summary>
            /// 相机平面
            /// </summary>
            static readonly Plane[] frustumPlanes = new Plane[6];

            private Texture2D noiseTex;

            /// <summary>
            /// 全屏三角形
            /// </summary>
            static Mesh _fullScreenMesh;

            static Mesh fullscreenMesh
            {
                get
                {
                    if (_fullScreenMesh != null)
                    {
                        return _fullScreenMesh;
                    }

                    float num = 1f;
                    float num2 = 0f;
                    Mesh val = new Mesh();
                    _fullScreenMesh = val;
                    _fullScreenMesh.SetVertices(new List<Vector3>
                    {
                        new Vector3(-1f, -1f, 0f),
                        new Vector3(-1f, 1f, 0f),
                        new Vector3(1f, -1f, 0f),
                        new Vector3(1f, 1f, 0f)
                    });
                    _fullScreenMesh.SetUVs(0, new List<Vector2>
                    {
                        new Vector2(0f, num2),
                        new Vector2(0f, num),
                        new Vector2(1f, num2),
                        new Vector2(1f, num)
                    });
                    _fullScreenMesh.SetIndices(new int[6] { 0, 1, 2, 2, 1, 3 }, (MeshTopology)0, 0, false);
                    _fullScreenMesh.UploadMeshData(true);
                    return _fullScreenMesh;
                }
            }

            public bool Setup(Shader ssrShader)
            {
                settings = VolumeManager.instance.stack.GetComponent<XKTSSR>();
                if (ssrShader==null||settings == null || !settings.IsActive()) return false;

                if (ssrShader!=null&&mat == null)
                {
                    mat = CoreUtils.CreateEngineMaterial(ssrShader);
                }

                if (noiseTex == null)
                {
                    noiseTex = XKnightRenderPipeline.asset.textures.blueNoise64LTex;
                }


                mat.SetTexture(XKTSSRShaderProperties.NoiseTex, noiseTex);
                mat.SetVector(XKTSSRShaderProperties.SSRSettings2,
                    new Vector4(settings.jitter.value, settings.contactHardening.value + 0.0001f,
                        settings.reflectionsMultiplier.value, 0));
                mat.SetVector(XKTSSRShaderProperties.SSRSettings4,
                    new Vector4(settings.separationPos.value, settings.reflectionsMinIntensity.value,
                        settings.reflectionsMaxIntensity.value, settings.specularSoftenPower.value));
                mat.SetVector(XKTSSRShaderProperties.SSRBlurStrength,
                    new Vector4(settings.blurStrength.value.x, settings.blurStrength.value.y,
                        settings.vignetteSize.value,
                        settings.vignettePower.value));
                mat.SetVector(XKTSSRShaderProperties.SSRSettings5,
                    new Vector4(settings.thickness.value*settings.thicknessFine.value, settings.smoothnessThreshold.value,
                        settings.skyboxIntensity.value, 0));
                float metallicBoost = settings.metallicBoost.value;
                mat.SetVector(XKTSSRShaderProperties.SSRSettings6,
                    new Vector4(settings.nearCameraAttenuationStart.value, settings.nearCameraAttenuationRange.value,
                        metallicBoost, settings.metallicBoostThreshold.value));
                // if (settings.specularControl.value) {
                //     mat.EnableKeyword(XKnightSSRShaderProperties.SKW_DENOISE);
                // } 
                // else 
                // {
                //     mat.DisableKeyword(XKnightSSRShaderProperties.SKW_DENOISE);
                // }
                mat.SetFloat(XKTSSRShaderProperties.MinimumBlur, settings.minimumBlur.value);
                //mat.SetInt(XKnightSSRShaderProperties.StencilValue, settings.stencilValue.value);
                //mat.SetInt(XKnightSSRShaderProperties.StencilCompareFunction, settings.stencilCheck.value ? (int)settings.stencilCompareFunction.value : (int)CompareFunction.Always);

                //TODO: BackFace
                // if (settings.computeBackFaces.value)
                // {
                //     Shader.EnableKeyword(XKnightSSRShaderProperties.BACK_FACES);
                //     Shader.SetGlobalFloat(XKnightSSRShaderProperties.MinimumThickness, settings.thicknessMinimum.value);
                // } 
                // else 
                // {
                //     Shader.DisableKeyword(XKnightSSRShaderProperties.BACK_FACES);
                //     
                // }

                if (settings.skyboxIntensity.value > 0)
                {
                    Shader.EnableKeyword(XKTSSRShaderProperties.SKYBOX);
                }
                else
                {
                    Shader.DisableKeyword(XKTSSRShaderProperties.SKYBOX);
                }
                if (settings.refineThickness.value) {
                    mat.EnableKeyword(XKTSSRShaderProperties.REFINE_THICKNESS);
                } else {
                    mat.DisableKeyword(XKTSSRShaderProperties.REFINE_THICKNESS);
                }


                if (rtPyramid == null || rtPyramid.Length != MIP_COUNT)
                {
                    rtPyramid = new int[MIP_COUNT];
                    for (int k = 0; k < rtPyramid.Length; k++)
                    {
                        rtPyramid[k] = Shader.PropertyToID("_BlurRTMip" + k);
                    }
                }

                return true;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;


                CommandBuffer cmd = CommandBufferPool.Get(SSRTag);


                RenderTextureDescriptor sourceDesc = renderingData.cameraData.cameraTargetDescriptor;
                //TODO: 是否开启HDR
                sourceDesc.colorFormat = RenderTextureFormat.ARGBHalf;
                ; //renderingData.cameraData.isHdrEnabled? RenderTextureFormat.ARGB32 :RenderTextureFormat.ARGBHalf;
                sourceDesc.width /= settings.downsampling.value;
                sourceDesc.height /= settings.downsampling.value;
                sourceDesc.msaaSamples = 1;
                //模拟LDS
                float goldenFactor = GOLDEN_RATIO;
                // if (settings.animatedJitter.value) {
                //     goldenFactor *= (Time.frameCount % 480);
                // }

                Shader.SetGlobalVector(XKTSSRShaderProperties.SSRSettings3,
                    new Vector4(sourceDesc.width, sourceDesc.height, goldenFactor, settings.depthBias.value));
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                var ReflectionsTex = RTHandles.Alloc(XKTSSRShaderProperties.ReflectionsTex);
                bool useReflectionsScripts = true;
                //反射组件数量
                int count = XKTReflections.instances.Count;
                if (count == 0) return;

                if (count > 0 && useReflectionsScripts)
                {
                    bool firstSSR = true;
                    //TODO:
                    // if (settings.skyboxIntensity.value > 0 && settings.skyboxContributionPass.value != SkyboxContributionPass.Deferred) {
                    //     cmd.EnableShaderKeyword(XKnightSSRShaderProperties.SKW_SKYBOX);
                    // } else {
                    //     cmd.DisableShaderKeyword(XKnightSSRShaderProperties.SKW_SKYBOX);
                    // }

                    //获取相机平面
                    GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);
                    //反射LayerMask
                    int reflectionsScriptsLayerMask = settings.reflectionsScriptsLayerMask.value;

                    //循环所有反射组件
                    for (int k = 0; k < count; k++)
                    {
                        XKTReflections go = XKTReflections.instances[k];
                        //组件错误||反射层级不对跳过
                        if (go == null || (reflectionsScriptsLayerMask & (1 << go.gameObject.layer)) == 0) continue;

                        //一个反射组件下的包含的反射对象
                        int rendererCount = go.ssrRenderers.Count;
                        //循环反射对象
                        for (int j = 0; j < rendererCount; j++)
                        {
                            XKTReflections.SSRRenderer ssrRenderer = go.ssrRenderers[j];

                            //Reflections.needUpdateMaterials由编辑器serializedObject.ApplyModifiedProperties()控制，当有属性变更就刷新材质属性
                            if (XKTReflections.needUpdateMaterials)
                            {
                                ssrRenderer.CheckMaterialChanges(mat);
                            }

                            //获取反射对象上的Renderer组件
                            Renderer goRenderer = ssrRenderer.renderer;
                            if (goRenderer == null || !goRenderer.isVisible) continue;

                            //物体包围盒检测是否在视锥范围内
                            //如果是静态物体
                            if (goRenderer.isPartOfStaticBatch)
                            {
                                //检测静态包围盒
                                if (ssrRenderer.hasStaticBounds)
                                {
                                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, ssrRenderer.staticBounds))
                                        continue;
                                }
                                //没有静态包围盒检测碰撞体
                                else if (ssrRenderer.collider != null)
                                {
                                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, ssrRenderer.collider.bounds))
                                        continue;
                                }
                                //非静态物体直接检测包围盒
                                else
                                {
                                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, goRenderer.bounds)) continue;
                                }
                            }

                            //初始化反射组件
                            if (!ssrRenderer.isInitialized)
                            {
                                ssrRenderer.Init(mat);
                                //更新材质
                                ssrRenderer.UpdateMaterialProperties(go, settings);
                            }
#if UNITY_EDITOR
                            //非运行状态下
                            else if (!Application.isPlaying)
                            {
                                //检测材质变更
                                ssrRenderer.CheckMaterialChanges(mat);
                                //更新材质
                                ssrRenderer.UpdateMaterialProperties(go, settings);
                            }
                            else if (XKTReflections.currentEditingXktReflections == go)
                            {
                                ssrRenderer.UpdateMaterialProperties(go, settings);
                            }
#endif

                            //if (ssrRenderer.exclude) continue;

                            if (firstSSR)
                            {
                                firstSSR = false;
                                //读取深度转化为线性深度存储
                                ComputeDepth(cmd, sourceDesc);
                                //声明反射图
                                cmd.GetTemporaryRT(XKTSSRShaderProperties.RayCast, sourceDesc, FilterMode.Point);
                                cmd.SetRenderTarget(XKTSSRShaderProperties.RayCast, 0, CubemapFace.Unknown, -1);
                                cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
                            }

                            //多维子材质支持
                            for (int s = 0; s < ssrRenderer.ssrMaterials.Length; s++)
                            {
                                if (go.subMeshMask <= 0 || ((1 << s) & go.subMeshMask) != 0)
                                {
                                    Material ssrMat = ssrRenderer.ssrMaterials[s];
                                    //绘制反射物体的法线（世界/视空间/法线贴图）、算 roughness / reflectivity、调 SSR ray marching（SSR_Pass）
                                    cmd.DrawRenderer(goRenderer, ssrMat, s, (int)Pass.SSRSurf);
                                }
                            }
                        }
                    }

                    XKTReflections.needUpdateMaterials = false;
                    if (firstSSR) return;
                }


                RenderTextureDescriptor copyDesc = sourceDesc;
                copyDesc.depthBufferBits = 0;

                if (settings.skyboxIntensity.value > 0)
                {
                    mat.SetVector(XKTSSRShaderProperties.CameraViewDir, camera.transform.forward);
                    cmd.EnableShaderKeyword(XKTSSRShaderProperties.SKYBOX);
                }
                else
                {
                    cmd.DisableShaderKeyword(XKTSSRShaderProperties.SKYBOX);
                }

                cmd.GetTemporaryRT(XKTSSRShaderProperties.ReflectionsTex, copyDesc);
                //把“SSR ray march 结果 + skybox fallback + 材质强度控制”合成最终反射颜色
                FullScreenBlit(cmd, source, XKTSSRShaderProperties.ReflectionsTex, Pass.Resolve);
                //Blitter.BlitCameraTexture(cmd,source,ReflectionsTex,mat,(int)Pass.Resolve);
                //TODO:看情况是否还需要做历史帧叠加

                RenderTargetIdentifier input = XKTSSRShaderProperties.ReflectionsTex;
                RenderTargetIdentifier reflectionsTex = input;
                int blurDownsampling = settings.blurDownsampling.value;
                copyDesc.width /= blurDownsampling;
                copyDesc.height /= blurDownsampling;
                //降采样模糊
                for (int k = 0; k < MIP_COUNT; k++)
                {
                    copyDesc.width = Mathf.Max(2, copyDesc.width / 2);
                    copyDesc.height = Mathf.Max(2, copyDesc.height / 2);
                    cmd.GetTemporaryRT(rtPyramid[k], copyDesc, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(XKTSSRShaderProperties.BlurRT, copyDesc, FilterMode.Bilinear);
                    FullScreenBlit(cmd, input, XKTSSRShaderProperties.BlurRT, Pass.BlurHoriz);
                    FullScreenBlit(cmd, XKTSSRShaderProperties.BlurRT, rtPyramid[k], Pass.BlurVert);
                    cmd.ReleaseTemporaryRT(XKTSSRShaderProperties.BlurRT);
                    input = rtPyramid[k];
                }

                //合成输出
                int finalPass;
                switch (settings.outputMode.value)
                {
                    case OutputMode.Final: finalPass = (int)Pass.Combine; break;
                    case OutputMode.SideBySideComparison: finalPass = (int)Pass.CombineWithCompare; break;
                    case OutputMode.DebugRayCast: finalPass = (int)Pass.DebugRayCast; break;

                    //case OutputMode.DebugDeferredNormals: finalPass = (int)Pass.DebugNormals; break;
                    default:
                        finalPass = (int)Pass.Debug; break;
                }
                FullScreenBlit(cmd, reflectionsTex, source, (Pass)finalPass);
                //
                // if (settings.stopNaN.value) {
                //     RenderTextureDescriptor nanDesc = sourceDesc;
                //     nanDesc.depthBufferBits = 0;
                //     nanDesc.msaaSamples = 1;
                //     cmd.GetTemporaryRT(NaNBuffer, nanDesc);
                //     FullScreenBlit(cmd, source, NaNBuffer, Pass.CopyExact);
                //     FullScreenBlit(cmd, NaNBuffer, source, Pass.CopyExact);
                // }

                // Clean up
                for (int k = 0; k < rtPyramid.Length; k++)
                {
                    cmd.ReleaseTemporaryRT(rtPyramid[k]);
                }

                cmd.ReleaseTemporaryRT(XKTSSRShaderProperties.ReflectionsTex);
                cmd.ReleaseTemporaryRT(XKTSSRShaderProperties.RayCast);
                cmd.ReleaseTemporaryRT(XKTSSRShaderProperties.DownscaledDepthRT);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            static void FullScreenBlit(CommandBuffer cmd, RenderTargetIdentifier source,
                RenderTargetIdentifier destination,
                Pass pass)
            {
                cmd.SetRenderTarget(destination, 0, CubemapFace.Unknown, -1);
                cmd.SetGlobalTexture(XKTSSRShaderProperties.MainTex, source);
                cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, mat, 0, (int)pass);
            }

            static void ComputeDepth(CommandBuffer cmd, RenderTextureDescriptor desc)
            {
                //TODO:BackFace
                //desc.colorFormat = settings.computeBackFaces.value ? RenderTextureFormat.RGHalf : RenderTextureFormat.RHalf;
                desc.colorFormat = RenderTextureFormat.RHalf;
                desc.sRGB = false;
                desc.depthBufferBits = 0;
                cmd.GetTemporaryRT(XKTSSRShaderProperties.DownscaledDepthRT, desc, FilterMode.Point);
                cmd.SetRenderTarget(XKTSSRShaderProperties.DownscaledDepthRT, 0, CubemapFace.Unknown, -1);
                cmd.DrawMesh(fullscreenMesh, Matrix4x4.identity, mat, 0, (int)Pass.CopyDepth);
            }


            public override void OnCameraCleanup(CommandBuffer cmd)
            {
            }

            public void CleanUp()
            {
                CoreUtils.Destroy(mat);
                // if (prevs != null) {
                //     foreach (RenderTexture rt in prevs.Values) {
                //         if (rt != null) {
                //             rt.Release();
                //             DestroyImmediate(rt);
                //         }
                //     }
                //     prevs.Clear();
                // }
            }
        }

        public Shader ssrShader;
        SSRRenderPass m_ScriptablePass;
        public LayerMask cameraLayerMask = -1;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public static bool installed;
        public static readonly string packagePath = " Packages/xkt.reflection";
        public override void Create()
        {
            m_ScriptablePass = new SSRRenderPass();
            
            m_ScriptablePass.renderPassEvent = renderPassEvent + 1;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            installed = true;
            Camera cam = renderingData.cameraData.camera;
            if ((cameraLayerMask.value & (1 << cam.gameObject.layer)) == 0) return;

            if (m_ScriptablePass.Setup(ssrShader))
            {
                if (SSRRenderPass.settings.skyboxIntensity.value > 0)
                {
                    //CheckSkyboxBaker();
                }

                // if (SSRRenderPass.settings.computeBackFaces.value) {
                //     backfacesPass.Setup(SSRPass.settings);
                //     renderer.EnqueuePass(backfacesPass);
                // }
                renderer.EnqueuePass(m_ScriptablePass);
            }
        }

        void OnDestroy()
        {
            installed = false;
            if (m_ScriptablePass != null)
            {
                m_ScriptablePass.CleanUp();
            }
        }
    }
}