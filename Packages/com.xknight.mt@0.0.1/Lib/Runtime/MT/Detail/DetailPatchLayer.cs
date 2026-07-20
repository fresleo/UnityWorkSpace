// Created By: WangYu  Date: 2022-10-10

using com.xknight.mt.Lib.Runtime.MT.DataContainer;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Log;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节 Patch 层
    /// </summary>
    internal class DetailPatchLayer : AbsDetailPatchLayer
    {
        #region 全局计数
        //当前最大的同时job数
        private const int c_max_current_jobcount = 4;
        //运行中的job数
        private static int s_job_running_count = 0;

        //增加1个调度的job
        private static bool AddScheduleJob()
        {
            if (s_job_running_count >= c_max_current_jobcount)
            {
                return false;
            }

            s_job_running_count++;
            return true;
        }

        //job完成了
        private static void JobDone()
        {
            s_job_running_count--;
        }
        #endregion 全局计数

        
        /// <summary>
        /// job状态
        /// </summary>
        private enum EJobState
        {
            Wait,
            Running,
            Done,
        }
        private EJobState m_state = EJobState.Wait;

        /// <summary>
        /// 产生完成
        /// </summary>
        public override bool IsSpawnDone => m_state == EJobState.Done;

        private DetailLayerCreateJob m_job;
        private JobHandle m_jobHandle;

        public DetailPatchLayer(DetailLayerData data, bool receiveShadow, DetailLayerCreateJob job) 
            : base(data, receiveShadow)
        {
            m_job = job;
        }

        public override void Clear()
        {
            base.Clear();
            ClearJob();
        }
        
        public override void OnActivate(bool rebuild)
        {
            base.OnActivate(rebuild);
            
            if (rebuild)
            {
                if (m_state != EJobState.Wait)
                {
                    MTLogger.LogWarning($"MTDetailPatchLayerJob OnActivate 状态不应该是 : {m_state}");
                    return;
                }

                TryScheduleJob();
            }
        }

        public override void OnDeactive()
        {
            base.OnDeactive();
            
            if (m_state == EJobState.Running)
            {
                JobDone();
            }

            m_jobHandle.Complete();
            DisposeJob();
            m_state = EJobState.Wait;
        }

        //清理job
        private void ClearJob()
        {
            if (m_job.noiseSeed.IsCreated)
            {
                m_job.noiseSeed.Dispose();
            }
            if (m_job.dataOffset.IsCreated)
            {
                m_job.dataOffset.Dispose();
            }
            
            if (m_job.minWidth.IsCreated)
            {
                m_job.minWidth.Dispose();
            }
            if (m_job.maxWidth.IsCreated)
            {
                m_job.maxWidth.Dispose();
            }
            if (m_job.minHeight.IsCreated)
            {
                m_job.minHeight.Dispose();
            }
            if (m_job.maxHeight.IsCreated)
            {
                m_job.maxHeight.Dispose();
            }
            if (m_job.noiseSpread.IsCreated)
            {
                m_job.noiseSpread.Dispose();
            }
            if (m_job.healthyColor.IsCreated)
            {
                m_job.healthyColor.Dispose();
            }
            if (m_job.dryColor.IsCreated)
            {
                m_job.dryColor.Dispose();
            }
        }
        
        //销毁job
        private void DisposeJob()
        {
            if (m_job.positions.IsCreated)
            {
                m_job.positions.Dispose();
            }
            if (m_job.scales.IsCreated)
            {
                m_job.scales.Dispose();
            }
            if (m_job.colors.IsCreated)
            {
                m_job.colors.Dispose();
            }
            if (m_job.orientations.IsCreated)
            {
                m_job.orientations.Dispose();
            }
            if (m_job.spawnedCount.IsCreated)
            {
                m_job.spawnedCount.Dispose();
            }
        }
        
        //尝试调度job
        private void TryScheduleJob()
        {
            if (AddScheduleJob())
            {
                m_job.spawnedCount = new NativeArray<int>(1, Allocator.TempJob);
                
                int maxCount = m_job.detailResolutionPerPatch * m_job.detailResolutionPerPatch * m_job.detailMaxDensity;
                m_job.positions = new NativeArray<float3>(maxCount, Allocator.TempJob);
                m_job.scales = new NativeArray<float3>(maxCount, Allocator.TempJob);
                m_job.colors = new NativeArray<float4>(maxCount, Allocator.TempJob);
                m_job.orientations = new NativeArray<float>(maxCount, Allocator.TempJob);
                
                m_jobHandle = m_job.Schedule();
                m_state = EJobState.Running;
            }
        }
        
        /// <summary>
        /// 每帧构建
        /// </summary>
        public override void TickBuild()
        {
            if (m_state == EJobState.Wait)
            {
                TryScheduleJob();
            }
            else if (m_state == EJobState.Running)
            {
                if (m_jobHandle.IsCompleted)
                {
                    JobDone();
                    m_state = EJobState.Done;
                    m_jobHandle.Complete();
                    
                    //拷贝，准备参数
                    m_totalPrototypeCount = m_job.spawnedCount[0];
                    if (m_totalPrototypeCount > 0)
                    {
                        //重新分配绘制参数
                        int batchCount = m_totalPrototypeCount / 1023 + 1;
                        if (m_drawParam == null)
                        {
                            m_drawParam = new MTArray<DetailPatchDrawParam>(batchCount);
                        }
                        m_drawParam.Reallocate(batchCount);
                        
                        for (int batch = 0; batch < batchCount; batch++)
                        {
                            var prototypeCount = Mathf.Min(1023, m_totalPrototypeCount - batch * 1023);
                            
                            var param = DetailPatchDrawParam.Pop();
                            param.Reset(prototypeCount);
                            param.used = prototypeCount;
                            
                            for (int i = 0; i < prototypeCount; i++)
                            {
                                var idxInJob = batch * 1023 + i;
                                Vector3 pos = m_job.positions[idxInJob];
                                
                                //浮在水面上的，需要获取水面的高度
                                MTHeightMap.GetHeightInterpolated(pos, ref pos.y);
                                if (m_layerData.waterFloating)
                                {
                                    pos.y = MTWaterHeight.GetWaterHeight(pos);
                                }

                                Quaternion quat = Quaternion.Euler(0, m_job.orientations[idxInJob], 0);
                                param.matrixs[i] = Matrix4x4.Translate(pos) * Matrix4x4.Scale(m_job.scales[idxInJob]) * Matrix4x4.Rotate(quat);
                                param.colors[i] = m_job.colors[idxInJob];
                            }

                            m_drawParam.Add(param);
                        }

                        OnDrawParamReady();
                    }
                    else
                    {
                        m_drawParam.Reset();
                    }
                    
                    DisposeJob();
                }
            }
        }
        
    }
}