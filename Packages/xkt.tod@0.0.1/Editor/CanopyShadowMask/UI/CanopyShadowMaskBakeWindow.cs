/*******************************************************************************
 * File: CanopyShadowMaskBakeWindow.cs
 * Author: WangYu
 * Date: 2026-06-30
 * Description:
 *******************************************************************************/

using UnityEditor;
using UnityEngine;

namespace XKT.TOD.CanopyShadowMask
{
    /// <summary>
    /// 2次烘焙树冠的 Shadowmask ，并合并到场景的原始纹理上。
    /// </summary>
    public sealed class CanopyShadowMaskBakeWindow : EditorWindow
    {
        private const string C_WINDOW_TITLE = "树冠 Shadowmask 二次烘焙";
        private const string C_MENU_PATH = "Window/TA工具集/" + C_WINDOW_TITLE;

        [MenuItem(C_MENU_PATH)]
        public static void Open()
        {
            var window = GetWindow<CanopyShadowMaskBakeWindow>(C_WINDOW_TITLE);
            window.minSize = new Vector2(520, 560);
            window.Show();
        }


        private readonly CanopyShadowMaskBakeParams _params = new();
        private Vector2 _scroll;
        private string _lastSummary = string.Empty;

        void OnEnable()
        {
            if (_params.mainLight == null)
            {
                _params.mainLight = CanopyShadowMaskBaker.FindDefaultMainLight();
            }

            SyncChannelFromMainLight(false);
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            try
            {
                DrawHeader();
                
                EditorGUILayout.Space();
                DrawLightSettings();
                
                EditorGUILayout.Space();
                DrawProxySettings();
                
                EditorGUILayout.Space();
                DrawActions();
                
                EditorGUILayout.Space();
                DrawSummary();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(C_WINDOW_TITLE, EditorStyles.whiteLargeLabel);

            EditorGUILayout.Space();
            string guide =
                "使用步骤："
                + "\n    1. 先完成 Unity 官方 Lightmap/Shadowmask 烘焙，参与 Shadowmask 的主灯要选 Mixed 模式。"
                + $"\n    2. 给树冠的 _ShadowOnly 代理对象标记 Tag: {CanopyShadowMaskBaker.C_CANOPY_SHADOW_ONLY_TAG}"
                + "\n    3. 点击【执行二次烘焙】，工具会读取官方 shadowmask，按主光方向渲染树冠临时 shadow map 后合并。"
                + "\n    4. 结果写入 *_canopy 副本，并在 shadowmask 目录生成映射 .asset；可用该资源按钮切换场景引用。"
                + "\n\n限制：" 
                + "\n    仅支持已参与 Lightmap 的 MeshRenderer 接收物；"
                + "\n    优先使用 uv2/lightmap UV，缺失时使用 uv0 兜底；"
                + "\n    Tree/Grass/Plant shader 会在内部从接收物收集中排除。";
            EditorGUILayout.HelpBox(guide, MessageType.Info);
        }

        private void DrawLightSettings()
        {
            EditorGUILayout.LabelField("光照", EditorStyles.whiteLargeLabel);
            
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            Light newMainLight = EditorGUILayout.ObjectField(
                "主灯光 (Mixed/Baked)",
                _params.mainLight, typeof(Light), true) as Light;
            if (EditorGUI.EndChangeCheck())
            {
                _params.mainLight = newMainLight;
                SyncChannelFromMainLight(false);
            }

            if (GUILayout.Button("自动查找方向主光"))
            {
                _params.mainLight = CanopyShadowMaskBaker.FindDefaultMainLight();
                SyncChannelFromMainLight(true);
            }

            _params.autoUseLightShadowmaskChannel = EditorGUILayout.ToggleLeft(
                "自动使用主光通道", _params.autoUseLightShadowmaskChannel);
            if (_params.autoUseLightShadowmaskChannel)
            {
                SyncChannelFromMainLight(false);

                using (new EditorGUI.DisabledScope(true))
                {
                    _params.channel = (EShadowMaskChannel)EditorGUILayout.EnumPopup(
                        "Shadowmask 通道", _params.channel);
                }
            }
            else
            {
                _params.channel = (EShadowMaskChannel)EditorGUILayout.EnumPopup(
                    "Shadowmask 通道", _params.channel);
            }

            _params.globalShadowStrength = EditorGUILayout.Slider(
                "全局阴影强度", _params.globalShadowStrength, 0, 1);
        }

        private void DrawProxySettings()
        {
            EditorGUILayout.LabelField("树冠代理", EditorStyles.whiteLargeLabel);
            
            EditorGUILayout.Space();
            EditorGUILayout.SelectableLabel(
                "Tag: " + CanopyShadowMaskBaker.C_CANOPY_SHADOW_ONLY_TAG,
                EditorStyles.helpBox, GUILayout.Height(EditorGUIUtility.singleLineHeight + 8));
            
            EditorGUILayout.Space();
            _params.shadowMapSoftShadow = EditorGUILayout.ToggleLeft(
                "软影 (4 采样)", _params.shadowMapSoftShadow);
            
            EditorGUILayout.Space();
            _params.atlasTexelStep = EditorGUILayout.IntSlider(
                "Atlas texel 步长", _params.atlasTexelStep, 1, 8);
            EditorGUILayout.HelpBox("控制遍历 shadowmask 图集像素时的采样间隔，是性能与精度的权衡。\n数值一般用 1, 2, 8", MessageType.Info);
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.whiteLargeLabel);
            
            EditorGUILayout.Space();
            _params.writeDebugTextures = EditorGUILayout.ToggleLeft(
                "Debug 模式（输出 debug 纹理）", _params.writeDebugTextures);

            if (GUILayout.Button("删除当前场景的 debug 纹理"))
            {
                _lastSummary = CanopyShadowMaskBaker.DeleteDebugTextures();
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("执行二次烘焙", GUILayout.Height(32)))
                {
                    CanopyShadowMaskBaker.BakeResult result = CanopyShadowMaskBaker.Bake(_params);
                    _lastSummary = result.message;
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请在 Editor 非 Play 模式下执行烘焙。", MessageType.Warning);
            }
        }

        private void DrawSummary()
        {
            EditorGUILayout.LabelField("结果", EditorStyles.whiteLargeLabel);
            
            EditorGUILayout.Space();
            MessageType messageType = GetSummaryMessageType();
            if (messageType != MessageType.None)
            {
                EditorGUILayout.HelpBox("结果文本可选中复制。", messageType);
            }

            string summary = string.IsNullOrEmpty(_lastSummary)
                ? "尚无操作记录。"
                : _lastSummary;
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            EditorGUILayout.TextArea(summary, textAreaStyle, GUILayout.MinHeight(58));
        }

        private MessageType GetSummaryMessageType()
        {
            if (string.IsNullOrEmpty(_lastSummary))
            {
                return MessageType.None;
            }

            if (_lastSummary.Contains("failed")
                || _lastSummary.Contains("aborted")
                || _lastSummary.Contains("失败"))
            {
                return MessageType.Error;
            }

            if (_lastSummary.Contains("modified texels is 0")
                || _lastSummary.Contains("Warning"))
            {
                return MessageType.Warning;
            }

            return MessageType.Info;
        }

        private void SyncChannelFromMainLight(bool logWarnings)
        {
            if (!_params.autoUseLightShadowmaskChannel)
            {
                return;
            }

            if (CanopyShadowMaskBaker.TryGetShadowMaskChannel(_params.mainLight, out EShadowMaskChannel channel))
            {
                _params.channel = channel;
                return;
            }

            if (logWarnings)
            {
                Debug.LogWarning($"主灯光没有有效的 shadowmask 通道。保持手动通道 {_params.channel}。");
            }
        }
        
    }
}