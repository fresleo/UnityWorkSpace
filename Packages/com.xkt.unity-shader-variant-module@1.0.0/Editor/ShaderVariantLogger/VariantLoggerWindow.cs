using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace XKT.ShaderVariantLogger
{
    /// <summary>
    /// 变体记录器窗口
    /// </summary>
    public class VariantLoggerWindow : EditorWindow
    {
        private const string k_windowTitle = "着色器变体记录";
        private const string k_MenuName = "Window/Rendering/XKT/" + k_windowTitle;
        [MenuItem(k_MenuName)]
        public static void Create()
        {
            GetWindow<VariantLoggerWindow>();
        }
        
        
        private static List<System.Type> s_menuItemTypes = null;

        // 构筑菜单项目类型列表
        private static List<System.Type> ConstructMenuItemTypes()
        {
            var itemTypes = new List<System.Type>();

            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in asms)
            {
                var types = asm.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsSubclassOf(typeof(AbsUIMenuItem)))
                    {
                        itemTypes.Add(type);
                    }
                }
            }

            return itemTypes;
        }

        private List<AbsUIMenuItem> m_uiMenuItems;
        private List<ToolbarToggle> m_uiToolbarToggles;
        private Dictionary<ToolbarToggle, VisualElement> m_uiItemBody;

        private void OnEnable()
        {
            this.titleContent = new GUIContent(k_windowTitle);

            if (s_menuItemTypes == null)
            {
                s_menuItemTypes = ConstructMenuItemTypes();
            }

            m_uiMenuItems = new List<AbsUIMenuItem>();
            foreach (var menuType in s_menuItemTypes)
            {
                var menu = System.Activator.CreateInstance(menuType) as AbsUIMenuItem;
                if (menu == null || !menu.enabled)
                {
                    continue;
                }

                m_uiMenuItems.Add(menu);
                menu.parent = this;
                try
                {
                    menu.OnEnable();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            m_uiMenuItems.Sort((left, right) => { return left.order - right.order; });

            var toolBar = new Toolbar();
            this.rootVisualElement.Add(toolBar);

            m_uiToolbarToggles = new List<ToolbarToggle>();
            m_uiItemBody = new Dictionary<ToolbarToggle, VisualElement>();

            int idx = 0;
            foreach (var menu in m_uiMenuItems)
            {
                var toggle = new ToolbarToggle();
                toggle.text = menu.toolbar;
                toggle.userData = menu;
                toggle.RegisterValueChangedCallback(OnChangeToolBar);
                toolBar.Add(toggle);

                m_uiToolbarToggles.Add(toggle);

                // 激活第一个 tab 按钮
                bool isFirst = idx == 0;
                toggle.SetValueWithoutNotify(isFirst);
                menu.rootVisualElement.visible = isFirst;
                menu.rootVisualElement.style.display = isFirst ? DisplayStyle.Flex : DisplayStyle.None;

                this.rootVisualElement.Add(menu.rootVisualElement);
                m_uiItemBody.Add(toggle, menu.rootVisualElement);

                ++idx;
            }
        }

        // 广播消息
        internal void BroadCastMessage(object obj, AbsUIMenuItem src)
        {
            if (m_uiMenuItems == null)
            {
                return;
            }

            foreach (var item in m_uiMenuItems)
            {
                // 不用广播给自己
                if (item == src) continue;

                item.OnReceiveMessage(obj);
            }
        }

        private void OnChangeToolBar(ChangeEvent<bool> itemValue)
        {
            var target = itemValue.target as ToolbarToggle;
            if(target == null) return;
            
            // 切换 tab 按钮的显隐
            if (!itemValue.newValue)
            {
                target.SetValueWithoutNotify(true);
                return;
            }

            // 切换 body 的显隐
            foreach (var toolbar in m_uiToolbarToggles)
            {
                if (target == toolbar) continue;

                toolbar.SetValueWithoutNotify(false);
                
                var body = m_uiItemBody[toolbar];
                if (body != null)
                {
                    body.visible = false;
                    body.style.display = DisplayStyle.None;
                }
            }

            var targetBody = m_uiItemBody[target];
            if (targetBody != null)
            {
                targetBody.visible = true;
                targetBody.style.display = DisplayStyle.Flex;
            }
        }
        
    }
}