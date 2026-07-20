// Created By: WangYu  Date: 2025-03-18

using System;
using UnityEngine;

namespace XKT.TOD
{
    public class RSerializedPropertyTable
    {
        // UnityEditor.SerializedPropertyTable
        
        private static Type s_type;
        
        public static Type GetType()
        {
            if (s_type == null)
            {
                s_type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SerializedPropertyTable");
            }

            return s_type;
        }
        
        
        // 操作实例化对象 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private object m_instance;
        
        public RSerializedPropertyTable(object instance)
        {
            m_instance = instance;
        }
        
        public RSerializedPropertyTreeView RGetTreeView()
        {
            Type type = GetType();
            
            var obj = ReflectionUtils.GetPropertyOrField(type, m_instance, "m_TreeView");
            if (obj == null)
            {
                Debug.LogError("没有反射到 m_TreeView");
                return null;
            }

            var rObj = new RSerializedPropertyTreeView(obj);
            return rObj;
        }
        
    }
}