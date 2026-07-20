// Created By: WangYu  Date: 2022-10-03

using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainSampling
{
    /// <summary>
    /// 采样的顶点数据
    /// </summary>
    public class SampleVertexData
    {
        const float c_mergeFactor = 0.5f;
        
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;

        /// <summary>
        /// 合并顶点数据
        /// </summary>
        public void Merge(SampleVertexData other)
        {
            position = c_mergeFactor * (position + other.position);
            normal = c_mergeFactor * (normal + other.normal);
            uv = c_mergeFactor * (uv + other.uv);
        }
    }
}