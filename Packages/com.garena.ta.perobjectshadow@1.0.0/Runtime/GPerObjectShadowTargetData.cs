using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Garena.TA
{
    /// <summary>
    /// 单个对象的逐物体阴影数据
    /// </summary>
    public class GPerObjectShadowTargetData
    {
        /// <summary>
        /// 关联的物体
        /// </summary>
        public GameObject go;

        /// <summary>
        /// 距离当前相机的距离
        /// </summary>
        public float distance;

        /// <summary>
        /// 世界空间包围盒
        /// </summary>
        public Bounds bounds;


        public Matrix4x4 shadowBoundLocalToWorld;

        public Vector3 shadowBoundsSize;

        /// <summary>
        /// 当前lod
        /// </summary>
        public int curLod;

        /// <summary>
        /// 物体上的LODGroup(可能为空)
        /// </summary>
        public LODGroup lodGroup;

        /// <summary>
        /// 每级LOD的渲染数据
        /// </summary>
        public List<List<GPerObjectShadowTargetRenderData>> lodRenderData = new List<List<GPerObjectShadowTargetRenderData>>();

        /// <summary>
        /// 每帧更新包围盒
        /// </summary>
        public bool updateBounds;

        /// <summary>
        /// 使用自定义包围盒
        /// </summary>
        public bool useCustomBounds;

        /// <summary>
        /// 自定义包围盒
        /// </summary>
        public Bounds customBounds;
        
        
        private List<Renderer> renderTargets = new List<Renderer>();

        /// <summary>
        /// 记录初始Renderer阴影开启状态
        /// </summary>
        private Dictionary<Renderer, GPerObjectShadowRendererState> saveCastShadow = new Dictionary<Renderer, GPerObjectShadowRendererState>();

        public bool enabled;

        public uint renderingLayerMask;

        #region 室内阴影相关

        /// <summary>
        /// 使用独立的光照信息
        /// </summary>
        public bool useCustomLightData = false;

        /// <summary>
        /// 自定义光源朝向
        /// </summary>
        public Vector3 customLightRotation;

        #endregion
        

        public void Enable()
        {
            if (enabled)
            {
                return;
            }

            ///这种设置下确保有初始包围盒
            if (!useCustomBounds && !updateBounds)
            {
                bounds = GPerObjectShadowUtils.GetBounds(go);
            }

            UpdateBounds();
            UpdateRenderData();
            SaveCastShadowState();
            EnablePerObjectShadow(true);

            enabled = true;
        }

        public void Disable()
        {
            EnablePerObjectShadow(false);

            enabled = false;
        }

        /// <summary>
        /// 收集投射阴影开启状态
        /// </summary>
        public void SaveCastShadowState(bool reset = false)
        {
            if (reset)
            {
                EnablePerObjectShadow(false);
            }

            renderTargets.Clear();
            saveCastShadow.Clear();
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var item in renderers)
            {
                if (item is ParticleSystemRenderer)
                    continue;

                GPerObjectShadowRendererState state = new GPerObjectShadowRendererState();
                state.shadowCastingMode = item.shadowCastingMode;
                state.renderingLayerMask = item.renderingLayerMask;
                saveCastShadow.Add(item, state);
                renderTargets.Add(item);
            }
        }

        /// <summary> CastingShadow是否准备完毕(防止外部修改导致的记录错误) </summary>
        public bool ShadowCastingReady()
        {
            return saveCastShadow != null && saveCastShadow.Count > 0;
        }

        /// <summary>
        /// 开启逐物体阴影时关闭原有阴影
        /// </summary>
        /// <param name="v"></param>
        public void EnablePerObjectShadow(bool v)
        {
            foreach (var item in saveCastShadow)
            {
                if (item.Key == null)
                {
                    continue;
                }
                item.Key.shadowCastingMode = v ? ShadowCastingMode.Off : item.Value.shadowCastingMode;

                if (!v)
                {
                    item.Key.renderingLayerMask = item.Value.renderingLayerMask;
                }
            }
        }
        
        /// <summary> 开启逐物体阴影(开启逐物体阴影时关闭原有阴影)</summary>
        public void IsEnablePerObjectShadow(bool v)
        {
            for (int i = 0; i < renderTargets.Count; i++)
            {
                var renderer = renderTargets[i];
                if (renderer == null)
                {
                    continue;
                }
                renderer.shadowCastingMode = v ? ShadowCastingMode.Off : saveCastShadow[renderTargets[i]].shadowCastingMode;
            }
        }

        public void ResetRenderingLayerState()
        {
            foreach (var item in saveCastShadow)
            {
                if (item.Key == null)
                {
                    continue;
                }
                item.Key.renderingLayerMask = item.Value.renderingLayerMask;
            }
        }

        /// <summary>
        /// 更新渲染数据
        /// </summary>
        public void UpdateRenderData()
        {
            lodRenderData.Clear();

            lodGroup = go.GetComponentInChildren<LODGroup>();

            if (lodGroup == null)
            {
                lodRenderData.Add(GetRenderData(go.GetComponentsInChildren<Renderer>()));
            }
            else
            {
                var lods = lodGroup.GetLODs();
                foreach (var lod in lods)
                {
                    lodRenderData.Add(GetRenderData(lod.renderers));
                }
            }
        }

        /// <summary>
        /// 更新包围盒
        /// </summary>
        public void UpdateBounds()
        {
            if (useCustomBounds)
            {
                bounds = customBounds;
            }
            else if (updateBounds)
            {
                //bounds = GPerObjectShadowUtils.GetBounds(go);
                //bounds = new Bounds();
                bool first = true;

                foreach (var item in saveCastShadow.Keys)
                {
                    if (item == null || !item.enabled)
                    {
                        continue;
                    }
                    
                    if (first)
                    {
                        bounds = item.bounds;
                    }
                    else
                    {
                        bounds.Encapsulate(item.bounds);
                    }

                    first = false;
                }
            }
        }

        /// <summary>
        /// 更新距离和LOD
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="lodBias"></param>
        public void UpdateCameraDistanceAndLOD(Camera camera, int lodBias)
        {
            if (lodGroup == null)
            {
                curLod = 0;
            }
            else
            {
                curLod = GPerObjectShadowUtils.GetVisibleLOD(lodGroup, camera) + lodBias;
            }

            distance = Vector3.Distance(camera.transform.position, go.transform.position);
        }

        public void UpdateRenderingLayerMask(int index)
        {
            renderingLayerMask = (uint)(1 << (int)(index + 16));

            var list = GetRenderDatas();
            foreach (var item in list)
            {
                if (item == null || item.renderer == null)
                {
                    continue;
                }
                item.renderer.renderingLayerMask = renderingLayerMask;
            }
        }

        public List<GPerObjectShadowTargetRenderData> GetRenderDatas()
        {
            if (curLod < lodRenderData.Count)
            {
                return lodRenderData[curLod];
            }

            return new List<GPerObjectShadowTargetRenderData>();
        }

        private List<GPerObjectShadowTargetRenderData> GetRenderData(Renderer[] rendererArr)
        {
            List<GPerObjectShadowTargetRenderData> renderDatas = new List<GPerObjectShadowTargetRenderData>();

            for (int i = 0; i < rendererArr.Length; i++)
            {
                var renderer = rendererArr[i];
                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    var data = new GPerObjectShadowTargetRenderData();
                    data.renderer = renderer;
                    data.submeshCount = skinnedMeshRenderer.sharedMesh.subMeshCount;
                    data.material = renderer.sharedMaterial;

                    if (data.material != null)
                    {
                        data.shaderPass = data.material.FindPass("ShadowCaster");
                        renderDatas.Add(data);
                    }
                }
                else
                {
                    var mf = renderer.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        var data = new GPerObjectShadowTargetRenderData();
                        data.renderer = renderer;
                        data.submeshCount = mf.sharedMesh.subMeshCount;
                        data.material = renderer.sharedMaterial;

                        if (data.material != null)
                        {
                            data.shaderPass = data.material.FindPass("ShadowCaster");
                            renderDatas.Add(data);
                        }
                    }
                }
            }
            return renderDatas;
        }
    }

    public class GPerObjectShadowTargetRenderData
    {
        public Renderer renderer;
        public int submeshCount;
        public Material material;
        public int shaderPass = 1;
    }

    public class GPerObjectShadowRendererState
    {
        public ShadowCastingMode shadowCastingMode;
        public uint renderingLayerMask;
    }

}