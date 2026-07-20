// Created By: WangYu  Date: 2022-10-10

/*

using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节渲染器
    /// </summary>
    public class DetailRenderer
    {
        private TerrainMeshConfig m_tmConfig;
        private Bounds m_bnd;
        private bool m_receiveShadow;
        
        private int m_patchX;
        private int m_patchZ;
        private AbsDetailPatch[] m_patches;
        
        private Vector3 m_patchParam = Vector3.zero; // x = offset x, y = offset z, z = patch size
        
        private DetailQuadTreeNode m_quadtree;
        
        private MTArray<int> m_currentVisible;
        private MTArray<int> m_activePatches;
        
        private NativeArray<byte> m_densityData;
        private int[] m_patchDataOffsets;
        
        //构建中的patch
        private List<int> m_buildingPatches = new List<int>();
        //可绘制的patch
        private List<int> m_drawablePatches = new List<int>();
        //剔除平面
        private Plane[] m_cullPlanes;
        

        public DetailRenderer(TerrainMeshConfig tmConfig, Bounds bnd, bool receiveShadow)
        {
            m_tmConfig = tmConfig;
            m_bnd = bnd;
            m_receiveShadow = receiveShadow;
            
            m_patchX = Mathf.CeilToInt((float)m_tmConfig.detailWidth / m_tmConfig.detailResolutionPerPatch);
            m_patchZ = Mathf.CeilToInt((float)m_tmConfig.detailHeight / m_tmConfig.detailResolutionPerPatch);
            m_patches = new AbsDetailPatch[m_patchX * m_patchZ];

            float x = m_bnd.min.x;
            float y = m_bnd.min.z;
            float z = Mathf.Max(m_bnd.size.x / m_patchX, m_bnd.size.z / m_patchZ);
            m_patchParam = new Vector3(x, y, z);
            
            int quadtreeDepth = Mathf.FloorToInt(Mathf.Log(Mathf.Max(m_patchX, m_patchZ), 2));
            m_quadtree = new DetailQuadTreeNode(quadtreeDepth, m_bnd, m_bnd);
            
            m_currentVisible = new MTArray<int>(m_patchX * m_patchZ);
            m_activePatches = new MTArray<int>(m_patchX * m_patchZ);
            
            //密度数据
            m_densityData = new NativeArray<byte>(m_tmConfig.detailLayers.bytes, Allocator.Persistent);

            //把数据偏移读出来
            int total = m_patchX * m_patchZ * m_tmConfig.detailPrototypes.Length;
            m_patchDataOffsets = new int[total];
            MemoryStream stream = new MemoryStream(m_tmConfig.detailLayers.bytes);
            for (int i = 0; i < m_patchDataOffsets.Length; i++)
            {
                m_patchDataOffsets[i] = MTStreamUtils.ReadInt(stream);
            }
            stream.Close();

            m_cullPlanes = new Plane[6];
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            if (m_patches != null)
            {
                foreach (var patch in m_patches)
                {
                    patch?.Clear();
                }
                m_patches = null;
            }

            if (m_currentVisible != null)
            {
                m_currentVisible.Clear();
                m_currentVisible = null;
            }
            if (m_activePatches != null)
            {
                m_activePatches.Clear();
                m_activePatches = null;
            }

            m_densityData.Dispose();
            
            DetailPatchDrawParam.Clear();
            
            m_buildingPatches.Clear();
            m_drawablePatches.Clear();

            m_cullPlanes = null;
        }
        
        /// <summary>
        /// 剔除
        /// </summary>
        public void Cull(Matrix4x4 projectionMatrix, Matrix4x4 worldToCameraMatrix)
        {
            //计算视锥平面
            Matrix4x4 worldToProjectionMatrix = projectionMatrix * worldToCameraMatrix;
            GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix, m_cullPlanes);
            
            //筛选出可见的 patch
            m_quadtree.CullQuadtree(m_cullPlanes, m_currentVisible);
            
            //todo new DetailPatch 会产生大量的gc，还得再想办法处理一下
            
            Profiler.BeginSample("ActivePatch");
            //激活 Patch
            for (int i = 0; i < m_currentVisible.Length; i++)
            {
                var pId = m_currentVisible[i];
                if (!m_activePatches.Contains(pId))
                {
                    ActivePatch(pId);
                }
            }
            Profiler.EndSample();

            Profiler.BeginSample("DeactivePatch");
            //停用 Patch
            for (int i = 0; i < m_activePatches.Length; i++)
            {
                var pId = m_activePatches[i];
                if (!m_currentVisible.Contains(pId))
                {
                    DeactivePatch(pId);
                }
            }
            Profiler.EndSample();
            
            //交换+重置
            (m_activePatches, m_currentVisible) = (m_currentVisible, m_activePatches);
            m_currentVisible.Reset();
        }
        
        private void ActivePatch(int pId)
        {
            if (m_patches[pId] == null)
            {
                int densityX = pId % m_patchX;
                int densityZ = pId / m_patchZ;
                m_patches[pId] = CreatePatch(densityX, densityZ);
            }

            var patch = m_patches[pId];
            if (patch != null)
            {
                patch.Activate();
                if (!patch.IsBuildDone)
                {
                    m_buildingPatches.Add(pId);
                }
            }
        }
        
        private AbsDetailPatch CreatePatch(int densityX, int densityZ)
        {
            return new DetailPatch(
                densityX, densityZ, m_patchX, m_patchZ, 
                m_patchParam, m_receiveShadow,
                m_tmConfig, m_patchDataOffsets, m_densityData);
        }

        private void DeactivePatch(int pId)
        {
            var patch = m_patches[pId];
            patch.Deactivate();
            
            if (!patch.IsBuildDone)
            {
                patch.PushData();
                m_buildingPatches.Remove(pId);
            }
        }

        /// <summary>
        /// 更新
        /// </summary>
        public void OnUpdate(Camera drawCamera)
        {
            for (int i = m_buildingPatches.Count - 1; i >= 0; i--)
            {
                var pid = m_buildingPatches[i];
                var patch = m_patches[pid];
                patch.TickBuild();
                if (patch.IsBuildDone)
                {
                    m_buildingPatches.RemoveAt(i);
                    m_drawablePatches.Add(pid);
                }
            }

            for (int i = m_drawablePatches.Count - 1; i >= 0; i--)
            {
                var pid = m_drawablePatches[i];
                var patch = m_patches[pid];
                patch.Draw(drawCamera, out bool invisible);
                if (invisible)
                {
                    patch.PushData();
                    m_drawablePatches.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 绘制调试
        /// </summary>
        public void DrawDebug()
        {
            for (int i = 0; i < m_activePatches.Length; i++)
            {
                var patch = m_patches[m_activePatches[i]];
                patch.DrawDebug();
            }
        }
        
    }
}

*/