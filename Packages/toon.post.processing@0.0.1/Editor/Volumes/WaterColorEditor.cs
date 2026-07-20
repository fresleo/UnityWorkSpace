// Created By: WangYu  Date: 2024-03-15

using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace ToonPostProcessing.Volumes
{
    [CustomEditor(typeof(WaterColor))]
    public class WaterColorEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_waterColor;
        private SerializedDataParameter m_xRadius, m_yRadius;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<WaterColor>(serializedObject);
            
            m_waterColor = Unpack(o.Find(x => x.waterColor));
            m_xRadius = Unpack(o.Find(x => x.xRadius));
            m_yRadius = Unpack(o.Find(x => x.yRadius));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_waterColor, new GUIContent("水彩调色"));
            PropertyField(m_xRadius, new GUIContent("X半径"));
            PropertyField(m_yRadius, new GUIContent("Y半径"));
        }
    }
}