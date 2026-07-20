using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Garena.TA.VolumetricLightingFog
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    public class LocalVolumetricFog : MonoBehaviour
    {
        public enum ShapeType
        {
            Cube,
            Sphere
        };
        [SerializeField]
        protected ShapeType m_shapeType = ShapeType.Cube;

        [SerializeField]
        [Range(4, 128)]
        protected int m_stepCount = 32;
        [Range(0.01f, 1)]
        [SerializeField]
        protected float m_fadeToBorder = 0.2f;
        [SerializeField]
        [Min(1)]
        protected float m_fadeToCamera = 6;
        [SerializeField]
        [Range(0, 1)]
        protected float m_coverage = 0.65f;
        [SerializeField]
        protected Color m_skyIrradiance = Color.gray;
        [SerializeField]
        [Header("散射系数")]
        protected Vector3 m_scatteringCoefficient = Vector3.one;
        [SerializeField]
        [Header("吸收系数")]
        protected Vector3 m_absorptionCoefficient = Vector3.zero;
        [SerializeField]
        [Header("消光缩放系数")]
        //extinction = (scattering + absorption) * m_extinctionScale   
        //非物理参数，如果需要真实物理效果，设置为1
        [Range(0.01f, 5)]
        protected float m_extinctionScale = 0.25f;
        [SerializeField]
        protected Texture2D m_mainShape;
        [SerializeField]
        protected float m_mainShapeTiling = 0.05f;
        [SerializeField]
        protected Vector3 m_mainShapeVelocity = Vector3.zero;
        [SerializeField]
        protected Texture3D m_detailShape;
        [SerializeField]
        protected float m_detailShapeTiling = 0.25f;
        [SerializeField]
        protected Vector3 m_detailShapeVelocity = Vector3.zero;
        [Range(0.01f, 100)]
        [SerializeField]
        protected float m_density = 1;
        [SerializeField]
        protected bool m_beamShadowmap = true;
        [SerializeField]
        [Min(1)]
        protected float m_shadowDistance = 100;
        [SerializeField]
        protected float m_shadowScale = 0.5f;
        [SerializeField]
        [Header("阴影偏移值，根据raymarch深度来调整")]
        protected float m_beamShadowBias = 0.1f;
        [SerializeField]
        protected Shader m_beamShadowmapShader;
        [SerializeField]
        protected int m_beamShadowSize = 256;

        protected Vector3 m_mainShapeOffset, m_detailShapeOffset;
        protected MaterialPropertyBlock m_mpb;
        protected Material m_mat;

        protected Material m_shadowMat;
        protected CommandBuffer m_shadowCB;
        [SerializeField]
        protected RenderTexture m_shadowRT;
        protected List<Vector3> m_corners = new List<Vector3>();
        protected Renderer m_cubeRenderer;

        const int MAX_LIGHT_COUNT = 4;
        private Vector4[] m_lightPositions = new Vector4[MAX_LIGHT_COUNT];
        private Vector4[] m_lightColors = new Vector4[MAX_LIGHT_COUNT];
        private Vector4[] m_lightDirections = new Vector4[MAX_LIGHT_COUNT];
        private Vector4[] m_lightParams = new Vector4[MAX_LIGHT_COUNT];

        private CinemachineVirtualCamera m_virtualCamera;
        #region prop id
        protected static readonly int VolumetricParamPropId = Shader.PropertyToID("_VolumetricParam");
        protected static readonly int ScatteringPropId = Shader.PropertyToID("_ScatteringCoefficient");
        protected static readonly int AbsorptionParamPropId = Shader.PropertyToID("_AbsorptionCoefficient");
        protected static readonly int ExtinctionScalePropId = Shader.PropertyToID("_ExtinctionScale");
        protected static readonly int CoveragePropId = Shader.PropertyToID("_Coverage");
        protected static readonly int FadeToCameraPropId = Shader.PropertyToID("_FadeToCamera");
        protected static readonly int MainShapeTexParamPropId = Shader.PropertyToID("_MainShape2DTexture");
        protected static readonly int MainShapeOffsetPropId = Shader.PropertyToID("_MainShapeOffset");

        protected static readonly int DetailShapeTexParamPropId = Shader.PropertyToID("_DetailShape3DTexture");
        protected static readonly int DetailShapeOffsetPropId = Shader.PropertyToID("_DetailShapeOffset");

        protected static readonly int SunIrradiancePropId = Shader.PropertyToID("_SunIrradiance");
        protected static readonly int SkyIrradiancePropId = Shader.PropertyToID("_SkyIrradiance");

        protected static readonly int ShadowTexturePropId = Shader.PropertyToID("_BeamShadowmap");
        protected static readonly int ShadowTextureSizePropId = Shader.PropertyToID("_ShadowmapTextureSize");
        protected static readonly int LightDirectionPropId = Shader.PropertyToID("_LightDirection");
        protected static readonly int ShadowVPPropId = Shader.PropertyToID("_ShadowVP");
        protected static readonly int ShadowIVPPropId = Shader.PropertyToID("_ShadowIVP");

        protected static readonly int LightCountPropId = Shader.PropertyToID("_LightCount");
        protected static readonly int LightPositionsPropId = Shader.PropertyToID("_LightPositions");
        protected static readonly int LightColorsPropId = Shader.PropertyToID("_LightColors");
        protected static readonly int LightDirectionsPropId = Shader.PropertyToID("_LightDirections");
        protected static readonly int LightParamPropId = Shader.PropertyToID("_LightParams");
        protected static readonly int PlayerPropId = Shader.PropertyToID("_PlayerPos");
        #endregion

        public int StepCount
        {
            get
            {
                return m_stepCount;
            }
            set
            {
                m_stepCount = Mathf.Max(0, value);
            }
        }
        void InitBoundCorners()
        {
            //通用方法
            //var mrs = GetComponentsInChildren<MeshRenderer>();
            //Bounds b = new Bounds();
            //bool init = false;
            //for (int i = 0; i < mrs.Length; ++i)
            //{
            //    if (!init)
            //    {
            //        init = true;
            //        b = new Bounds(mrs[i].bounds.center, mrs[i].bounds.size);
            //    }
            //    else
            //    {
            //        b.Encapsulate(mrs[i].bounds);
            //    }
            //}

            //针对cube的特例
            Vector3 center = transform.position;
            Vector3 size = transform.lossyScale;
            Vector3 corner1 = center - transform.right * size.x * 0.5f - transform.up * size.y * 0.5f - transform.forward * size.z * 0.5f;
            Vector3 corner2 = center + transform.right * size.x * 0.5f - transform.up * size.y * 0.5f - transform.forward * size.z * 0.5f;
            Vector3 corner3 = center + transform.right * size.x * 0.5f - transform.up * size.y * 0.5f + transform.forward * size.z * 0.5f;
            Vector3 corner4 = center - transform.right * size.x * 0.5f - transform.up * size.y * 0.5f + transform.forward * size.z * 0.5f;
            Vector3 corner5 = center - transform.right * size.x * 0.5f + transform.up * size.y * 0.5f - transform.forward * size.z * 0.5f;
            Vector3 corner6 = center + transform.right * size.x * 0.5f + transform.up * size.y * 0.5f - transform.forward * size.z * 0.5f;
            Vector3 corner7 = center + transform.right * size.x * 0.5f + transform.up * size.y * 0.5f + transform.forward * size.z * 0.5f;
            Vector3 corner8 = center - transform.right * size.x * 0.5f + transform.up * size.y * 0.5f + transform.forward * size.z * 0.5f;
            m_corners.Clear();
            m_corners.Add(corner1);
            m_corners.Add(corner2);
            m_corners.Add(corner3);
            m_corners.Add(corner4);
            m_corners.Add(corner5);
            m_corners.Add(corner6);
            m_corners.Add(corner7);
            m_corners.Add(corner8);

        }
        private void OnEnable()
        {
#if UNITY_EDITOR

            if (m_beamShadowmapShader == null)
            {
                m_beamShadowmapShader = Shader.Find("Unlit/LocalVolumetricFogShadow");
            }
#endif
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var data = mainCamera.GetUniversalAdditionalCameraData();
                data.requiresDepthTexture = true;
            }

            if (m_shadowMat == null && m_beamShadowmapShader != null)
            {
                m_shadowMat = CoreUtils.CreateEngineMaterial(m_beamShadowmapShader);
            }
            m_cubeRenderer = GetComponent<MeshRenderer>();
            m_mpb = new MaterialPropertyBlock();
            m_cubeRenderer.GetPropertyBlock(m_mpb);
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                m_mat = m_cubeRenderer.material;
            }
            else
            {
                m_mat = m_cubeRenderer.sharedMaterial;
            }
#else
            m_mat = m_cubeRenderer.material;
#endif
            UpdateMaterial(true);
            InitBoundCorners();

            if (m_beamShadowmap)
            {
                m_shadowCB = new CommandBuffer();
                m_shadowCB.name = "VolumetricFogShadow";

                m_beamShadowSize = Mathf.Max(4, m_beamShadowSize);
                m_shadowRT = new RenderTexture(m_beamShadowSize, m_beamShadowSize, 0, RenderTextureFormat.RGHalf);
                m_shadowRT.name = "VolumetricFogShadow";

            }
        }
        private void OnDisable()
        {
            if (m_shadowCB != null)
            {
                m_shadowCB.Dispose();
                m_shadowCB = null;
            }
            if (m_shadowRT != null)
            {
                m_shadowRT.Release();
            }
            if (m_shadowMat != null)
            {
                CoreUtils.Destroy(m_shadowMat);
                m_shadowMat = null;
            }
            m_mpb = null;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateMaterial();
            RenderShadow();
        }
        void UpdateMaterial(bool forceUpdate = false)
        {
            if (m_cubeRenderer == null || m_mpb == null || m_mainShape == null || m_detailShape == null)
                return;

            m_mainShapeOffset += m_mainShapeVelocity * Time.deltaTime;
            m_detailShapeOffset += m_detailShapeVelocity * Time.deltaTime;
            m_mpb.SetVector(MainShapeOffsetPropId, new Vector4(m_mainShapeOffset.x, m_mainShapeOffset.y, m_mainShapeOffset.z, m_mainShapeTiling));
            m_mpb.SetVector(DetailShapeOffsetPropId, new Vector4(m_detailShapeOffset.x, m_detailShapeOffset.y, m_detailShapeOffset.z, m_detailShapeTiling));

            if (m_beamShadowmap && m_shadowMat != null)
            {
                m_shadowMat.SetVector(MainShapeOffsetPropId, new Vector4(m_mainShapeOffset.x, m_mainShapeOffset.y, m_mainShapeOffset.z, m_mainShapeTiling));
                m_shadowMat.SetVector(DetailShapeOffsetPropId, new Vector4(m_detailShapeOffset.x, m_detailShapeOffset.y, m_detailShapeOffset.z, m_detailShapeTiling));
            }

//#if !UNITY_EDITOR
//            if (!forceUpdate)
//            {
//                m_cubeRenderer.SetPropertyBlock(m_mpb);
//                return;
//            }
//#endif

            m_mpb.SetVector(VolumetricParamPropId, new Vector4(Time.frameCount, m_stepCount, 1.0f / m_fadeToBorder, m_density));
            m_mpb.SetVector(ScatteringPropId, m_scatteringCoefficient);
            m_mpb.SetVector(AbsorptionParamPropId, m_absorptionCoefficient);
            m_mpb.SetFloat(ExtinctionScalePropId, m_extinctionScale);
            m_mpb.SetFloat(CoveragePropId, m_coverage);
            m_mpb.SetFloat(FadeToCameraPropId, 1.0f / m_fadeToCamera);
            m_mpb.SetTexture(MainShapeTexParamPropId, m_mainShape);
            m_mpb.SetTexture(DetailShapeTexParamPropId, m_detailShape);

            //if (RenderSettings.ambientMode == AmbientMode.Skybox)
            //{
            //    Color[] result = new Color[1] { Color.black };
            //    RenderSettings.ambientProbe.Evaluate(new Vector3[] { Vector3.up }, result);
            //    m_mpb.SetColor(SkyIrradiancePropId, result[0]);
            //}
            //else
            //{
            //    m_mpb.SetColor(SkyIrradiancePropId, RenderSettings.ambientLight);
            //}
            m_mpb.SetColor(SkyIrradiancePropId, m_skyIrradiance);
            var mainLight = RenderSettings.sun;
            if (mainLight != null)
            {
                m_mpb.SetColor(SunIrradiancePropId, mainLight.color * mainLight.intensity);
            }
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
            m_mpb.SetInt(LightCountPropId, Mathf.Min(index, MAX_LIGHT_COUNT));
            m_mpb.SetVectorArray(LightPositionsPropId, m_lightPositions);
            m_mpb.SetVectorArray(LightColorsPropId, m_lightColors);
            m_mpb.SetVectorArray(LightDirectionsPropId, m_lightDirections);
            m_mpb.SetVectorArray(LightParamPropId, m_lightParams);

            if (m_beamShadowmap && m_shadowRT != null)
            {
                float size = m_shadowRT.width;
                float oneOverSize = 1.0f / size;
                m_mpb.SetVector(ShadowTextureSizePropId, new Vector4(size, oneOverSize, m_beamShadowBias, m_shadowScale));
                m_shadowMat.SetVector(ShadowTextureSizePropId, new Vector4(size, oneOverSize, m_beamShadowBias, m_shadowScale));
            }

            m_cubeRenderer.SetPropertyBlock(m_mpb);

            if (m_beamShadowmap && m_shadowMat != null)
            {
                m_shadowMat.SetVector(VolumetricParamPropId, new Vector4(Time.frameCount, m_stepCount, 1.0f / m_fadeToBorder, m_density));
                m_shadowMat.SetVector(ScatteringPropId, m_scatteringCoefficient);
                m_shadowMat.SetVector(AbsorptionParamPropId, m_absorptionCoefficient);
                m_shadowMat.SetTexture(MainShapeTexParamPropId, m_mainShape);
                m_shadowMat.SetTexture(DetailShapeTexParamPropId, m_detailShape);
                m_shadowMat.SetFloat(CoveragePropId, m_coverage);
                m_shadowMat.SetFloat(FadeToCameraPropId, 1.0f / m_fadeToCamera);
            }

            if (m_beamShadowmap)
                m_mat.EnableKeyword("_BEAM_SHADOWMAP");
            else
                m_mat.DisableKeyword("_BEAM_SHADOWMAP");

            //找到player TODO:不能保证场景中就一个CinemachineVirtualCamera
            if (m_virtualCamera == null)
            {
                m_virtualCamera = GameObject.FindObjectOfType<CinemachineVirtualCamera>();
            }
            if (m_virtualCamera != null && m_virtualCamera.Follow != null)
            {
                m_mpb.SetVector(PlayerPropId, m_virtualCamera.Follow.position);
                if (m_beamShadowmap && m_shadowMat != null)
                {
                    m_shadowMat.SetVector(PlayerPropId, m_virtualCamera.Follow.position);
                }
            }
        }
        bool IsInShadowDistance()
        {
            Vector3 cameraPos = Vector3.zero;
            if (Camera.main != null)
            {
                cameraPos = Camera.main.transform.position;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView != null)
                cameraPos = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
#endif

            if (Vector3.Distance(cameraPos, m_cubeRenderer.bounds.ClosestPoint(cameraPos)) > m_shadowDistance)
                return false;
            return true;
        }
        void RenderShadow()
        {
            if (m_shadowMat == null)
            {
                return;
            }
            if (!IsInShadowDistance())
            {
                return;
            }

            if (m_beamShadowmap && m_shadowCB != null && m_shadowRT != null)
            {
                var mainLight = RenderSettings.sun;
                if (mainLight == null)
                    return;

                if (m_shadowRT.width != m_beamShadowSize)
                {
                    m_shadowRT.Release();
                    m_shadowRT = new RenderTexture(m_beamShadowSize, m_beamShadowSize, 0, RenderTextureFormat.RGHalf);
                    m_shadowRT.name = "VolumetricFogShadow";
                }

                if (m_shadowCB == null)
                {
                    m_shadowCB = new CommandBuffer();
                    m_shadowCB.name = "VolumetricFogShadow";
                }


                //TODO:如果Cube没有变化，可以只需要执行一次
                InitBoundCorners();
                ShadowUtils.CalculateCubeViewProjMatrix(mainLight.transform, m_corners, out var viewMat, out var projMat);

                m_shadowMat.SetVector(LightDirectionPropId, -mainLight.transform.forward);
                var vp = projMat * viewMat;
                var ivp = vp.inverse;
                m_shadowMat.SetMatrix(ShadowVPPropId, vp);
                m_shadowMat.SetMatrix(ShadowIVPPropId, ivp);

                m_shadowCB.Clear();
                Graphics.ExecuteCommandBuffer(m_shadowCB);
                m_shadowCB.SetRenderTarget(m_shadowRT);
                m_shadowCB.ClearRenderTarget(false, true, Color.clear);
                m_shadowCB.SetViewProjectionMatrices(viewMat, projMat);
                m_shadowCB.DrawRenderer(m_cubeRenderer, m_shadowMat);
                Graphics.ExecuteCommandBuffer(m_shadowCB);

                if (m_mpb != null)
                {
                    m_mpb.SetTexture(ShadowTexturePropId, m_shadowRT);
                    m_mpb.SetMatrix(ShadowVPPropId, vp);
                    m_mpb.SetMatrix(ShadowIVPPropId, ivp);
                    m_cubeRenderer.SetPropertyBlock(m_mpb);
                }
            }
        }
    }
}