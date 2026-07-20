/*******************************************************************
* FileName:     SimpleTimer.cs
* Author:       junwei.li
* Date:         2026/03/16
* Description: 简易定时触发器，支持延迟触发回调、移除触发及销毁管理
*******************************************************************/

using System;
using System.Collections.Generic;

namespace XKT.TOD
{
    /// <summary>
    /// 简易定时触发器
    /// </summary>
    public class SimpleTimer
    {
        /// <summary>
        /// 定时触发项
        /// </summary>
        private struct TriggerItem
        {
            /// <summary>唯一标识</summary>
            public int Id;
            /// <summary>剩余触发时间（秒）</summary>
            public float RemainingTime;
            /// <summary>到期回调</summary>
            public Action Callback;
        }

        /// <summary>当前所有待触发项</summary>
        private List<TriggerItem> _triggers;
        /// <summary>自增ID分配器</summary>
        private int _nextId;
        /// <summary>是否已初始化</summary>
        private bool _initialized;

        /// <summary>
        /// 初始化定时触发器，创建内部数据结构并重置状态
        /// </summary>
        public void InitTimer()
        {
            _triggers = new List<TriggerItem>();
            _nextId = 0;
            _initialized = true;
        }

        /// <summary>
        /// 每帧更新，推进所有定时项的剩余时间，到期则执行回调并移除
        /// </summary>
        /// <param name="deltaTime">本帧耗时（秒）</param>
        public void Update(float deltaTime)
        {
            if (!_initialized || _triggers.Count == 0)
                return;

            for (int i = _triggers.Count - 1; i >= 0; i--)
            {
                var item = _triggers[i];
                item.RemainingTime -= deltaTime;

                if (item.RemainingTime <= 0f)
                {
                    item.Callback?.Invoke();
                    _triggers.RemoveAt(i);
                }
                else
                {
                    _triggers[i] = item;
                }
            }
        }

        /// <summary>
        /// 设置一个延迟触发项
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="callback">到期执行的回调</param>
        /// <returns>触发项唯一ID，可用于移除；未初始化时返回-1</returns>
        public int SetTrigger(float delay, Action callback)
        {
            if (!_initialized)
                return -1;

            int id = _nextId++;
            _triggers.Add(new TriggerItem
            {
                Id = id,
                RemainingTime = delay,
                Callback = callback
            });
            return id;
        }

        /// <summary>
        /// 移除指定ID的未触发定时项
        /// </summary>
        /// <param name="triggerId">SetTrigger返回的唯一ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveTrigger(int triggerId)
        {
            if (!_initialized)
                return false;

            for (int i = 0; i < _triggers.Count; i++)
            {
                if (_triggers[i].Id == triggerId)
                {
                    _triggers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 销毁定时触发器，清空所有未触发项并释放引用
        /// </summary>
        public void Destroy()
        {
            if (!_initialized)
                return;

            _triggers.Clear();
            _triggers = null;
            _initialized = false;
        }
    }
}
