// Created By: WangYu  Date: 2022-10-02

using System.Collections.Generic;
using com.xknight.mt.Lib.Editor.MT.TerrainSampling;
using com.xknight.mt.Lib.Runtime.MT.TerrainMesh;

namespace com.xknight.mt.Lib.Editor.MT.Jobs
{
    /// <summary>
    /// 产生三角测量数据的工作
    /// </summary>
    public class TriangulateDataJob : TriangulateJob
    {
        /// <summary>
        /// 存储每1级lod的全部树索引
        /// </summary>
        List<int> m_lodLvs = new ();
        /// <summary>
        /// 树列表
        /// </summary>
        List<SamplerTree> m_trees = new ();

        public TriangulateDataJob(UnityTerrainScanner[] scanners, float minTriArea) : base(scanners, minTriArea)
        {
            int totalLen = 0;
            foreach (var lod in base.scanners)
            {
                totalLen += lod.Trees.Length;
                m_lodLvs.Add(totalLen);
                m_trees.AddRange(lod.Trees);
            }

            meshDatas = new TriangulateMeshData[m_trees.Count];
        }
        
        public override void Update()
        {
            if (IsDone)
            {
                return;
            }

            var lodLv = GetLodLv(m_curMeshIdx);
            var tree = m_trees[m_curMeshIdx];
            
            meshDatas[m_curMeshIdx] = new TriangulateMeshData(m_curMeshIdx, tree.BND, lodLv);
            meshDatas[m_curMeshIdx].lods = new TriangulateMeshData.LOD[1];
            
            var lodData = new TriangulateMeshData.LOD();
            RunTriangulate(tree.svdLs, lodData, tree.BND);
            meshDatas[m_curMeshIdx].lods[0] = lodData;
            
            m_curMeshIdx++; //下1个
        }
        
        //根据索引判断所属的lod级别
        private int GetLodLv(int idx)
        {
            for (int i = 0; i < m_lodLvs.Count; i++)
            {
                if (idx < m_lodLvs[i])
                {
                    return i;
                }
            }

            return 0;
        }
        
    }
}