// Created By: WangYu  Date: 2024-06-06

using System.Collections.Generic;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    /// <summary>
    /// Yield 指令池
    /// </summary>
    public static class YieldInstructionPool
    {
        private static WaitForFixedUpdate s_fixedUpdate;
        /// <summary>
        /// 等待下一个 Fixed 帧
        /// </summary>
        public static WaitForFixedUpdate FixedUpdate
        {
            get
            {
                if (s_fixedUpdate == null)
                {
                    s_fixedUpdate = new WaitForFixedUpdate();
                }
                return s_fixedUpdate;
            }
        }
        
        private static WaitForEndOfFrame s_endOfFrame;
        /// <summary>
        /// 等待帧结束
        /// </summary>
        public static WaitForEndOfFrame EndOfFrame
        {
            get
            {
                if (s_endOfFrame == null)
                {
                    s_endOfFrame = new WaitForEndOfFrame();
                }
                return s_endOfFrame;
            }
        }
        
        /// <summary>
        /// Float 比较器
        /// </summary>
        private class FloatComparer : IEqualityComparer<float>
        {
            bool IEqualityComparer<float>.Equals(float x, float y)
            {
                return Mathf.Approximately(x, y);
            }
            int IEqualityComparer<float>.GetHashCode(float obj)
            {
                return obj.GetHashCode();
            }
        }

        private static Dictionary<float, WaitForSeconds> s_waitForSecondsMap = null;
        private static Dictionary<float, WaitForSeconds> WaitForSecondsMap
        {
            get
            {
                TryInitialize(100);
                return s_waitForSecondsMap;
            }
        }
        
        /// <summary>
        /// 尝试初始化，不强制
        /// </summary>
        /// <param name="capacity">初始化容量</param>
        public static void TryInitialize(int capacity)
        {
            if (s_waitForSecondsMap == null)
            {
                s_waitForSecondsMap = new(capacity, new FloatComparer());
            }
        }
        
        /// <summary>
        /// 清理
        /// </summary>
        public static void Clear()
        {
            s_fixedUpdate = null;
            s_endOfFrame = null;
            WaitForSecondsMap.Clear();
        }
        
        /// <summary>
        /// 获取缓存的 WaitForSeconds
        /// </summary>
        public static WaitForSeconds GetWaitForSeconds(float seconds)
        {
            if (!WaitForSecondsMap.TryGetValue(seconds, out WaitForSeconds wfs))
            {
                WaitForSecondsMap.Add(seconds, wfs = new WaitForSeconds(seconds));
            }

            return wfs;
        }
        
    }
}