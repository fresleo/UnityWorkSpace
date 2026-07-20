// Created by: WangYu   Date: 2025-10-09

using UnityEngine.UIElements;

namespace XKT.ShaderVariantLogger
{
    /// <summary>
    /// 菜单分页项
    /// </summary>
    public abstract class AbsUIMenuItem
    {
        public abstract string toolbar { get; }
        public abstract int order { get; }

        public virtual bool enabled => true;

        private VisualElement m_rootVisualElement;

        public VisualElement rootVisualElement
        {
            get
            {
                if (m_rootVisualElement == null)
                {
                    m_rootVisualElement = new VisualElement();
                }

                return m_rootVisualElement;
            }
        }

        public VariantLoggerWindow parent { get; set; }

        public abstract void OnEnable();

        protected void BroadCastMessage(object obj)
        {
            if (parent != null)
            {
                parent.BroadCastMessage(obj, this);
            }
        }

        public virtual void OnReceiveMessage(object obj)
        {
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            AbsUIMenuItem item = obj as AbsUIMenuItem;
            if (item == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            return true;
        }
        
    }
}