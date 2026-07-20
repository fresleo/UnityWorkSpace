// Created By: WangYu  Date: 2023-12-01

using System.IO;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    public abstract class AbsQuadTreeNode : IQuadTreeNode
    {
        public abstract void Serialize(Stream stream);
        public abstract void Deserialize(Stream stream, Vector3 centerOffset);

        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds bnd;
        
        /// <summary>
        /// 节点的单元格id
        /// </summary>
        public int cellId = -1;
        /// <summary>
        /// 子节点的单元格id
        /// </summary>
        public int[] children;

        public virtual void Clear()
        {
            children = null;
        }
        
        public virtual void Initialize(int cid)
        {
            cellId = cid;
        }
        
    }
}