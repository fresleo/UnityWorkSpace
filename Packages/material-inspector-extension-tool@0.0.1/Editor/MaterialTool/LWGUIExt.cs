// Created By: WangYu  Date: 2024-07-05

#if LWGUI_EXIST

using System.Collections.Generic;
using System.Reflection;
using LWGUI;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.MaterialTool
{
    /// <summary>
    /// 针对 LWGUI 插件的扩展
    /// </summary>
    public class LWGUIExt
    {
        private static List<string> s_tempStringList = new();

        private static bool ReflectMainDrawer(MainDrawer drawer, out string keyword, out bool toggleDisplayed)
        {
            var drawerType = typeof(MainDrawer);
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            keyword = null;
            toggleDisplayed = true;

            var _keyword_fi = drawerType.GetField("_keyword", bindingFlags);
            var _defaultToggleDisplayed_fi = drawerType.GetField("_defaultToggleDisplayed", bindingFlags);
            if (_keyword_fi == null || _defaultToggleDisplayed_fi == null)
            {
                return false;
            }

            keyword = (string)_keyword_fi.GetValue(drawer);
            toggleDisplayed = (bool)_defaultToggleDisplayed_fi.GetValue(drawer);

            return true;
        }

        /// <summary>
        /// 检查基于 LWGUI 工作的属性
        /// </summary>
        public static bool CheckPropertiesWithLWGUI(Shader shader, out string[] skipKeywords)
        {
            s_tempStringList.Clear();

            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);

                var drawer = PublicExtension.ReflectionHelper.GetPropertyDrawer(shader, propertyName);
                if (drawer is MainDrawer mainDrawer)
                {
                    if (ReflectMainDrawer(mainDrawer, out string keyword, out bool toggleDisplayed))
                    {
                        // 用来序列化的关键字
                        string serializedKeyword = Helper.GetKeyWord(keyword, propertyName);
                        
                        // 这种情况下，会产生1个无效的关键字，且是无法删除的
                        if (!toggleDisplayed)
                        {
                            s_tempStringList.Add(serializedKeyword);
                        }

                        //Debug.LogError($"Base 属性: {propertyName} - {toggleDisplayed} - {keyword} - {serializedKeyword}");
                    }
                }
            }

            skipKeywords = s_tempStringList.ToArray();
            return skipKeywords.Length > 0;
        }
        
    }
}

#endif // LWGUI_EXIST
