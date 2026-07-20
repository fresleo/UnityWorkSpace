using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA
{
    /// <summary>
    /// 渲染逐物体阴影贴图
    /// </summary>
    public class GPerObjectShadowMapPass : ScriptableRenderPass
    {
        public static GPerObjectShadowMapPass Current { get; private set; }

        private GPerObjectShadowSettings settings;
        private GPerObjectShadowPassSettings passSettings;
        private GPerObjectSelfShadowPassSettings selfPassSettings;
        private GPerObjectShadowPassSettings selfPassSettingsCopy;

        // 合批用Shader与Material
        private Shader shadowOnlyShader;
        private Material shadowOnlyMaterial;

        public GPerObjectShadowData data => GPerObjectShadowManager.Instance.data;
        public GPerObjectShadowData selfData => GPerObjectShadowManager.Instance.selfData;

        private RTHandle m_EmptyShadowmapTexture;

        private ProfilingSampler selfShadowprofilingSampler;

        public GPerObjectShadowMapPass(GPerObjectShadowSettings settings)
        {
            this.settings = settings;
            this.passSettings = settings.shadowPassSettings;
            this.selfPassSettings = settings.selfShadowPassSetting;

            shadowOnlyShader = passSettings.shadowOnlyShader;
            shadowOnlyMaterial = shadowOnlyShader == null ? null : new Material(shadowOnlyShader);

            this.renderPassEvent = passSettings.Event;
            base.profilingSampler = new ProfilingSampler("GPerObjectShadow");
            this.selfShadowprofilingSampler = new ProfilingSampler("GPerObjectSelfShadow");

            m_EmptyShadowmapTexture = RTHandles.Alloc(Texture2D.blackTexture);
        }

        public void Dispose()
        {
            data.Release();
            selfData.Release();
            m_EmptyShadowmapTexture?.Release();
        }

        public void Disable()
        {
            data.Release();
            selfData.Release();

            // 分配贴图
            XKnightShadowUtils.ShadowRTReAllocateIfNeeded(ref m_EmptyShadowmapTexture, 1, 1,
                GPerObjectShadowData.ShadowmapBufferBits, name: GPerObjectShadowPropertyID.GEmptyPerObjectShadowMapName);

            // 为两种阴影效果设置空效果贴图
            Shader.SetGlobalTexture(GPerObjectShadowPropertyID.PID_GPerObjectShadowMap, m_EmptyShadowmapTexture);
            Shader.SetGlobalTexture(GPerObjectShadowPropertyID.PID_GPO_CharacterShadowMap, m_EmptyShadowmapTexture);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            DoConfigure();
        }

        public void DoConfigure()
        {
            Current = this;

            if (passSettings.enable)
            {
                data.UpdateShadowTextureSize(passSettings);
                XKnightShadowUtils.ShadowRTReAllocateIfNeeded(ref data.ShadowmapTexture,
                    data.ShadowmapTextureWidth,
                    data.ShadowmapTextureHeight,
                    GPerObjectShadowData.ShadowmapBufferBits,
                    name: GPerObjectShadowPropertyID.GPerObjectShadowMapName);
            }

            UpdateSelfShadowData();
            if (selfPassSettings.enable)
            {
                selfData.UpdateShadowTextureSize(selfPassSettingsCopy);
                XKnightShadowUtils.ShadowRTReAllocateIfNeeded(ref selfData.ShadowmapTexture,
                    selfData.ShadowmapTextureWidth,
                    selfData.ShadowmapTextureHeight,
                    GPerObjectShadowData.ShadowmapBufferBits,
                    name: GPerObjectShadowPropertyID.PID_GPO_CharacterShadowMapName);
            }

            // UUM-63146 - glClientWaitSync: Expected application to have kicked everything until job: 96089 (possibly by calling glFlush)" are thrown in the Android Player on some devices with PowerVR Rogue GE8320
            // Resetting of target would clean up the color attachment buffers and depth attachment buffers, which inturn is preventing the leak in the said platform. This is likely a symptomatic fix, but is solving the problem for now.
            if (Application.platform == RuntimePlatform.Android && PlatformAutoDetect.isRunningOnPowerVRGPU)
                ResetTarget();


            // 默认逐物体阴影，如果逐物体阴影关闭且自阴影开启，那就设置渲染目标为自阴影贴图
            RTHandle targetRT = data.ShadowmapTexture;
            if (selfPassSettings.enable && !passSettings.enable)
            {
                targetRT = selfData.ShadowmapTexture;
            }

            ConfigureTarget(targetRT);
            ConfigureClear(ClearFlag.All, Color.white);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!passSettings.enable && !selfPassSettings.enable) return;

            var cullResults = renderingData.cullResults;
            var lightData = renderingData.lightData;
            var shadowData = renderingData.shadowData;

            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return;

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];

            Bounds bounds;
            if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                return;

            var cmd = renderingData.commandBuffer;

            if (passSettings.enable)
            {
                data.UpdateSliceData(passSettings, shadowLight.light.transform.rotation);

                using (new ProfilingScope(cmd, this.profilingSampler))
                {
                    DrawRenderer(context, ref renderingData, data);
                    SetPerObjectShadowProperty(cmd);
                }

                if (selfPassSettings.enable)
                {
                    cmd.SetRenderTarget(selfData.ShadowmapTexture);
                    cmd.ClearRenderTarget(RTClearFlags.All, Color.white, 1.0f, 0);
                }
            }

            if (selfPassSettings.enable)
            {
                selfData.UpdateSliceData(selfPassSettingsCopy, shadowLight.light.transform.rotation, selfPassSettings.shadowOffset);
                using (new ProfilingScope(cmd, this.selfShadowprofilingSampler))
                {
                    DrawRenderer(context, ref renderingData, selfData);
                    SetPerObjectSelfShadowProperty(cmd);
                }
            }

            //SetupMainLightShadowReceiverConstants(cmd, ref renderingData, ref shadowLight, ref shadowData);
        }

        /// <summary>
        /// 开启SRP Batcher会在常规渲染前生成ShadowMap
        /// </summary>
        public void ExecuteBeforeNormalRendering(ScriptableRenderContext context, Camera camera, CommandBuffer cmd, Light light)
        {
            if (!passSettings.enable && !selfPassSettings.enable)
                return;

            if (light == null)
                return;

            DoConfigure();

            cmd.SetRenderTarget(data.ShadowmapTexture);
            cmd.ClearRenderTarget(RTClearFlags.All, Color.white, 1.0f, 0);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (passSettings.enable)
            {
                data.UpdateSliceData(passSettings, light.transform.rotation);

                using (new ProfilingScope(cmd, this.profilingSampler))
                {
                    DrawRenderer(context, cmd, camera, light, data);
                    SetPerObjectShadowProperty(cmd);
                }

                if (selfPassSettings.enable)
                {
                    cmd.SetRenderTarget(selfData.ShadowmapTexture);
                    cmd.ClearRenderTarget(RTClearFlags.All, Color.white, 1.0f, 0);
                }
            }

            if (selfPassSettings.enable)
            {
                selfData.UpdateSliceData(selfPassSettingsCopy, light.transform.rotation, selfPassSettings.shadowOffset);
                using (new ProfilingScope(cmd, this.selfShadowprofilingSampler))
                {
                    DrawRenderer(context, cmd, camera, light, selfData);
                    SetPerObjectSelfShadowProperty(cmd);
                }
            }

            //SetupMainLightShadowReceiverConstants(cmd, ref renderingData, ref shadowLight, ref shadowData);
        }

        #region 私有方法

        // 绘制渲染目标
        private void DrawRenderer(ScriptableRenderContext context, ref RenderingData renderingData, GPerObjectShadowData localData)
        {
            var lightIndex = renderingData.lightData.mainLightIndex;
            if (lightIndex != -1)
            {
                var light = renderingData.lightData.visibleLights[lightIndex].light;

                DrawRenderer(context, renderingData.commandBuffer, renderingData.cameraData.camera, light, localData);
            }
        }

        private void DrawRenderer(ScriptableRenderContext context, CommandBuffer cmd, Camera camera, Light shadowLight, GPerObjectShadowData localData)
        {
            //var cmd = renderingData.commandBuffer;
            // using (new ProfilingScope(cmd, this.profilingSampler))
            // {

            // Need to start by setting the Camera position and worldToCamera Matrix as that is not set for passes executed before normal rendering
            XKnightShadowUtils.SetCameraPosition(cmd, camera.transform.position);//, renderingData.cameraData.worldSpaceCameraPos);

            // Need set the worldToCamera Matrix as that is not set for passes executed before normal rendering,
            // otherwise shadows will behave incorrectly when Scene and Game windows are open at the same time (UUM-63267).
            XKnightShadowUtils.SetWorldToCameraAndCameraToWorldMatrices(cmd, camera.worldToCameraMatrix);// renderingData.cameraData.GetViewMatrix());

            for (int i = 0; i < localData.ValidSliceCount; ++i)
            {
                var slice = localData.CulledSliceData[i];

                //Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex, ref renderingData.shadowData, m_CascadeSlices[i].projectionMatrix, m_CascadeSlices[i].resolution);
                //XKnightShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, passSettings.ShadowBias);
                XKnightShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, passSettings.ShadowBias);

                //CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CastingPunctualLightShadow, false);

                ////Render Slice
                cmd.SetGlobalDepthBias(1.0f, 2.5f); // these values match HDRP defaults (see https://github.com/Unity-Technologies/Graphics/blob/9544b8ed2f98c62803d285096c91b44e9d8cbc47/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowAtlas.cs#L197 )

                Material srpMaterial = passSettings.useShadowOnlyShader ? shadowOnlyMaterial : null;
                //slice.Render(ref context, ref renderingData, (uint)i, srpMaterial);
                slice.Render(ref context, cmd, camera, (uint)i, srpMaterial);
            }

            //ref CameraData cameraData = ref renderingData.cameraData;
            //Camera camera = cameraData.camera;
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            // 把软阴影的主控统一挪到主灯的 Pass 上了，这里就不再单独进行控制了，避免互相干扰
            // renderingData.shadowData.isKeywordSoftShadowsEnabled = shadowLight.light.shadows == LightShadows.Soft && renderingData.shadowData.supportsSoftShadows;
            // CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, renderingData.shadowData.mainLightShadowCascadesCount == 1);
            // CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, renderingData.shadowData.mainLightShadowCascadesCount > 1);
            // XKnightShadowUtils.SetSoftShadowQualityShaderKeywords(cmd, ref renderingData.shadowData);

            // }
        }

        // 设置主灯光阴影接收器常量（应该可以移除，所有采样操作都在主光源阴影后）
        private void SetupMainLightShadowReceiverConstants(CommandBuffer cmd, ref RenderingData renderingData, ref VisibleLight shadowLight, ref ShadowData shadowData)
        {
            GPerObjectShadowData localData = data;
            if (!passSettings.enable) localData = selfData;

            Light light = shadowLight.light;
            bool softShadows = shadowLight.light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;


            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m22 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;

            float invShadowAtlasWidth = 1.0f / localData.ShadowmapTextureWidth;
            float invShadowAtlasHeight = 1.0f / localData.ShadowmapTextureHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            float softShadowsProp = XKnightShadowUtils.SoftShadowQualityToShaderProperty(light, softShadows);

            float m_MaxShadowDistanceSq = renderingData.cameraData.maxShadowDistance * renderingData.cameraData.maxShadowDistance;
            float m_CascadeBorder = renderingData.shadowData.mainLightShadowCascadeBorder;
            XKnightShadowUtils.GetScaleAndBiasForLinearDistanceFade(m_MaxShadowDistanceSq, m_CascadeBorder, out float shadowFadeScale, out float shadowFadeBias);

            cmd.SetGlobalVector("_MainLightShadowParams",
                new Vector4(light.shadowStrength, softShadowsProp, shadowFadeScale, shadowFadeBias));

            // Inside shader soft shadows are controlled through global keyword.
            // If any additional light has soft shadows it will force soft shadows on main light too.
            // As it is not trivial finding out which additional light has soft shadows, we will pass main light properties if soft shadows are supported.
            // This workaround will be removed once we will support soft shadows per light.
            if (shadowData.supportsSoftShadows)
            {
                cmd.SetGlobalVector("_MainLightShadowOffset0",
                    new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight,
                        invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight));
                cmd.SetGlobalVector("_MainLightShadowOffset1",
                    new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight,
                        invHalfShadowAtlasWidth, invHalfShadowAtlasHeight));

                cmd.SetGlobalVector("_MainLightShadowmapSize", new Vector4(invShadowAtlasWidth,
                    invShadowAtlasHeight,
                    localData.ShadowmapTextureWidth, localData.ShadowmapTextureHeight));
            }
        }

        /// <summary> 将自阴影的settings转换为适应当前结构的settings </summary>
        private void UpdateSelfShadowData()
        {
            if (selfPassSettingsCopy == null) selfPassSettingsCopy = new GPerObjectShadowPassSettings();
            selfPassSettingsCopy.enable = selfPassSettings.enable;
            selfPassSettingsCopy.MaxCount = selfPassSettings.MaxCount;
            selfPassSettingsCopy.srpBatcher = passSettings.srpBatcher;
            selfPassSettingsCopy.CombineBounds = selfPassSettings.CombineBounds;
            selfPassSettingsCopy.SliceTextureSize = selfPassSettings.SliceTextureSize;
            selfPassSettingsCopy.FrustumExtendUsePercent = selfPassSettings.FrustumExtendUsePercent;
            selfPassSettingsCopy.FrustumExtend = selfPassSettings.FrustumExtend;
            selfPassSettingsCopy.OverrideLightRotation = selfPassSettings.OverrideLightRotation;
            selfPassSettingsCopy.LightRotation = selfPassSettings.LightRotation;
            selfPassSettingsCopy.ShadowBias = selfPassSettings.ShadowBias;
        }

        // 设置逐对象全局变量
        private void SetPerObjectShadowProperty(CommandBuffer cmd)
        {
            cmd.SetGlobalInteger(GPerObjectShadowPropertyID.PID_GPerObjectShadowCount, data.ValidSliceCount);
            cmd.SetGlobalTexture(GPerObjectShadowPropertyID.PID_GPerObjectShadowMap, data.ShadowmapTexture);
            cmd.SetGlobalMatrixArray(GPerObjectShadowPropertyID.PID_GPerObjectWorldToShadow, data.shadowMatris);
            cmd.SetGlobalVectorArray(GPerObjectShadowPropertyID.PID_GPerObjectShadowUVRect, data.shadowUVRect);
            cmd.SetGlobalFloatArray(GPerObjectShadowPropertyID.PID_GPerObjectShadowIntensity, data.shadowIntensity);

            cmd.SetGlobalVector(GPerObjectShadowPropertyID.PID_GPerObjectShadowmapTextureSize,
                new Vector4(1f / data.ShadowmapTextureWidth,
                1f / data.ShadowmapTextureHeight,
                data.ShadowmapTextureWidth,
                data.ShadowmapTextureHeight));
        }

        // 设置逐对象全局变量 自阴影用
        private void SetPerObjectSelfShadowProperty(CommandBuffer cmd)
        {
            cmd.SetGlobalInteger(GPerObjectShadowPropertyID.PID_GPO_CharacterCount, selfData.ValidSliceCount);
            cmd.SetGlobalTexture(GPerObjectShadowPropertyID.PID_GPO_CharacterShadowMap, selfData.ShadowmapTexture);
            cmd.SetGlobalMatrixArray(GPerObjectShadowPropertyID.PID_GPO_CharacterWorldToShadow, selfData.shadowMatris);
            cmd.SetGlobalVectorArray(GPerObjectShadowPropertyID.PID_GPO_CharacterShadowUVRect, selfData.shadowUVRect);

            cmd.SetGlobalVector(GPerObjectShadowPropertyID.PID_GPO_CharacterShadowMapSize,
                new Vector4(1f / selfData.ShadowmapTextureWidth,
                1f / selfData.ShadowmapTextureHeight,
                selfData.ShadowmapTextureWidth,
                selfData.ShadowmapTextureHeight));
        }

        #endregion
    }
}
