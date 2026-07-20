// Created By: WangYu  Date: 2022-10-03

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Log;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainSampling
{
    /// <summary>
    /// 采样器树
    /// </summary>
    public class SamplerTree
    {
        public const byte c_cornerLB = 0;
        public const byte c_cornerLT = 1;
        public const byte c_cornerRT = 2;
        public const byte c_cornerRB = 3;
        
        public const byte c_borderB = 4;
        public const byte c_borderT = 5;
        public const byte c_borderL = 6;
        public const byte c_borderR = 7;
        
        /// <summary>
        /// 顶点列表
        /// </summary>
        public List<SampleVertexData> svdLs = new List<SampleVertexData>();
        /// <summary>
        /// 边界字典
        /// </summary>
        public Dictionary<byte, List<SampleVertexData>> boundaries = new Dictionary<byte, List<SampleVertexData>>();
        /// <summary>
        /// 缝边
        /// </summary>
        public HashSet<byte> stitchedBorders = new HashSet<byte>();

        /// <summary>
        /// 包围盒
        /// </summary>
        public Bounds BND { get; set; }
        /// <summary>
        /// 最小uv
        /// </summary>
        public Vector2 UVMin { get; set; }
        /// <summary>
        /// 最大uv
        /// </summary>
        public Vector2 UVMax { get; set; }
        
        //树根
        private AbsSamplerBase m_node;
        
        public SamplerTree(int sub, Vector3 center, Vector2 size, Vector2 uv, Vector2 uvStep)
        {
            m_node = new SamplerNode(sub, center, size, uv, uvStep);
            UVMin = uv - 0.5f * uvStep;
            UVMax = uv + 0.5f * uvStep;
        }

        //合并树
        private void CombineTree(float angleErr)
        {
            if (m_node is SamplerNode node)
            {
                node.CombineNode(angleErr);
                if (node.IsFullLeaf)
                {
                    SamplerLeaf leaf = node.CombineLeaf(angleErr);
                    if (leaf != null)
                    {
                        m_node = leaf;
                    }
                }
            }
        }

        /// <summary>
        /// 初始化边界
        /// </summary>
        public void InitBoundary()
        {
            for (byte flag = c_cornerLB; flag <= c_borderR; flag++)
            {
                boundaries.Add(flag, new List<SampleVertexData>());
            }
        }
        
        /// <summary>
        /// 添加边界
        /// </summary>
        public void AddBoundary(int subdivision, int segmentsX, int segmentsZ, byte boundaryKey, SampleVertexData svd)
        {
            if (m_node is SamplerNode node)
            {
                node.AddBoundary(subdivision, segmentsX, segmentsZ, boundaryKey, svd);
            }
        }
        
        /// <summary>
        /// 合并边界
        /// </summary>
        public void MergeBoundary(byte flag, float minDis, List<SampleVertexData> src)
        {
            if (!boundaries.ContainsKey(flag))
            {
                MTLogger.LogError("需要合并的边界不存在");
            }
            
            foreach (var item in src)
            {
                boundaries[flag].Add(item);
            }
        }

        /// <summary>
        /// 运行采样
        /// </summary>
        public void RunSampler(ITerrainScanner scaner)
        {
            m_node.RunSample(scaner);
        }

        /// <summary>
        /// 填充数据
        /// </summary>
        public void FillData(float angleErr)
        {
            if (angleErr > 0)
            {
                CombineTree(angleErr);
            }

            m_node.GetData(svdLs, boundaries);
        }

        /// <summary>
        /// 缝合边界
        /// </summary>
        public void StitchBorder(byte flag, byte nextFlag, float minDis, SamplerTree neighbour)
        {
            if (neighbour == null)
            {
                return;
            }
            if (flag <= c_cornerRB || nextFlag <= c_cornerRB)
            {
                return;
            }

            if (!boundaries.ContainsKey(flag))
            {
                MTLogger.LogError($"采样器树边界不包含角 : {flag}");
                return;
            }
            if (!neighbour.boundaries.ContainsKey(nextFlag))
            {
                MTLogger.LogError($"采样器树相邻边界不包含角 : {nextFlag}");
                return;
            }

            //已经缝过了
            if (stitchedBorders.Contains(flag) && neighbour.stitchedBorders.Contains(nextFlag))
            {
                return;
            }
            
            if (boundaries[flag].Count > neighbour.boundaries[nextFlag].Count)
            {
                neighbour.boundaries[nextFlag].Clear();
                neighbour.boundaries[nextFlag].AddRange(boundaries[flag]);
            }
            else
            {
                boundaries[flag].Clear();
                boundaries[flag].AddRange(neighbour.boundaries[nextFlag]);
            }
            
            stitchedBorders.Add(flag);
            neighbour.stitchedBorders.Add(nextFlag);
        }
        
    }
}