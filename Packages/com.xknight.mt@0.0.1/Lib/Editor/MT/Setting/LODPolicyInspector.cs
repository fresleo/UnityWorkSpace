// Created By: WangYu  Date: 2023-12-21

using com.xknight.mt.Lib.Editor.MT.Utils;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using UnityEditor;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.Setting
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LODPolicy))]
    public class LODPolicyInspector : AbsInspector<LODPolicy>
    {
        private SerializedProperty m_screenCover_prop;

        protected override void ExecuteOnEnable(LODPolicy script)
        {
            base.ExecuteOnEnable(script);
            
            m_screenCover_prop = serializedObject.FindProperty(nameof(script.screenCover));
        }

        protected override void DrawAutoApplyGUI(LODPolicy script)
        {
            EditorGUILayout.HelpBox(new GUIContent("这个值应该根据实际情况，进行灵活调整"));
            
            EditorGUILayout.PropertyField(m_screenCover_prop, new GUIContent("屏幕覆盖率与LOD的对应关系"));
            EditorGUILayout.Space(5);
        }
        
    }
}