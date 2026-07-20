// Created By: WangYu  Date: 2022-10-25

using System;
using com.xknight.mt.Lib.Runtime.MT.Log;

namespace com.xknight.mt.Lib.Runtime.MT.Utils
{
    /// <summary>
    /// 数组包装
    /// </summary>
    public class MTArray<T>
    {
        private T[] m_datas;
        
        /// <summary>
        /// 可访问的数据长度
        /// </summary>
        public int Length { get; private set; }
        
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="len">容器尺寸</param>
        public MTArray(int len)
        {
            Reallocate(len);
        }

        /// <summary>
        /// 重新分配
        /// </summary>
        public void Reallocate(int len)
        {
            if (m_datas != null && len < m_datas.Length)
            {
                return;
            }
            
            m_datas = new T[len];
            Length = 0;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            m_datas = null;
            Length = 0;
        }

        /// <summary>
        /// 清理
        /// </summary>
        /// <param name="clearFunc">清理方法</param>
        public void Clear(Action<T> clearFunc)
        {
            if (m_datas != null)
            {
                for (int i = 0; i < m_datas.Length; i++)
                {
                    var item = m_datas[i];
                    clearFunc?.Invoke(item);
                }
            }
            
            Clear();
        }
        
        /// <summary>
        /// 重置
        /// 只阻止访问，但不会真的清除数据
        /// </summary>
        public void Reset()
        {
            Length = 0;
        }

        /// <summary>
        /// 增加
        /// </summary>
        public void Add(T item)
        {
            if (m_datas == null || m_datas.Length <= Length)
            {
                MTLogger.LogError($"MTArray<T> 容器异常 : {typeof(T)}");
                return;
            }

            m_datas[Length] = item;
            Length++;
        }

        /// <summary>
        /// 是否包含
        /// </summary>
        public bool Contains(T item)
        {
            for (int i = 0; i < Length; i++)
            {
                if (m_datas[i].Equals(item))
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// 根据索引获取数据
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                {
                    return default;
                }
                
                return m_datas[index];
            }
        }
        
        /// <summary>
        /// 获取数据数组
        /// </summary>
        public T[] Datas => m_datas;
        
    }
}