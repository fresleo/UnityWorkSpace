using System.Collections.Generic;
using System.Linq;
using AirSticker.Runtime.Core;
using UnityEngine;

namespace AirSticker.Runtime.Logic
{
    /// <summary>
    /// 接收对象的 ConvexPolygonInfo 池
    /// ConvexPolygonInfo 在池中注册，并将接收器对象作为键。
    /// </summary>
    public sealed class ReceiverObjectTrianglePolygonsPool
    {
        private readonly Dictionary<GameObject, List<ConvexPolygonInfo>> _trianglePolygonsPool = new();

        /// <summary>
        /// 池
        /// </summary>
        public IReadOnlyDictionary<GameObject, List<ConvexPolygonInfo>> TrianglePolygonsPool => _trianglePolygonsPool;

        public void Dispose()
        {
            foreach (var iter in _trianglePolygonsPool)
            {
                iter.Value?.Clear();
            }
            _trianglePolygonsPool.Clear();
        }
        
        public void RegisterTrianglePolygons(GameObject receiverObject, List<ConvexPolygonInfo> trianglePolygonInfos)
        {
            if (receiverObject && !this.Contains(receiverObject))
            {
                _trianglePolygonsPool.Add(receiverObject, trianglePolygonInfos);
            }
        }
        
        public int GetPoolSize()
        {
            return _trianglePolygonsPool.Count;
        }
        
        public bool Contains(GameObject receiverObject)
        {
            return _trianglePolygonsPool.ContainsKey(receiverObject);
        }
        
        /// <summary>
        /// 垃圾收集
        /// </summary>
        public void GarbageCollect()
        {
            // 如果注册的接收方对象已失效，则会将其从池中删除。
            var deleteList = _trianglePolygonsPool.Where(item => item.Key == null).ToList();
            foreach (var item in deleteList)
            {
                _trianglePolygonsPool.Remove(item.Key);
            }
        }
        
    }
}