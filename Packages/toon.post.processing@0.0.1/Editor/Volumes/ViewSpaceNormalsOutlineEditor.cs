// Created By: WangYu  Date: 2024-08-05

using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace ToonPostProcessing.Volumes
{
    [CustomEditor(typeof(ViewSpaceNormalsOutline))]
    public class ViewSpaceNormalsOutlineEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_outlineColor;

        private SerializedDataParameter m_outlineDistanceFade;
        private SerializedDataParameter m_outlineScale;
        private SerializedDataParameter m_depthThreshold, m_depthDiffMultiplier;
        private SerializedDataParameter m_normalThreshold;
        private SerializedDataParameter m_steepAngleThreshold, m_steepAngleMultiplier;
        private SerializedDataParameter m_enableAntiAliasing;
        
        
        public override void OnEnable()
        {
            var o = new PropertyFetcher<ViewSpaceNormalsOutline>(serializedObject);
            
            m_outlineColor = Unpack(o.Find(x => x.outlineColor));
            
            m_outlineDistanceFade = Unpack(o.Find(x => x.outlineDistanceFade));
            m_outlineScale = Unpack(o.Find(x => x.outlineScale));
            
            m_depthThreshold = Unpack(o.Find(x => x.depthThreshold));
            m_depthDiffMultiplier = Unpack(o.Find(x => x.depthDiffMultiplier));
            
            m_normalThreshold = Unpack(o.Find(x => x.normalThreshold));
            
            m_steepAngleThreshold = Unpack(o.Find(x => x.steepAngleThreshold));
            m_steepAngleMultiplier = Unpack(o.Find(x => x.steepAngleMultiplier));
            
            m_enableAntiAliasing = Unpack(o.Find(x => x.enableAntiAliasing));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_outlineColor, new GUIContent("颜色"));
            
            EditorGUILayout.Space();
            PropertyField(m_outlineDistanceFade, new GUIContent("摄像机的有效距离（单位米）"));
            PropertyField(m_outlineScale, new GUIContent("描边比例"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("描边过滤设置");
            EditorGUI.indentLevel++;
            PropertyField(m_depthThreshold, new GUIContent("深度阈值"));
            PropertyField(m_depthDiffMultiplier, new GUIContent("深度差增强倍数"));
            PropertyField(m_normalThreshold, new GUIContent("法线阈值"));
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("深度法线关系设置");
            EditorGUI.indentLevel++;
            PropertyField(m_steepAngleThreshold, new GUIContent("陡峭角度阈值"));
            PropertyField(m_steepAngleMultiplier, new GUIContent("陡峭角度乘数"));
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("其它设置");
            EditorGUI.indentLevel++;
            PropertyField(m_enableAntiAliasing, new GUIContent("描边图抗锯齿"));
            EditorGUI.indentLevel--;
        }
        
    }
}