using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShaderHotSwap
{
    public partial class ShaderHotSwapWindow
    {
        private class Styles
        {
            private const string c_logoName = "Logo.png";
            
            public GUIStyle log;
            public GUIStyle scroll;
            public Texture2D icon;
            public GUIStyle normalButton;
            public GUIStyle warningButton;
            
            public void Build(ScriptableObject parent)
            {
                if (log == null)
                {
                    log = new GUIStyle(GUI.skin.textArea);
                    log.richText = true;
                    //m_log.normal.background = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f));
                }

                if (scroll == null)
                {
                    scroll = new GUIStyle(GUI.skin.scrollView);
                    scroll.margin = new RectOffset(0, 0, 2, 2);
                }

                if (icon == null)
                {
                    try
                    {
                        var iconPath = Path.Combine(GetIconDirectoryPath(parent), c_logoName);
                        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{s_logHeader} Icon 没找到。\n{ex}");
                    }
                }

                if (normalButton == null)
                {
                    normalButton = new GUIStyle(EditorStyles.toolbarButton);
                }
                
                if (warningButton == null)
                {
                    warningButton = new GUIStyle(EditorStyles.toolbarButton);
                    warningButton.normal.textColor = Color.yellow;
                }
            }

            private static string GetIconDirectoryPath(ScriptableObject parent)
            {
                var scriptAsset = MonoScript.FromScriptableObject(parent);
                var path = AssetDatabase.GetAssetPath(scriptAsset);
                string dire = Path.GetDirectoryName(path);
                string iconDire = Path.Combine(dire, "Icon");
                return iconDire;
            }
        }
    }
}