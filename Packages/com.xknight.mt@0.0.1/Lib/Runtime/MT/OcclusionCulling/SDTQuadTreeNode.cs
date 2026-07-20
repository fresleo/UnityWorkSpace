// Created By: WangYu  Date: 2024-05-31

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    [Serializable]
    public class SDTQuadTreeNode : AbsQuadTreeNode
    {
        // 数据 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// 空间数据标记的id
        /// </summary>
        public List<int> holdIds = new();
        
        /// <summary>
        /// 对象包围盒
        /// </summary>
        public List<Bounds> holdBounds = new ();
        
        /// <summary>
        /// 持有的世界矩阵
        /// </summary>
        public List<Matrix4x4> holdWorldMatrixs = new ();
        
        
        // 方法 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        public override void Serialize(Stream stream)
        {
            MTStreamUtils.WriteVector3(stream, bnd.center);
            MTStreamUtils.WriteVector3(stream, bnd.size);
            
            int len;

            len = holdIds.Count;
            MTStreamUtils.WriteInt(stream, len);
            foreach (var item in holdIds)
            {
                MTStreamUtils.WriteInt(stream, item);
            }

            len = holdBounds.Count;
            MTStreamUtils.WriteInt(stream, len);
            foreach (var item in holdBounds)
            {
                MTStreamUtils.WriteVector3(stream, item.center);
                MTStreamUtils.WriteVector3(stream, item.size);
            }

            len = holdWorldMatrixs.Count;
            MTStreamUtils.WriteInt(stream, len);
            foreach (var item in holdWorldMatrixs)
            {
                MTStreamUtils.WriteVector4(stream, item.GetColumn(0));
                MTStreamUtils.WriteVector4(stream, item.GetColumn(1));
                MTStreamUtils.WriteVector4(stream, item.GetColumn(2));
                MTStreamUtils.WriteVector4(stream, item.GetColumn(3));
            }
            
            len = children != null ? children.Length : 0;
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

            int len;
            
            // id
            len = MTStreamUtils.ReadInt(stream);
            holdIds.Clear();
            for (int i = 0; i < len; i++)
            {
                int gid = MTStreamUtils.ReadInt(stream);
                holdIds.Add(gid);
            }
            
            // 包围盒
            len = MTStreamUtils.ReadInt(stream);
            holdBounds.Clear();
            for (int i = 0; i < len; i++)
            {
                center = MTStreamUtils.ReadVector3(stream);
                size = MTStreamUtils.ReadVector3(stream);
                holdBounds.Add(new Bounds(center, size));
            }
            
            // 世界矩阵
            len = MTStreamUtils.ReadInt(stream);
            holdWorldMatrixs.Clear();
            for (int i = 0; i < len; i++)
            {
                var column0 = MTStreamUtils.ReadVector4(stream);
                var column1 = MTStreamUtils.ReadVector4(stream);
                var column2 = MTStreamUtils.ReadVector4(stream);
                var column3 = MTStreamUtils.ReadVector4(stream);
                
                Matrix4x4 matr = new Matrix4x4(column0, column1, column2, column3);
                holdWorldMatrixs.Add(matr);
            }
            
            // 子节点的id
            len = MTStreamUtils.ReadInt(stream);
            children = new int[len];
            for (int i = 0; i < len; i++)
            {
                children[i] = MTStreamUtils.ReadInt(stream);
            }
        }

        public override void Clear()
        {
            base.Clear();
            holdIds.Clear();
            holdBounds.Clear();
            holdWorldMatrixs.Clear();
        }
    }
}