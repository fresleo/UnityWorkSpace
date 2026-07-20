// Created By: WangYu  Date: 2023-11-28

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.StaticObject
{
    /// <summary>
    /// 静态对象的4叉树构建节点
    /// </summary>
    public partial class SOQuadTreeBuildNode : AbsQuadTreeBuildNode<SOQuadTreeBuildNode>
    {
        //数据 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// 持有的gid
        /// </summary>
        public List<int> holdGids = new();
        
        /// <summary>
        /// 持有的资源索引id
        /// </summary>
        public List<int> holdAssetIdxs = new();
        
        /// <summary>
        /// 持有的世界矩阵
        /// </summary>
        public List<Matrix4x4> holdWorldMatrixs = new();
        
        /// <summary>
        /// Lightmap 配置数据
        /// </summary>
        public Dictionary<int, LightmapConfig[]> holdLightmapDatas = new();
        
        
        public override void Clear()
        {
            base.Clear();
            
            holdGids.Clear();
            holdAssetIdxs.Clear();
            holdWorldMatrixs.Clear();
            holdLightmapDatas.Clear();
        }

        protected override bool HasData()
        {
            return holdGids.Count > 0;
        }
    }
}