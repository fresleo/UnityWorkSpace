// Created By: WangYu  Date: 2025-03-11

using System;
using UnityEditor;
using UnityEngine;

namespace XKT.TOD
{
    public static class RLightingExplorerTab
    {
        private static Type s_type = typeof(LightingExplorerTab);
        
        public static void ROnDisable(this LightingExplorerTab instance)
        {
            ReflectionUtils.CallMethodWithVoid(s_type, instance, "OnDisable");
        }

        public static void ROnInspectorUpdate(this LightingExplorerTab instance)
        {
            ReflectionUtils.CallMethodWithVoid(s_type, instance, "OnInspectorUpdate");
        }

        public static void ROnSelectionChange(this LightingExplorerTab instance)
        {
            ReflectionUtils.CallMethodWithVoid(s_type, instance, "OnSelectionChange");
        }

        public static void ROnSelectionChange(this LightingExplorerTab instance, int[] instanceIDs)
        {
            ReflectionUtils.CallMethodWithVoid(s_type, instance, "OnSelectionChange", 
                new Type[] { typeof(int[]) }, 
                new object[] { instanceIDs });
        }

        public static void ROnHierarchyChange(this LightingExplorerTab instance)
        {
            ReflectionUtils.CallMethodWithVoid(s_type, instance, "OnHierarchyChange");
        }

        public static void ROnGUI(this LightingExplorerTab instance)
        {
            ReflectionUtils.CallMethodWithVoid(s_type, instance, "OnGUI");
        }

        public static GUIContent RGetTitle(this LightingExplorerTab instance)
        {
            return ReflectionUtils.GetPropertyOrField(s_type, instance, "title") as GUIContent;
        }

        public static RSerializedPropertyTable RGetLightTable(this LightingExplorerTab instance)
        {
            var obj = ReflectionUtils.GetPropertyOrField(s_type, instance, "m_LightTable");
            if (obj == null)
            {
                Debug.LogError("没有反射到 m_LightTable");
                return null;
            }
            
            var rObj = new RSerializedPropertyTable(obj);
            return rObj;
        }
        
    }
}