using System.Collections.Generic;

namespace XKAsset
{
	public class LinkedListNodeCache<T>
	{
		int m_NodesCreated = 0;
		LinkedList<T> m_NodeCache;
		/// <summary>
		/// Creates or returns a LinkedListNode of the requested type and set the value.
		/// </summary>
		/// <param name="val">The value to set to returned node to.</param>
		/// <returns>A LinkedListNode with the value set to val.</returns>
		public LinkedListNode<T> Acquire(T val)
		{
			if (m_NodeCache != null)
			{
				var n = m_NodeCache.First;
				if (n != null)
				{
					m_NodeCache.RemoveFirst();
					n.Value = val;
					return n;
				}
			}
			m_NodesCreated++;
			return new LinkedListNode<T>(val);
		}

		/// <summary>
		/// Release the linked list node for later use.
		/// </summary>
		/// <param name="node"></param>
		public void Release(LinkedListNode<T> node)
		{
			if (m_NodeCache == null)
				m_NodeCache = new LinkedList<T>();

			node.Value = default(T);
			m_NodeCache.AddLast(node);
		}

		internal int CreatedNodeCount { get { return m_NodesCreated; } }
		internal int CachedNodeCount { get { return m_NodeCache == null ? 0 : m_NodeCache.Count; } }
	}
	
	internal static class GlobalLinkedListNodeCache<T>
	{
		static LinkedListNodeCache<T> m_globalCache;
		public static LinkedListNode<T> Acquire(T val)
		{
			if (m_globalCache == null)
				m_globalCache = new LinkedListNodeCache<T>();
			return m_globalCache.Acquire(val);
		}

		public static void Release(LinkedListNode<T> node)
		{
			if (m_globalCache == null)
				m_globalCache = new LinkedListNodeCache<T>();
			m_globalCache.Release(node);
		}
	}
}