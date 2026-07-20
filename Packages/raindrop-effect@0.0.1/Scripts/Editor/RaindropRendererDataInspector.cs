// Created By: WangYu  Date: 2024-11-19

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaindropEffect
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RaindropRendererData))]
    public class RaindropRendererDataInspector : Editor
    {
        private RaindropRendererData m_script;
        
        private void OnEnable()
        {
            m_script = this.target as RaindropRendererData;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("重新加载渲染资源"))
            {
                m_script.Cleanup();
                ResourceReloader.ReloadAllNullIn(m_script, RaindropRendererData.c_packagePath);
            }
            
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}