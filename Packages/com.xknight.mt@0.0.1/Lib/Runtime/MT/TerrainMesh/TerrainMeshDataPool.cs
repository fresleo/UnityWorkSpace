// Created By: WangYu  Date: 2022-10-15

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Serialize;

namespace com.xknight.mt.Lib.Runtime.MT.TerrainMesh
{
    /// <summary>
    /// 地形网格数据池
    /// </summary>
    internal class TerrainMeshDataPool
    {
        //配置数据
        private TerrainMeshConfig m_tmConfig;
        //网格数据加载器
        private IBytesLoader m_loader;

        //数据流的缓存，因为1个包里有不止1个mesh，所以当包还没解析完时，缓存用。一旦所有数据解析到mesh中，它就没用了，要回收它
        private Dictionary<string, TerrainMeshDataStreamCache> m_streamCaches = new ();
        //已经解析的mesh
        private Dictionary<int, TerrainMeshData> m_parsedMesh = new ();
        
        public TerrainMeshDataPool(TerrainMeshConfig config, IBytesLoader loader)
        {
            m_tmConfig = config;
            m_loader = loader;
        }

        public void Clear()
        {
            foreach (var val in m_streamCaches.Values)
            {
                m_loader?.UnloadAsset(val.Path);
                val.Clear();
            }
            m_streamCaches.Clear();

            foreach (var val in m_parsedMesh.Values)
            {
                val.Clear();
            }
            m_parsedMesh.Clear();
        }

        /// <summary>
        /// 取出网格
        /// </summary>
        public TerrainMeshData PopMesh(int meshId)
        {
            //解析mesh数据
            if (!m_parsedMesh.ContainsKey(meshId) && meshId >= 0)
            {
                TerrainMeshDataStreamCache msc;
                
                //计算静态网格id
                int staticMeshId = meshId / m_tmConfig.meshDataPack * m_tmConfig.meshDataPack;
                var path = $"{m_tmConfig.meshPrefix}_{staticMeshId}";
                //加载到流缓存中
                if (!m_streamCaches.ContainsKey(path) && m_loader != null)
                {
                    var meshBytes = m_loader.LoadAsset(path);
                    msc = new TerrainMeshDataStreamCache(path, meshBytes, m_tmConfig.meshDataPack);
                    m_streamCaches.Add(path, msc);
                }

                //取mesh
                msc = m_streamCaches[path];
                var rm = msc.GetMesh(meshId);
                m_parsedMesh.Add(meshId, rm);

                //回收用完的缓存
                if (msc.RunOutOf)
                {
                    m_streamCaches.Remove(msc.Path);
                    m_loader?.UnloadAsset(msc.Path);
                    msc.Clear();
                }
            }

            //从缓存中取出网格
            if (m_parsedMesh.TryGetValue(meshId, out var mesh))
            {
                return mesh;
            }

            return null;
        }
        
    }
}