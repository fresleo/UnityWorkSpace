// Created By: WangYu  Date: 2024-06-01

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.Profiling;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public partial class DynamicOcclusionCuller : MonoBehaviour
    {
        /// <summary>
        /// 测试用 Go
        /// </summary>
        public GameObject testGo;
        
        /// <summary>
        /// box 缩放
        /// >1 可以避免因为平面物体（如墙壁）产生闪烁
        /// </summary>
        public float boxScale = 1.01f;

        /// <summary>
        /// 调试 box
        /// </summary>
        public bool debugBoxes;
        
        /// <summary>
        /// 最大分组数
        /// </summary>
        public int maxGroup = 2;
        
        /// <summary>
        /// 每组最大测试数
        /// </summary>
        public int maxTestPerGroup = 511;

        /// <summary>
        /// 剔除摄像机
        /// </summary>
        public Camera cullCamera;
        
        /// <summary>
        /// 直接可见的距离
        /// </summary>
        public float directVisibilityDistance = 100;
        
        
        // 测试用的材质和网格
        private Material m_testMaterial;
        private Mesh m_testMesh;
        
        // 使用异步GPU回读
        private bool m_useAsyncReadback;
        
        // 初始化数据
        private GpuData[] m_initialData;
        // 当前的数据
        private GpuData[] m_currentData;
        // 要用来测试的世界矩阵
        private Matrix4x4[] m_testWorldMatrices;
        
        // 测试的计算缓冲区
        private ComputeBuffer[] m_testBuffers;
        // 当前缓冲区的索引
        private int m_currentBufferIndex = 0;
        
        // mpb 池
        private IObjectPool<MaterialPropertyBlock> m_mpbPool;
        
        // 测试条目列表
        private List<TestEntry> m_testEntryList = new();
        // 绘制矩阵分组
        private List<Matrix4x4[]> m_drawMatricesGroups = new();
        // 更新动作列表
        private List<UnityAction> m_updateActionList = new();
        
        
        private void OnDestroy()
        {
            EventManager.GetInstance.Clear(); // 清理事件管理器
            
            m_initialData = null;
            m_currentData = null;
            m_testWorldMatrices = null;
            
            // 回收缓冲区
            if (m_testBuffers != null)
            {
                for (int i = 0; i < m_testBuffers.Length; i++)
                {
                    if (m_testBuffers[i] != null)
                    {
                        m_testBuffers[i].Release();
                    }
                }
                m_testBuffers = null;
                m_currentBufferIndex = 0;
            }
            
            if (m_mpbPool != null)
            {
                m_mpbPool.Clear();
                m_mpbPool = null;
            }
            
            m_testEntryList.Clear();
            m_drawMatricesGroups.Clear();
            m_updateActionList.Clear();
        }

        private void Awake()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("需要支持 ComputeShader 才能运行该剔除系统");
                return;
            }

            if (testGo == null)
            {
                Debug.LogError("遮挡对象不能为 null");
                return;
            }

            var mr = testGo.GetComponent<MeshRenderer>();
            var mf = testGo.GetComponent<MeshFilter>();
            if (mr == null || mf == null)
            {
                Debug.LogError("遮挡对象需要有 MeshRenderer 和 MeshFilter 组件来提供资源");
                return;
            }

            m_testMaterial = mr.sharedMaterial;
            m_testMesh = mf.sharedMesh;
            
            // 是否支持异步GPU回读
            m_useAsyncReadback = SystemInfo.supportsAsyncGPUReadback;

            // 总元素数
            int totalElements = maxGroup * maxTestPerGroup;
            
            // 用于初始化缓冲区的数据
            m_initialData = new GpuData[totalElements];
            for (int i = 0; i < m_initialData.Length; i++)
            {
                m_initialData[i] = new GpuData { visible = 0 };
            }
            // 当前返回的数据
            m_currentData = new GpuData[totalElements];
            // 矩阵数组
            m_testWorldMatrices = new Matrix4x4[totalElements];
            
            // 创建 ComputeBuffer
            m_testBuffers = new ComputeBuffer[3];
            for (int i = 0; i < m_testBuffers.Length; i++)
            {
                m_testBuffers[i] = CreateComputeBuffer(totalElements);
            }

            m_mpbPool = new LinkedPool<MaterialPropertyBlock>(CreateMpb, ClearMpb, ClearMpb, ClearMpb);

            // 鱼线初始化绘制矩阵分组
            m_drawMatricesGroups.Clear();
            for (int i = 0; i < maxGroup; i++)
            {
                var array = new Matrix4x4[maxTestPerGroup];
                m_drawMatricesGroups.Add(array);
            }
        }

        private void Update()
        {
            HandleGpuData();
            VisibilityTesting();
            
            UpdateActions();
        }

        
        /// <summary>
        /// 创建 ComputeBuffer
        /// </summary>
        private ComputeBuffer CreateComputeBuffer(int count)
        {
            // int stride = 4;
            int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(GpuData));
            
            var buffer = new ComputeBuffer(count, stride, ComputeBufferType.Default);
            buffer.SetData(m_initialData);
            return buffer;
        }
        
        private MaterialPropertyBlock CreateMpb()
        {
            return new MaterialPropertyBlock();
        }

        private void ClearMpb(MaterialPropertyBlock mpb)
        {
            mpb.Clear();
        }
    }
}