// Created By: WangYu  Date: 2025-03-12

using System;
using System.Reflection;
using UnityEngine;

namespace XKT.TOD
{
    public class RGUIContent
    {
        private static Type s_type = typeof(GUIContent);

        public static GUIContent Temp(string t)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            
            MethodInfo mi = s_type.GetMethod("Temp", flags,
                null, 
                new Type[] { typeof(string) }, 
                null);
            if (mi == null) return null;
            
            return (GUIContent)mi.Invoke(null, new object[] { t });
        }
        
    }
}