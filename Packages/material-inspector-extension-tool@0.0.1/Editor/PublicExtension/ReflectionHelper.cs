// Created By: WangYu  Date: 2024-07-05

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.PublicExtension
{
	/// <summary>
	/// 反射助手
	/// </summary>
    public class ReflectionHelper
    {
        private static Assembly UnityEditor_Assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        
        #region MaterialProperty 的处理

		private static Type         MaterialPropertyHandler_Type                    = UnityEditor_Assembly.GetType("UnityEditor.MaterialPropertyHandler");
		private static MethodInfo   MaterialPropertyHandler_GetHandler_Method       = MaterialPropertyHandler_Type.GetMethod("GetHandler", BindingFlags.Static | BindingFlags.NonPublic);
		private static PropertyInfo MaterialPropertyHandler_PropertyDrawer_Property = MaterialPropertyHandler_Type.GetProperty("propertyDrawer");
		private static FieldInfo    MaterialPropertyHandler_DecoratorDrawers_Field  = MaterialPropertyHandler_Type.GetField("m_DecoratorDrawers", BindingFlags.NonPublic | BindingFlags.Instance);
		
		public static MaterialPropertyDrawer GetPropertyDrawer(Shader shader, string materialPropertyName, out List<MaterialPropertyDrawer> decoratorDrawers)
		{
			decoratorDrawers = new List<MaterialPropertyDrawer>();
			var handler = MaterialPropertyHandler_GetHandler_Method.Invoke(null, new System.Object[] { shader, materialPropertyName });
			if (handler != null && handler.GetType() == MaterialPropertyHandler_Type)
			{
				decoratorDrawers = MaterialPropertyHandler_DecoratorDrawers_Field.GetValue(handler) as List<MaterialPropertyDrawer>;
				return MaterialPropertyHandler_PropertyDrawer_Property.GetValue(handler, null) as MaterialPropertyDrawer;
			}
			return null;
		}
		
		public static MaterialPropertyDrawer GetPropertyDrawer(Shader shader, MaterialProperty prop, out List<MaterialPropertyDrawer> decoratorDrawers)
		{
			return GetPropertyDrawer(shader, prop.name, out decoratorDrawers);
		}

		public static MaterialPropertyDrawer GetPropertyDrawer(Shader shader, MaterialProperty prop)
		{
			List<MaterialPropertyDrawer> decoratorDrawers;
			return GetPropertyDrawer(shader, prop, out decoratorDrawers);
		}

		public static MaterialPropertyDrawer GetPropertyDrawer(Shader shader, string materialPropertyName)
		{
			List<MaterialPropertyDrawer> decoratorDrawers;
			return GetPropertyDrawer(shader, materialPropertyName, out decoratorDrawers);
		}

		#endregion // MaterialProperty 的处理
		
    }
}