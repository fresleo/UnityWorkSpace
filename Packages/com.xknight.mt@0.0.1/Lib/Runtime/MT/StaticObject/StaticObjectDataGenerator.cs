// Created By: WangYu  Date: 2023-11-18

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Common;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.StaticObject
{
    /// <summary>
    /// 静态对象体积组件
    /// </summary>
    public partial class StaticObjectDataGenerator : AbsGenerator<StaticObjectDataGenerator>
    {
        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds bnd;
        /// <summary>
        /// 扩大
        /// </summary>
        public float expand;
        
        
        /// <summary>
        /// 设置树的深度
        /// </summary>
        public int treeDepth;
        
        /// <summary>
        /// 4叉树的根节点
        /// </summary>
        public SOQuadTreeBuildNode quadTreeRoot;
        
        
        /// <summary>
        /// 子对象列表
        /// </summary>
        public List<GameObject> childrenGos = new();

        /// <summary>
        /// 原型对象列表
        /// </summary>
        public List<GameObject> prototypes = new();
        
        
        /// <summary>
        /// 自动创建加载器
        /// </summary>
        public bool autoCreateLoader;

    }
}