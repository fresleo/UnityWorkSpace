using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using XKT.ShaderVariantStripping.Shared;

namespace XKT.ShaderVariantLogger
{
    /// <summary>
    /// 通用设置
    /// </summary>
    public class GeneralSettingsUI : AbsUIMenuItem
    {
        public override string toolbar => "通用";

        public override int order => 0;

        private const string k_uiTreePath = ConstDefine.k_PackagesRootPath + "Editor/ShaderVariantLogger/UXML/GeneralSettingsUI.uxml";
        
        private Toggle m_loggerEnableToggle;
        
        private Toggle m_clearShaderCacheToggle;
        private TextField m_fileHeaderTextField;
        
        private Button m_openDirBtn, m_clearLogsBtn;
        
        private ScrollView m_logListView;
        
        public override void OnEnable()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_uiTreePath);
            this.rootVisualElement.Add(tree.CloneTree());
            
            m_loggerEnableToggle = this.rootVisualElement.Q<Toggle>("LoggerEnable");
            
            m_clearShaderCacheToggle = this.rootVisualElement.Q<Toggle>("ClearShaderCache");
            m_fileHeaderTextField = this.rootVisualElement.Q<TextField>("FileHeader");
            
            m_openDirBtn = this.rootVisualElement.Q<Button>("OpenDir");
            m_clearLogsBtn = this.rootVisualElement.Q<Button>("ClearLogs");
            
            m_logListView = this.rootVisualElement.Q<ScrollView>("LogList"); 
            
            // 设置 UI
            m_loggerEnableToggle.value = EditorShaderVariantLoggerConfig.Enabled;
            m_clearShaderCacheToggle.value = EditorShaderVariantLoggerConfig.ClearShaderCache;
            m_fileHeaderTextField.value = EditorShaderVariantLoggerConfig.FileHeader;
            
            SetupLogListView();
            
            m_loggerEnableToggle.RegisterValueChangedCallback(OnChangeLoggerEnableToggle);
            
            m_clearShaderCacheToggle.RegisterValueChangedCallback(OnChangeClearShaderCacheToggle);
            
            m_fileHeaderTextField.RegisterCallback<FocusOutEvent>(OnChangeFileHeaderTextField);
            
            m_openDirBtn.clicked += OnClickOpenDirBtn;
            m_clearLogsBtn.clicked += OnClickClearLogsBtn;
        }

        private void SetupLogListView()
        {
            m_logListView.Clear();
            var files = GetLogFiles();
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                m_logListView.Add(new Label(fileName));
            }
        }
        
        /// <summary>
        /// 获取所有日志文件地址
        /// </summary>
        public static List<string> GetLogFiles()
        {
            string logSaveDir = EditorShaderVariantLoggerConfig.k_LogSaveDir;
            
            if (!Directory.Exists(logSaveDir))
            {
                return new List<string>();
            }
            
            var files = Directory.GetFiles(logSaveDir);
            var list = new List<string>(files.Length);
            foreach (var file in files)
            {
                list.Add(file);
            }
            list.Sort();
            
            return list;
        }
        
        private void OnChangeLoggerEnableToggle(ChangeEvent<bool> val)
        {
            EditorShaderVariantLoggerConfig.Enabled = val.newValue;
        }
        
        private void OnChangeClearShaderCacheToggle(ChangeEvent<bool> val)
        {
            EditorShaderVariantLoggerConfig.ClearShaderCache = val.newValue;
        }

        private void OnChangeFileHeaderTextField(FocusOutEvent evt)
        {
            EditorShaderVariantLoggerConfig.FileHeader = m_fileHeaderTextField.value;
        }
        
        private void OnClickOpenDirBtn()
        {
            string logSaveDir = EditorShaderVariantLoggerConfig.k_LogSaveDir;
            
            if (!Directory.Exists(logSaveDir))
            {
                Directory.CreateDirectory(logSaveDir);
            }
            
            EditorUtility.RevealInFinder(logSaveDir);
        }

        private void OnClickClearLogsBtn()
        {
            string logSaveDir = EditorShaderVariantLoggerConfig.k_LogSaveDir;

            if (Directory.Exists(logSaveDir))
            {
                Directory.Delete(logSaveDir, true);
            }

            SetupLogListView();
        }
        
    }
}
