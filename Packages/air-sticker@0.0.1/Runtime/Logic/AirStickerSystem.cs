using System;
using System.Collections;
using System.Collections.Generic;
using AirSticker.Runtime.Core;
using UnityEngine;

namespace AirSticker.Runtime.Logic
{
    /// <summary>
    /// 贴花系统
    /// </summary>
    public sealed partial class AirStickerSystem : MonoBehaviour
    {
        private static AirStickerSystem s_instance;
        
        private readonly DecalMeshPool m_decalMeshPool = new();
        private readonly ReceiverObjectTrianglePolygonsPool m_receiverObjectTrianglePolygonsPool = new();
        private readonly DecalProjectorLauncher m_decalProjectorLauncher = new();
        
        /// <summary>
        /// DecalMesh 池
        /// </summary>
        public static DecalMeshPool DecalMeshPool
        {
            get
            {
                if (!s_instance) return null;
                return s_instance.m_decalMeshPool;
            }
        }

        /// <summary>
        /// 每个接收对象的多边形数组池
        /// </summary>
        public static ReceiverObjectTrianglePolygonsPool ReceiverObjectTrianglePolygonsPool
        {
            get
            {
                if (!s_instance) return null;
                return s_instance.m_receiverObjectTrianglePolygonsPool;
            }
        }
        
        /// <summary>
        /// 投影器启动器
        /// 管理所有投影器的启动请求
        /// </summary>
        public static DecalProjectorLauncher DecalProjectorLauncher
        {
            get
            {
                if (!s_instance) return null;
                return s_instance.m_decalProjectorLauncher;
            }
        }
        
        // 三角面多边形工厂
        private TrianglePolygonsFactory m_trianglePolygonsFactory = new();

        /// <summary>
        /// 确保系统存在
        /// </summary>
        public static void EnsureSystem()
        {
            if (s_instance) return;

            var newGo = new GameObject(nameof(AirStickerSystem));
            newGo.AddComponent<AirStickerSystem>();
        }

        
        private void Awake()
        {
            Debug.Assert(s_instance == null, $"{nameof(AirStickerSystem)} 不能被多次实例化，但是它已经被实例化了。");
            s_instance = this;
        }
        
        private void OnDestroy()
        {
            s_instance = null;
            m_trianglePolygonsFactory.Dispose();
            
            m_decalMeshPool.Dispose();
            m_receiverObjectTrianglePolygonsPool.Dispose();
            m_decalProjectorLauncher.Dispose();
        }

        private void Update()
        {
            if(!s_instance) return;
            
            m_receiverObjectTrianglePolygonsPool.GarbageCollect();
            m_decalMeshPool.GarbageCollect();
            m_decalProjectorLauncher.Update();
        }
        
        
        /// <summary>
        /// 从接收对象构建三角形多边形
        /// </summary>
        internal static IEnumerator BuildTrianglePolygonsFromReceiverObject(
            MeshFilter[] meshFilters, MeshRenderer[] meshRenderers,
            SkinnedMeshRenderer[] skinnedMeshRenderers,
            Terrain[] terrains,
            List<ConvexPolygonInfo> convexPolygonInfos)
        {
            if (!s_instance) yield break;
            
            yield return s_instance.m_trianglePolygonsFactory.BuildFromReceiverObject(
                meshFilters, meshRenderers,
                skinnedMeshRenderers,
                terrains,
                convexPolygonInfos);
        }

        /// <summary>
        /// 获取接收对象的三角面多边形信息
        /// </summary>
        internal static List<ConvexPolygonInfo> GetTrianglePolygonsFromPool(GameObject receiverObject)
        {
            if (!s_instance) return null;

            var convexPolygonInfos = s_instance.m_receiverObjectTrianglePolygonsPool.TrianglePolygonsPool[receiverObject];
            foreach (var info in convexPolygonInfos)
            {
                info.IsOutsideClipSpace = false;
            }
            return convexPolygonInfos;
        }
        
    }
}
