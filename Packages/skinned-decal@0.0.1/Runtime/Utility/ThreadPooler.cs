using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

namespace SkinnedDecals
{
    /// <summary>
    /// 线程池
    /// </summary>
    public class ThreadPooler : MonoBehaviour
    {
        // 初始化标记
        private static bool s_initialized = false;

        /// <summary>
        /// 总线程数
        /// </summary>
        public static int totalThreads = 8;
        // 线程数
        private static int s_numThreads = 0;
        
        // action 列表
        private List<Action> m_actions = new();
        // 当前 action 列表
        private List<Action> m_currentActions = new();
        
        private static ThreadPooler s_instance;
        /// <summary>
        /// 实例
        /// </summary>
        public static ThreadPooler Instance
        {
            get
            {
                Initialize();
                return s_instance;
            }
        }

        private void OnDestroy()
        {
            s_instance = null;
            s_initialized = false;
        }
        
        private void Awake()
        {
            s_instance = this;
            s_initialized = true;
        }

        private static void Initialize()
        {
            if(s_initialized) return;
            if (!Application.isPlaying) return;

            s_initialized = true;

            var go = new GameObject(nameof(ThreadPooler));
            s_instance = go.AddComponent<ThreadPooler>();
            DontDestroyOnLoad(go);
        }

        /// <summary>
        /// 在主线程上运行
        /// </summary>
        public static void RunOnMainThread(Action action)
        {
            lock (Instance.m_actions)
            {
                Instance.m_actions.Add(action);
            }
        }

        /// <summary>
        /// 运行在线程上
        /// </summary>
        public static void RunOnThread(Action action)
        {
            Initialize();

            while (s_numThreads >= totalThreads)
            {
                Thread.Sleep(1);
            }

            Interlocked.Increment(ref s_numThreads);
            ThreadPool.QueueUserWorkItem(RunAction, action);
        }

        private static void RunAction(object action)
        {
            try
            {
                ((Action)action)();
            }
            catch (Exception ex)
            {
                Debug.LogError($"尝试运行 action 时出现异常: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Interlocked.Decrement(ref s_numThreads);
            }
        }

        private void Update()
        {
            lock (m_actions)
            {
                m_currentActions.Clear();
                m_currentActions.AddRange(m_actions);
                m_actions.Clear();
            }

            for (int i = 0; i < m_currentActions.Count; i++)
            {
                m_currentActions[i]();
            }
        }
        
    }
}