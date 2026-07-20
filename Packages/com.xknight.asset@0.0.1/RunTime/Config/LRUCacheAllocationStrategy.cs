using System;
using System.Collections.Generic;

namespace XKAsset
{
	public class LRUCacheAllocationStrategy : IAllocationStrategy
    {
        int m_poolMaxSize;
        int m_poolInitialCapacity;
        int m_poolCacheMaxSize;
        List<List<object>> m_poolCache = new List<List<object>>();
        Dictionary<int, List<object>> m_cache = new Dictionary<int, List<object>>();
        /// <summary>
        /// Create a new LRUAllocationStrategy objct.
        /// </summary>
        /// <param name="poolMaxSize">The max size of each pool.</param>
        /// <param name="poolCapacity">The initial capacity to create each pool list with.</param>
        /// <param name="poolCacheMaxSize">The max size of the internal pool cache.</param>
        /// <param name="initialPoolCacheCapacity">The initial number of pools to create.</param>
        public LRUCacheAllocationStrategy(int poolMaxSize, int poolCapacity, int poolCacheMaxSize, int initialPoolCacheCapacity)
        {
            m_poolMaxSize = poolMaxSize;
            m_poolInitialCapacity = poolCapacity;
            m_poolCacheMaxSize = poolCacheMaxSize;
            for (int i = 0; i < initialPoolCacheCapacity; i++)
                m_poolCache.Add(new List<object>(m_poolInitialCapacity));
        }

        List<object> GetPool()
        {
            int count = m_poolCache.Count;
            if (count == 0)
                return new List<object>(m_poolInitialCapacity);
            var pool = m_poolCache[count - 1];
            m_poolCache.RemoveAt(count - 1);
            return pool;
        }

        void ReleasePool(List<object> pool)
        {
            if (m_poolCache.Count < m_poolCacheMaxSize)
                m_poolCache.Add(pool);
        }

        /// <inheritdoc/>
        public object New(Type type, int typeHash)
        {
            List<object> pool;
            if (m_cache.TryGetValue(typeHash, out pool))
            {
                var count = pool.Count;
                var v = pool[count - 1];
                pool.RemoveAt(count - 1);
                if (count == 1)
                {
                    m_cache.Remove(typeHash);
                    ReleasePool(pool);
                }
                return v;
            }
            return Activator.CreateInstance(type);
        }

        /// <inheritdoc/>
        public void Release(int typeHash, object obj)
        {
            List<object> pool;
            if (!m_cache.TryGetValue(typeHash, out pool))
                m_cache.Add(typeHash, pool = GetPool());
            if (pool.Count < m_poolMaxSize)
                pool.Add(obj);
        }
    }
}