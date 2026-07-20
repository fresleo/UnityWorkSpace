using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AirSticker.Runtime.Logic
{
    /// <summary>
    /// DecalMesh 的池
    /// </summary>
    public sealed class DecalMeshPool
    {
        private readonly Dictionary<int, DecalMesh> m_decalMeshes = new();
        
        private static StringBuilder s_sb = new();

        /// <summary>
        /// 释放渲染对象
        /// </summary>
        public void Dispose()
        {
            foreach (var iter in m_decalMeshes)
            {
                iter.Value?.Dispose();
            }
            m_decalMeshes.Clear();

            s_sb.Clear();
        }
        
        /// <summary>
        /// 注册 DecalMesh
        /// </summary>
        /// <param name="hash">
        /// 哈希值
        /// 应该通过 CalculateHash 方法计算得到。
        /// </param>
        /// <param name="decalMesh">要注册的 DecalMesh</param>
        public void RegisterDecalMesh(int hash, DecalMesh decalMesh)
        {
            m_decalMeshes.Add(hash, decalMesh);
        }
        
        public int GetPoolSize()
        {
            return m_decalMeshes.Count;
        }
        
        public bool Contains(int hash)
        {
            return m_decalMeshes.ContainsKey(hash);
        }
        
        public bool TryGetDecalMesh(int hash, out DecalMesh decalMesh)
        {
            return m_decalMeshes.TryGetValue(hash, out decalMesh);
        }
        
        /// <summary>
        /// 垃圾收集
        /// </summary>
        public void GarbageCollect()
        {
            var removeList = m_decalMeshes.Where(item => item.Value.CanRemoveFromPool()).ToList();
            foreach (var item in removeList)
            {
                item.Value.Dispose();
                m_decalMeshes.Remove(item.Key);
            }
        }
        
        /// <summary>
        /// 计算要注册到池中的哈希值
        /// </summary>
        public static int CalculateHash(GameObject receiverObject, Material decalMaterial, Component component)
        {
            int hash1 = receiverObject.GetInstanceID();
            int hash2 = decalMaterial.GetInstanceID();
            int hash3 = component.GetInstanceID();
            
            unchecked // 允许整数溢出
            {
                int hash = 17; // 起始质数
                hash = hash * 31 + hash1;
                hash = hash * 31 + hash2;
                hash = hash * 31 + hash3;
                return hash;
            }
        }
        
    }
}
