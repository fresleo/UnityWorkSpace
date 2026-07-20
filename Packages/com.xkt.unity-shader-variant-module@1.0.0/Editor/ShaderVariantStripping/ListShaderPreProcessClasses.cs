using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace XKT.ShaderVariantStripping
{
    /// <summary>
    /// 列出所有实现了 IPreprocessShaders 接口的类
    /// </summary>
    internal class ListShaderPreProcessClasses : EditorWindow
    {
        public static void ShowDebugWindow()
        {
            var window = CreateInstance<ListShaderPreProcessClasses>();
            window.titleContent = new GUIContent("[Debug] 列出所有实现了 IPreprocessShaders 接口的类");
            window.ShowAuxWindow();

            float winWidth = 600;
            float winHeight = 150;
            float winPosX = (Screen.currentResolution.width - winWidth) / 2;
            float winPosY = (Screen.currentResolution.height - winHeight) / 2;
            window.position = new Rect(winPosX, winPosY, winWidth, winHeight);
        }

        private void OnEnable()
        {
            var scroll = new ScrollView();
            this.rootVisualElement.Add(scroll);

            var types = GetSubClasses<UnityEditor.Build.IPreprocessShaders>();
            
            // foreach (var type in types)
            // {
            //     scroll.Add(new Label(type.FullName));
            // }
            
            var entries = new List<(Type type, int order)>();
            foreach (var type in types)
            {
                if (type.IsAbstract) continue;
                
                int order = int.MaxValue;
                try
                {
                    var cons = type.GetConstructor(Type.EmptyTypes);
                    if (cons != null)
                    {
                        var instance = Activator.CreateInstance(type);
                        if (instance is UnityEditor.Build.IPreprocessShaders cb)
                        {
                            order = cb.callbackOrder;
                        }
                    }
                }
                catch
                {
                    // 忽略无法创建实例的类型（保持 order 为 int.MaxValue）
                }

                entries.Add((type, order));
            }
            
            var sorted = entries
                .OrderBy(e => e.order)
                .ThenBy(e => e.type.FullName)
                .ToList();
            
            foreach (var e in sorted)
            {
                string labelText = $"{e.type.FullName}   callbackOrder: {(e.order == int.MaxValue ? "N/A" : e.order.ToString())}";
                scroll.Add(new Label(labelText));
            }
        }

        private static List<System.Type> GetSubClasses<T>()
        {
            var result = new List<System.Type>();

            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }
                
                foreach (var t in types)
                {
                    if (t == typeof(T))
                    {
                        continue;
                    }

                    if (typeof(T).IsAssignableFrom(t))
                    {
                        result.Add(t);
                    }
                }
            }

            return result;
        }
        
    }
}