// Created By: WangYu  Date: 2022-10-05

using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainMesh
{
    /// <summary>
    /// 地形网格的4叉树构建节点
    /// 它会等效的导出为 TMQuadTreeNode 节点
    /// </summary>
    public class TMQuadTreeBuildNode
    {
        const int c_childrenCount = 4;
        
        public Bounds bnd;
        public int meshId = -1;
        public byte lodLv;
        
        public Vector2 uvMin;
        public Vector2 uvMax;
        public TMQuadTreeBuildNode[] childrenNodes;

        public TMQuadTreeBuildNode(
            int depth,
            Vector3 min, Vector3 max,
            Vector2 uvmin, Vector2 uvmax)
        {
            uvMin = uvmin;
            uvMax = uvmax;

            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            Vector2 uvCenter = (uvMin + uvMax) * 0.5f;
            Vector2 uvSize = uvMax - uvMin;
            
            bnd = new Bounds(center, size);

            if (depth > 0)
            {
                childrenNodes = new TMQuadTreeBuildNode[c_childrenCount];
                int nextDepth = depth - 1;

                Vector3 subMin, subMax;
                Vector2 uvSubMin, uvSubMax;
                
                subMin = new Vector3(MTRuntimeUtils.MinusHalf(center.x, size.x), min.y, MTRuntimeUtils.MinusHalf(center.z, size.z));
                subMax = new Vector3(center.x, max.y, center.z);
                uvSubMin = new Vector2(MTRuntimeUtils.MinusHalf(uvCenter.x, uvSize.x), MTRuntimeUtils.MinusHalf(uvCenter.y, uvSize.y));
                uvSubMax = new Vector2(uvCenter.x, uvCenter.y);
                childrenNodes[0] = new TMQuadTreeBuildNode(nextDepth, subMin, subMax, uvSubMin, uvSubMax);

                subMin = new Vector3(center.x, min.y, MTRuntimeUtils.MinusHalf(center.z, size.z));
                subMax = new Vector3(MTRuntimeUtils.AddHalf(center.x, size.x), max.y, center.z);
                uvSubMin = new Vector2(uvCenter.x, MTRuntimeUtils.MinusHalf(uvCenter.y, uvSize.y));
                uvSubMax = new Vector2(MTRuntimeUtils.AddHalf(uvCenter.x, uvSize.x), uvCenter.y);
                childrenNodes[1] = new TMQuadTreeBuildNode(nextDepth, subMin, subMax, uvSubMin, uvSubMax);

                subMin = new Vector3(MTRuntimeUtils.MinusHalf(center.x, size.x), min.y, center.z);
                subMax = new Vector3(center.x, max.y, MTRuntimeUtils.AddHalf(center.z, size.z));
                uvSubMin = new Vector2(MTRuntimeUtils.MinusHalf(uvCenter.x, uvSize.x), uvCenter.y);
                uvSubMax = new Vector2(uvCenter.x, MTRuntimeUtils.AddHalf(uvCenter.y, uvSize.y));
                childrenNodes[2] = new TMQuadTreeBuildNode(nextDepth, subMin, subMax, uvSubMin, uvSubMax);

                subMin = new Vector3(center.x, min.y, center.z);
                subMax = new Vector3(MTRuntimeUtils.AddHalf(center.x, size.x), max.y, MTRuntimeUtils.AddHalf(center.z, size.z));
                uvSubMin = new Vector2(uvCenter.x, uvCenter.y);
                uvSubMax = new Vector2(MTRuntimeUtils.AddHalf(uvCenter.x, uvSize.x), MTRuntimeUtils.AddHalf(uvCenter.y, uvSize.y));
                childrenNodes[3] = new TMQuadTreeBuildNode(nextDepth, subMin, subMax, uvSubMin, uvSubMax);
            }
        }

        
        /// <summary>
        /// 把数据添加到树中
        /// </summary>
        public bool AddMesh(TriangulateMeshData data)
        {
            //填到自己里
            if (bnd.Contains(data.BND.center) && bnd.size.x * 0.5f < data.BND.size.x)
            {
                meshId = data.MeshId;
                lodLv = (byte)data.lodLv;
                data.lods[0].uvMin = uvMin;
                data.lods[0].uvMax = uvMax;
                return true;
            }

            //填到子节点里
            if (childrenNodes != null)
            {
                for (int i = 0; i < c_childrenCount; i++)
                {
                    if (childrenNodes[i].AddMesh(data))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 是否是目标包围盒
        /// </summary>
        public bool IsTargetBounds(int meshId, ref Bounds bnd)
        {
            //自己就是
            if (childrenNodes == null && this.meshId == meshId)
            {
                bnd = this.bnd;
                return true;
            }

            //递归子节点
            if (childrenNodes != null)
            {
                for (int i = 0; i < c_childrenCount; i++)
                {
                    if (childrenNodes[i].IsTargetBounds(meshId, ref bnd))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
    }
}