// Created By: WangYu  Date: 2025-04-01

using System.Collections.Generic;
using UnityEngine;
using XKT.TOD.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XKT.TOD.Lightmap
{
    public static class ColliderExt
    {
        #if UNITY_EDITOR
        
        /// <summary>
        /// 查找碰撞器内的所有贡献 GI 的渲染器
        /// </summary>
        public static void FindRenderersInsideColliderWithGI(this Collider targetCollider, List<MeshRenderer> rendererList)
        {
            if (rendererList == null)
            {
                rendererList = new List<MeshRenderer>();
            }
            
            Bounds colliderBounds = targetCollider.bounds;
            
            var allRenderers = TODUtils.FindObjectsOfTypeInActiveScene<MeshRenderer>();
            foreach (MeshRenderer renderer in allRenderers)
            {
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                
                // 检查 Renderer 的边界是否与 Collider 的边界相交
                bool b1 = colliderBounds.Intersects(renderer.bounds);
                // 是否开了 GI
                bool b2 = (flags & StaticEditorFlags.ContributeGI) != 0;
                if (b1 && b2)
                {
                    rendererList.Add(renderer);
                }
            }
        }
        
        #endif // UNITY_EDITOR
        
    }
}