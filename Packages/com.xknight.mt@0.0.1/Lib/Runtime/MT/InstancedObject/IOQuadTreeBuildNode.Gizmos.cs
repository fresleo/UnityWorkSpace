// Created By: WangYu  Date: 2023-11-30

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.InstancedObject
{
    public partial class IOQuadTreeBuildNode
    {
        public void DrawDebug()
        {
            //只画有数据的叶子节点
            if (childrenNodes.Count == 0)
            {
                //节点范围
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(bnd.center, bnd.size);
            }
            else
            {
                foreach (var child in childrenNodes)
                {
                    child.DrawDebug();
                }
            }
        }
        
    }
}