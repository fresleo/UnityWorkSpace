/*******************************************************************************
 * File: RulerSceneGUIEditor.cs
 * Author: WangYu
 * Date: 2026-02-12
 * Description: 类或文件功能描述
 *
 * Notice: 注意事项描述（无可省略）
 *******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace XKT.Editor.SceneViewRuler2D
{
    [InitializeOnLoad]
    public static class RulerSceneGUIEditor
    {
        private const string c_windowPath = "Window/2D/SceneView 2D 世界坐标刻度";

        static RulerSceneGUIEditor()
        {
            RulerSceneGUI.show = EditorPrefs.GetBool(c_windowPath, false);
            EditorApplication.delayCall += () => { ConfigurePreferences(RulerSceneGUI.show); };
        }

        [MenuItem(c_windowPath)]
        private static void ToggleAction()
        {
            ConfigurePreferences(!RulerSceneGUI.show);
            SceneView.RepaintAll();
        }

        public static void ConfigurePreferences(bool enabled)
        {
            Menu.SetChecked(c_windowPath, enabled);
            EditorPrefs.SetBool(c_windowPath, enabled);
            RulerSceneGUI.show = enabled;
        }
    }

    // SceneView 顶部工具栏用来当开关的按钮
    [EditorToolbarElement(RulerToolbarToggle.Id, typeof(SceneView))]
    internal sealed class RulerToolbarToggle : EditorToolbarToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }

        public const string Id = "XKT/Ruler2DToggle";
        private const string c_iconPath = "Packages/xkt.inspector/Editor/SceneView/Ruler2D/Gizmos/rulerSmall.png";

        public RulerToolbarToggle()
        {
            this.text = "";
            this.tooltip = "SceneView 2D 世界坐标刻度";

            var btnIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(c_iconPath);
            if (btnIcon != null)
            {
                this.onIcon = btnIcon;
                this.offIcon = btnIcon;
            }
            else
            {
                this.onIcon = this.offIcon = EditorGUIUtility.FindTexture("d_Grid.BoxTool");
            }

            SetValueWithoutNotify(RulerSceneGUI.show);
            RegisterCallback<ChangeEvent<bool>>(OnValueChanged);
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            RulerSceneGUIEditor.ConfigurePreferences(evt.newValue);
            SceneView.RepaintAll();
        }
    }

    [Overlay(typeof(SceneView), "", true
        , defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top, defaultDockIndex = 5)]
    internal class RulerToolbarOverlay : ToolbarOverlay
    {
        public RulerToolbarOverlay() : base(RulerToolbarToggle.Id)
        {
        }
    }
}