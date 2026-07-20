using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace XKT.ShaderVariantStripping
{
    /// <summary>
    /// 着色器关键字调试窗口
    /// </summary>
    public class ShaderKeywordDebugWindow : EditorWindow
    {
        private ScrollView m_keywordsView;

        private void OnEnable()
        {
            this.titleContent = new GUIContent("着色器关键字调试窗口");
            
            var objField = new ObjectField("目标着色器");
            objField.objectType = typeof(Shader);
            objField.RegisterValueChangedCallback(OnChangeShader);
            this.rootVisualElement.Add(objField);

            m_keywordsView = new ScrollView();
            this.rootVisualElement.Add(m_keywordsView);
            
            // 挤个位置，好看
            var spacer = new VisualElement();
            spacer.style.height = 20;
            this.rootVisualElement.Add(spacer);
        }

        private void OnChangeShader(ChangeEvent<Object> evt)
        {
            m_keywordsView.Clear();

            var newShader = evt.newValue as Shader;
            if (newShader == null)
            {
                return;
            }

            var getter = new ShaderKeywordMaskGetter(newShader);
            SetupKeywordList(getter);
        }
        
        
        #region 关键字列表

        private void SetupKeywordList(ShaderKeywordMaskGetter getter)
        {
            var listResultElement = new VisualElement();
            listResultElement.style.flexDirection = FlexDirection.Row;
            listResultElement.style.marginLeft = 20;
            listResultElement.style.marginTop = 10;
            
            var keywordsElement = new VisualElement();
            var vertResultElement = new VisualElement();
            var fragResultElement = new VisualElement();
            var geometryResultElement = new VisualElement();
            var hullResultElement = new VisualElement();
            var domainResultElement = new VisualElement();
            var raytraceResultElement = new VisualElement();

            // 设置边框
            SetupBoarder(keywordsElement);
            SetupBoarder(vertResultElement);
            SetupBoarder(fragResultElement);
            SetupBoarder(geometryResultElement);
            SetupBoarder(hullResultElement);
            SetupBoarder(domainResultElement);
            SetupBoarder(raytraceResultElement, true);

            // 表头
            keywordsElement.Add(CreateTableElementLabel("关键字"));
            vertResultElement.Add(CreateTableElementLabel("顶点"));
            fragResultElement.Add(CreateTableElementLabel("片元"));
            geometryResultElement.Add(CreateTableElementLabel("geometry"));
            hullResultElement.Add(CreateTableElementLabel("hull"));
            domainResultElement.Add(CreateTableElementLabel("domain"));
            raytraceResultElement.Add(CreateTableElementLabel("raytrace"));

            var allKeywords = getter.allKeywords;
            foreach (var keyword in allKeywords)
            {
                keywordsElement.Add(CreateTableElementLabel(keyword));
                
                vertResultElement.Add(CreateYesNoLabel(getter.IsUsedForVertexProgram(keyword)));
                fragResultElement.Add(CreateYesNoLabel(getter.IsUsedForFragmentProgram(keyword)));
                geometryResultElement.Add(CreateYesNoLabel(getter.IsUsedForGeometryProgram(keyword)));
                hullResultElement.Add(CreateYesNoLabel(getter.IsUsedForHullProgram(keyword)));
                domainResultElement.Add(CreateYesNoLabel(getter.IsUsedForDomainProgram(keyword)));
                raytraceResultElement.Add(CreateYesNoLabel(getter.IsUsedForRaytraceProgram(keyword)));
            }
            
            listResultElement.Add(keywordsElement);
            listResultElement.Add(vertResultElement);
            listResultElement.Add(fragResultElement);
            listResultElement.Add(geometryResultElement);
            listResultElement.Add(hullResultElement);
            listResultElement.Add(domainResultElement);
            listResultElement.Add(raytraceResultElement);

            m_keywordsView.Add(listResultElement);
        }

        private void SetupBoarder(VisualElement elem, bool isRight = false)
        {
            if (isRight)
            {
                elem.style.borderRightWidth = 3;
                elem.style.borderRightColor = Color.white;
            }

            elem.style.borderLeftWidth = 3;
            elem.style.borderLeftColor = Color.white;
        }
        
        private Label CreateTableElementLabel(string str)
        {
            Label label = new Label(str);
            label.style.paddingLeft = 10;
            label.style.paddingRight = 10;
            label.style.paddingTop = 5;
            label.style.paddingBottom = 5;

            label.style.borderTopWidth = 1;
            label.style.borderBottomWidth = 1;
            label.style.borderTopColor = Color.white;
            label.style.borderBottomColor = Color.white;
            return label;
        }
        
        private VisualElement CreateYesNoLabel(bool flag)
        {
            if (flag)
            {
                var label = CreateTableElementLabel("TRUE");
                label.style.color = Color.red;
                return label;
            }
            else
            {
                var label = CreateTableElementLabel("FALSE");
                label.style.color = Color.blue;
                return label;
            }
        }

        #endregion 关键字列表
        
    }
}