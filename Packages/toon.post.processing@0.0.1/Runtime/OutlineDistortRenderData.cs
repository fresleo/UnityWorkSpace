// Created by: WangYu   Date: 2025-12-15

using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace ToonPostProcessing
{
    public class OutlineDistortRenderData : ScriptableObject
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/卡通后处理/轮廓扭曲渲染数据")]
        private static void CreateAsset()
        {
            PackageUtils.CreateScriptableObject<OutlineDistortRenderData>(nameof(OutlineDistortRenderData));
        }
#endif // UNITY_EDITOR

        [Serializable, ReloadGroup]
        public sealed class RenderResources
        {
            [Reload("Materials/OutlineDistortMask.mat")]
            public Material outlineDistortMaskMat;
            
            [Reload("Materials/OutlineDistortPS.mat")]
            public Material outlineDistortPSMat;

            public bool HasAllLoaded => outlineDistortMaskMat != null && outlineDistortPSMat != null;
        }
        
        public RenderResources renderResources;
        
    }
}