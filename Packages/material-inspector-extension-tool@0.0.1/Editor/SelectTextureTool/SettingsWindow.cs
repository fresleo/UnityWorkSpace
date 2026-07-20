using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    /// <summary>
    /// 设置窗口
    /// </summary>
    public class SettingsWindow : EditorWindow
    {
        private bool m_isUISeting;
        private bool m_isPluginSeting;
        
        private static int s_instanceId;
        private static List<SelectTextureWindowPlugins> s_plugins = new();
        private static List<bool> s_isPluginsToggleValue = new();

        public static void Open(Rect r)
        {
            var go = GetWindow<SettingsWindow>(true, "设置");
            go.position = r;
            go.Show();
        }

        private void OnGUI()
        {
            m_isUISeting = EditorGUILayout.BeginFoldoutHeaderGroup(m_isUISeting, "界面设置");
            if (m_isUISeting)
            {
                using (new GUILayout.HorizontalScope("box"))
                {
                    GUILayout.Label("窗口背景颜色:");
                    SelectTextureWindow.s_windowData.windowBackgroundColor = EditorGUILayout.ColorField(SelectTextureWindow.s_windowData.windowBackgroundColor);
                }

                using (new GUILayout.HorizontalScope("box"))
                {
                    GUILayout.Label("选择框颜色:");
                    SelectTextureWindow.s_windowData.selectColor = EditorGUILayout.ColorField(SelectTextureWindow.s_windowData.selectColor);
                }

                using (new GUILayout.HorizontalScope("box"))
                {
                    GUILayout.Label("贴图背景:");
                    SelectTextureWindow.skin.customStyles[0].normal.background =
                        EditorGUILayout.ObjectField(SelectTextureWindow.skin.customStyles[0].normal.background, typeof(Texture2D), false) as Texture2D;
                }

                using (new GUILayout.HorizontalScope("box"))
                {
                    GUILayout.Label("窗口背景图片:");
                    SelectTextureWindow.s_windowData.windowBackgroundTexture = 
                        EditorGUILayout.ObjectField(SelectTextureWindow.s_windowData.windowBackgroundTexture, typeof(Texture2D), false) as Texture2D;
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            m_isPluginSeting = EditorGUILayout.Foldout(m_isPluginSeting, "插件");
            if (m_isPluginSeting)
            {
                if (s_plugins.Count > 0)
                {
                    for (int i = 0; i < s_plugins.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            string content = i + 1 + ". " + s_plugins[i].PluginName;
                            s_isPluginsToggleValue[i] = EditorGUILayout.Foldout(s_isPluginsToggleValue[i], content);
                            s_plugins[i].IsEnable = GUILayout.Toggle(s_plugins[i].IsEnable, "开启");
                        }
                        
                        if (s_isPluginsToggleValue[i])
                        {
                            GUILayout.Label(s_plugins[i].PluginTips);
                        }
                    }
                }

                if (GUILayout.Button("重新加载插件"))
                {
                    s_plugins.Clear();
                    s_isPluginsToggleValue.Clear();
                    
                    var types = TypeCache.GetTypesDerivedFrom<SelectTextureWindowPlugins>();
                    for (int j = 0; j < types.Count; j++)
                    {
                        var stwp = Activator.CreateInstance(types[j]) as SelectTextureWindowPlugins;
                        s_plugins.Add(stwp);
                        s_isPluginsToggleValue.Add(false);
                    }
                }
            }

            if (GUILayout.Button("保存"))
            {
                SelectTextureWindow.SaveData();
            }
        }
        
    }
}