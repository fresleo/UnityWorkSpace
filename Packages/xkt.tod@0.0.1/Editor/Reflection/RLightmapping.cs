// Created By: WangYu  Date: 2025-06-28

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace XKT.TOD
{
    public static class RLightmapping
    {
        private static Type s_type = typeof(Lightmapping);
        
        public static bool BakeReflectionProbeSnapshot(ReflectionProbe reflectionProbe)
        {
            if (reflectionProbe == null) return false;
            
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            
            MethodInfo mi = s_type.GetMethod("BakeReflectionProbeSnapshot", flags);
            if (mi == null) return false;
            
            bool result = (bool)mi.Invoke(null, new object[] { reflectionProbe });
            return result;
        }
        
    }
}