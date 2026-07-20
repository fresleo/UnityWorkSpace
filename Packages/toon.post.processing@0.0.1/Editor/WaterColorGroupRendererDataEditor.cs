// Created by: WangYu   Date: 2025-12-15

using System;
using UnityEditor;
using UnityEngine.Rendering;

namespace ToonPostProcessing
{
    [CustomEditor(typeof(WaterColorGroupRendererData))]
    public class WaterColorGroupRendererDataEditor : Editor
    {
        private WaterColorGroupRendererData CurrentTarget => this.target as WaterColorGroupRendererData;
        
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