// Created By: WangYu  Date: 2022-10-10

/*

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节 Patch
    /// </summary>
    public class DetailPatch : AbsDetailPatch
    {
        private bool m_buildDone = false;
        public override bool IsBuildDone => m_buildDone;

        private TerrainMeshConfig m_tmConfig;

        public DetailPatch(int densityX, int densityZ, int patchX, int patchZ, 
            Vector3 pos, bool receiveShadow,
            TerrainMeshConfig tmConfig, int[] patchDataOffsets, NativeArray<byte> densityData)
            : base(densityX, densityZ, pos)
        {
            m_tmConfig = tmConfig;
            
            var combinedLayers = new Dictionary<int, List<int>>();
            for (int l = 0; l < m_tmConfig.detailPrototypes.Length; l++)
            {
                int index = l * patchX * patchZ + m_densityZ * patchX + m_densityX;
                int dataOffset = patchDataOffsets[index];
                if (dataOffset < 0)
                {
                    continue;
                }
                
                DetailLayerData layerData = m_tmConfig.detailPrototypes[l];
                int instanceId = layerData.prototype.GetInstanceID();
                
                if (!combinedLayers.ContainsKey(instanceId))
                {
                    combinedLayers.Add(instanceId, new List<int>());
                }
                combinedLayers[instanceId].Add(l);
            }

            m_layers = new AbsDetailPatchLayer[combinedLayers.Count];
            
            int layerCounter = 0;
            using var combinedLayerIter = combinedLayers.GetEnumerator();
            while (combinedLayerIter.MoveNext())
            {
                var layerIds = combinedLayerIter.Current.Value;
                var combineCount = layerIds.Count;
                
                var job = new DetailLayerCreateJob();
                job.densityData = densityData;
                job.densityX = m_densityX;
                job.densityZ = m_densityZ;
                job.posParam = pos;
                job.detailResolutionPerPatch = m_tmConfig.detailResolutionPerPatch;
                job.localScale = Vector3.one;
                job.detailMaxDensity = 0;
                //输入
                job.noiseSeed = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.dataOffset = new NativeArray<int>(combineCount, Allocator.Persistent);
                //属性定义
                job.minWidth = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.maxWidth = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.minHeight = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.maxHeight = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.noiseSpread = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.healthyColor = new NativeArray<float4>(combineCount, Allocator.Persistent);
                job.dryColor = new NativeArray<float4>(combineCount, Allocator.Persistent);
                
                DetailLayerData layerData = null;
                for (int i = 0; i < layerIds.Count; i++)
                {
                    var l = layerIds[i];
                    layerData = m_tmConfig.detailPrototypes[l];
                    
                    job.localScale = layerData.prototype.transform.localScale;
                    job.detailMaxDensity = Mathf.Min(byte.MaxValue, job.detailMaxDensity + layerData.maxDensity);
                    job.dataOffset[i] = patchDataOffsets[l * patchX * patchZ + m_densityZ * patchX + m_densityX];
                    job.noiseSeed[i] = (float)l / m_tmConfig.detailPrototypes.Length;
                    //属性定义
                    job.minWidth[i] = layerData.minWidth;
                    job.maxWidth[i] = layerData.maxWidth;
                    job.minHeight[i] = layerData.minHeight;
                    job.maxHeight[i] = layerData.maxHeight;
                    job.noiseSpread[i] = layerData.noiseSpread;
                    job.healthyColor[i] = new float4(layerData.healthyColor.r, layerData.healthyColor.g, layerData.healthyColor.b, layerData.healthyColor.a);
                    job.dryColor[i] = new float4(layerData.dryColor.r, layerData.dryColor.g, layerData.dryColor.b, layerData.dryColor.a);
                }

                m_layers[layerCounter] = new DetailPatchLayer(layerData, receiveShadow, job);
                layerCounter++;
            }
        }
        
        
        public override void PushData()
        {
            base.PushData();
            m_buildDone = false;
        }

        public override void Clear()
        {
            m_buildDone = false;
            foreach (var l in m_layers)
            {
                l.Clear();
            }
        }
        
        public override void Activate()
        {
            bool rebuild = !m_buildDone;
            for (int l = 0; l < m_layers.Length; l++)
            {
                m_layers[l].OnActivate(rebuild);
            }
        }

        public override void TickBuild()
        {
            if (m_buildDone)
            {
                return;
            }
            m_buildDone = true;
            
            foreach (var l in m_layers)
            {
                l.TickBuild();
                if (!l.IsSpawnDone)
                {
                    m_buildDone = false;
                    break;
                }
            }
        }
        
    }
}

*/