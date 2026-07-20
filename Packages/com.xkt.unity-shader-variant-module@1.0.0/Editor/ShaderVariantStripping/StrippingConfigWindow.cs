using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using XKT.ShaderVariantStripping.Shared;

namespace XKT.ShaderVariantStripping
{
    /// <summary>
    /// 着色器变体剥离配置窗口
    /// </summary>
    internal class StrippingConfigWindow : EditorWindow
    {
        private const string k_windowTitle = "着色器变体剥离";
        private const string k_MenuName = "Window/Rendering/XKT/" + k_windowTitle;
        [MenuItem(k_MenuName)]
        public static void Create()
        {
            GetWindow<StrippingConfigWindow>();
        }


        private const string k_uiTreePath = ConstDefine.k_PackagesRootPath + "Editor/ShaderVariantStripping/UXML/ConfigUI.uxml";
        
        private Toggle m_enabledToggle;
        
        private Toggle m_logEnableToggle;
        private Button m_resetInitializeBtn;
        private Button m_clearLogBtn;
        
        private Toggle m_strictVariantStrippingToggle;
        private Toggle m_disableUnityStripToggle;
        
        private IntegerField m_executeOrderIntegerField;
        private Button m_execOrderMinBtn;
        private Button m_execOrderMaxBtn;

        private Toggle m_ignoreStgeOnlyKeyword;
        
        private Button m_debugListProcessorBtn;
        private Button m_debugShaderKeywords;

        private Button m_appendExcludeBtn;
        private ListView m_excludeList;
        
        private List<ShaderVariantCollection> m_excludeSvcs;

        private void OnDisable()
        {
            EditorStripShaderVariantConfig.SetExcludeVariantCollection(m_excludeSvcs);
        }
        
        private void OnEnable()
        {
            this.titleContent = new GUIContent(k_windowTitle);
            
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_uiTreePath);
            this.rootVisualElement.Add(tree.CloneTree());

            m_enabledToggle = this.rootVisualElement.Q<Toggle>("Enabled");
            
            m_logEnableToggle = this.rootVisualElement.Q<Toggle>("LogEnable");
            m_resetInitializeBtn = this.rootVisualElement.Q<Button>("ResetInitializeBtn");
            m_clearLogBtn = this.rootVisualElement.Q<Button>("ClearLogBtn");
            
            m_strictVariantStrippingToggle = this.rootVisualElement.Q<Toggle>("StrictVariantStripping");
            m_disableUnityStripToggle = this.rootVisualElement.Q<Toggle>("DisableUnityStrip");
            
            m_executeOrderIntegerField = this.rootVisualElement.Q<IntegerField>("ExecuteOrder");
            m_execOrderMinBtn = this.rootVisualElement.Q<Button>("ExecOrderMinBtn");
            m_execOrderMaxBtn = this.rootVisualElement.Q<Button>("ExecOrderMaxBtn");
            
            m_ignoreStgeOnlyKeyword = this.rootVisualElement.Q<Toggle>("IgnoreStgeOnlyKeyword");
            
            m_debugListProcessorBtn = this.rootVisualElement.Q<Button>("DebugListProcessorBtn");
            m_debugShaderKeywords = this.rootVisualElement.Q<Button>("DebugShaderKeywords");
            
            m_excludeList = this.rootVisualElement.Q<ListView>("ExcludeList");
            m_appendExcludeBtn = this.rootVisualElement.Q<Button>("AppendExcludeBtn");
            
            // 设置 UI
            m_enabledToggle.SetValueWithoutNotify(EditorStripShaderVariantConfig.IsEnable);
            m_logEnableToggle.SetValueWithoutNotify(EditorStripShaderVariantConfig.IsLogEnable);
            m_strictVariantStrippingToggle.SetValueWithoutNotify(EditorStripShaderVariantConfig.StrictVariantStripping);
            m_disableUnityStripToggle.SetValueWithoutNotify(EditorStripShaderVariantConfig.DisableUnityStrip);
            m_executeOrderIntegerField.SetValueWithoutNotify(EditorStripShaderVariantConfig.Order);
            m_ignoreStgeOnlyKeyword.SetValueWithoutNotify(EditorStripShaderVariantConfig.IgnoreStageOnlyKeyword);

            m_enabledToggle.RegisterValueChangedCallback(OnChangeEnableToggle);
            m_logEnableToggle.RegisterValueChangedCallback(OnChangeLogEnableToggle);
            m_strictVariantStrippingToggle.RegisterValueChangedCallback(OnChangeStrictModeToggle);
            m_disableUnityStripToggle.RegisterValueChangedCallback(OnChangeDisableUnityStripToggle);
            m_ignoreStgeOnlyKeyword.RegisterValueChangedCallback(OnChangeIgnoreStageOnlyKeywordToggle);

            m_resetInitializeBtn.clicked += OnClickResetInitializeBtn;
            m_clearLogBtn.clicked += OnClickClearLogBtn;
            
            m_executeOrderIntegerField.RegisterCallback<FocusOutEvent>(OnLostFocusIntField);
            m_execOrderMinBtn.clicked += OnClickMinButton;
            m_execOrderMaxBtn.clicked += OnClickMaxButton;
            
            m_debugListProcessorBtn.clicked += OnClickDebugListViewBtn;
            m_debugShaderKeywords.clicked += OnClickShaderKeywordDebugBtn;
            
            m_appendExcludeBtn.clicked += OnClickAddExclude;

            SetUIActiveAtEnabled(m_enabledToggle.value);
            SetUIActiveAtStrictMode(m_strictVariantStrippingToggle.value);
            SetupExcludeRules();
            
            StrippingByVariantCollection.ResetInitialize();
        }
        
        private void SetUIActiveAtEnabled(bool enabled)
        {
            m_strictVariantStrippingToggle.SetEnabled(enabled);
            m_disableUnityStripToggle.SetEnabled(enabled);
            
            m_executeOrderIntegerField.SetEnabled(enabled);
            m_execOrderMinBtn.SetEnabled(enabled);
            m_execOrderMaxBtn.SetEnabled(enabled);
            
            m_ignoreStgeOnlyKeyword.SetEnabled(enabled);
        }

        private void SetUIActiveAtStrictMode(bool enabled)
        {
            m_disableUnityStripToggle.SetEnabled(enabled);
            m_disableUnityStripToggle.SetValueWithoutNotify(EditorStripShaderVariantConfig.DisableUnityStrip);
        }
        
        private void SetupExcludeRules()
        {
            m_excludeSvcs = EditorStripShaderVariantConfig.GetExcludeVariantCollectionAsset();
            
            m_excludeList.fixedItemHeight = 20;
            m_excludeList.reorderable = true;
            m_excludeList.makeItem = () =>
            {
                var itemUI = new VariantCollectionUI(OnChangeExcludeValue, OnRemoveExclude);
                return itemUI;
            };
            m_excludeList.bindItem = (ve, i) =>
            {
                var itemUI = (ve as VariantCollectionUI);
                itemUI.variantCollection = m_excludeSvcs[i];
                itemUI.ListIndex = i;
            };
            m_excludeList.itemsSource = m_excludeSvcs;

            RefreshExcludeUI();
        }
        
        private void RefreshExcludeUI()
        {
            m_excludeList.Rebuild();
            if (m_excludeSvcs.Count == 0)
            {
                m_excludeList.style.height = m_excludeList.fixedItemHeight;
            }
            else
            {
                m_excludeList.style.height = m_excludeList.fixedItemHeight * m_excludeSvcs.Count;
            }
        }


        private void OnChangeEnableToggle(ChangeEvent<bool> val)
        {
            EditorStripShaderVariantConfig.IsEnable = val.newValue;
            SetUIActiveAtEnabled(val.newValue);
        }

        private void OnChangeLogEnableToggle(ChangeEvent<bool> val)
        {
            EditorStripShaderVariantConfig.IsLogEnable = val.newValue;
        }

        private void OnChangeStrictModeToggle(ChangeEvent<bool> val)
        {
            EditorStripShaderVariantConfig.StrictVariantStripping = val.newValue;
            SetUIActiveAtStrictMode(val.newValue);
        }

        private void OnChangeDisableUnityStripToggle(ChangeEvent<bool> val)
        {
            EditorStripShaderVariantConfig.DisableUnityStrip = val.newValue;
        }

        private void OnChangeIgnoreStageOnlyKeywordToggle(ChangeEvent<bool> val)
        {
            EditorStripShaderVariantConfig.IgnoreStageOnlyKeyword = val.newValue;
        }


        private void OnClickResetInitializeBtn()
        {
            StrippingByVariantCollection.ResetInitialize();
        }

        private void OnClickClearLogBtn()
        {
            string logDirectory = StrippingByVariantCollection.k_LogDirectory;
            if (System.IO.Directory.Exists(logDirectory))
            {
                System.IO.Directory.Delete(logDirectory, true);
            }
        }
        
        private void OnLostFocusIntField(FocusOutEvent evt)
        {
            EditorStripShaderVariantConfig.Order = m_executeOrderIntegerField.value;
        }

        private void OnClickMinButton()
        {
            EditorStripShaderVariantConfig.Order = int.MinValue;
            m_executeOrderIntegerField.SetValueWithoutNotify(int.MinValue);
        }

        private void OnClickMaxButton()
        {
            EditorStripShaderVariantConfig.Order = int.MaxValue;
            m_executeOrderIntegerField.SetValueWithoutNotify(int.MaxValue);
        }
        
        private void OnClickDebugListViewBtn()
        {
            ListShaderPreProcessClasses.ShowDebugWindow();
        }

        private void OnClickShaderKeywordDebugBtn()
        {
            CreateWindow<ShaderKeywordDebugWindow>();
        }

        private void OnClickAddExclude()
        {
            m_excludeSvcs.Add(null);
            RefreshExcludeUI();
        }
        
        // VariantCollectionUI 的回调事件 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        private void OnChangeExcludeValue(VariantCollectionUI itemUI)
        {
            m_excludeSvcs[itemUI.ListIndex] = itemUI.variantCollection;
            EditorStripShaderVariantConfig.SetExcludeVariantCollection(m_excludeSvcs);
        }
        
        private void OnRemoveExclude(VariantCollectionUI itemUI)
        {
            m_excludeSvcs.RemoveAt(itemUI.ListIndex);
            RefreshExcludeUI();
            EditorStripShaderVariantConfig.SetExcludeVariantCollection(m_excludeSvcs);
        }
        
    }
}