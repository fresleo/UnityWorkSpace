// Created By: WangYu  Date: 2023-12-01

using System.Collections.Generic;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    public abstract class AbsQuadTreeBuildNode<TBuildNode> : IQuadTreeBuildNode 
        where TBuildNode : AbsQuadTreeBuildNode<TBuildNode>, new()
    {
        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds bnd;

        /// <summary>
        /// 子节点
        /// </summary>
        public List<TBuildNode> childrenNodes = new();

        
        /// <summary>
        /// 构建简单4叉树节点
        /// </summary>
        /// <param name="bnd">节点包围盒</param>
        /// <param name="depth">深度</param>
        public void BuildSimpleQuadTreeNode(Bounds bnd, int depth)
        {
            this.bnd = bnd;
            if (depth > 0)
            {
                BuildSimpleQuadTree(depth);
            }
        }
        
        //构建简单4叉树
        private void BuildSimpleQuadTree(int depth)
        {
            //添加子节点
            int subDepth = depth - 1;

            //子节点的包围盒中心偏移量
            Vector3 offsetSize = bnd.size;
            offsetSize /= 4.0f;
            //子节点包围盒的尺寸
            Vector3 childSize = bnd.size / 2.0f;
            //4叉树不需要在y轴上细分
            childSize.y = bnd.size.y;
            Vector3 center = bnd.center;

            //分成4份
            Bounds topLeft = new Bounds(new Vector3(center.x - offsetSize.x, center.y, center.z - offsetSize.z), childSize);
            Bounds bottomRight = new Bounds(new Vector3(center.x + offsetSize.x, center.y, center.z + offsetSize.z), childSize);
            Bounds topRight = new Bounds(new Vector3(center.x - offsetSize.x, center.y, center.z + offsetSize.z), childSize);
            Bounds bottomLeft = new Bounds(new Vector3(center.x + offsetSize.x, center.y, center.z - offsetSize.z), childSize);
            
            childrenNodes.Clear();

            var node0 = new TBuildNode();
            node0.BuildSimpleQuadTreeNode(topLeft, subDepth);
            childrenNodes.Add(node0);
            
            var node1 = new TBuildNode();
            node1.BuildSimpleQuadTreeNode(bottomRight, subDepth);
            childrenNodes.Add(node1);
            
            var node2 = new TBuildNode();
            node2.BuildSimpleQuadTreeNode(topRight, subDepth);
            childrenNodes.Add(node2);
            
            var node3 = new TBuildNode();
            node3.BuildSimpleQuadTreeNode(bottomLeft, subDepth);
            childrenNodes.Add(node3);
        }


        public virtual void Clear()
        {
            foreach (var item in childrenNodes)
            {
                item?.Clear();
            }
            childrenNodes.Clear();
        }

        public bool ClearEmpty()
        {
            bool canSafeDelete = false;

            //删集合得倒着遍历
            int count = childrenNodes.Count;
            while (count > 0)
            {
                count--;
                if (childrenNodes[count].ClearEmpty())
                {
                    childrenNodes.RemoveAt(count);
                }
            }

            //子节点没有，数据也没有
            if (childrenNodes.Count == 0 && !HasData())
            {
                canSafeDelete = true;
            }
            
            return canSafeDelete;
        }
        
        protected abstract bool HasData();

    }
}