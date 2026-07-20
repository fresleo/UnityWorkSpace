/*
 * 功能说明：
 * - 显示当前场景中所有启用了逐对象阴影的游戏对象列表
 * - 以滚动视图的方式展示 GPerObjectShadowManager 管理的目标对象
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Garena.TA
{
    public class GPerObjectShadowEditor : EditorWindow
    {
        [MenuItem("Window/TA工具集/资源-检测工具/逐对象阴影检测工具/PerObjectShadow", false, 51)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<GPerObjectShadowEditor>();
        }

        Vector2 scrollPos;

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            var targetList = GPerObjectShadowManager.Instance.FinalTargetList;
            foreach (var item in targetList)
            {
                EditorGUILayout.ObjectField(item.go, typeof(GameObject), false);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    [CustomEditor(typeof(GPerObjectShadowRendererFeature))]
    public class GPerObjectShadowSettingsEditor : Editor
    {
        GPerObjectShadowRendererFeature Target => target as GPerObjectShadowRendererFeature;
        GPerObjectShadowPassSettings ShadowMapPassSettings => Target.settings.shadowPassSettings;
        GPerObjectShadowResolvePassSettings ResolveSettings => Target.settings.resolvePassSettings;
        GPerObjectShadowApplyPassSettings ApplySettings => Target.settings.applyPassSettings;

        enum OptionEnum
        {
            None,
            InShader,
            CurRT,
            CurRT_Post,
            NewRT,
            NewRT_Post,
            SSSRT,
            SSSRT_Post
        }

        string[] options = new string[] {
            "关",
            "直接在Shader中采样",
            "叠加到当前画面-贴花方式" ,
            "叠加到当前画面-后处理方式" ,
            "渲染到单独RT-贴花方式",
            "渲染到单独RT-后处理方式",
            "渲染到屏幕空间阴影RT-贴花方式",
            "渲染到屏幕空间阴影RT-后处理方式"
        };

        public override void OnInspectorGUI()
        {
            GUILayout.Label("预设：");

            EditorGUI.BeginChangeCheck();
            int select = GUILayout.SelectionGrid(-1, options, 2);

            if (EditorGUI.EndChangeCheck())
            {
                ShadowMapPassSettings.enable = false;
                ResolveSettings.enable = false;
                ApplySettings.enable = false;

                switch ((OptionEnum)select)
                {
                    case OptionEnum.None:
                        ShadowMapPassSettings.enable = false;
                        ResolveSettings.enable = false;
                        ApplySettings.enable = false;
                        break;
                    case OptionEnum.InShader:
                        ShadowMapPassSettings.enable = true;
                        ResolveSettings.enable = false;
                        ApplySettings.enable = false;
                        break;
                    case OptionEnum.CurRT:
                        ShadowMapPassSettings.enable = true;

                        ResolveSettings.enable = true;
                        ResolveSettings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents;
                        ResolveSettings.resolveToRenderTexture = false;
                        ResolveSettings.resolveToScreenSpaceShadow = false;

                        ResolveSettings.usePostMethod = false;
                        ResolveSettings.srcBlend = UnityEngine.Rendering.BlendMode.SrcAlpha;
                        ResolveSettings.dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

                        ApplySettings.enable = false;
                        break;
                    case OptionEnum.CurRT_Post:
                        ShadowMapPassSettings.enable = true;

                        ResolveSettings.enable = true;
                        ResolveSettings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents;
                        ResolveSettings.resolveToRenderTexture = false;
                        ResolveSettings.resolveToScreenSpaceShadow = false;

                        ResolveSettings.usePostMethod = true;
                        ResolveSettings.srcBlend = UnityEngine.Rendering.BlendMode.SrcAlpha;
                        ResolveSettings.dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

                        ApplySettings.enable = false;
                        break;
                    case OptionEnum.NewRT:
                        ShadowMapPassSettings.enable = true;

                        ResolveSettings.enable = true;
                        ResolveSettings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingOpaques;
                        ResolveSettings.resolveToRenderTexture = true;
                        ResolveSettings.resolveToScreenSpaceShadow = false;

                        ResolveSettings.usePostMethod = false;
                        ResolveSettings.srcBlend = UnityEngine.Rendering.BlendMode.SrcAlpha;
                        ResolveSettings.dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

                        ApplySettings.enable = true;
                        ApplySettings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents;
                        ApplySettings.srcBlend = UnityEngine.Rendering.BlendMode.SrcAlpha;
                        ApplySettings.dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        break;
                    case OptionEnum.NewRT_Post:
                        ShadowMapPassSettings.enable = true;

                        ResolveSettings.enable = true;
                        ResolveSettings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingOpaques;
                        ResolveSettings.resolveToRenderTexture = true;
                        ResolveSettings.resolveToScreenSpaceShadow = false;

                        ResolveSettings.usePostMethod = true;
                        ResolveSettings.srcBlend = UnityEngine.Rendering.BlendMode.SrcAlpha;
                        ResolveSettings.dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

                        ApplySettings.enable = true;
                        ApplySettings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents;
                        ApplySettings.srcBlend = UnityEngine.Rendering.BlendMode.SrcAlpha;
                        ApplySettings.dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                        break;
                    case OptionEnum.SSSRT:
                        ShadowMapPassSettings.enable = true;

                        ResolveSettings.enable = true;
                        ResolveSettings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents;
                        ResolveSettings.resolveToRenderTexture = true;
                        ResolveSettings.resolveToScreenSpaceShadow = true;

                        ApplySettings.enable = false;
                        break;
                    case OptionEnum.SSSRT_Post:
                        ShadowMapPassSettings.enable = true;

                        ResolveSettings.enable = true;
                        ResolveSettings.Event = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents;
                        ResolveSettings.resolveToRenderTexture = true;
                        ResolveSettings.resolveToScreenSpaceShadow = true;

                        ApplySettings.enable = false;
                        break;

                    default:
                        break;
                }
           
                serializedObject.ApplyModifiedProperties();
                //EditorUtility.SetDirty(Target);
            }

            base.OnInspectorGUI();
        }
    }
}
