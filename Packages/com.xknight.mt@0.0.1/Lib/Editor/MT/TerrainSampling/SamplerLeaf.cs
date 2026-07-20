// Created By: WangYu  Date: 2022-10-03

using System.Collections.Generic;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainSampling
{
    /// <summary>
    /// 采样器的叶子
    /// </summary>
    public class SamplerLeaf : AbsSamplerBase
    {
        public SamplerLeaf(Vector3 center, Vector2 uv)
        {
            m_svd = new SampleVertexData();
            m_svd.position = center;
            m_svd.uv = uv;
        }

        public SamplerLeaf(SampleVertexData vert)
        {
            m_svd = vert;
        }
        
        public override void GetData(List<SampleVertexData> svdLs, Dictionary<byte, List<SampleVertexData>> boundaries)
        {
            svdLs.Add(m_svd);
            
            foreach (var key in boundaryDict.Keys)
            {
                if (!boundaries.ContainsKey(key))
                {
                    boundaries.Add(key, new List<SampleVertexData>());
                }
                boundaries[key].Add(boundaryDict[key]);
            }
        }

        public override void AddBoundary(int subdivision, int segmentsX, int segmentsZ, byte boundaryKey, SampleVertexData svd)
        {
            boundaryDict.Add(boundaryKey, svd);
        }
        
        /// <summary>
        /// 法线
        /// </summary>
        public Vector3 Normal => m_svd != null ? m_svd.normal : Vector3.up;
        
    }
}