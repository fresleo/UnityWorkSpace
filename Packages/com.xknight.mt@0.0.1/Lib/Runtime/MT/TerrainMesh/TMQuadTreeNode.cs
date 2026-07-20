// Created By: WangYu  Date: 2022-10-18

using System;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.TerrainMesh
{
    /// <summary>
    /// 地形网格的4叉树节点
    /// </summary>
    [Serializable]
    public class TMQuadTreeNode : AbsQuadTreeNode
    {
        //数据 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        public int meshId = -1;
        public byte lodLv;
        
        //节点直径
        private float m_diameter;
        
        public override void Serialize(Stream stream)
        {
            MTStreamUtils.WriteVector3(stream, bnd.center);
            MTStreamUtils.WriteVector3(stream, bnd.size);
            
            MTStreamUtils.WriteInt(stream, meshId);
            MTStreamUtils.WriteInt(stream, cellId);
            MTStreamUtils.WriteByte(stream, lodLv);

            int len = children != null ? children.Length : 0;
            MTStreamUtils.WriteInt(stream, len);
            for (int i = 0; i < len; i++)
            {
                int cid = children[i];
                MTStreamUtils.WriteInt(stream, cid);
            }
        }

        public override void Deserialize(Stream stream, Vector3 centerOffset)
        {
            Vector3 center = MTStreamUtils.ReadVector3(stream);
            Vector3 size = MTStreamUtils.ReadVector3(stream);
            bnd = new Bounds(center + centerOffset, size);
            
            meshId = MTStreamUtils.ReadInt(stream);
            cellId = MTStreamUtils.ReadInt(stream);
            lodLv = MTStreamUtils.ReadByte(stream);
            
            int len = MTStreamUtils.ReadInt(stream);
            children = new int[len];
            for (int i = 0; i < len; i++)
            {
                children[i] = MTStreamUtils.ReadInt(stream);
            }

            InnerInit();
        }
        
        private void InnerInit()
        {
            //当做一个平面看，创建的patch会更少
            Vector3 horizonSize = bnd.size;
            horizonSize.y = 0;
            m_diameter = horizonSize.magnitude;
        }
        
        public float PixelSize(Vector3 viewerPos, float fov, float screenH)
        {
            float pixelSize = MTRuntimeUtils.PixelSize(viewerPos, fov, screenH, bnd.center, m_diameter);
            return pixelSize;
        }
        
    }
}