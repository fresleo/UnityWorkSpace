// Created By: WangYu  Date: 2023-11-23

using UnityEditor;

namespace com.xknight.mt.Lib.Editor.MT.Utils
{
    public abstract class AbsInspector<TScriptType> : UnityEditor.Editor 
        where TScriptType : UnityEngine.Object
    {
        protected virtual void OnEnable()
        {
            var obj = base.target;
            var script = obj as TScriptType;
            if (script != null)
            {
                ExecuteOnEnable(script);
            }
        }
        
        public override void OnInspectorGUI()
        {
            var obj = base.target;
            var script = obj as TScriptType;
            if (script != null)
            {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                
                DrawAutoApplyGUI(script);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(obj, $"对象 \"{obj.name}\" 上的 \"{obj.GetType()}\" 脚本发生改变");
                    EditorUtility.SetDirty(obj);
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
            if (this.DrawBaseInspectorGUI) base.OnInspectorGUI();
        }
        
        protected void OnSceneGUI()
        {
            //因为是渲染被选中的对象，所以只能处理1个目标
            var obj = base.target;
            var script = obj as TScriptType;
            if (script == null) return;
            
            ExecuteOnSceneGUI(script);
        }
        
        
        /// <summary>
        /// 是否绘制基类的检视GUI
        /// </summary>
        protected virtual bool DrawBaseInspectorGUI => false;
        
        /// <summary>
        /// 确保有脚本的 OnEnable 事件
        /// </summary>
        protected virtual void ExecuteOnEnable(TScriptType script)
        {
        }
        
        /// <summary>
        /// 绘制能自动 Apply 的 GUI
        /// </summary>
        protected abstract void DrawAutoApplyGUI(TScriptType script);
        
        /// <summary>
        /// 确保有脚本的 OnSceneGUI 事件
        /// </summary>
        protected virtual void ExecuteOnSceneGUI(TScriptType script)
        {
        }
        
    }
}