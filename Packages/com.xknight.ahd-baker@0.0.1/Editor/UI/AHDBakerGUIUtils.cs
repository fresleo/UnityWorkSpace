/*******************************************************************************
 * File: AHDBakerGUIUtils.cs
 * Author: WangYu
 * Date: 2026-07-07
 * Description:
 * Notice:
 *******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XKnight.AHDBaker.Editor
{
    /// <summary>
    /// AHD Baker 编辑器窗口 GUI 辅助工具。
    /// </summary>
    public static class AHDBakerGUIUtils
    {
        public const int c_EditorFontSize = 15, c_EditorTitleFontSize = 22;

        static GUIStyle 
            s_ScaledHelpBoxStyle
            , s_TitleLabelStyle
            , s_EvenSectionStyle
            , s_OddSectionStyle;

        const string 
            c_EvenSectionStyleName = "TV Selection" // 偶数的
            , c_OddSectionStyleName = "U2D.createRect"; // 奇数的

        private const string
            c_EditorFontStyleName = "helpBox"
            , c_EditorFontStyleName_2 = "HelpBox";

        /// <summary>
        /// 交替背景色的设置区块容器
        /// </summary>
        public sealed class SettingsSectionScope : IDisposable
        {
            public SettingsSectionScope(int sectionIndex)
            {
                GUIStyle style = GetSectionStyle(sectionIndex);
                EditorGUILayout.BeginVertical(style);
            }

            public void Dispose()
            {
                EditorGUILayout.EndVertical();
            }

            static GUIStyle GetSectionStyle(int sectionIndex)
            {
                bool isEven = sectionIndex % 2 == 0;
                if (isEven)
                {
                    if (s_EvenSectionStyle == null)
                    {
                        s_EvenSectionStyle = CreateSectionStyle(c_EvenSectionStyleName);
                    }

                    return s_EvenSectionStyle;
                }

                if (s_OddSectionStyle == null)
                {
                    s_OddSectionStyle = CreateSectionStyle(c_OddSectionStyleName);
                }

                return s_OddSectionStyle;
            }

            static GUIStyle CreateSectionStyle(string skinStyleName)
            {
                GUIStyle source = GUI.skin.GetStyle(skinStyleName);
                if (source == null)
                {
                    source = EditorStyles.helpBox;
                }

                var style = new GUIStyle(source)
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(0, 0, 2, 2)
                };

                if (skinStyleName == c_OddSectionStyleName)
                {
                    style.border = new RectOffset(3, 3, 3, 3);
                }

                return style;
            }
        }

        /// <summary>
        /// 放大字号后的章节标题样式
        /// </summary>
        public static GUIStyle TitleLabelStyle
        {
            get
            {
                if (s_TitleLabelStyle != null)
                {
                    return s_TitleLabelStyle;
                }

                s_TitleLabelStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
                {
                    fontSize = c_EditorTitleFontSize,
                    wordWrap = false,
                    clipping = TextClipping.Overflow
                };
                s_TitleLabelStyle.fixedHeight = 0;
                s_TitleLabelStyle.stretchHeight = false;
                
                return s_TitleLabelStyle;
            }
        }

        /// <summary>
        /// 绘制放大字号后的章节标题
        /// </summary>
        public static void DrawTitleLabel(GUIContent content)
        {
            GUIStyle style = TitleLabelStyle;
            float height = style.CalcHeight(content, EditorGUIUtility.currentViewWidth);
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.LabelField(rect, content, style);
        }
        
        /// <summary>
        /// 绘制放大字号后的章节标题
        /// </summary>
        public static void DrawTitleLabel(string title)
        {
            var content = new GUIContent(title);
            DrawTitleLabel(content);
        }

        /// <summary>
        /// 绘制放大字号后的 HelpBox
        /// </summary>
        /// <param name="message">提示内容</param>
        /// <param name="type">提示类型</param>
        public static void DrawHelpBox(string message, MessageType type)
        {
            GUIStyle helpBoxStyle = GetScaledHelpBoxStyle();
            
            GUIContent content;
            if (type == MessageType.None)
            {
                content = new GUIContent(message);
            }
            else
            {
                content = new GUIContent(message, GetMessageTypeIcon(type));
            }

            float width = EditorGUIUtility.currentViewWidth - 24;
            float height = helpBoxStyle.CalcHeight(content, width);
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.LabelField(rect, content, helpBoxStyle);
        }

        static GUIStyle GetScaledHelpBoxStyle()
        {
            if (s_ScaledHelpBoxStyle != null)
            {
                return s_ScaledHelpBoxStyle;
            }

            GUIStyle source = GUI.skin.GetStyle(c_EditorFontStyleName);
            if (source == null)
            {
                source = EditorStyles.helpBox;
            }

            s_ScaledHelpBoxStyle = new GUIStyle(source)
            {
                fontSize = c_EditorFontSize,
                wordWrap = true,
                richText = true,
                imagePosition = ImagePosition.ImageLeft
            };
            s_ScaledHelpBoxStyle.fixedHeight = 0;
            
            return s_ScaledHelpBoxStyle;
        }

        static Texture GetMessageTypeIcon(MessageType type)
        {
            switch (type)
            {
                case MessageType.Info:
                    return EditorGUIUtility.IconContent("console.infoicon").image;
                case MessageType.Warning:
                    return EditorGUIUtility.IconContent("console.warnicon").image;
                case MessageType.Error:
                    return EditorGUIUtility.IconContent("console.erroricon").image;
                default:
                    return null;
            }
        }

        static void TryAddSkinStyle(List<GUIStyle> styles, string styleName)
        {
            if (string.IsNullOrEmpty(styleName))
            {
                return;
            }

            GUIStyle style = GUI.skin.GetStyle(styleName);
            if (style != null)
            {
                styles.Add(style);
            }
        }

        /// <summary>
        /// 临时放大 AHD Baker 窗口字号，Dispose 时恢复。
        /// </summary>
        public sealed class AHDBakerEditorFontScope : IDisposable
        {
            struct SavedFontSize
            {
                public GUIStyle Style;
                public int FontSize;
            }
            readonly SavedFontSize[] _savedFontSizes;

            public AHDBakerEditorFontScope()
            {
                GUIStyle[] styles = GetScaledStyles();
                
                _savedFontSizes = new SavedFontSize[styles.Length];
                for (int i = 0; i < styles.Length; i++)
                {
                    GUIStyle style = styles[i];
                    _savedFontSizes[i] = new SavedFontSize
                    {
                        Style = style,
                        FontSize = style.fontSize
                    };
                    style.fontSize = c_EditorFontSize;
                }
            }

            public void Dispose()
            {
                for (int i = 0; i < _savedFontSizes.Length; i++)
                {
                    _savedFontSizes[i].Style.fontSize = _savedFontSizes[i].FontSize;
                }
            }

            static GUIStyle[] GetScaledStyles()
            {
                var styles = new List<GUIStyle>
                {
                    EditorStyles.label,
                    EditorStyles.boldLabel,
                    EditorStyles.wordWrappedLabel,
                    EditorStyles.toggle,
                    EditorStyles.radioButton,
                    EditorStyles.popup,
                    EditorStyles.numberField,
                    EditorStyles.textField,
                    EditorStyles.helpBox,
                    GUI.skin.label,
                    GUI.skin.button,
                    GUI.skin.toggle,
                    GUI.skin.textField,
                    GUI.skin.box
                };

                TryAddSkinStyle(styles, c_EditorFontStyleName);
                TryAddSkinStyle(styles, c_EditorFontStyleName_2);
                
                return styles.ToArray();
            }
        }
        
    }
}