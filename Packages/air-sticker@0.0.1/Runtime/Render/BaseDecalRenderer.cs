// Created By: WangYu  Date: 2025-02-21

using System;
using System.Collections.Generic;
using AirSticker.Runtime.Logic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AirSticker.Runtime.Render
{
    /// <summary>
    /// 基础贴花网格渲染器
    /// 包含生命周期逻辑，但是不包含显示逻辑
    /// </summary>
    public class BaseDecalRenderer : AbsDecalRenderer
    {
        /// <summary>
        /// 克隆出来的材质球
        /// </summary>
        protected Material m_cloneMaterial;
        
        // 贴花等待执行的回调
        protected Dictionary<long, Action<long>> m_waitForCallbacks = new();

        // 任务数据
        protected List<RendererTaskData> m_taskDatas = new();
        // 运行中的任务
        protected List<RendererTask> m_runningTasks = new();
        
        // 已完成的任务数据
        private List<RendererTaskData> m_completedTaskDatas = new();
        
        private static readonly int _AlphaControl = Shader.PropertyToID("_AlphaControl");
        
        
        public override void SetDisplayResource(Material cloneMaterial, Mesh mesh)
        {
            if (cloneMaterial == null || mesh == null)
            {
                Debug.LogError($"{nameof(Material)} 或 {nameof(Mesh)} 为空： {nameof(BaseDecalRenderer)}");
                return;
            }

            m_cloneMaterial = cloneMaterial;
            
            if (m_meshRenderer != null)
            {
                m_meshRenderer.material = m_cloneMaterial;
                m_meshFilter.mesh = mesh;
            }
            else if (m_skinnedMeshRenderer != null)
            {
                m_skinnedMeshRenderer.material = m_cloneMaterial;
                m_skinnedMeshRenderer.sharedMesh = mesh;
            }
            
            RenderUtil.SetFloat(m_cloneMaterial, _AlphaControl, 0);
        }

        public override void CreateLifecycle(long uniqueKey, AbsDecalConfig lifeConfig, Action<long> callback)
        {
            base.CreateLifecycle(uniqueKey, lifeConfig, callback);
            
            var realLifeConfig = m_lifeConfig as BaseDecalConfig;
            if (realLifeConfig == null)
            {
                return;
            }
            
            m_waitForCallbacks.Add(uniqueKey, callback);

            var newTaskData = new RendererTaskData
            {
                uniqueKey = uniqueKey,
                
                fadeinTime = realLifeConfig.fadeinTime,
                fadeinCurve = realLifeConfig.fadeinCurve,
                
                duration = realLifeConfig.duration,
                
                fadeoutTime = realLifeConfig.fadeoutTime,
                fadeoutCurve = realLifeConfig.fadeoutCurve,
            };
            m_taskDatas.Add(newTaskData);

            var newTask = new RendererTask
            {
                uniqueKey = uniqueKey,
            };
            m_runningTasks.Add(newTask);
        }

        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (m_cloneMaterial != null)
            {
                UnityObject.Destroy(m_cloneMaterial);
                m_cloneMaterial = null;
            }
            
            m_waitForCallbacks.Clear();
            m_taskDatas.Clear();
            m_runningTasks.Clear();
            m_completedTaskDatas.Clear();
        }

        protected virtual void Update()
        {
            float deltaTime = Time.deltaTime;
            
            int taskTotal = m_runningTasks.Count;
            if (taskTotal > 0)
            {
                UpdateLife(deltaTime);
            }
            else
            {
                ReleaseRendering();
            }
        }

        private void UpdateLife(float deltaTime)
        {
            int taskCount = m_taskDatas.Count;
            for (int i = 0; i < taskCount; i++)
            {
                var data = m_taskDatas[i];
                
                var task = m_runningTasks[i];
                
                // 淡入
                if (task.fadeinTimer < data.fadeinTime)
                {
                    float et = Mathf.Clamp01(task.fadeinTimer / data.fadeinTime);
                    float progress = data.fadeinCurve.Evaluate(et);
                    OnFadein(progress);
                    
                    task.fadeinTimer += deltaTime;
                }
                // 永生
                else if(data.duration <= 0)
                {
                    continue;
                }
                // 生存
                else if (task.durationTimer < data.duration)
                {
                    float progress = Mathf.Clamp01(task.durationTimer / data.duration);
                    OnDuration(progress);
                    
                    task.durationTimer += deltaTime;
                }
                // 淡出
                else if (task.fadeoutTimer < data.fadeoutTime)
                {
                    float et = Mathf.Clamp01(task.fadeoutTimer / data.fadeoutTime);
                    float progress = data.fadeoutCurve.Evaluate(et);
                    OnFadeout(progress);
                    
                    task.fadeoutTimer += deltaTime;
                }
                // 生命周期结束，开始回收
                else
                {
                    // 添加进回收队列
                    m_completedTaskDatas.Add(data);
                    // 触发回调
                    if (m_waitForCallbacks.TryGetValue(task.uniqueKey, out var callback))
                    {
                        callback?.Invoke(task.uniqueKey);
                    }
                }
            }

            // 移除已完成的数据
            foreach (var data in m_completedTaskDatas)
            {
                m_waitForCallbacks.Remove(data.uniqueKey);
                
                m_taskDatas.Remove(data);

                var task = m_runningTasks.Find(item => item.uniqueKey == data.uniqueKey);
                m_runningTasks.Remove(task);
            }
            m_completedTaskDatas.Clear();
        }

        protected virtual void OnFadein(float progress)
        {
            RenderUtil.SetFloat(m_cloneMaterial, _AlphaControl, progress);
        }
        
        protected virtual void OnDuration(float progress)
        {
            RenderUtil.SetFloat(m_cloneMaterial, _AlphaControl, 1);
        }
        
        protected virtual void OnFadeout(float progress)
        {
            RenderUtil.SetFloat(m_cloneMaterial, _AlphaControl, 1 - progress);
        }
        
    }
}
