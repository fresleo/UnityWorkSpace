using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace XKT.ShaderVariantStripping
{
    /// <summary>
    /// 变体集合 UI 元素
    /// </summary>
    public class VariantCollectionUI : VisualElement
    {
        private Action<VariantCollectionUI> m_onChangeData, m_onRemoveData;
        
        private ObjectField m_objField;
        private Button m_removeBtn;
        
        /// <summary>
        /// 着色器变体集合
        /// </summary>
        public ShaderVariantCollection variantCollection
        {
            get => m_objField.value as ShaderVariantCollection;
            set => m_objField.value = value;
        }

        /// <summary>
        /// 列表的索引
        /// </summary>
        public int ListIndex { set; get; }


        public VariantCollectionUI(Action<VariantCollectionUI> onChange, Action<VariantCollectionUI> onRemove)
        {
            m_onChangeData = onChange;
            m_onRemoveData = onRemove;
            
            this.style.flexDirection = FlexDirection.Row;

            m_objField = new ObjectField();
            m_objField.objectType = typeof(ShaderVariantCollection);
            m_objField.RegisterValueChangedCallback(OnValueChange);
            this.Add(m_objField);

            m_removeBtn = new Button();
            m_removeBtn.style.width = 20;
            m_removeBtn.text = "X";
            m_removeBtn.clicked += OnClickRemove;
            this.Add(m_removeBtn);
        }

        private void OnValueChange(ChangeEvent<UnityEngine.Object> obj)
        {
            m_onChangeData?.Invoke(this);
        }
        
        private void OnClickRemove()
        {
            m_onRemoveData?.Invoke(this);
        }
        
    }
}