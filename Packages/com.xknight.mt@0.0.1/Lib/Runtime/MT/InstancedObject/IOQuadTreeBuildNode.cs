// Created By: WangYu  Date: 2023-11-30

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.InstancedObject
{
    public partial class IOQuadTreeBuildNode : AbsQuadTreeBuildNode<IOQuadTreeBuildNode>
    {
        //数据 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// 对象包围盒
        /// </summary>
        public List<Bounds> holdBounds = new();
        
        /// <summary>
        /// 世界矩阵
        /// </summary>
        public List<Matrix4x4> holdWorldMatrixs = new();
        
        /// <summary>
        /// Lightmap 配置数据
        /// </summary>
        public List<LightmapConfig> holdLightmapDatas = new();
        
        
        public override void Clear()
        {
            base.Clear();
            
            holdBounds.Clear();
            holdWorldMatrixs.Clear();
            holdLightmapDatas.Clear();
        }

        protected override bool HasData()
        {
            return holdBounds.Count > 0;
        }
        
    }
}