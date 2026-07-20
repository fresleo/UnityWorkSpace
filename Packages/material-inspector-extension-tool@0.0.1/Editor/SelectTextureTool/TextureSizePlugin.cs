using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.SelectTextureTool
{
    public class TextureSizePlugin : SelectTextureWindowPlugins
    {
        private Rect m_rect;
        
        private static bool s_isSDButtonValue;
        private static bool s_isHDButtonValue;
        private static bool s_isFHDButtonValue;
        
        private static bool s_isChang;
        private static int s_tempInt;
        
        public override string PluginName
        {
            get => "ML_自定义贴图大小";
            set => throw new System.NotImplementedException();
        }

        public override string PluginTips
        {
            get => "根据规则快速选择筛选贴图大小";
            set => throw new System.NotImplementedException();
        }

        private bool m_isEnable;

        public override bool IsEnable
        {
            get => m_isEnable;
            set => m_isEnable = value;
        }
        
        public override void Draw()
        {
            using (new GUILayout.AreaScope(m_rect))
            {
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        string styleStr = "LargeButtonMid";
                        var expandWidth = GUILayout.ExpandWidth(false);
                        var width100 = GUILayout.Width(100);
                        
                        s_isSDButtonValue = GUILayout.Toggle(s_isSDButtonValue, "SD", styleStr, expandWidth, width100);
                        s_isHDButtonValue = GUILayout.Toggle(s_isHDButtonValue, "HD", styleStr, expandWidth, width100);
                        s_isFHDButtonValue = GUILayout.Toggle(s_isFHDButtonValue, "FHD", styleStr, expandWidth, width100);
                    }
                    s_isChang = EditorGUI.EndChangeCheck();

                    if (s_isSDButtonValue && s_tempInt != 1)
                    {
                        s_tempInt = 1;
                        s_isHDButtonValue = false;
                        s_isFHDButtonValue = false;
                        
                        for (int i = 0; i < SelectTextureWindow.s_windowData.textureSizeTypes.Count; i++)
                        {
                            SelectTextureWindow.s_windowData.textureSizeTypes[SelectTextureWindow.s_windowData.textureSizeTypes.Keys.ToArray()[i]] =
                                SelectTextureWindow.s_windowData.textureSizeTypes.Keys.ToArray()[i] <= 128;
                        }

                        SizeFilterPopupWindow<int>.IsAllIsFalse = false;
                        SelectTextureWindow.RefreshFilter();
                    }

                    if (s_isHDButtonValue && s_tempInt != 2)
                    {
                        s_tempInt = 2;
                        s_isSDButtonValue = false;
                        s_isFHDButtonValue = false;
                        
                        for (int i = 0; i < SelectTextureWindow.s_windowData.textureSizeTypes.Count; i++)
                        {
                            if (SelectTextureWindow.s_windowData.textureSizeTypes.Keys.ToArray()[i] == 256)
                            {
                                SelectTextureWindow.s_windowData.textureSizeTypes[SelectTextureWindow.s_windowData.textureSizeTypes.Keys.ToArray()[i]] = true;
                            }
                            else
                            {
                                SelectTextureWindow.s_windowData.textureSizeTypes[SelectTextureWindow.s_windowData.textureSizeTypes.Keys.ToArray()[i]] = false;
                            }
                        }

                        SizeFilterPopupWindow<int>.IsAllIsFalse = false;
                        SelectTextureWindow.RefreshFilter();
                    }

                    if (s_isFHDButtonValue && s_tempInt != 3)
                    {
                        s_tempInt = 3;
                        s_isHDButtonValue = false;
                        s_isSDButtonValue = false;
                        
                        for (int i = 0; i < SelectTextureWindow.s_windowData.textureSizeTypes.Count; i++)
                        {
                            if (SelectTextureWindow.s_windowData.textureSizeTypes.Keys.ToArray()[i] > 256)
                            {
                                SelectTextureWindow.s_windowData.textureSizeTypes[SelectTextureWindow.s_windowData.textureSizeTypes.Keys.ToArray()[i]] = true;
                            }
                            else
                            {
                                SelectTextureWindow.s_windowData.textureSizeTypes[SelectTextureWindow.s_windowData.textureSizeTypes.Keys.ToArray()[i]] = false;
                            }
                        }

                        SizeFilterPopupWindow<int>.IsAllIsFalse = false;
                        SelectTextureWindow.RefreshFilter();
                    }

                    if (s_isChang && !s_isSDButtonValue && !s_isHDButtonValue && !s_isFHDButtonValue)
                    {
                        s_tempInt = 0;
                        
                        SizeFilterPopupWindow<int>.IsAllIsFalse = true;
                        SelectTextureWindow.RefreshFilter();
                    }
                }
            }
        }
        
    }
}