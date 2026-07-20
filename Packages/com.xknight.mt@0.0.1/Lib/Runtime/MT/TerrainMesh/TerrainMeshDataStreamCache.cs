// Created By: WangYu  Date: 2022-10-16

using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Utils;

namespace com.xknight.mt.Lib.Runtime.MT.TerrainMesh
{
    /// <summary>
    /// 地形网格数据流的缓存
    /// </summary>
    internal class TerrainMeshDataStreamCache
    {
        private string m_path;
        private MemoryStream m_memStream;
        private int[] m_offsets;
        
        private int m_usedCount = 0;

        /// <summary>
        /// 路径
        /// </summary>
        public string Path => m_path;

        /// <summary>
        /// 耗尽
        /// </summary>
        public bool RunOutOf => m_offsets == null || m_offsets.Length == m_usedCount;

        public TerrainMeshDataStreamCache(string path, byte[] data, int pack)
        {
            m_path = path;
            m_memStream = new MemoryStream(data);
            m_offsets = new int[pack];
            for (int i = 0; i < pack; i++)
            {
                m_offsets[i] = MTStreamUtils.ReadInt(m_memStream);
            }
        }

        public void Clear()
        {
            m_memStream.Close();
        }

        public TerrainMeshData GetMesh(int meshId)
        {
            int stride = meshId % m_offsets.Length;
            int offset = m_offsets[stride];
            m_memStream.Position = offset;

            var tmd = new TerrainMeshData();
            MTMeshUtils.Deserialize(m_memStream, tmd);

            m_usedCount++;
            return tmd;
        }
        
    }
}