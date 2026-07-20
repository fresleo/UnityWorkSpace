// Created By: WangYu  Date: 2025-05-06

using UnityEngine;
using UnityEngine.Events;

namespace AirSticker.Runtime.Logic
{
    partial class AirStickerProjector
    {
        private const string c_ProjectorName = "Air Sticker Projector";
        
        public static AirStickerProjector Create(
            GameObject owner,
            GameObject receiver, 
            AbsDecalConfig mdConfig, int order = 0)
        {
            AirStickerSystem.EnsureSystem();

            var projector = owner.AddComponent<AirStickerProjector>();

            projector.receiverGameObject = receiver;
            projector.childMeshRenderers = null;
            projector.childMeshFilters = null;
            projector.childSkinnedMeshRenderers = null;
            projector.childTerrains = null;
            
            projector.mdConfig = mdConfig;
            projector.onFinishedLaunch = new UnityEvent<EState>();
            projector.order = order;

            return projector;
        }
        
        /// <summary>
        /// 创建投影器的 GameObject 对象
        /// </summary>
        /// <param name="projectorPosition">投影器的位置</param>
        /// <param name="projectorRotation">投影器的旋转</param>
        /// <param name="receiver">接收贴花的对象</param>
        /// <param name="mdConfig">网格贴花配置</param>
        /// <param name="order">顺序</param>
        /// <returns>投影器组件</returns>
        public static AirStickerProjector Create(
            Vector3 projectorPosition, Quaternion projectorRotation,
            GameObject receiver, 
            AbsDecalConfig mdConfig, int order = 0)
        {
            var projectorGo = new GameObject(c_ProjectorName);
            projectorGo.transform.position = projectorPosition;
            projectorGo.transform.rotation = projectorRotation;
            projectorGo.transform.localScale = Vector3.one;

            return Create(projectorGo,
                receiver, 
                mdConfig, order);
        }

        public static AirStickerProjector Create(
            GameObject owner,
            GameObject receiver, 
            MeshRenderer[] childMeshRenderers, MeshFilter[] childMeshFilters, SkinnedMeshRenderer[] childSkinnedMeshRenderers, Terrain[] childTerrains,
            AbsDecalConfig mdConfig, int order = 0)
        {
            AirStickerSystem.EnsureSystem();

            var projector = owner.AddComponent<AirStickerProjector>();

            projector.receiverGameObject = receiver;
            projector.childMeshRenderers = childMeshRenderers;
            projector.childMeshFilters = childMeshFilters;
            projector.childSkinnedMeshRenderers = childSkinnedMeshRenderers;
            projector.childTerrains = childTerrains;
            
            projector.mdConfig = mdConfig;
            projector.onFinishedLaunch = new UnityEvent<EState>();
            projector.order = order;

            return projector;
        }
        
        public static AirStickerProjector Create(
            Vector3 projectorPosition, Quaternion projectorRotation,
            GameObject receiver, 
            MeshRenderer[] childMeshRenderers, MeshFilter[] childMeshFilters, SkinnedMeshRenderer[] childSkinnedMeshRenderers, Terrain[] childTerrains,
            AbsDecalConfig mdConfig, int order = 0)
        {
            var projectorGo = new GameObject(c_ProjectorName);
            projectorGo.transform.position = projectorPosition;
            projectorGo.transform.rotation = projectorRotation;
            projectorGo.transform.localScale = Vector3.one;

            return Create(projectorGo,
                receiver, 
                childMeshRenderers, childMeshFilters, childSkinnedMeshRenderers, childTerrains,
                mdConfig, order);
        }
        
    }
}