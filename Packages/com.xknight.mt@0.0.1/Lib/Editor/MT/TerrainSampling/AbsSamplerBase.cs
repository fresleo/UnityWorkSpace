// Created By: WangYu  Date: 2022-10-03

using System.Collections.Generic;

namespace com.xknight.mt.Lib.Editor.MT.TerrainSampling
{
    /// <summary>
    /// 采样器基类
    /// </summary>
    public abstract class AbsSamplerBase
    {
        /// <summary>
        /// 采样顶点数据
        /// </summary>
        protected SampleVertexData m_svd;
        
        /// <summary>
        /// 边界字典
        /// </summary>
        public Dictionary<byte, SampleVertexData> boundaryDict = new ();
        
        /// <summary>
        /// 从节点提取数据
        /// </summary>
        public abstract void GetData(List<SampleVertexData> svdLs, Dictionary<byte, List<SampleVertexData>> boundaries);
        
        /// <summary>
        /// 添加边界
        /// </summary>
        public abstract void AddBoundary(int subdivision, int segmentsX, int segmentsZ, byte boundaryKey, SampleVertexData svd);
        
        /// <summary>
        /// 通过传入的扫描器，来查询树节点在地形上的位置和法线
        /// </summary>
        public virtual void RunSample(ITerrainScanner scaner)
        {
            scaner.Run(m_svd.position, out m_svd.position, out m_svd.normal);
        }
        
    }
}