// Created By: WangYu  Date: 2022-10-03

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Log;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainSampling
{
    /// <summary>
    /// unity地形的扫描器
    /// </summary>
    public class UnityTerrainScanner : ITerrainScanner
    {
        //细分级别
        private int m_subdivision;
        //坡度角误差
        private float m_slopeAngleErr;
        //轴向上的最大格子数
        private int m_maxX, m_maxZ;
        //1个格子的尺寸
        private Vector2 m_gridSize;
        //1个格子1颗树
        private SamplerTree[] m_trees;
        /// <summary>
        /// 格子树
        /// </summary>
        public SamplerTree[] Trees => m_trees;
        
        //要细分成的段数
        private int m_subdivisionSegments = 1;
        //是否缝边
        private bool m_stitchBorder = true;
        
        private Terrain m_terrain;
        private Bounds m_volumeBound;
        private Vector3 m_checkStart;
        
        private int m_curXIdx = 0;
        private int m_curZIdx = 0;
        
        public UnityTerrainScanner(Terrain terrain, Bounds volumeBound, int subdivision, float slopeAngleErr, int maxX, int maxZ, bool stitchBorder)
        {
            m_terrain = terrain;
            m_volumeBound = volumeBound;

            m_subdivision = Mathf.Max(1, subdivision);
            m_slopeAngleErr = slopeAngleErr;

            m_maxX = maxX;
            m_maxZ = maxZ;
            
            m_subdivisionSegments = (int)Mathf.Pow(2, m_subdivision);
            m_stitchBorder = stitchBorder;

            m_gridSize = new Vector2(m_volumeBound.size.x / m_maxX, m_volumeBound.size.z / m_maxZ);
            m_trees = new SamplerTree[m_maxX * m_maxZ];

            //左上角
            float x = m_volumeBound.center.x - m_volumeBound.size.x * 0.5f;
            float y = m_volumeBound.center.y + m_volumeBound.size.y * 0.5f;
            float z = m_volumeBound.center.z - m_volumeBound.size.z * 0.5f;
            m_checkStart = new Vector3(x, y, z);
        }
        
        void ITerrainScanner.Run(Vector3 center, out Vector3 hitPos, out Vector3 hitNormal)
        {
            hitPos = center;
            
            hitPos.y = m_terrain.SampleHeight(center) + m_terrain.gameObject.transform.position.y;
            
            float fx = (center.x - m_volumeBound.min.x) / m_volumeBound.size.x;
            float fy = (center.z - m_volumeBound.min.z) / m_volumeBound.size.z;
            hitNormal = m_terrain.terrainData.GetInterpolatedNormal(fx, fy);
        }
        
        /// <summary>
        /// 完成
        /// </summary>
        public bool IsDone => m_curXIdx >= m_maxX && m_curZIdx >= m_maxZ;

        /// <summary>
        /// 进度
        /// </summary>
        public float Progress => (float)(m_curXIdx + m_curZIdx * m_maxX) / (m_maxX * m_maxZ);
        
        /// <summary>
        /// 更新
        /// </summary>
        public void Update()
        {
            if (IsDone)
            {
                return;
            }
            
            float fx = (m_curXIdx + 0.5f) * m_gridSize.x;
            float fz = (m_curZIdx + 0.5f) * m_gridSize.y;
            Vector3 center = m_checkStart + fx * Vector3.right + fz * Vector3.forward;
            
            Vector2 uv = new Vector2((m_curXIdx + 0.5f) / m_maxX, (m_curZIdx + 0.5f) / m_maxZ);
            Vector2 uvStep = new Vector2(1f / m_maxX, 1f / m_maxZ);
            
            if (m_trees[m_curXIdx * m_maxZ + m_curZIdx] == null)
            {
                var tree = new SamplerTree(m_subdivision, center, m_gridSize, uv, uvStep);

                Vector3 tc = new Vector3(center.x, center.y, center.z);
                Vector3 ts = new Vector3(m_gridSize.x, m_volumeBound.size.y / 2, m_gridSize.y);
                tree.BND = new Bounds(tc, ts);
                
                m_trees[m_curXIdx * m_maxZ + m_curZIdx] = tree;
            }

            ScanTree(m_trees[m_curXIdx * m_maxZ + m_curZIdx]);
            
            //更新索引
            m_curXIdx++;
            //换行
            if (m_curXIdx >= m_maxX)
            {
                if (m_curZIdx < m_maxZ - 1)
                {
                    m_curXIdx = 0;
                }
                m_curZIdx++;
            }
        }
        
        
        //扫描树
        void ScanTree(SamplerTree sampler)
        {
            sampler.RunSampler(this);
            if (!m_stitchBorder)
            {
                return;
            }
            
            //在地形上的坐标
            float terrainX = m_curXIdx * m_gridSize.x;
            float terrainZ = m_curZIdx * m_gridSize.y;
            //边界偏移
            float borderOffset = 0;
            if (m_curXIdx == 0 || m_curZIdx == 0 || m_curXIdx == m_maxX - 1 || m_curZIdx == m_maxZ - 1)
            {
                borderOffset = 0.000001f;
            }
            //该索引位置的段数
            int segmentsX = m_curXIdx * m_subdivisionSegments;
            int segmentsZ = m_curZIdx * m_subdivisionSegments;
            
            RayCastBoundary(
                terrainX + borderOffset, terrainZ + borderOffset, 
                segmentsX, segmentsZ, 
                SamplerTree.c_cornerLB, sampler);
            RayCastBoundary(
                terrainX + borderOffset, terrainZ + m_gridSize.y - borderOffset, 
                segmentsX, segmentsZ + m_subdivisionSegments - 1, 
                SamplerTree.c_cornerLT, sampler);
            RayCastBoundary(
                terrainX + m_gridSize.x - borderOffset, terrainZ + m_gridSize.y - borderOffset, 
                segmentsX + m_subdivisionSegments - 1, segmentsZ + m_subdivisionSegments - 1, 
                SamplerTree.c_cornerRT, sampler);
            RayCastBoundary(
                terrainX + m_gridSize.x - borderOffset, terrainZ + borderOffset, 
                segmentsX + m_subdivisionSegments - 1, segmentsZ, 
                SamplerTree.c_cornerRB, sampler);
            
            for (int u = 1; u < m_subdivisionSegments; u++)
            {
                float tmpTerrainX = (m_curXIdx + (float)u / m_subdivisionSegments) * m_gridSize.x;
                
                RayCastBoundary(
                    tmpTerrainX, terrainZ + borderOffset, 
                    u + segmentsX, segmentsZ, 
                    SamplerTree.c_borderB, sampler);
                RayCastBoundary(
                    tmpTerrainX, terrainZ + m_gridSize.y - borderOffset, 
                    u + segmentsX, segmentsZ + m_subdivisionSegments - 1, 
                    SamplerTree.c_borderT, sampler);
            }
            
            for (int v = 1; v < m_subdivisionSegments; v++)
            {
                float tmpTerrainZ = (m_curZIdx + (float)v / m_subdivisionSegments) * m_gridSize.y;
                
                RayCastBoundary(
                    terrainX + borderOffset, tmpTerrainZ, 
                    segmentsX, v + segmentsZ, 
                    SamplerTree.c_borderL, sampler);
                RayCastBoundary(
                    terrainX + m_gridSize.x - borderOffset, tmpTerrainZ, 
                    segmentsX + m_subdivisionSegments - 1, v + segmentsZ, 
                    SamplerTree.c_borderR, sampler);
            }
        }

        //射线投射边界
        void RayCastBoundary(float tx, float tz, int sx, int sz, byte boundaryKey, SamplerTree sampler)
        {
            Vector3 hitPos = m_checkStart + tx * Vector3.right + tz * Vector3.forward;
            hitPos.x = Mathf.Clamp(hitPos.x, m_volumeBound.min.x, m_volumeBound.max.x);
            hitPos.z = Mathf.Clamp(hitPos.z, m_volumeBound.min.z, m_volumeBound.max.z);
            hitPos.y = m_terrain.SampleHeight(hitPos) + m_terrain.gameObject.transform.position.y;
            
            float localX = (hitPos.x - m_volumeBound.min.x) / m_volumeBound.size.x;
            float localY = (hitPos.z - m_volumeBound.min.z) / m_volumeBound.size.z;
            var hitNormal = m_terrain.terrainData.GetInterpolatedNormal(localX, localY);

            var svd = new SampleVertexData();
            svd.position = hitPos;
            svd.normal = hitNormal;

            float u = tx / m_maxX / m_gridSize.x;
            float v = tz / m_maxZ / m_gridSize.y;
            svd.uv = new Vector2(u, v);
            
            sampler.AddBoundary(m_subdivision, sx, sz, boundaryKey, svd);
        }

        //获取子树
        SamplerTree GetSubTree(int x, int z)
        {
            if (x < 0 || x >= m_maxX || z < 0 || z >= m_maxZ)
            {
                return null;
            }
            return m_trees[x * m_maxZ + z];
        }

        //平均法线
        private Vector3 AverageNormal(List<SampleVertexData> svdLs)
        {
            Vector3 normal = Vector3.up;
            for (int i = 0; i < svdLs.Count; i++)
            {
                normal += svdLs[i].normal;
            }

            return normal.normalized;
        }

        //合并角
        private void MergeCorners(
            List<SampleVertexData> l0, 
            List<SampleVertexData> l1, 
            List<SampleVertexData> l2,
            List<SampleVertexData> l3)
        {
            var svdLs = new List<SampleVertexData>();
            
            svdLs.Add(l0[0]);
            if (l1 != null)
            {
                svdLs.Add(l1[0]);
            }
            if (l2 != null)
            {
                svdLs.Add(l2[0]);
            }
            if (l3 != null)
            {
                svdLs.Add(l3[0]);
            }
            
            Vector3 normal = AverageNormal(svdLs);
            
            l0[0].normal = normal;
            if (l1 != null)
            {
                l1[0].normal = normal;
            }
            if (l2 != null)
            {
                l2[0].normal = normal;
            }
            if (l3 != null)
            {
                l3[0].normal = normal;
            }
        }

        //缝合角
        void StitchCorner(int x, int z)
        {
            SamplerTree center = GetSubTree(x, z);
            if (!center.boundaries.ContainsKey(SamplerTree.c_cornerLB))
            {
                MTLogger.LogError("边界数据丢失");
                return;
            }

            SamplerTree top = GetSubTree(x, z + 1);
            SamplerTree down = GetSubTree(x, z - 1);
            SamplerTree left = GetSubTree(x - 1, z);
            SamplerTree right = GetSubTree(x + 1, z);
            SamplerTree leftTop = GetSubTree(x - 1, z + 1);
            SamplerTree rightTop = GetSubTree(x + 1, z + 1);
            SamplerTree leftDown = GetSubTree(x - 1, z - 1);
            SamplerTree rightDown = GetSubTree(x + 1, z - 1);
            
            if (!center.stitchedBorders.Contains(SamplerTree.c_cornerLB))
            {
                var l0 = center.boundaries[SamplerTree.c_cornerLB];
                var l1 = left?.boundaries[SamplerTree.c_cornerRB];
                var l2 = leftDown?.boundaries[SamplerTree.c_cornerRT];
                var l3 = down?.boundaries[SamplerTree.c_cornerLT];
                MergeCorners(l0, l1, l2, l3);
                
                center.stitchedBorders.Add(SamplerTree.c_cornerLB);
                if (left != null)
                {
                    left.stitchedBorders.Add(SamplerTree.c_cornerRB);
                }
                if (leftDown != null)
                {
                    leftDown.stitchedBorders.Add(SamplerTree.c_cornerRT);
                }
                if (down != null)
                {
                    left.stitchedBorders.Add(SamplerTree.c_cornerLT);
                }
            }

            if (!center.stitchedBorders.Contains(SamplerTree.c_cornerRB))
            {
                var l0 = center.boundaries[SamplerTree.c_cornerRB];
                var l1 = right?.boundaries[SamplerTree.c_cornerLB];
                var l2 = rightDown?.boundaries[SamplerTree.c_cornerLT];
                var l3 = down?.boundaries[SamplerTree.c_cornerRT];
                MergeCorners(l0, l1, l2, l3);
                
                center.stitchedBorders.Add(SamplerTree.c_cornerRB);
                if (right != null)
                {
                    right.stitchedBorders.Add(SamplerTree.c_cornerLB);
                }
                if (rightDown != null)
                {
                    rightDown.stitchedBorders.Add(SamplerTree.c_cornerLT);
                }
                if (down != null)
                {
                    down.stitchedBorders.Add(SamplerTree.c_cornerRT);
                }
            }

            if (!center.stitchedBorders.Contains(SamplerTree.c_cornerLT))
            {
                var l0 = center.boundaries[SamplerTree.c_cornerLT];
                var l1 = left?.boundaries[SamplerTree.c_cornerRT];
                var l2 = leftTop?.boundaries[SamplerTree.c_cornerRB];
                var l3 = top?.boundaries[SamplerTree.c_cornerLB];
                MergeCorners(l0, l1, l2, l3);
                
                center.stitchedBorders.Add(SamplerTree.c_cornerLT);
                if (left != null)
                {
                    left.stitchedBorders.Add(SamplerTree.c_cornerRT);
                }
                if (leftTop != null)
                {
                    leftTop.stitchedBorders.Add(SamplerTree.c_cornerRB);
                }
                if (top != null)
                {
                    top.stitchedBorders.Add(SamplerTree.c_cornerLB);
                }
            }

            if (!center.stitchedBorders.Contains(SamplerTree.c_cornerRT))
            {
                var l0 = center.boundaries[SamplerTree.c_cornerRT];
                var l1 = right?.boundaries[SamplerTree.c_cornerLT];
                var l2 = rightTop?.boundaries[SamplerTree.c_cornerLB];
                var l3 = top?.boundaries[SamplerTree.c_cornerRB];
                MergeCorners(l0, l1, l2, l3);
                
                center.stitchedBorders.Add(SamplerTree.c_cornerRT);
                if (right != null)
                {
                    right.stitchedBorders.Add(SamplerTree.c_cornerLT);
                }
                if (rightTop != null)
                {
                    rightTop.stitchedBorders.Add(SamplerTree.c_cornerLB);
                }
                if (top != null)
                {
                    top.stitchedBorders.Add(SamplerTree.c_cornerRB);
                }
            }
        }

        /// <summary>
        /// 填充数据
        /// </summary>
        public void FillData()
        {
            for (int i = 0; i < m_trees.Length; i++)
            {
                m_trees[i].FillData(m_slopeAngleErr);
            }
            
            float minDis = Mathf.Min(m_gridSize.x, m_gridSize.y) / m_subdivisionSegments / 2f;
            for (int x = 0; x < m_maxX; x++)
            {
                for (int z = 0; z < m_maxZ; z++)
                {
                    SamplerTree center = GetSubTree(x, z);
                    //缝角
                    StitchCorner(x, z);
                    //缝边
                    center.StitchBorder(SamplerTree.c_borderB, SamplerTree.c_borderT, minDis, GetSubTree(x, z - 1));
                    center.StitchBorder(SamplerTree.c_borderL, SamplerTree.c_borderR, minDis, GetSubTree(x - 1, z));
                    center.StitchBorder(SamplerTree.c_borderR, SamplerTree.c_borderL, minDis, GetSubTree(x + 1, z));
                    center.StitchBorder(SamplerTree.c_borderT, SamplerTree.c_borderB, minDis, GetSubTree(x, z + 1));
                }
            }

            //将边界的顶点和原本的顶点合并，好进行镶嵌
            for (int i = 0; i < m_trees.Length; i++)
            {
                foreach (var svdLs in m_trees[i].boundaries.Values)
                {
                    m_trees[i].svdLs.AddRange(svdLs);
                }
            }
        }
        
    }
}