// Created By: WangYu  Date: 2023-11-30

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.InstancedObject
{
    public partial class InstancedObjectDataGenerator : AbsGenerator<InstancedObjectDataGenerator>
    {
        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds bnd;
        /// <summary>
        /// 扩大范围
        /// </summary>
        public float expand;
        
        
        /// <summary>
        /// 数据类型
        /// </summary>
        public IOGroupConfig.EDataType dataType = IOGroupConfig.EDataType.Flat;
        
        /// <summary>
        /// 设置树的深度
        /// </summary>
        public int treeDepth;
        
        
        /// <summary>
        /// lod网格
        /// </summary>
        public Mesh[] lodMeshes;

        /// <summary>
        /// lod材质
        /// </summary>
        public Material[] lodMaterials;
        
        
        /// <summary>
        /// 子对象
        /// </summary>
        public List<InstancedObjectMarker> childrenMarkers = new();
        
        
        /// <summary>
        /// 4叉树的根节点
        /// </summary>
        public IOQuadTreeBuildNode quadTreeRoot;
        
        /// <summary>
        /// 自动创建加载器
        /// </summary>
        public bool autoCreateLoader;
        
    }
}