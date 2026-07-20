// Created By: WangYu  Date: 2025-03-11

using System;
using System.Reflection;
using UnityEngine;

namespace XKT.TOD
{
    public static class ReflectionUtils
    {
        public static void SetPropertyOrField(
            Type reflectionType, object reflectionInstance, 
            string name, object value, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (reflectionType == null || reflectionInstance == null)
            {
                return;
            }
            
            // 尝试属性
            PropertyInfo pi = reflectionType.GetProperty(name, flags);
            if (pi != null && pi.CanWrite)
            {
                pi.SetValue(reflectionInstance, value);
                return;
            }
            
            // 尝试字段
            FieldInfo fi = reflectionType.GetField(name, flags);
            if (fi != null)
            {
                fi.SetValue(reflectionInstance, value);
                return;
            }
            
            Debug.LogError($"在类型 {reflectionType.FullName} 中未找到名为 '{name}' 的属性或字段");
        }
        
        public static object GetPropertyOrField(
            Type reflectionType, object reflectionInstance, 
            string name, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (reflectionType == null || reflectionInstance == null)
            {
                return null;
            }
            
            // 尝试属性
            PropertyInfo prop = reflectionType.GetProperty(name, flags);
            if (prop != null)
            {
                return prop.GetValue(reflectionInstance);
            }

            // 尝试字段
            FieldInfo field = reflectionType.GetField(name, flags);
            if (field != null)
            {
                return field.GetValue(reflectionInstance);
            }

            Debug.LogError($"在类型 {reflectionType.FullName} 中未找到名为 '{name}' 的属性或字段");
            return null;
        }
        
        public static void CallMethodWithVoid(
            Type reflectionType, object reflectionInstance, 
            string methodName, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (reflectionType == null || reflectionInstance == null) return;
            
            MethodInfo mi = reflectionType.GetMethod(methodName, flags, 
                null, new Type[0], null);
            if(mi == null) return;
            
            mi.Invoke(reflectionInstance, new object[0]);
        }
        
        public static void CallMethodWithVoid(
            Type reflectionType, object reflectionInstance, 
            string methodName, Type[] types, object[] arguments, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (reflectionType == null || reflectionInstance == null) return;
            
            MethodInfo mi = reflectionType.GetMethod(methodName, flags,
                null, types, null);
            if(mi == null) return;
            
            mi.Invoke(reflectionInstance, arguments);
        }
        
    }
}