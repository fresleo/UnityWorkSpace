// Created By: WangYu  Date: 2024-01-09

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Setting
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(IOGroupConfig))]
    public class IOGroupConfigInspector : AbsInspector<IOGroupConfig>
    {
        private SerializedProperty m_dataType_prop;
        private SerializedProperty m_byteDataPath_prop;
        
        protected override void ExecuteOnEnable(IOGroupConfig script)
        {
            base.ExecuteOnEnable(script);

            m_dataType_prop = serializedObject.FindProperty(nameof(script.dataType));
            m_byteDataPath_prop = serializedObject.FindProperty(nameof(script.byteDataPath));
        }

        protected override void DrawAutoApplyGUI(IOGroupConfig script)
        {
            EditorGUILayout.PropertyField(m_dataType_prop, new GUIContent("数据类型"));
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(m_byteDataPath_prop, new GUIContent("2进制数据"));
            EditorGUILayout.Space(5);
        }
    }
}