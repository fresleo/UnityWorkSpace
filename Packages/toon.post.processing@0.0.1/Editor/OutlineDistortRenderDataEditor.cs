// Created by: WangYu   Date: 2025-12-15

using System;
using UnityEditor;
using UnityEngine.Rendering;

namespace ToonPostProcessing
{
    [CustomEditor(typeof(OutlineDistortRenderData))]
    public class OutlineDistortRenderDataEditor : Editor
    {
        private OutlineDistortRenderData CurrentTarget => this.target as OutlineDistortRenderData;
        
        private void OnEnable()
        {
            if(!CurrentTarget) return;

            if (!CurrentTarget.renderResources.HasAllLoaded)
            {
                ResourceReloader.ReloadAllNullIn(CurrentTarget, PackageConst.c_packagePath);
            }
        }
    }
}