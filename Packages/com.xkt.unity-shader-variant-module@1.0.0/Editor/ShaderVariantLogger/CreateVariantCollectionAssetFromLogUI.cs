using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.UIElements;
using XKT.ShaderVariantStripping.Shared;

namespace XKT.ShaderVariantLogger
{
    /// <summary>
    /// 从日志创建 ShaderVariantCollection
    /// </summary>
    public class CreateVariantCollectionAssetFromLogUI : AbsUIMenuItem
    {
        public override string toolbar => "创建变体集";

        public override int order => 1;

        private const string k_uiTreePath = ConstDefine.k_PackagesRootPath + "Editor/ShaderVariantLogger/UXML/CreateVariantCollectionAssetFromLogUI.uxml";

        private ObjectField m_targetObjectField;
        private Button m_clearExecBtn;

        private Toggle m_includeAssetsToggle;
        private Toggle m_includePackagesToggle;
        private Toggle m_includeBuiltInToggle;
        private Toggle m_includeBuiltInExtraToggle;
        private Toggle m_includeOthersToggle;
        
        private Button m_addExecBtn;
        
        private Button m_openDirBtn;
        private ScrollView m_logListView;

        public override void OnEnable()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_uiTreePath);
            this.rootVisualElement.Add(tree.CloneTree());

            m_targetObjectField = this.rootVisualElement.Q<ObjectField>("TargetAsset");
            m_clearExecBtn = this.rootVisualElement.Q<Button>("ClearExec");

            m_includeAssetsToggle = this.rootVisualElement.Q<Toggle>("IncludeAssets");
            m_includePackagesToggle = this.rootVisualElement.Q<Toggle>("IncludePackages");
            m_includeBuiltInToggle = this.rootVisualElement.Q<Toggle>("IncludeUnityBuiltIn");
            m_includeBuiltInExtraToggle = this.rootVisualElement.Q<Toggle>("IncludeUnityBuiltinExtra");
            m_includeOthersToggle = this.rootVisualElement.Q<Toggle>("IncludeOthers");
            
            m_addExecBtn = this.rootVisualElement.Q<Button>("AddExec");
            
            m_openDirBtn = this.rootVisualElement.Q<Button>("OpenDir");
            m_logListView = this.rootVisualElement.Q<ScrollView>("LogList");

            // 设置 UI
            m_targetObjectField.objectType = typeof(ShaderVariantCollection);
            string svcGuid = EditorShaderVariantLoggerConfig.SvcGuid;
            if (!string.IsNullOrEmpty(svcGuid))
            {
                string svcAssetPath = AssetDatabase.GUIDToAssetPath(svcGuid);
                var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(svcAssetPath);
                if (svc != null)
                {
                    m_targetObjectField.value = svc;
                }
            }
            
            m_includeAssetsToggle.value = EditorShaderVariantLoggerConfig.IncludeAssets;
            m_includePackagesToggle.value = EditorShaderVariantLoggerConfig.IncludePackages;
            m_includeBuiltInToggle.value = EditorShaderVariantLoggerConfig.IncludeUnityBuiltIn;
            m_includeBuiltInExtraToggle.value = EditorShaderVariantLoggerConfig.IncludeUnityBuiltinExtra;
            m_includeOthersToggle.value = EditorShaderVariantLoggerConfig.IncludeOthers;
            
            m_targetObjectField.RegisterValueChangedCallback(OnChangeTargetObjectField);
            m_clearExecBtn.clicked += OnClickClearExec;
            
            m_includeAssetsToggle.RegisterValueChangedCallback(OnChangeIncludeAssetsToggle);
            m_includePackagesToggle.RegisterValueChangedCallback(OnChangeIncludePackagesToggle);
            m_includeBuiltInToggle.RegisterValueChangedCallback(OnChangeIncludeBuiltInToggle);
            m_includeBuiltInExtraToggle.RegisterValueChangedCallback(OnChangeIncludeBuiltInExtraToggle);
            m_includeOthersToggle.RegisterValueChangedCallback(OnChangeIncludeOthersToggle);
            
            m_addExecBtn.clicked += OnClickAddExecute;
            
            m_openDirBtn.clicked += OnClickOpenDirectory;
            SetupLogListView();
        }


        private void OnChangeTargetObjectField(ChangeEvent<UnityEngine.Object> val)
        {
            var svc = val.newValue as ShaderVariantCollection;
            
            if (svc == null)
            {
                EditorShaderVariantLoggerConfig.SvcGuid = "";
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(svc);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            EditorShaderVariantLoggerConfig.SvcGuid = guid;
        }
        
        private void OnClickClearExec()
        {
            var targetAsset = m_targetObjectField.value as ShaderVariantCollection;
            if (targetAsset != null)
            {
                targetAsset.Clear();
                
                EditorUtility.SetDirty(targetAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void OnChangeIncludeAssetsToggle(ChangeEvent<bool> val)
        {
            EditorShaderVariantLoggerConfig.IncludeAssets = val.newValue;
        }

        private void OnChangeIncludePackagesToggle(ChangeEvent<bool> val)
        {
            EditorShaderVariantLoggerConfig.IncludePackages = val.newValue;
        }

        private void OnChangeIncludeBuiltInToggle(ChangeEvent<bool> val)
        {
            EditorShaderVariantLoggerConfig.IncludeUnityBuiltIn = val.newValue;
        }

        private void OnChangeIncludeBuiltInExtraToggle(ChangeEvent<bool> val)
        {
            EditorShaderVariantLoggerConfig.IncludeUnityBuiltinExtra = val.newValue;
        }

        private void OnChangeIncludeOthersToggle(ChangeEvent<bool> val)
        {
            EditorShaderVariantLoggerConfig.IncludeOthers = val.newValue;
        }
        
        private void OnClickAddExecute()
        {
            var targetAsset = m_targetObjectField.value as ShaderVariantCollection;
            if (!targetAsset)
            {
                string filePath = EditorUtility.SaveFilePanelInProject("创建着色器变体文件", "ShaderVariantCollection", "shadervariants", "设置要创建的着色器变体文件");
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                targetAsset = new ShaderVariantCollection();
                ProcessingShaderVariantCollectionAsset(targetAsset);
                
                AssetDatabase.CreateAsset(targetAsset, filePath);
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("完成", "创建着色器变体集合", "ok");
            }
            else
            {
                ProcessingShaderVariantCollectionAsset(targetAsset);
                
                EditorUtility.SetDirty(targetAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("完成", "添加着色器变体到集合中", "ok");
            }
        }
        
        // 处理 ShaderVariantCollection 资源
        private void ProcessingShaderVariantCollectionAsset(ShaderVariantCollection targetAsset)
        {
            if(targetAsset == null) return;
            
            // 遍历日志文件
            var filePaths = GeneralSettingsUI.GetLogFiles();
            for (int i = 0, max = filePaths.Count; i < max; i++)
            {
                string filePath = filePaths[i];
                float progress = (float)i / max;
                
                EditorUtility.DisplayProgressBar("添加到集合", filePath, progress);
                AppendVariantFromLog(targetAsset, filePath);
            }
            EditorUtility.ClearProgressBar();
            
            SetupLogListView(); // 刷新日志列表
        }
        
        private void OnClickOpenDirectory()
        {
            string logSaveDir = EditorShaderVariantLoggerConfig.k_LogSaveDir;
            
            if (!Directory.Exists(logSaveDir))
            {
                Directory.CreateDirectory(logSaveDir);
            }
            
            EditorUtility.RevealInFinder(logSaveDir);
        }
        
        private void SetupLogListView()
        {
            m_logListView.Clear();
            var files = GeneralSettingsUI.GetLogFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                m_logListView.Add(new Label(fileName));
            }
        }
        
        
        /// <summary>
        /// 根据日志，添加变体到集合中
        /// </summary>
        public static void AppendVariantFromLog(ShaderVariantCollection collection, string logFilePath)
        {
            ShaderVariantCollection.ShaderVariant variant;
            
            var lines = File.ReadAllLines(logFilePath);
            for (int i = 1, max = lines.Length; i < max; ++i)
            {
                string line = lines[i];
                
                bool result = GetVariantFromLogLine(line, out variant);
                if (result && !collection.Contains(variant))
                {
                    collection.Add(variant);
                }
            }
        }
        
        /// <summary>
        /// 根据日志，添加变体到集合中
        /// </summary>
        public static void AppendVariantFromLog(ShaderVariantCollection collection, IEnumerable<string> logFilePaths)
        {
            foreach (string logFilePath in logFilePaths)
            {
                AppendVariantFromLog(collection, logFilePath);
            }
        }

        // 从日志中提取变体
        private static bool GetVariantFromLogLine(string line, out ShaderVariantCollection.ShaderVariant variant)
        {
            variant = new ShaderVariantCollection.ShaderVariant();

            var datas = line.Split(',');
            if (datas.Length < 7)
            {
                return false;
            }
            
            var shaderName = datas[1];
            var pass = datas[4];
            var stage = datas[5];
            var keywords = datas[6];
            
            // “Shader.CompileGPUProgram” 的关键字与 “Shader.CreateGPUProgram” 不同
            // 需要做点什么 todo.....
            if (stage == "EditorCompile")
            {
                return false;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                return false;
            }
            
            if (IsExcludedShader(shader))
            {
                return false;
            }

            variant.shader = shader;
            
            var keywordArray = GetKeywordArray(keywords);
            variant.keywords = keywordArray;
            
            var lightMode = ShaderPassLightModeConverter.GetLightModeByPassName(shader, pass);
            var passType = GetPassType(lightMode);
            variant.passType = passType;

            return true;
        }

        private static bool IsExcludedShader(Shader shader)
        {
            string shaderPath = AssetDatabase.GetAssetPath(shader).ToLower();
            
            if (shaderPath.StartsWith("assets/"))
            {
                if (!EditorShaderVariantLoggerConfig.IncludeAssets)
                {
                    return true;
                }
            }
            else if (shaderPath.StartsWith("packages/"))
            {
                if (!EditorShaderVariantLoggerConfig.IncludePackages)
                {
                    return true;
                }
            }
            else if (shaderPath == "resources/unity_builtin")
            {
                if (!EditorShaderVariantLoggerConfig.IncludeUnityBuiltIn)
                {
                    return true;
                }
            }
            else if (shaderPath == "resources/unity_builtin_extra")
            {
                if (!EditorShaderVariantLoggerConfig.IncludeUnityBuiltinExtra)
                {
                    return true;
                }
            }
            else
            {
                Debug.Log("找到的 other 着色器: " + shader.name + "::" + shaderPath);
                
                if (!EditorShaderVariantLoggerConfig.IncludeOthers)
                {
                    return true;
                }
            }

            return false;
        }
        
        private static string[] GetKeywordArray(string keywords)
        {
            string[] keywordArray;
            if (string.IsNullOrEmpty(keywords) || keywords == "<no keywords>")
            {
                keywordArray = new [] { "" };
            }
            else
            {
                keywordArray = keywords.Split(' ');
            }

            return keywordArray;
        }

        private static PassType GetPassType(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return PassType.Normal;
            }

            str = str.ToUpper();
            switch (str)
            {
                case "":
                case "ALWAYS":
                    return PassType.Normal;
                
                case "VERTEX":
                    return PassType.Vertex;
                
                case "VERTEXLM":
                    return PassType.VertexLM;
                
                case "VERTEXLMRGBM":
                    return PassType.ForwardBase;
                
                case "FORWARDADD":
                    return PassType.ForwardAdd;
                
                case "PREPASSBASE":
                    return PassType.LightPrePassBase;
                
                case "PREPASSFINAL":
                    return PassType.LightPrePassFinal;
                
                case "SHADOWCASTER":
                    return PassType.ShadowCaster;
                
                case "DEFERRED":
                    return PassType.Deferred;
                
                case "META":
                    return PassType.Meta;
                
                case "MOTIONVECTORS":
                    return PassType.MotionVectors;
                
                case "SRPDEFAULTUNLIT":
                    return PassType.ScriptableRenderPipelineDefaultUnlit;
            }

            // PassType.ScriptableRenderPipelineDefaultUnlit
            return PassType.ScriptableRenderPipeline;
        }

    }
}
