// Created By: WangYu  Date: 2022-10-02

using com.xknight.mt.Lib.Editor.MT.TerrainSampling;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Jobs
{
    /// <summary>
    /// 创建网格的工作
    /// </summary>
    public class CreateMeshJob : IMTJob
    {
        public UnityTerrainScanner[] scanners;
        
        private int m_curLodIdx = 0;

        public CreateMeshJob(Terrain terr, Bounds volumeBound, int maxX, int maxZ, LODSetting[] settings)
        {
            scanners = new UnityTerrainScanner[settings.Length];
            for (int i = 0; i < settings.Length; i++)
            {
                LODSetting item = settings[i];
                
                //lod=0 做缝边，其它的靠提升边缘精细度，来避免看出撕裂
                scanners[i] = new UnityTerrainScanner(terr, volumeBound, item.subdivision, item.slopeAngleError, maxX, maxZ, i == 0);
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
            UnityTerrainScanner lod0 = scanners[0];
            lod0.FillData();
            
            for (int i = 1; i < scanners.Length; i++)
            {
                UnityTerrainScanner item = scanners[i];
                
                for (int t = 0; t < lod0.Trees.Length; t++)
                {
                    SamplerTree t0 = lod0.Trees[t];
                    SamplerTree ti = item.Trees[t];
                    
                    foreach (var b0 in t0.boundaries)
                    {
                        ti.boundaries.Add(b0.Key, b0.Value);
                    }
                }

                item.FillData();
            }
        }
        
    }
}