// Created By: WangYu  Date: 2024-07-16

using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace ToonPostProcessing.Volumes
{
    [CustomEditor(typeof(SobelOutline))]
    public class SobelOutlineEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_outlineColor;
        
        private SerializedDataParameter m_outlineThickness;
        private SerializedDataParameter m_outlineDistanceFade;

        private SerializedDataParameter m_outlineEdgeMultiplier, m_outlineEdgeBias;
        
        public override void OnEnable()
        {
            var o = new PropertyFetcher<SobelOutline>(serializedObject);
            
            m_outlineColor = Unpack(o.Find(x => x.outlineColor));
            
            m_outlineThickness = Unpack(o.Find(x => x.outlineThickness));
            m_outlineDistanceFade = Unpack(o.Find(x => x.outlineDistanceFade));
            
            m_outlineEdgeMultiplier = Unpack(o.Find(x => x.outlineEdgeMultiplier));
            m_outlineEdgeBias = Unpack(o.Find(x => x.outlineEdgeBias));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_outlineColor, new GUIContent("描边颜色"));
            
            PropertyField(m_outlineDistanceFade, new GUIContent("摄像机的有效距离（单位米）"));
            PropertyField(m_outlineThickness, new GUIContent("描边粗细"));
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("注意：以下参数主要是为了改善描边在 OpenGL ES 图形 API 下的水波纹效应", MessageType.Warning);
            PropertyField(m_outlineEdgeMultiplier, new GUIContent("边缘的倍率乘数", "越大边缘会越明显"));
            PropertyField(m_outlineEdgeBias, new GUIContent("边缘的 Bias", "越大越会增强高频细节"));
        }
    }
}