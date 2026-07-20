using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace XKT.Editor.Inspectors
{
    internal sealed class TransformRotationGUI
    {
        private readonly object m_instance;
        private readonly MethodInfo m_onEnableMethod;
        private readonly MethodInfo m_rotationFieldMethod;

        public TransformRotationGUI()
        {
            var type = Type.GetType("UnityEditor.TransformRotationGUI,UnityEditor");
            
            m_onEnableMethod = type.GetMethod("OnEnable");
            m_rotationFieldMethod = type.GetMethod("RotationField", new Type[] { });

            m_instance = Activator.CreateInstance(type);
        }

        public void OnEnable(SerializedProperty property)
        {
            var parameters = new object[] { property, new GUIContent(string.Empty) };
            m_onEnableMethod.Invoke(m_instance, parameters);
        }

        public void RotationField()
        {
            m_rotationFieldMethod.Invoke(m_instance, null);
        }
        
    }
}