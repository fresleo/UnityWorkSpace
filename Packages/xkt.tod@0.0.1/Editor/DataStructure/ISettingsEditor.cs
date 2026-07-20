// Created by: WangYu   Date: 2025-11-03

using UnityEditor;

namespace XKT.TOD.DataStructure
{
    public interface ISettingsEditor
    {
        SerializedProperty Target { get; }
        
        void Enable();
        void InspectorGUI();
    }
}