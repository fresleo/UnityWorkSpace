// Created By: WangYu  Date: 2024-05-31

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public partial class SDTQuadTreeBuildNode
    {
        public void DrawDebugGizmos()
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
                    child.DrawDebugGizmos();
                }
            }
        }
        
        public string DebugLabel
        {
            get
            {
                //持有数据的信息
                string label = "";
                for (int i = 0; i < holdIds.Count; i++)
                {
                    if (i > 0)
                    {
                        label += ",";
                    }
                    label += holdIds[i];
                }

                return label;
            }
        }
    }
}