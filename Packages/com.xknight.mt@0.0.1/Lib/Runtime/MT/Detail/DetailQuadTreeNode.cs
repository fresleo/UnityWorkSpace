// Created By: WangYu  Date: 2022-10-10

using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节4叉树节点
    /// </summary>
    internal class DetailQuadTreeNode
    {
        private int m_depth;
        private Bounds m_bnd;
        
        //子节点
        private DetailQuadTreeNode[] m_children;
        //叶子节点上记录的 patch id
        private int m_patchId = -1;

        public DetailQuadTreeNode(int depth, Bounds bnd, Bounds worldBounds)
        {
            m_depth = depth;
            m_bnd = bnd;
            
            //是叶子节点
            if (m_depth < 1)
            {
                var localCenter = bnd.center - worldBounds.min;
                int pX = Mathf.FloorToInt(localCenter.x / bnd.size.x);
                int pZ = Mathf.FloorToInt(localCenter.z / bnd.size.z);
                
                int pWidth = Mathf.FloorToInt(worldBounds.size.x / bnd.size.x);
                
                m_patchId = pZ * pWidth + pX;
                return;
            }

            //是树干节点
            m_children = new DetailQuadTreeNode[4];
            int subDepth = depth - 1;
            
            Vector3 subSize = bnd.size;
            subSize.x *= 0.5f;
            subSize.z *= 0.5f;
            Vector3 subCenter = bnd.center;
            subCenter.x -= 0.5f * subSize.x;
            subCenter.z -= 0.5f * subSize.z;
            Bounds subBnd = new Bounds(subCenter, subSize);
            m_children[0] = new DetailQuadTreeNode(subDepth, subBnd, worldBounds);
            
            subCenter = bnd.center;
            subCenter.x += 0.5f * subSize.x;
            subCenter.z -= 0.5f * subSize.z;
            subBnd = new Bounds(subCenter, subSize);
            m_children[1] = new DetailQuadTreeNode(subDepth, subBnd, worldBounds);
            
            subCenter = bnd.center;
            subCenter.x += 0.5f * subSize.x;
            subCenter.z += 0.5f * subSize.z;
            subBnd = new Bounds(subCenter, subSize);
            m_children[2] = new DetailQuadTreeNode(subDepth, subBnd, worldBounds);
            
            subCenter = bnd.center;
            subCenter.x -= 0.5f * subSize.x;
            subCenter.z += 0.5f * subSize.z;
            subBnd = new Bounds(subCenter, subSize);
            m_children[3] = new DetailQuadTreeNode(subDepth, subBnd, worldBounds);
        }

        /// <summary>
        /// 剔除4叉树
        /// </summary>
        public void CullQuadtree(Plane[] cullPlanes, MTArray<int> visible)
        {
            if (GeometryUtility.TestPlanesAABB(cullPlanes, m_bnd))
            {
                if (m_children == null)
                {
                    visible.Add(m_patchId);
                }
                else
                {
                    foreach (var child in m_children)
                    {
                        child.CullQuadtree(cullPlanes, visible);
                    }
                }
            }
        }
        
    }
}