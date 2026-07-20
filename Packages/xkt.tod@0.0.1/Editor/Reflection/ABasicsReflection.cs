// Created By: WangYu  Date: 2025-03-11

using System;
using System.Reflection;
using UnityEngine;

namespace XKT.TOD
{
    public class ABasicsReflection
    {
        protected Type m_reflectionType;
        protected object m_reflectionInstance;
        
        public void SetPropertyOrField(
            string name, object value, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (m_reflectionType == null || m_reflectionInstance == null)
            {
                return;
            }
            
            ReflectionUtils.SetPropertyOrField(
                m_reflectionType, m_reflectionInstance, 
                name, value, flags);
        }
        
        public object GetPropertyOrField(
            string name, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (m_reflectionType == null || m_reflectionInstance == null)
            {
                return null;
            }
            
            return ReflectionUtils.GetPropertyOrField(
                m_reflectionType, m_reflectionInstance, 
                name, flags);
        }
        
        public void CallMethodWithVoid(
            string methodName, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            ReflectionUtils.CallMethodWithVoid(
                m_reflectionType, m_reflectionInstance, 
                methodName, flags);
        }
        
        public void CallMethodWithVoid(
            string methodName, Type[] types, object[] arguments, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            ReflectionUtils.CallMethodWithVoid(
                m_reflectionType, m_reflectionInstance, 
                methodName, types, arguments, flags);
        }
        
    }
}