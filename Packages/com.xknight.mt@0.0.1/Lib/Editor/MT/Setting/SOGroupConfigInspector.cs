// Created By: WangYu  Date: 2024-01-09

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Setting
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SOGroupConfig))]
    public class SOGroupConfigInspector : AbsInspector<SOGroupConfig>
    {
        private SerializedProperty m_treeDataPath_prop;
        
        protected override void ExecuteOnEnable(SOGroupConfig script)
        {
            base.ExecuteOnEnable(script);

            m_treeDataPath_prop = serializedObject.FindProperty(nameof(script.treeDataPath));
        }

        protected override void DrawAutoApplyGUI(SOGroupConfig script)
        {
            EditorGUILayout.PropertyField(m_treeDataPath_prop, new GUIContent("4叉树数据"));
            EditorGUILayout.Space(5);
        }
    }
}