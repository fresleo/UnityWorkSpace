// Created By: WangYu  Date: 2024-05-30

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Common;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    /// <summary>
    /// 空间数据标记构建节点
    /// </summary>
    public partial class SDTQuadTreeBuildNode : AbsQuadTreeBuildNode<SDTQuadTreeBuildNode>
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
        protected override bool HasData()
        {
            return holdIds.Count > 0;
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