// Created By: WangYu  Date: 2025-05-06

using System.Collections.Generic;
using UnityEngine;

namespace AirSticker.Runtime.Logic
{
    partial class AirStickerSystem
    {
        /// <summary>
        /// 确保为每个组件对象创建1个 DecalMesh 容器做管理
        /// </summary>
        public static void CollectDecalMeshes(
            List<DecalMesh> results, 
            GameObject receiverObject, Material decalMaterial, Component component)
        {
            int hash = DecalMeshPool.CalculateHash(receiverObject, decalMaterial, component);
            var pool = s_instance.m_decalMeshPool;
            
            // 确保每个接收对象上，每种材质只对应有1个 DecalMesh 对象做控制
            if (pool.TryGetDecalMesh(hash, out DecalMesh dm))
            {
                results.Add(dm);
            }
            else
            {
                var newMesh = new DecalMesh(receiverObject, decalMaterial, component);
                results.Add(newMesh);
                pool.RegisterDecalMesh(hash, newMesh);
            }
        }
        
        /// <summary>
        /// 收集属于接收对象的 DecalMesh 容器
        /// 自己从接收器对象上获取子组件
        /// </summary>
        internal static void CollectDecalMeshes(
            List<DecalMesh> results, 
            GameObject receiverObject, Material decalMaterial, 
            out MeshRenderer[] meshRenderers, out MeshFilter[] meshFilters, 
            out SkinnedMeshRenderer[] skinnedMeshRenderers, 
            out Terrain[] terrains)
        {
            meshRenderers = null;
            meshFilters = null;
            skinnedMeshRenderers = null;
            terrains = null;

            if (!s_instance) return;
            
            // 静态网格
            CollectDecalMeshesOfMeshRenderers(
                results, receiverObject, decalMaterial, 
                out meshRenderers, out meshFilters);

            return; // todo: 先不收集蒙皮网格，功能还未测试完善
            
            // 蒙皮网格
            CollectDecalMeshesOfSkinnedMeshRenderers(
                results, receiverObject, decalMaterial, 
                out skinnedMeshRenderers);
            
            // 地形
            CollectDecalMeshesOfTerrains(
                results, receiverObject, decalMaterial, 
                out terrains);
        }
        
        private static readonly List<MeshRenderer> s_tempMRL = new();
        private static readonly List<MeshFilter> s_tempMFL = new();
        private static readonly List<SkinnedMeshRenderer> s_tempSMRL = new();
        
        private static void CollectDecalMeshesOfMeshRenderers(
            List<DecalMesh> results, GameObject receiverObject, Material decalMaterial, 
            out MeshRenderer[] meshRenderers, out MeshFilter[] meshFilters)
        {
            s_tempMRL.Clear();
            s_tempMFL.Clear();
            
            var tempArray = receiverObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var mr in tempArray)
            {
                bool result = CheckDecalMeshOfMeshRenderer(mr, out MeshFilter ml);
                if (!result) continue;
                
                s_tempMRL.Add(mr);
                s_tempMFL.Add(ml);
                
                CollectDecalMeshes(results, receiverObject, decalMaterial, mr);
            }

            meshRenderers = s_tempMRL.ToArray();
            meshFilters = s_tempMFL.ToArray();
            s_tempMRL.Clear();
            s_tempMFL.Clear();
        }

        public static bool CheckDecalMeshOfMeshRenderer(MeshRenderer mr, out MeshFilter ml)
        {
            ml = null;
            
            if (mr.name.Contains(IDecalMeshRenderer.c_rendererName))
            {
                return false;
            }
            
            ml = mr.GetComponent<MeshFilter>();
            if (ml.sharedMesh == null)
            {
                Debug.LogError($"{ml.name} 的 {nameof(MeshFilter)} 上没有挂 Mesh");
                return false;
            }
            
            if (!ml.sharedMesh.isReadable)
            {
                Debug.LogError($"{ml.sharedMesh.name} 的 Mesh 没有开启 Read/Write 权限");
                return false;
            }

            return true;
        }

        private static void CollectDecalMeshesOfSkinnedMeshRenderers(
            List<DecalMesh> results, GameObject receiverObject, Material decalMaterial, 
            out SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            s_tempSMRL.Clear();
            
            var tempArray = receiverObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in tempArray)
            {
                bool result = CheckDecalMeshOfSkinnedMeshRenderer(smr);
                if(!result) continue;
                
                s_tempSMRL.Add(smr);
                
                CollectDecalMeshes(results, receiverObject, decalMaterial, smr);
            }

            skinnedMeshRenderers = s_tempSMRL.ToArray();
            s_tempSMRL.Clear();
        }

        public static bool CheckDecalMeshOfSkinnedMeshRenderer(SkinnedMeshRenderer smr)
        {
            if (smr.sharedMesh == null)
            {
                Debug.LogError($"{smr.name} 的 {nameof(SkinnedMeshRenderer)} 上没有挂 Mesh");
                return false;
            }

            if (!smr.sharedMesh.isReadable)
            {
                Debug.LogError($"{smr.sharedMesh.name} 的 Mesh 没有开启 Read/Write 权限");
                return false;
            }

            return true;
        }

        private static void CollectDecalMeshesOfTerrains(
            List<DecalMesh> results, GameObject receiverObject, Material decalMaterial, 
            out Terrain[] terrains)
        {
            terrains = receiverObject.GetComponentsInChildren<Terrain>();
            foreach (var item in terrains)
            {
                // todo: 地形是否需要开启读写权限？
                
                CollectDecalMeshes(results, receiverObject, decalMaterial, item);
            }
        }
        
    }
}