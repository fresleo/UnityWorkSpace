/*******************************************************************************
 * File: TimeOfDayEditorTabBase.cs
 * Author: Codex
 * Date: 2026/04/24
 * Description: TOD 编辑窗口页签基类，统一页签生命周期和绘制接口。
 *******************************************************************************/

using UnityEngine;

namespace XKT.TOD
{
    /// <summary>
    /// TOD 编辑窗口页签基类。
    /// </summary>
    internal abstract class TimeOfDayEditorTabBase
    {
        protected TimeOfDayEditorWindow Window { get; private set; }

        /// <summary>
        /// 当前页签标题。
        /// </summary>
        public abstract GUIContent Title { get; }

        /// <summary>
        /// 当前页签提示文案。
        /// </summary>
        public abstract GUIContent Tip { get; }

        /// <summary>
        /// 将页签绑定到主窗口。
        /// </summary>
        public void Bind(TimeOfDayEditorWindow window)
        {
            Window = window;
        }

        /// <summary>
        /// 页签启用时调用。
        /// </summary>
        public virtual void OnEnable()
        {
        }

        /// <summary>
        /// 页签销毁时调用。
        /// </summary>
        public virtual void OnDisable()
        {
        }

        /// <summary>
        /// Inspector 更新时调用。
        /// </summary>
        public virtual void OnInspectorUpdate()
        {
        }

        /// <summary>
        /// Selection 变化时调用。
        /// </summary>
        public virtual void OnSelectionChange()
        {
        }

        /// <summary>
        /// Hierarchy 变化时调用。
        /// </summary>
        public virtual void OnHierarchyChange()
        {
        }

        /// <summary>
        /// 绘制页签界面。
        /// </summary>
        public abstract void OnGUI();
    }
}
