// Created By: WangYu  Date: 2025-03-12

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKT.TOD
{
    public static class RSupportedRenderingFeatures
    {
        private static Type s_type = typeof(SupportedRenderingFeatures);
        
        public static bool IsLightmapBakeTypeSupported(LightmapBakeType bakeType)
        {
            var flags = BindingFlags.Public | BindingFlags.Static;
            
            MethodInfo mi = s_type.GetMethod("IsLightmapBakeTypeSupported", flags);
            if (mi == null) return false;
            
            return (bool)mi.Invoke(null, new object[] { bakeType });
        }
        
    }
}