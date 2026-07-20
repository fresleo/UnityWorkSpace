// Created By: WangYu  Date: 2025-04-10

using System;
using UnityEngine;
using XKT.TOD.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace XKT.TOD.Tag
{
    public abstract class AbsTodTag : MonoBehaviour
    {
        protected virtual void Start()
        {
        }

        protected virtual void Reset()
        {
            UpdateScriptId();
#if UNITY_EDITOR
            UpdateHierarchyPath();
#endif // UNITY_EDITOR
        }

        /// <summary>
        /// 脚本的唯一 id
        /// </summary>
        public string scriptId;
        
#if UNITY_EDITOR
        /// <summary>
        /// hierarchy 路径，只是用来辅助查看的
        /// </summary>
        public string hierarchyPath;
#endif // UNITY_EDITOR

        public void UpdateScriptId()
        {
            scriptId = Guid.NewGuid().ToString();
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        public void UpdateHierarchyPath()
        {
            this.hierarchyPath = TODUtils.GetHierarchyPath(this.transform);
        }
#endif // UNITY_EDITOR
        
    }
}