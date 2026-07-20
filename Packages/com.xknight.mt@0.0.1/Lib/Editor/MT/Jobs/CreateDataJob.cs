// Created By: WangYu  Date: 2022-10-02

using com.xknight.mt.Lib.Editor.MT.TerrainSampling;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Jobs
{
    /// <summary>
    /// 创建数据的工作
    /// </summary>
    public class CreateDataJob : IMTJob
    {
        public UnityTerrainScanner[] scanners;
        
        private float m_minEdgeLen;
        private int m_curLodIdx = 0;

        public CreateDataJob(Terrain terr, Bounds volumeBound, int depth, LODSetting[] settings, float minEdgeLen)
        {
            scanners = new UnityTerrainScanner[settings.Length];
            m_minEdgeLen = minEdgeLen;

            int depth_stride = Mathf.Max(1, depth / settings.Length);
            
            for (int i = 0; i < settings.Length; i++)
            {
                LODSetting item = settings[i];
                
                //格子数量逐步递减
                int sub_depth = Mathf.Max(1, depth - i * depth_stride);
                int grid_count = (int)Mathf.Pow(2, sub_depth);
                
                //lod=0 做缝边，其它的靠提升边缘精细度，来避免看出撕裂
                scanners[i] = new UnityTerrainScanner(terr, volumeBound, item.subdivision, item.slopeAngleError, grid_count, grid_count, i == 0);
            }
        }

        public bool IsDone => m_curLodIdx >= scanners.Length;

        public float Progress
        {
            get
            {
                if (m_curLodIdx < scanners.Length)
                {
                    return (m_curLodIdx + scanners[m_curLodIdx].Progress) / scanners.Length;
                }

                return 1;
            }
        }

        public void Update()
        {
            if (IsDone)
            {
                return;
            }

            scanners[m_curLodIdx].Update();
            if (scanners[m_curLodIdx].IsDone)
            {
                m_curLodIdx++;
            }
        }

        public void EndProcess()
        {
            scanners[0].FillData();
            
            for (int i = 1; i < scanners.Length; i++)
            {
                UnityTerrainScanner preScan = scanners[i - 1];
                UnityTerrainScanner curScan = scanners[i];
                
                foreach (var curTree in curScan.Trees)
                {
                    curTree.InitBoundary();
                    
                    //开始收集边界
                    foreach (var preTree in preScan.Trees)
                    {
                        if (curTree.BND.Contains(preTree.BND.center))
                        {
                            AddBoundaryFromPre(curTree, preTree, m_minEdgeLen * 0.5f);
                        }
                    }
                }

                curScan.FillData();
            }
        }

        
        private byte GetBorderType(Bounds cur, Bounds pre)
        {
            const float threshold = 0.01f;
            byte bType = byte.MaxValue;
            
            float l_border = cur.center.x - cur.extents.x;
            float r_border = cur.center.x + cur.extents.x;
            float l_child_border = pre.center.x - pre.extents.x;
            float r_child_border = pre.center.x + pre.extents.x;
            if (Mathf.Abs(l_border - l_child_border) < threshold)
            {
                bType = SamplerTree.c_borderL;
            }
            if (Mathf.Abs(r_border - r_child_border) < threshold)
            {
                bType = SamplerTree.c_borderR;
            }
            
            float b_border = cur.center.z - cur.extents.z;
            float t_border = cur.center.z + cur.extents.z;
            float b_child_border = pre.center.z - pre.extents.z;
            float t_child_border = pre.center.z + pre.extents.z;
            if (Mathf.Abs(t_border - t_child_border) < threshold)
            {
                if (bType == SamplerTree.c_borderL)
                {
                    bType = SamplerTree.c_cornerLT;
                }
                else if (bType == SamplerTree.c_borderR)
                {
                    bType = SamplerTree.c_cornerRT;
                }
                else
                {
                    bType = SamplerTree.c_borderT;
                }
            }
            if (Mathf.Abs(b_border - b_child_border) < threshold)
            {
                if (bType == SamplerTree.c_borderL)
                {
                    bType = SamplerTree.c_cornerLB;
                }
                else if (bType == SamplerTree.c_borderR)
                {
                    bType = SamplerTree.c_cornerRB;
                }
                else
                {
                    bType = SamplerTree.c_borderB;
                }
            }

            return bType;
        }

        private void AddBoundaryFromPre(SamplerTree curTree, SamplerTree preTree, float minDis)
        {
            byte bt = GetBorderType(curTree.BND, preTree.BND);
            switch (bt)
            {
                case SamplerTree.c_borderL:
                    curTree.MergeBoundary(SamplerTree.c_borderL, minDis, preTree.boundaries[SamplerTree.c_cornerLT]);
                    curTree.MergeBoundary(SamplerTree.c_borderL, minDis, preTree.boundaries[SamplerTree.c_borderL]);
                    curTree.MergeBoundary(SamplerTree.c_borderL, minDis, preTree.boundaries[SamplerTree.c_cornerLB]);
                    break;
                case SamplerTree.c_cornerLT:
                    curTree.MergeBoundary(SamplerTree.c_borderT, minDis, preTree.boundaries[SamplerTree.c_borderT]);
                    curTree.MergeBoundary(SamplerTree.c_borderT, minDis, preTree.boundaries[SamplerTree.c_cornerRT]);
                    curTree.MergeBoundary(SamplerTree.c_cornerLT, minDis, preTree.boundaries[SamplerTree.c_cornerLT]);
                    curTree.MergeBoundary(SamplerTree.c_borderL, minDis, preTree.boundaries[SamplerTree.c_borderL]);
                    curTree.MergeBoundary(SamplerTree.c_borderL, minDis, preTree.boundaries[SamplerTree.c_cornerLB]);
                    break;
                case SamplerTree.c_cornerLB:
                    curTree.MergeBoundary(SamplerTree.c_borderB, minDis, preTree.boundaries[SamplerTree.c_borderB]);
                    curTree.MergeBoundary(SamplerTree.c_borderB, minDis, preTree.boundaries[SamplerTree.c_cornerRB]);
                    curTree.MergeBoundary(SamplerTree.c_cornerLB, minDis, preTree.boundaries[SamplerTree.c_cornerLB]);
                    curTree.MergeBoundary(SamplerTree.c_borderL, minDis, preTree.boundaries[SamplerTree.c_borderL]);
                    curTree.MergeBoundary(SamplerTree.c_borderL, minDis, preTree.boundaries[SamplerTree.c_cornerLT]);
                    break;
                case SamplerTree.c_borderB:
                    curTree.MergeBoundary(SamplerTree.c_borderB, minDis, preTree.boundaries[SamplerTree.c_borderB]);
                    curTree.MergeBoundary(SamplerTree.c_borderB, minDis, preTree.boundaries[SamplerTree.c_cornerLB]);
                    curTree.MergeBoundary(SamplerTree.c_borderB, minDis, preTree.boundaries[SamplerTree.c_cornerRB]);
                    break;
                case SamplerTree.c_cornerRB:
                    curTree.MergeBoundary(SamplerTree.c_borderB, minDis, preTree.boundaries[SamplerTree.c_borderB]);
                    curTree.MergeBoundary(SamplerTree.c_borderB, minDis, preTree.boundaries[SamplerTree.c_cornerLB]);
                    curTree.MergeBoundary(SamplerTree.c_cornerRB, minDis, preTree.boundaries[SamplerTree.c_cornerRB]);
                    curTree.MergeBoundary(SamplerTree.c_borderR, minDis, preTree.boundaries[SamplerTree.c_borderR]);
                    curTree.MergeBoundary(SamplerTree.c_borderR, minDis, preTree.boundaries[SamplerTree.c_cornerRT]);
                    break;
                case SamplerTree.c_borderR:
                    curTree.MergeBoundary(SamplerTree.c_borderR, minDis, preTree.boundaries[SamplerTree.c_cornerRT]);
                    curTree.MergeBoundary(SamplerTree.c_borderR, minDis, preTree.boundaries[SamplerTree.c_borderR]);
                    curTree.MergeBoundary(SamplerTree.c_borderR, minDis, preTree.boundaries[SamplerTree.c_cornerRB]);
                    break;
                case SamplerTree.c_cornerRT:
                    curTree.MergeBoundary(SamplerTree.c_borderT, minDis, preTree.boundaries[SamplerTree.c_borderT]);
                    curTree.MergeBoundary(SamplerTree.c_borderT, minDis, preTree.boundaries[SamplerTree.c_cornerLT]);
                    curTree.MergeBoundary(SamplerTree.c_cornerRT, minDis, preTree.boundaries[SamplerTree.c_cornerRT]);
                    curTree.MergeBoundary(SamplerTree.c_borderR, minDis, preTree.boundaries[SamplerTree.c_borderR]);
                    curTree.MergeBoundary(SamplerTree.c_borderR, minDis, preTree.boundaries[SamplerTree.c_cornerRB]);
                    break;
                case SamplerTree.c_borderT:
                    curTree.MergeBoundary(SamplerTree.c_borderT, minDis, preTree.boundaries[SamplerTree.c_cornerRT]);
                    curTree.MergeBoundary(SamplerTree.c_borderT, minDis, preTree.boundaries[SamplerTree.c_borderT]);
                    curTree.MergeBoundary(SamplerTree.c_borderT, minDis, preTree.boundaries[SamplerTree.c_cornerLT]);
                    break;
            }
        }
        
    }
}