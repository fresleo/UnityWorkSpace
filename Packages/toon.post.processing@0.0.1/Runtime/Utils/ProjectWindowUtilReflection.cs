// Created by: WangYu   Date: 2025-12-15

#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;

namespace ToonPostProcessing
{
    /// <summary>
    /// 反射调用 ProjectWindowUtil 类
    /// </summary>
    public static class ProjectWindowUtilReflection
    {
        /// <summary>
        /// 使用反射获取 Project 窗口中当前活动的文件夹路径
        /// </summary>
        public static string GetActiveFolderPath()
        {
            try
            {
                Type projectWindowUtilType = typeof(ProjectWindowUtil);
                BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", bindingFlags);

                if (getActiveFolderPath != null)
                {
                    object result = getActiveFolderPath.Invoke(null, null);
                    return result?.ToString();
                }
            }
            catch
            {
                // 反射失败
            }

            return null;
        }
    }
}

#endif // UNITY_EDITOR
