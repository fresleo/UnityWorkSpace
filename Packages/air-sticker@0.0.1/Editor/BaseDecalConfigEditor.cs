// Created By: WangYu  Date: 2025-04-28

using System;
using AirSticker.Runtime.Render;
using UnityEditor;

namespace AirSticker
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BaseDecalConfig))]
    public class BaseDecalConfigEditor : Editor
    {
        private BaseDecalConfig CurrentTarget => this.target as BaseDecalConfig;

        private BaseDecalConfigGUI m_gui;
        private MaterialGUI m_matGui;

        private void OnEnable()
        {
            if(CurrentTarget == null) return;

            m_gui = new BaseDecalConfigGUI(CurrentTarget);
            m_matGui = new MaterialGUI();
        }

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            if(CurrentTarget == null) return;
            
            m_gui.OnInspectorGUI();
            m_matGui.OnInspectorGUI(this.CurrentTarget.material);
        }
    }
}