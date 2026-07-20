using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

namespace Garena.TA
{
    /// <summary>
    /// 每个分区的数据
    /// </summary>
    public class GPerObjectShadowSliceData
    {
        /// <summary>
        /// 世界空间包围盒
        /// </summary>
        public Bounds BoundsWS;

        public Matrix4x4 ShadowMeshLocalToWorld;

        /// <summary>
        /// 视图矩阵
        /// </summary>
        public Matrix4x4 ViewMatrix;

        /// <summary>
        /// 投影矩阵
        /// </summary>
        public Matrix4x4 ProjMatrix;

        /// <summary>
        /// 世界坐标到ShadowUV
        /// </summary>
        public Matrix4x4 WorldToShadowMatrix;

        /// <summary>
        /// 渲染到阴影纹理的区域
        /// </summary>
        public Rect ViewPort = new Rect();

        /// <summary>
        /// 分区偏移值
        /// </summary>
        public Vector2 SliceOffset;


        /// <summary>内部在开启SRP合批时，计算阴影剔除使用的变量，不会留存上一帧的信息</summary>
        private Plane[] srpCullingPlanes = new Plane[6];


        public List<GPerObjectShadowTargetRenderData> renderDatas = new List<GPerObjectShadowTargetRenderData>();

        private GPerObjectShadowPassSettings settings;

        public GPerObjectShadowSliceData(GPerObjectShadowPassSettings settings)
        {
            this.settings = settings;
        }


        public void UpdateMatrix(Quaternion shadowLightRotation, float halfAngleLerp = 0, bool useSettingsLightRotation = true)
        {
            Quaternion lightRotation = shadowLightRotation;

            if (useSettingsLightRotation && settings.OverrideLightRotation)
            {
                lightRotation = Quaternion.Euler(settings.LightRotation);
            }
            
            Camera cam = GetShadowMatrixCamera();
            
            // 插值，混合摄像机与灯光方向
            lightRotation = QuaternionLerp(lightRotation, cam, halfAngleLerp);

            Vector3 ex = settings.FrustumExtend;

            GPerObjectShadowUtils.GetLightMatrix(cam, lightRotation, BoundsWS, settings.FrustumExtendUsePercent, ex,
            out ViewMatrix, out ProjMatrix, out ShadowMeshLocalToWorld);

            //var descriptor = renderingData.cameraData.cameraTargetDescriptor;

            //if (renderingData.cameraData.camera.orthographic)
            //{
            //    ProjMatrix[0, 3] -= (GPerObjectShadowManager.Instance.Offset.x * 2 - 1) / descriptor.width;
            //    ProjMatrix[1, 3] -= (GPerObjectShadowManager.Instance.Offset.y * 2 - 1) / descriptor.height;
            //}
            //else
            //{
            //    ProjMatrix[0, 2] += (GPerObjectShadowManager.Instance.Offset.x * 2 - 1) / descriptor.width;
            //    ProjMatrix[1, 2] += (GPerObjectShadowManager.Instance.Offset.y * 2 - 1) / descriptor.height;
            //}

            //WorldToShadowMatrix = GPerObjectShadowUtils.GetShadowTransform(GL.GetGPUProjectionMatrix(ProjMatrix, true), ViewMatrix);

            WorldToShadowMatrix = GPerObjectShadowUtils.GetShadowTransform(ProjMatrix, ViewMatrix);
        }

        private static Camera GetShadowMatrixCamera()
        {
            Camera cam = null;
            if (Application.isPlaying)
            {
                cam = Camera.main;
            }
#if UNITY_EDITOR
            else
            {
                UnityEditor.SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    cam = sceneView.camera;
                }
            }
#endif

            if (cam == null)
            {
                if(Camera.current == null && Camera.allCameras.Count() > 0)
                {
                    cam = Camera.allCameras[0];
                }
                else
                {
                    cam = Camera.current;   //这里还是有可能为Null
                }
            }

            return cam;
        }

        /// <summary>
        /// 执行渲染，渲染到阴影纹理的对应分区上
        /// </summary>
        public void Render(ref ScriptableRenderContext context, ref RenderingData renderingData, uint index, Material srpMaterial = null)
        {
            Render(ref context, renderingData.commandBuffer, renderingData.cameraData.camera, index, srpMaterial);
        }

        public void Render(ref ScriptableRenderContext context, CommandBuffer cmd, Camera camera, uint index, Material srpMaterial = null)
        {
            cmd.SetViewport(ViewPort);
            cmd.SetViewProjectionMatrices(ViewMatrix, ProjMatrix);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (settings.srpBatcher)
            {
                uint layer = (uint)(1 << (int)(index + 16));
                //var camera = renderingData.cameraData.camera;
                if (camera == null) 
                    return;

                CullingResults cullResults;// = renderingData.cullResults;

                if (camera.TryGetCullingParameters(out var cullingParams))
                {
                    cullingParams.cullingMatrix = ProjMatrix * ViewMatrix;
                    GeometryUtility.CalculateFrustumPlanes(cullingParams.cullingMatrix, srpCullingPlanes);
                    for (int i = 0; i < 6; i++)
                    {
                        cullingParams.SetCullingPlane(i, srpCullingPlanes[i]);
                    }
                    cullResults = context.Cull(ref cullingParams);
                }
                else
                {
                    return;
                }

                DrawingSettings drawingSettings = new DrawingSettings(new ShaderTagId("ShadowCaster"), new SortingSettings(camera));

                if (srpMaterial != null)
                {
                    drawingSettings.overrideMaterial = srpMaterial;
                }

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, -1, layer);

                var rendererListParams = new RendererListParams(cullResults, drawingSettings, filteringSettings);
                var rendererList = context.CreateRendererList(ref rendererListParams);

                cmd.DrawRendererList(rendererList);
            }
            else
            {
                foreach (var item in renderDatas)
                {
                    if (!item.renderer.enabled)
                        continue;

                    int submeshCount = item.submeshCount;
                    for (int i = 0; i < submeshCount; i++)
                    {
                        //TODO : 使用item.shadowPass绘制
                        cmd.DrawRenderer(item.renderer, item.material, i, item.shaderPass);
                    }
                }
            }
        }

        // 对灯光向量和摄像机向量进行插值
        public Quaternion QuaternionLerp(Quaternion light, Camera camera, float t)
        {
            if (t == 0 || camera == null) return light;

            Vector3 _x = light * Vector3.back;
            Vector3 _y = (camera.transform.position - BoundsWS.center).normalized;
            float l = Mathf.Clamp01((Vector2.Dot(new Vector2(_x.x, _x.z).normalized, 
                new Vector2(_y.x, _y.z).normalized) + 1) * 4);
            
            Vector3 dir = Vector3.Lerp(_x, _y, Mathf.Pow(t, 0.45f) * l).normalized;
            return Quaternion.LookRotation(-dir);
            
            // 这个方法有误差，阴影会闪
            // return Quaternion.Lerp(light, Quaternion.LookRotation(-_y), t * l);
        }

    }
}
