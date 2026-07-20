// Created By: WangYu  Date: 2024-05-30

using com.xknight.mt.Lib.Runtime.MT.Common;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public partial class SpatialDataTagGenerator : AbsGenerator<SpatialDataTagGenerator>
    {
        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds bnd;
        
        /// <summary>
        /// 设置树的深度
        /// </summary>
        public int treeDepth;
        
        /// <summary>
        /// 4叉树的根节点
        /// </summary>
        public SDTQuadTreeBuildNode quadTreeRoot;
        
        /// <summary>
        /// 自动创建加载器
        /// </summary>
        public bool autoCreateLoader;
        
    }
}