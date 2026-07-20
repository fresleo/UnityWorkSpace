// Created By: WangYu  Date: 2022-10-10

using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节 Patch 层的基类
    /// </summary>
    public abstract class AbsDetailPatchLayer
    {
        public abstract bool IsSpawnDone { get; }
        
        protected DetailLayerData m_layerData;
        protected Mesh m_mesh;
        protected Material m_material_lod0;
        protected Material m_material_lod1;
        protected bool m_receiveShadow;
        protected MTArray<DetailPatchDrawParam> m_drawParam;
        protected int m_totalPrototypeCount;
        
        private DetailPatchCutoffAnim m_cutoffAnim;
        
        protected AbsDetailPatchLayer(DetailLayerData data, bool receiveShadow)
        {
            m_layerData = data;
            m_mesh = data.prototype.GetComponent<MeshFilter>().sharedMesh;
            var matSrc = data.prototype.GetComponent<MeshRenderer>().sharedMaterial;
            m_receiveShadow = receiveShadow;
            
            m_material_lod0 = new Material(matSrc);
            if (m_receiveShadow)
            {
                m_material_lod0.EnableKeyword("_MAIN_LIGHT_SHADOWS");
                
                if (UniversalRenderPipeline.asset.supportsSoftShadows)
                {
                    m_material_lod0.EnableKeyword("_SHADOWS_SOFT");
                }
                else
                {
                    m_material_lod0.DisableKeyword("_SHADOWS_SOFT");
                }

                if (UniversalRenderPipeline.asset.shadowCascadeCount > 1)
                {
                    m_material_lod0.EnableKeyword("_MAIN_LIGHT_SHADOWS_CASCADE");
                }
                else
                {
                    m_material_lod0.DisableKeyword("_MAIN_LIGHT_SHADOWS_CASCADE");
                }
            }

            m_material_lod1 = new Material(m_material_lod0);
            m_material_lod1.DisableKeyword("_NORMALMAP");
            m_material_lod1.EnableKeyword("FORCE_UP_NORMAL");
            
            m_cutoffAnim = new DetailPatchCutoffAnim(m_material_lod1);
        }

        /// <summary>
        /// 清理
        /// </summary>
        public virtual void Clear()
        {
            PushData();
            
            m_cutoffAnim = null;
            
            if (m_material_lod0 != null)
            {
                UnityEngine.Object.Destroy(m_material_lod0);
                m_material_lod0 = null;
            }
            if (m_material_lod1 != null)
            {
                UnityEngine.Object.Destroy(m_material_lod1);
                m_material_lod1 = null;
            }
        }
        
        /// <summary>
        /// 回收参数
        /// </summary>
        public virtual void PushData()
        {
            if (m_drawParam != null)
            {
                for (int i = 0; i < m_drawParam.Length; i++)
                {
                    DetailPatchDrawParam.Push(m_drawParam[i]);
                }
                m_drawParam.Reset();
            }
        }
        
        /// <summary>
        /// 当激活时
        /// </summary>
        public virtual void OnActivate(bool rebuild)
        {
            if (m_cutoffAnim.State != DetailPatchCutoffAnim.EState.PlayDone)
            {
                m_cutoffAnim.Replay(false);
            }

            if (rebuild)
            {
                m_totalPrototypeCount = 0;
            }
        }
        
        /// <summary>
        /// 当不激活时
        /// </summary>
        public virtual void OnDeactive()
        {
            m_cutoffAnim.Replay(true);
        }

        /// <summary>
        /// 每帧构建
        /// </summary>
        public abstract void TickBuild();
        
        /// <summary>
        /// 当绘制参数准备好时
        /// </summary>
        public virtual void OnDrawParamReady()
        {
            m_cutoffAnim.Replay(false);
        }

        /// <summary>
        /// 当绘制时
        /// </summary>
        public virtual void OnDraw(Camera drawCamera, int lod, out bool matInvisible)
        {
            matInvisible = true;
            if (m_drawParam == null)
            {
                return;
            }

            m_cutoffAnim.Update();
            if (m_cutoffAnim.MatInvisible)
            {
                matInvisible = true;
                return;
            }
            matInvisible = false;

            for (int i = 0; i < m_drawParam.Length; i++)
            {
                if (m_drawParam[i].used <= 0)
                {
                    continue;
                }

                var mat = m_material_lod0;
                if (lod > 0)
                {
                    mat = m_material_lod1;
                }

                m_drawParam[i].matBlock.SetVectorArray("_PerInstanceColor", m_drawParam[i].colors);

                int layer = LayerMask.NameToLayer("Default");
                Graphics.DrawMeshInstanced(m_mesh, 0, mat,
                    m_drawParam[i].matrixs, m_drawParam[i].used, m_drawParam[i].matBlock,
                    ShadowCastingMode.Off, m_receiveShadow, layer, drawCamera);
            }
        }

    }
}