// Created By: WangYu  Date: 2025-03-12

using System;
using System.Reflection;
using UnityEngine;

namespace XKT.TOD
{
    public class RSerializedPropertyTreeView
    {
        // UnityEditor.SerializedPropertyTreeView
        
        private static Type s_type;
        private static Type s_stylesType;
        
        private static FieldInfo s_filterSelection_fi;
        private static FieldInfo s_showInactiveObjects_fi;

        public static Type GetType()
        {
            if (s_type == null)
            {
                s_type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SerializedPropertyTreeView");
            }

            return s_type;
        }
        
        public static Type GetStylesType()
        {
            Type type = GetType();
            
            if (s_stylesType == null)
            {
                s_stylesType = type.GetNestedType("Styles", BindingFlags.Public | BindingFlags.NonPublic);
            }

            return s_stylesType;
        }
        
        // Styles.filterSelection >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private static FieldInfo GetStylesFilterSelection_FI()
        {
            Type type = GetStylesType();
            
            if (s_filterSelection_fi == null)
            {
                s_filterSelection_fi = type.GetField("filterSelection",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return s_filterSelection_fi;
        }
        
        public static GUIContent GetStylesFilterSelection_GC()
        {
            var fi = GetStylesFilterSelection_FI();
            if (fi == null) return null;

            return fi.GetValue(null) as GUIContent;
        }

        // Styles.showInactiveObjects 字段 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private static FieldInfo GetStylesShowInactiveObjects_FI()
        {
            Type type = GetStylesType();
            
            if (s_showInactiveObjects_fi == null)
            {
                s_showInactiveObjects_fi = type.GetField("showInactiveObjects", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return s_showInactiveObjects_fi;
        }
        
        public static GUIContent GetStylesShowInactiveObjects_GC()
        {
            var fi = GetStylesShowInactiveObjects_FI();
            if (fi == null) return null;
            
            return fi.GetValue(null) as GUIContent;
        }


        // 操作实例化对象 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private object m_instance;
        
        public RSerializedPropertyTreeView(object instance)
        {
            m_instance = instance;
        }

        public void SetShowInactiveObjects(bool val)
        {
            ReflectionUtils.SetPropertyOrField(s_type, m_instance, "m_ShowInactiveObjects", val);
        }
        
    }
}