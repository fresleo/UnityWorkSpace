// Created By: WangYu  Date: 2025-02-18

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirSticker.Runtime.Logic
{
    /// <summary>
    /// 贴花网格渲染器的抽象类
    /// </summary>
    public abstract class AbsDecalRenderer : MonoBehaviour, IDecalMeshRenderer
    {
        public long decalUniqueKey;
        public Component receiverComponent;
        
        protected MeshRenderer m_meshRenderer;
        protected MeshFilter m_meshFilter;
        protected SkinnedMeshRenderer m_skinnedMeshRenderer;
        
        protected bool m_isDestroyed;
        protected AbsDecalConfig m_lifeConfig;
        
        
        public void InitRenderer()
        {
            if (this.receiverComponent is MeshRenderer || this.receiverComponent is Terrain)
            {
                m_meshRenderer = gameObject.AddComponent<MeshRenderer>();
                m_meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            else if (this.receiverComponent is SkinnedMeshRenderer smr)
            {
                m_skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
                m_skinnedMeshRenderer.rootBone = smr.rootBone;
                m_skinnedMeshRenderer.bones = smr.bones;
            }
        }
        
        public abstract void SetDisplayResource(Material cloneMaterial, Mesh mesh);

        protected virtual void OnDestroy()
        {
            // 必须在 mono 的 OnDestroy 里标记一下。当程序意外结束，在回收 DecalMesh 时，会调 ReleaseRendering，但是 m_isDestroyed 的状态没变过，GO 却已经回收了，就会导致报错
            ReleaseRendering();
        }

        public void ReleaseRendering()
        {
            if(m_isDestroyed) return;
            
            UnityUtils.DestroyUnityObject(this.gameObject);
            m_isDestroyed = true;
        }
        
        public virtual void CreateLifecycle(long uniqueKey, AbsDecalConfig lifeConfig, Action<long> callback)
        {
            m_lifeConfig = lifeConfig;
        }
        
    }
}
