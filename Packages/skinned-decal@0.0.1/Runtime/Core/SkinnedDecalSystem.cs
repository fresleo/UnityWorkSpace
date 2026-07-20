using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnedDecals
{
    /// <summary>
    /// 蒙皮贴花系统
    /// </summary>
    public class SkinnedDecalSystem : MonoBehaviour
    {
        public const string c_skipTag = "NoSkinnedDecalMesh";
        
        /// <summary>
        /// 找到子节点上的所有蒙皮网格
        /// </summary>
        public bool findAllChildSkinnedMeshes = true;
        /// <summary>
        /// 根据 tag 跳过蒙皮网格渲染器
        /// </summary>
        public bool skipSmrWithTag = true;
        
        /// <summary>
        /// 受控的蒙皮网格渲染器
        /// </summary>
        public SkinnedMeshRenderer[] skinnedMeshes = new SkinnedMeshRenderer[0];
        // 以集合的方式进行存储
        private List<SkinnedMeshRenderer> m_skinnedMeshes = new();
        
        /// <summary>
        /// 总边界
        /// </summary>
        public Bounds totalBounds;
        
        /// <summary>
        /// 每个蒙皮网格渲染器都会搭配1个构建器
        /// </summary>
        public List<SkinnedDecalBuilder> builders = new();
        
        // 完成初始化的标记
        private bool m_initialized = false;

        /// <summary>
        /// 启用多线程
        /// </summary>
        public bool runThreaded = true;
        /// <summary>
        /// 动态 mesh - 如果对 mesh 的修改很频繁，有好处
        /// </summary>
        public bool markDynamic = false;
        /// <summary>
        /// 每个贴花都实例化单独的材质球
        /// </summary>
        public bool instantiateMaterial = false;
        /// <summary>
        /// 闲置等待时间 - 太闲了就继续回收部分资源
        /// </summary>
        public float waitTimeout = 10;

        /// <summary>
        /// 用于收集中心点的近似值
        /// </summary>
        public float usedToCollectTheApproximatelyOfTheCenterPoint = 0.02f;

        
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器测试贴花
        /// </summary>
        public SkinnedDecal editorDecal;
        /// <summary>
        /// 编辑器测试旋转角度
        /// </summary>
        public float editorAngle;
#endif // UNITY_EDITOR


        private void OnDestroy()
        {
            skinnedMeshes = null;
            m_skinnedMeshes.Clear();
            builders.Clear();
            
            m_decalTasks.Clear();
        }

        private void Awake()
        {
            Initialize();
        }

        private void Start() {}

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            // 收集所有蒙皮渲染器
            if (findAllChildSkinnedMeshes)
            {
                var array = GetComponentsInChildren<SkinnedMeshRenderer>();
                
                if (skipSmrWithTag)
                {
                    var tempList = new List<SkinnedMeshRenderer>();
                    foreach (var item in array)
                    {
                        if(item.gameObject.CompareTag(c_skipTag)) continue;
                        tempList.Add(item);
                    }
                    skinnedMeshes = tempList.ToArray();
                }
                else
                {
                    skinnedMeshes = array;
                }
            }
            m_skinnedMeshes.Clear();
            m_skinnedMeshes.AddRange(skinnedMeshes);

            // 计算总边界
            totalBounds = new Bounds(transform.position, Vector3.zero);
            for (int i = 0; i < m_skinnedMeshes.Count; i++)
            {
                totalBounds.Encapsulate(m_skinnedMeshes[i].bounds);
            }

            // 创建构建器
            for (int i = 0; i < m_skinnedMeshes.Count; i++)
            {
                var smr = m_skinnedMeshes[i];
                
                var builder = smr.gameObject.AddComponent<SkinnedDecalBuilder>();
                builder.Initialize(this);
                builders.Add(builder);
            }

            m_initialized = true;
        }
        
        
        #region 对外接口
        
        /// <summary>
        /// 创建贴花 - 根据 up 方向加旋转
        /// </summary>
        /// <param name="decalType">贴花配置</param>
        /// <param name="origin">原点</param>
        /// <param name="direction">正方向</param>
        /// <param name="up">上方向</param>
        public void CreateDecal(SkinnedDecal decalType, Vector3 origin, Vector3 direction, Vector3 up)
        {
            if (!gameObject.activeInHierarchy) return;
            if (decalType == null) return;
            if (!m_initialized)
            {
                Initialize();
            }

            float decalSizeX = decalType.sizeX;
            float decalSizeY = decalType.sizeY;
            byte atlasIndex = decalType.GetAtlasIndex();
            
            // 在每个构建器上创建贴花
            for (int i = 0; i < builders.Count; i++)
            {
                var builder = builders[i];
                
                // 如果没有重叠，则提前退出
                if (builder.Intersects(origin, direction, up, decalSizeX, decalSizeY))
                {
                    builder.TakeSnapshot();
                    long decalUniqueKey = builder.ReadyToCreate(decalType, false);
                    
                    if (runThreaded && Application.isPlaying)
                    {
                        // 添加到任务队列中，等待分线程执行
                        m_decalTasks.Enqueue(
                            new DecalTask(
                                builder, decalUniqueKey, 
                                decalType.normalClip, origin, direction, decalSizeX, decalSizeY,
                                atlasIndex));
                    }
                    else
                    {
                        // 直接创建贴花
                        builder.CreateDecal(
                            decalUniqueKey,
                            decalType.normalClip, origin, direction, decalSizeX, decalSizeY,
                            atlasIndex);
                        builder.UpdateMesh(decalUniqueKey);
                    }
                }
            }
        }
        
        /// <summary>
        /// 创建贴花 - 根据角度加旋转
        /// </summary>
        /// <param name="decalType">贴花配置</param>
        /// <param name="origin">原点</param>
        /// <param name="direction">方向</param>
        /// <param name="angle">旋转角度</param>
        public void CreateDecal(SkinnedDecal decalType, Vector3 origin, Vector3 direction, float angle)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, direction);
            Vector3 up = Vector3.up;
            up = rotation * up;
            
            CreateDecal(decalType, origin, direction, up);
        }

        /// <summary>
        /// 修改显示进度
        /// </summary>
        public void ChangeShowProgress(float progress)
        {
            for (int i = 0; i < builders.Count; i++)
            {
                builders[i].ChangeShowProgress(progress);
            }
        }

        /// <summary>
        /// 评估当前贴花世界空间命中点
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="worldSpaceHitPoints"></param>
        /// <returns></returns>
        public bool EvaluateCurrentDecalWorldSpaceHitPoint(Vector3 origin, ref List<Vector3> worldSpaceHitPoints)
        {
            if (worldSpaceHitPoints == null) worldSpaceHitPoints = new List<Vector3>();
            worldSpaceHitPoints.Clear();
            
            foreach (var builder in this.builders)
            {
                if(builder == null) continue;
                
                bool isValid = builder.EvaluateCurrentDecalWorldSpaceHitPoint(origin, out Vector3 worldSpaceHitPoint);
                if(!isValid) continue;
                
                worldSpaceHitPoints.Add(worldSpaceHitPoint);
            }

            bool hasValid = worldSpaceHitPoints.Count > 0;
            return hasValid;
        }
        
        #endregion //对外接口

        
        #region 多线程

        // 工作中标记
        private bool m_workInProgress = false;
        
        // 贴花任务队列
        private Queue<DecalTask> m_decalTasks = new();
        // 当前任务
        private DecalTask m_currentTask;

        /// <summary>
        /// 贴花任务
        /// </summary>
        private class DecalTask
        {
            public SkinnedDecalBuilder builder;
            public long decalUniqueKey;
            
            public float normalClip;
            public Vector3 origin, direction;
            public float decalSizeX;
            public float decalSizeY;
            public byte atlasIndex;

            public DecalTask(
                SkinnedDecalBuilder builder, long decalUniqueKey, 
                float normalClip, Vector3 origin, Vector3 direction, float decalSizeX, float decalSizeY, 
                byte atlasIndex)
            {
                this.builder = builder;
                this.decalUniqueKey = decalUniqueKey;
                
                this.normalClip = normalClip;
                this.origin = origin;
                this.direction = direction;
                this.decalSizeX = decalSizeX;
                this.decalSizeY = decalSizeY;
                this.atlasIndex = atlasIndex;
            }
        }

        private void LateUpdate()
        {
            // 空闲时才启动
            if (!m_workInProgress && m_decalTasks.Count > 0)
            {
                m_workInProgress = true;
                m_currentTask = m_decalTasks.Dequeue();

                // todo 理论上这里应该改成用 JobSystem 驱动的，对 CPU 会更友好一点，可以后续继续改进
                ThreadPooler.RunOnThread(() =>
                {
                    m_currentTask.builder.CreateDecal(
                        m_currentTask.decalUniqueKey, 
                        m_currentTask.normalClip, m_currentTask.origin, m_currentTask.direction, m_currentTask.decalSizeX, m_currentTask.decalSizeY, 
                        m_currentTask.atlasIndex);

                    ThreadPooler.RunOnMainThread(() =>
                    {
                        m_currentTask.builder.UpdateMesh(m_currentTask.decalUniqueKey);
                        m_workInProgress = false;
                    });
                });
            }
        }

        #endregion // 多线程
        
    }
}