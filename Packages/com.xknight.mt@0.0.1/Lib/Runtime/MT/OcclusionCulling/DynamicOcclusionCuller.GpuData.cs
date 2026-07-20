// Created By: WangYu  Date: 2024-06-03

using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public partial class DynamicOcclusionCuller
    {
        /// <summary>
        /// 处理 GPU 数据
        /// </summary>
        private void HandleGpuData()
        {
            // 从 GPU 回读结果数据
            int bufferIndex = GetPreviousBufferIndex(m_currentBufferIndex, m_testBuffers.Length);
            ComputeBuffer prevBuffer = m_testBuffers[bufferIndex];
            if (m_useAsyncReadback)
            {
                AsyncGPUReadback.Request(prevBuffer, HandleReadbackRequest);
            }
            else
            {
                prevBuffer.GetData(m_currentData);
                UpdateAllVisibility(m_currentData);
            }
            
            // 重置 Gpu 上的数据
            m_testBuffers[m_currentBufferIndex].SetData(m_initialData);
            Graphics.ClearRandomWriteTargets();
            Graphics.SetRandomWriteTarget(1, m_testBuffers[m_currentBufferIndex]);
            
            m_currentBufferIndex = RollNextIndex(m_currentBufferIndex, m_testBuffers.Length);
        }

        private void HandleReadbackRequest(AsyncGPUReadbackRequest req)
        {
            if (m_testBuffers != null && req.done)
            {
                var currentData = req.GetData<GpuData>();

                // 因为是异步的，所以需要判断程序是否还在运行，脚本是否还在启用
                if (Application.isPlaying && this.enabled)
                {
                    UpdateAllVisibility(currentData);
                }
            }
        }
        
        /// <summary>
        /// 获取上1个缓冲区的索引
        /// </summary>
        private static int GetPreviousBufferIndex(int currentIndex, int total)
        {
            int offset = total - 1;
            int bufferIndex = currentIndex - offset;
            if (bufferIndex < 0)
            {
                bufferIndex = total + bufferIndex;
            }
            
            return bufferIndex;
        }
        
        /// <summary>
        /// 滚动到下1个索引
        /// </summary>
        private static int RollNextIndex(int currentIndex, int total)
        {
            int nextIndex = currentIndex;
            if (total > 0)
            {
                nextIndex = (currentIndex + 1) % total;
            }

            return nextIndex;
        }
        
        /// <summary>
        /// 更新可见性
        /// 在这里开关 Renderer
        /// </summary>
        private void UpdateAllVisibility(GpuData[] datas)
        {
            for (int i = 0; i < m_testEntryList.Count; i++)
            {
                // 超出范围的直接跳过，减少遍历次数
                if (i >= datas.Length)
                {
                    break;
                }
                
                GpuData data = datas[i];
                TestEntry testEntry = m_testEntryList[i];
                
                UpdateVisibility(data, testEntry);
            }
        }

        private void UpdateAllVisibility(NativeArray<GpuData> datas)
        {
            for (int i = 0; i < m_testEntryList.Count; i++)
            {
                // 超出范围的直接跳过，减少遍历次数
                if (i >= datas.Length)
                {
                    break;
                }

                GpuData data = datas[i];
                TestEntry testEntry = m_testEntryList[i];
                
                UpdateVisibility(data, testEntry);
            }
        }

        private void UpdateVisibility(GpuData data, TestEntry testEntry)
        {
            // 计算和剔除相机之间的距离
            Vector3 testPoint = cullCamera.transform.position;
            Vector3 closestPoint = testEntry.bounds.ClosestPoint(testPoint);
            float dis = Vector3.Distance(testPoint, closestPoint);

            int key;
            // gpu 检查可见或在直接可见的范围内，都是可见的
            if (data.visible > 0 || dis <= directVisibilityDistance)
                // if (data.visible > 0)
            {
                key = (int)SDTRendererId.EEventType.Show + testEntry.rid;
            }
            else
            {
                key = (int)SDTRendererId.EEventType.Hide + testEntry.rid;
            }
            EventManager.GetInstance.TriggerEvent(key);
        }
    }
}