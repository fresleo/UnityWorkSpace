using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.ATAA;

namespace Garena.TA.VolumetricLightingFog
{
    /// <summary>
    /// 参考 https://github.com/CristianQiu/Unity-URP-Volumetric-Light
    /// </summary>
    public class GlobalVolumetricLightingFogPass : ScriptableRenderPass
    {
        protected GlobalVolumetricLightingFogSetting m_baseSetting;
        protected GlobalVolumetricLightingFogSettings m_setting;
        protected Material m_volumetricMaterial;
        protected Material m_downsampleDepthMaterial;

        protected int m_downsampleDepthRenderPassIndex;
        protected int m_volumetricRenderPassIndex;
        protected int m_horizontalBlurPassIndex;
        protected int m_verticalBlurPassIndex;
        protected int m_upsamplePassIndex;

        protected RTHandle m_downsampleDepthRt;
        protected RTHandle m_volumetricLightingRt;
        
        protected RTHandle m_blurRt;

        const int MAX_LIGHT_COUNT = 4;
        #region PropId define

        protected static readonly int FrameCountPropId = Shader.PropertyToID("_FrameCount");
        protected static readonly int StepSizePropId = Shader.PropertyToID("_StepSize");

        protected static readonly int AmbientColorPropId = Shader.PropertyToID("_AmbientColor");
        protected static readonly int VolumetricLightingParamPropId = Shader.PropertyToID("_VolumetricLightingParam"); //x: minH y:maxH z:density w:extinctionScale
        protected static readonly int VolumetricLightingParam2PropId = Shader.PropertyToID("_VolumetricLightingParam2");//x: Anisotropy y:Contrast
        protected static readonly int VolumetricFogParamPropId = Shader.PropertyToID("_VolumetricFogParam");//x:minH  y:maxH  z:density w:FadeoutDistance
        protected static readonly int VolumetricFogColorPropId = Shader.PropertyToID("_VolumetricFogColor");
        protected static readonly int DepthTexPropId = Shader.PropertyToID("_DepthTexture");

        protected static readonly int LightCountPropId = Shader.PropertyToID("_LightCount");
        protected static readonly int LightPositionPropId = Shader.PropertyToID("_LightPositions");
        protected static readonly int LightColorPropId = Shader.PropertyToID("_LightColors");
        protected static readonly int LightDirectionPropId = Shader.PropertyToID("_LightDirections");
        protected static readonly int LightParamPropId = Shader.PropertyToID("_LightParams");
        #endregion
        private Vector4[] m_lightPositions = new Vector4[MAX_LIGHT_COUNT];
        private Vector4[] m_lightColors = new Vector4[MAX_LIGHT_COUNT];
        private Vector4[] m_lightDirections = new Vector4[MAX_LIGHT_COUNT];
        private Vector4[] m_lightParams = new Vector4[MAX_LIGHT_COUNT];
        private FilteringSettings m_localVolumetricFilteringSettings;
        private ShaderTagId LocalVolumetricShaderTagId = new ShaderTagId("LocalVolumetric");
        private static readonly ProfilingSampler s_ProfilingSampler = new ProfilingSampler("VolumetricLightingPass");
        public GlobalVolumetricLightingFogPass(GlobalVolumetricLightingFogSetting setting)
        {
            m_baseSetting = setting;
            renderPassEvent = ATAAPass.RENDER_PASS_EVENT_ORDER_1;
            
            ConfigureInput(ScriptableRenderPassInput.Depth);

            if (m_baseSetting.VolumetricLightingShader != null && m_baseSetting.VolumetricLightingShader.isSupported)
            {
                m_volumetricMaterial = CoreUtils.CreateEngineMaterial(m_baseSetting.VolumetricLightingShader);
            }
            if (m_baseSetting.DownsampleDepthShader != null&& m_baseSetting.DownsampleDepthShader.isSupported)
            {
                m_downsampleDepthMaterial = CoreUtils.CreateEngineMaterial(m_baseSetting.DownsampleDepthShader);
            }
            
            if (m_volumetricMaterial != null)
            {
                m_volumetricRenderPassIndex = m_volumetricMaterial.FindPass("VolumetricRender");
                m_horizontalBlurPassIndex = m_volumetricMaterial.FindPass("HorizontalBlur");
                m_verticalBlurPassIndex = m_volumetricMaterial.FindPass("VerticalBlur");
            }
            if (m_downsampleDepthMaterial != null)
            {
                m_downsampleDepthRenderPassIndex = m_downsampleDepthMaterial.FindPass("DownsampleDepth");
                m_upsamplePassIndex = m_downsampleDepthMaterial.FindPass("VolumetricUpsample");
            }
            //
            m_localVolumetricFilteringSettings = new FilteringSettings(RenderQueueRange.transparent);
        }

        public void Setup(GlobalVolumetricLightingFogSettings setting)
        {
            m_setting = setting;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);

            if (m_setting == null || (!m_setting.OpenVolumetricFog.value && !m_setting.OpenVolumetricLight.value && !m_setting.OpenLocalVolumetricFog.value))
                return;

            RenderTextureDescriptor descripter = renderingData.cameraData.cameraTargetDescriptor;
            descripter.depthBufferBits = (int)DepthBits.None;
            descripter.msaaSamples = 1;

            descripter.width /= (int)m_baseSetting.Downsample;
            descripter.height /= (int)m_baseSetting.Downsample;

            if(SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, FormatUsage.Render))
                descripter.graphicsFormat = GraphicsFormat.R32_SFloat;
            else
                descripter.graphicsFormat = GraphicsFormat.R16_SFloat;
            //RenderingUtils.ReAllocateIfNeeded(ref m_downsampleDepthRt, descripter, wrapMode: TextureWrapMode.Clamp, name: "_DownsampleDepth");
            if (m_downsampleDepthRt == null || !m_downsampleDepthRt.rt.IsCreated() || m_downsampleDepthRt.rt.width != descripter.width)
            {
                m_downsampleDepthRt?.Release();
                m_downsampleDepthRt = RTHandles.Alloc(descripter.width, descripter.height,
                    colorFormat: descripter.graphicsFormat,
                    name: "_DownsampleDepth");
            }

            if (Application.isMobilePlatform)
                descripter.colorFormat = RenderTextureFormat.ARGB32;
            else
                descripter.colorFormat = RenderTextureFormat.ARGBHalf;
            //RenderingUtils.ReAllocateIfNeeded(ref m_volumetricLightingRt, descripter, wrapMode: TextureWrapMode.Clamp, name: "_VolumetricLighting");
            if (m_volumetricLightingRt == null || !m_volumetricLightingRt.rt.IsCreated() || m_volumetricLightingRt.rt.width != descripter.width)
            {
                m_volumetricLightingRt?.Release();
                m_volumetricLightingRt = RTHandles.Alloc(descripter.width, descripter.height,
                    colorFormat: descripter.graphicsFormat,
                    name: "_VolumetricLighting");
            }

            if (m_setting.BlurIterations.value > 0)
            {
                //RenderingUtils.ReAllocateIfNeeded(ref m_blurRt, descripter, wrapMode: TextureWrapMode.Clamp, name: "_VolumetricLightingBlur");
                if (m_blurRt == null || m_blurRt.rt == null || !m_blurRt.rt.IsCreated() || m_blurRt.rt.width != descripter.width)
                {
                    m_blurRt?.Release();
                    m_blurRt = RTHandles.Alloc(descripter.width, descripter.height,
                        colorFormat: descripter.graphicsFormat,
                        name: "_VolumetricLightingBlur");
                }
            }
            else
                m_blurRt?.Release();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_setting == null || (!m_setting.OpenVolumetricFog.value && !m_setting.OpenVolumetricLight.value && !m_setting.OpenLocalVolumetricFog.value))//
                return;
            if (m_volumetricMaterial == null || m_downsampleDepthMaterial == null)
                return;

            if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView)
                return;

            if (renderingData.lightData.visibleLights.Length <= 0)
                return;

            if(m_downsampleDepthRt == null || m_volumetricLightingRt == null)
            {
                Debug.LogError("m_downsampleDepthRt or m_volumetricLightingRt is null");
                return;
            }
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, s_ProfilingSampler))
            {
                RTHandle srcColorRt = renderingData.cameraData.renderer.cameraColorTargetHandle;
                //RTHandle srcDepthRt = renderingData.cameraData.renderer.cameraDepthTargetHandle;
                UpdateMaterialParams(m_isForceUpdate);
                m_isForceUpdate = false;

                //downsample depth buffer  这里不能直接传srcDepthRt作为深度图进行处理，因为没有处理MSAA的情况
                Blitter.BlitCameraTexture(cmd, m_downsampleDepthRt, m_downsampleDepthRt, m_downsampleDepthMaterial, m_downsampleDepthRenderPassIndex);
                m_volumetricMaterial.SetTexture(DepthTexPropId, m_downsampleDepthRt);

                //do local volumetric lighting
                CoreUtils.SetRenderTarget(cmd, m_volumetricLightingRt);
                CoreUtils.ClearRenderTarget(cmd, ClearFlag.Color, Color.black);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (m_setting.OpenLocalVolumetricFog.value)
                {
                    Shader.SetGlobalTexture(DepthTexPropId, m_downsampleDepthRt);
                    var drawSettings = CreateDrawingSettings(LocalVolumetricShaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_localVolumetricFilteringSettings);
                }
                //do volumetric lighting
                if(m_setting.OpenVolumetricFog.value || m_setting.OpenVolumetricLight.value)
                    Blitter.BlitCameraTexture(cmd, m_downsampleDepthRt, m_volumetricLightingRt, m_volumetricMaterial, m_volumetricRenderPassIndex);
                
                //blur
                int blurIterations = m_setting.BlurIterations.value;

                for (int i = 0; i < blurIterations; ++i)
                {
                    Blitter.BlitCameraTexture(cmd, m_volumetricLightingRt, m_blurRt, m_volumetricMaterial, m_horizontalBlurPassIndex);
                    Blitter.BlitCameraTexture(cmd, m_blurRt, m_volumetricLightingRt, m_volumetricMaterial, m_verticalBlurPassIndex);
                }

                //upsample to full screen
                m_downsampleDepthMaterial.SetTexture(DepthTexPropId, m_downsampleDepthRt);
                Blitter.BlitCameraTexture(cmd, m_volumetricLightingRt, srcColorRt, m_downsampleDepthMaterial, m_upsamplePassIndex);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            
        }

        bool m_isForceUpdate = true;
        void UpdateMaterialParams(bool forceUpdate = false)
        {
            if (m_volumetricMaterial == null || m_setting == null)
                return;

            //传递灯光数据到mat
            Light[] allLights = GameObject.FindObjectsOfType<Light>();
            //TODO:根据距离排序？
            int index = 0;
            for (int i = 0; i < allLights.Length && i < MAX_LIGHT_COUNT; ++i)
            {
                Light light = allLights[i];
                if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    m_lightColors[index] = light.color * light.intensity;
                    m_lightDirections[index] = light.transform.forward;
                    m_lightPositions[index] = light.transform.position;
                    // 参数：x=range, y=spotAngle(弧度), z=lightType(0=point,1=spot), w=innerSpotAngle
                    float spotAngleRad = light.spotAngle * Mathf.Deg2Rad;
                    float innerSpotAngleRad = (light.innerSpotAngle > 0 ? light.innerSpotAngle : light.spotAngle * 0.8f) * Mathf.Deg2Rad;

                    m_lightParams[index] = new Vector4(
                        light.range,
                        spotAngleRad,
                        light.type == LightType.Spot ? 1.0f : 0.0f,
                        innerSpotAngleRad
                    );

                    index++;
                }
            }
            m_volumetricMaterial.SetInt(LightCountPropId, Mathf.Min(index, MAX_LIGHT_COUNT));
            m_volumetricMaterial.SetVectorArray(LightPositionPropId, m_lightPositions);
            m_volumetricMaterial.SetVectorArray(LightColorPropId, m_lightColors);
            m_volumetricMaterial.SetVectorArray(LightDirectionPropId, m_lightDirections);
            m_volumetricMaterial.SetVectorArray(LightParamPropId, m_lightParams);

            m_volumetricMaterial.SetInteger(FrameCountPropId, Time.frameCount);

            //if (!forceUpdate && !Application.isEditor)
            //    return;

            m_volumetricMaterial.SetFloat(StepSizePropId, m_setting.StepSize.value);

            if (RenderSettings.ambientMode == AmbientMode.Skybox)
            {
                Color[] result = new Color[1] { Color.black };
                RenderSettings.ambientProbe.Evaluate(new Vector3[] { Vector3.up }, result);
                m_volumetricMaterial.SetColor(AmbientColorPropId, result[0]);
            }
            else
            {
                m_volumetricMaterial.SetColor(AmbientColorPropId, RenderSettings.ambientLight);
            }
            //m_volumetricMaterial.SetColor(AmbientColorPropId, m_setting.SkyIrradiance);

            m_volumetricMaterial.SetVector(VolumetricLightingParamPropId, 
                new Vector4(
                    m_setting.VolumetricLightMinHeight.value, 
                    m_setting.VolumetricLightMaxHeight.value, 
                    m_setting.VolumetricLightDensity.value, 
                    m_setting.LightingScale.value));
            m_volumetricMaterial.SetVector(VolumetricLightingParam2PropId, 
                new Vector4( 
                    m_setting.LightingAnisotropy.value, 
                    m_setting.LightingContrast.value * 0.25f, 
                    0, 
                    0));
            m_volumetricMaterial.SetVector(VolumetricFogParamPropId, 
                new Vector4(
                    m_setting.VolumetricFogMinHeight.value, 
                    m_setting.VolumetricFogMaxHeight.value, 
                    m_setting.VolumetricFogDensity.value, 
                    Mathf.Clamp01(1.0f / m_setting.FadeoutDistance.value)));
            m_volumetricMaterial.SetVector(VolumetricFogColorPropId, m_setting.VolumetricFogColor.value);

            if(m_setting.OpenVolumetricLight.value)
                m_volumetricMaterial.EnableKeyword("_OPEN_VOLUMETRIC_LIGHTING");
            else
                m_volumetricMaterial.DisableKeyword("_OPEN_VOLUMETRIC_LIGHTING");
            if (m_setting.OpenVolumetricFog.value)
                m_volumetricMaterial.EnableKeyword("_OPEN_VOLUMETRIC_FOG");
            else
                m_volumetricMaterial.DisableKeyword("_OPEN_VOLUMETRIC_FOG");
        }

        public void Dispose()
        {
            if (m_volumetricMaterial != null)
            {
                CoreUtils.Destroy(m_volumetricMaterial);
            }
            if (m_downsampleDepthMaterial != null)
            {
                CoreUtils.Destroy(m_downsampleDepthMaterial);
            }

            m_blurRt?.Release();
            m_downsampleDepthRt?.Release();
            m_volumetricLightingRt?.Release();
        }
    }
}