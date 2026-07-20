// Created By: WangYu  Date: 2022-10-10

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节 Patch 的基类
    /// </summary>
    public abstract class AbsDetailPatch
    {
        /// <summary>
        /// 构建完成
        /// </summary>
        public abstract bool IsBuildDone { get; }
        
        protected int m_densityX;
        protected int m_densityZ;
        protected Vector3 m_pos;
        
        protected Vector2 m_centerPos;
        protected float m_lod0Range;
        
        //细节层
        protected AbsDetailPatchLayer[] m_layers;

        public AbsDetailPatch(int densityX, int densityZ, Vector3 pos)
        {
            m_densityX = densityX;
            m_densityZ = densityZ;
            m_pos = pos;

            float x = m_pos.x + (m_densityX + 0.5f) * m_pos.z;
            float y = m_pos.y + (m_densityZ + 0.5f) * m_pos.z;
            m_centerPos = new Vector2(x, y);
            
            m_lod0Range = m_pos.z * 1.5f;
        }

        
        public virtual void PushData()
        {
            for (int i = 0; i < m_layers.Length; i++)
            {
                m_layers[i].PushData();
            }
        }

        public virtual void Clear()
        {
            for (int i = 0; i < m_layers.Length; i++)
            {
                m_layers[i].Clear();
            }
        }
        
        public virtual void Activate()
        {
            for (int i = 0; i < m_layers.Length; i++)
            {
                m_layers[i].OnActivate(true);
            }
        }

        public virtual void Deactivate()
        {
            for (int i = 0; i < m_layers.Length; i++)
            {
                m_layers[i].OnDeactive();
            }
        }
        
        public abstract void TickBuild();

        
        /// <summary>
        /// 绘制
        /// </summary>
        public void Draw(Camera drawCamera, out bool invisible)
        {
            //根据与摄像机的距离，判断LOD
            int lod = 1;
            if (drawCamera != null)
            {
                var pos = drawCamera.transform.position;
                float x = m_centerPos.x - pos.x;
                float y = m_centerPos.y - pos.z;
                Vector2 distance = new Vector2(x, y);
                if (distance.magnitude < m_lod0Range)
                {
                    lod = 0;
                }
            }

            invisible = true;
            for (int i = 0; i < m_layers.Length; i++)
            {
                m_layers[i].OnDraw(drawCamera, lod, out bool matInvisible);
                if (!matInvisible)
                {
                    invisible = false;
                }
            }
        }

        public void DrawDebug()
        {
#if UNITY_EDITOR
            var lastColor = Gizmos.color;
            {
                Gizmos.color = Color.yellow;

                float x = m_pos.x + m_densityX * m_pos.z;
                float z = m_pos.y + m_densityZ * m_pos.z;
                var min = new Vector3(x, 0, z);

                const float height = 10;
                var size = new Vector3(m_pos.z, height, m_pos.z);
                var center = min + size * 0.5f;
                
                Gizmos.DrawWireCube(center, size);
            }
            Gizmos.color = lastColor;
#endif //UNITY_EDITOR
        }
        
    }
}