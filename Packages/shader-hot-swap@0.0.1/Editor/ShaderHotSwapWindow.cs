using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ShaderHotSwap.Protocol;
using ShaderHotSwap.Util;
using UnityEngine;
using UnityEditor;

namespace ShaderHotSwap
{
    public partial class ShaderHotSwapWindow : EditorWindow
    {
        [MenuItem("Window/TA工具集/着色器热切换", false, 0)]
        static void Init()
        {
            var win = EditorWindow.GetWindow<ShaderHotSwapWindow>();
            win.Show();
        }


        private static string s_logHeader = $"[{nameof(ShaderHotSwapWindow)}]";
        
        /// <summary>
        /// 着色器数据列表
        /// </summary>
        [SerializeField] private ShaderData[] m_shaderDataList = null;
        
        /// <summary>
        /// Url 前缀
        /// </summary>
        public static string UrlPrefix { get; private set; }
        
        private Styles m_styles;
        private bool m_needToSetTitle = true;
        private Vector2 m_scrollPos;

        private bool m_hasPortMapping;
        
        private Editor m_editor = null;
        
        private void OnEnable()
        {
            CheckPortMapping();
        }

        private void OnGUI()
        {
            if (m_styles == null)
            {
                m_styles = new Styles();
            }
            m_styles.Build(this);

            SetTitle();
            ShowToolbarUI();

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, m_styles.scroll, GUILayout.ExpandHeight(true));
            {
                // 把检查器的 editor 画出来
                if (m_editor == null)
                {
                    m_editor = Editor.CreateEditor(this);
                }
                m_editor?.OnInspectorGUI();

                GUILayout.FlexibleSpace();
                ShowLogUI();
            }
            EditorGUILayout.EndScrollView();

            ShowHelpMessage();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        
        private void SetTitle()
        {
            if (m_needToSetTitle)
            {
                titleContent = new GUIContent("着色器热切换", m_styles.icon);
                m_needToSetTitle = false;
            }
        }

        private void CheckPortMapping()
        {
            (string result, string errorMsg) = CommandLineExecutor.ExecuteCommand(Application.dataPath, "adb", " forward --list");
            if (!string.IsNullOrEmpty(errorMsg))
            {
                SetHelpMessage("命令执行失败", MessageType.Error);
                Debug.LogError(errorMsg);
            }

            m_hasPortMapping = result.Contains(" tcp:8090 tcp:8090");
            Repaint();
        }

        private void SetPortMapping()
        {
            (string result, string errorMsg) = CommandLineExecutor.ExecuteCommand(Application.dataPath, "adb", " forward tcp:8090 tcp:8090");
            if (!string.IsNullOrEmpty(errorMsg))
            {
                SetHelpMessage("命令执行失败", MessageType.Error);
                Debug.LogError(errorMsg);
            }

            m_hasPortMapping = result.Contains("8090");
            Repaint();
            
            SetHelpMessage("设置端口映射", MessageType.Info);
            m_log = "设置端口映射";
        }

        private void ClearPortMapping()
        {
            (string result, string errorMsg) = CommandLineExecutor.ExecuteCommand(Application.dataPath, "adb", " forward --remove tcp:8090");
            if (!string.IsNullOrEmpty(errorMsg))
            {
                SetHelpMessage("命令执行失败", MessageType.Error);
                Debug.LogError(errorMsg);
            }
            
            m_hasPortMapping = false;
            Repaint();
            
            SetHelpMessage("清理端口映射", MessageType.Info);
            m_log = "清理端口映射";
        }
        
        private void ShowToolbarUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginDisabledGroup(m_hasPortMapping);
                {
                    var btnGs = m_hasPortMapping ? m_styles.normalButton : m_styles.warningButton;
                    if (GUILayout.Button("adb端口映射: 8090", btnGs))
                    {
                        SetPortMapping();
                    }
                }
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("清理端口映射", m_styles.normalButton))
                {
                    ClearPortMapping();
                }
                
                if (GUILayout.Button("清理缓存", m_styles.normalButton))
                {
                    var dir = GetAssetBundleOutputDir();
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                    
                    SetHelpMessage("清理缓存", MessageType.Info);
                    m_log = "清理缓存";
                }
            }
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                const string editorPrefsKey = "ShaderHotSwap_UrlPrefix";
                if (UrlPrefix == null)
                {
                    UrlPrefix = EditorPrefs.GetString(editorPrefsKey, "http://localhost:8090");
                }
                
                string newUrlPrefix = EditorGUILayout.TextField(UrlPrefix, EditorStyles.toolbarTextField);
                if (UrlPrefix != newUrlPrefix)
                {
                    UrlPrefix = newUrlPrefix;
                    EditorPrefs.SetString(editorPrefsKey, UrlPrefix);
                }

                if (GUILayout.Button("切换着色器", m_styles.normalButton))
                {
                    RequestSwapShader();
                }
            }
        }

        
        private string m_log = string.Empty;

        private void ShowLogUI()
        {
            GUILayout.Label("日志:");
            GUILayout.TextArea(m_log, m_styles.log, GUILayout.ExpandHeight(true));
        }

        private void RequestSwapShader()
        {
            m_log = string.Empty;
            SetHelpMessage("查询环境...", MessageType.Info);
            
            ShaderHotSwapClient.QueryEnv(UrlPrefix, (queryEnvResJson, queryEnvError) =>
            {
                if (!string.IsNullOrEmpty(queryEnvError))
                {
                    SetHelpMessage(queryEnvError, MessageType.Error);
                    m_log = queryEnvError;
                    return;
                }

                var queryEnvRes = JsonUtility.FromJson<QueryEnvRes>(queryEnvResJson);
                if (!string.IsNullOrEmpty(queryEnvRes.error))
                {
                    SetHelpMessage(queryEnvRes.error, MessageType.Error);
                    m_log = queryEnvRes.error;
                    return;
                }

                var buildTarget = ConvertRuntimePlatformToBuildTarget(queryEnvRes.platform);
                SetHelpMessage("打包着色器...", MessageType.Info);

                var swapShaderReqJson = ComposeSwapShaderReq(buildTarget, m_shaderDataList);
                SetHelpMessage("切换着色器...", MessageType.Info);

                ShaderHotSwapClient.SwapShaders(UrlPrefix, swapShaderReqJson, (swapShadersResJson, swapShadersError) =>
                {
                    if (!string.IsNullOrEmpty(swapShadersError))
                    {
                        SetHelpMessage(swapShadersError, MessageType.Error);
                        m_log = swapShadersError;
                        return;
                    }

                    var swapShadersRes = JsonUtility.FromJson<SwapShadersRes>(swapShadersResJson);
                    Debug.Log($"{s_logHeader} 切换着色器的远程日志:\n{swapShadersRes.log}");

                    if (!string.IsNullOrEmpty(swapShadersRes.error))
                    {
                        SetHelpMessage(swapShadersRes.error, MessageType.Error);
                        m_log = swapShadersRes.error;
                        return;
                    }

                    SwapShadersResToString(swapShadersRes);

                    SetHelpMessage("成功！", MessageType.Info);
                    Repaint();
                });
            });
        }

        private void SwapShadersResToString(SwapShadersRes res)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("切换着色器 OK。\n");

            foreach (var shader in res.shaders)
            {
                sb.AppendFormat("着色器 <b>[{0}]</b>\n", shader.shader.name);

                foreach (var mat in shader.materials)
                {
                    sb.AppendFormat("  材质 <b>[{0}]</b>\n", mat.material.name);

                    foreach (var renderer in mat.renderers)
                    {
                        sb.AppendFormat("    渲染器 <b>[{0}]</b>\n", renderer.name);
                    }
                }
            }

            m_log = sb.ToString();
        }

        
        private static BuildTarget ConvertRuntimePlatformToBuildTarget(string runtimePlatform)
        {
            BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
            switch (runtimePlatform)
            {
                case "OSXEditor":
                case "OSXPlayer":
                    buildTarget = BuildTarget.StandaloneOSX;
                    break;
                case "WindowsEditor":
                case "WindowsPlayer":
                    buildTarget = BuildTarget.StandaloneWindows;
                    break;
                case "IPhonePlayer":
                    buildTarget = BuildTarget.iOS;
                    break;
                case "Android":
                    buildTarget = BuildTarget.Android;
                    break;
                case "LinuxEditor":
                case "LinuxPlayer":
                    buildTarget = BuildTarget.StandaloneLinux;
                    break;
                case "WebGLPlayer":
                    buildTarget = BuildTarget.WebGL;
                    break;
                case "WSAPlayerX86":
                case "WSAPlayerX64":
                case "WSAPlayerARM":
                    buildTarget = BuildTarget.WSAPlayer;
                    break;
                case "PS4":
                    buildTarget = BuildTarget.PS4;
                    break;
                case "XboxOne":
                    buildTarget = BuildTarget.XboxOne;
                    break;
                case "tvOS":
                    buildTarget = BuildTarget.tvOS;
                    break;
                case "Switch":
                    buildTarget = BuildTarget.Switch;
                    break;
            }

            Debug.Log($"{s_logHeader} 运行时平台: {runtimePlatform} ,构建目标: {buildTarget}");
            return buildTarget;
        }

        private static string ComposeSwapShaderReq(BuildTarget buildTarget, ShaderData[] shaderDataList)
        {
            var req = new SwapShadersReq();
            req.shaders = new List<RemoteShader>();

            foreach (var shaderData in shaderDataList)
            {
                if (shaderData == null)
                {
                    Debug.LogError($"{s_logHeader} shaderData 是空的。");
                    continue;
                }

                req.shaders.Add(new RemoteShader()
                {
                    name = shaderData.shader.name,
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shaderData.shader)),
                });
            }

            if (req.shaders.Count == 0)
            {
                throw new Exception($"{s_logHeader} 没有着色器");
            }

            req.assetBundleBase64 = ShaderPack.PackShaders(buildTarget, GetAssetBundleOutputDir(), shaderDataList);

            var reqContent = JsonUtility.ToJson(req);
            return reqContent;
        }

        private static string GetAssetBundleOutputDir()
        {
            string outputDir = Path.Combine(Application.dataPath, "..");
            outputDir = Path.Combine(outputDir, "Library");
            outputDir = Path.Combine(outputDir, "ShaderHotSwap");
            return outputDir;
        }
        
    }
}