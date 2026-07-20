// Created By: WangYu  Date: 2024-07-31

using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace ToonPostProcessing.Volumes
{
    [CustomEditor(typeof(PreObjectIdOutline))]
    public class PreObjectIdOutlineEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_outlineColor;
        
        private SerializedDataParameter m_outlineIntensityMultiplier;
        private SerializedDataParameter m_outlineDistanceFade;
        private SerializedDataParameter m_outlineMinSeparation;
        private SerializedDataParameter m_outlineWidth;

        private SerializedDataParameter m_blurIntensity;

        private SerializedDataParameter m_enableAntiAliasing;
        
        public override void OnEnable()
        {
            var o = new PropertyFetcher<PreObjectIdOutline>(serializedObject);
            
            m_outlineColor = Unpack(o.Find(x => x.outlineColor));
            
            m_outlineIntensityMultiplier = Unpack(o.Find(x => x.outlineIntensityMultiplier));
            m_outlineDistanceFade = Unpack(o.Find(x => x.outlineDistanceFade));
            m_outlineMinSeparation = Unpack(o.Find(x => x.outlineMinSeparation));
            m_outlineWidth = Unpack(o.Find(x => x.outlineWidth));
            
            m_blurIntensity = Unpack(o.Find(x => x.blurIntensity));
            
            m_enableAntiAliasing = Unpack(o.Find(x => x.enableAntiAliasing));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_outlineColor, new GUIContent("颜色"));
            
            PropertyField(m_outlineDistanceFade, new GUIContent("摄像机的有效距离（单位米）"));
            PropertyField(m_outlineIntensityMultiplier, new GUIContent("强度乘数"));
            PropertyField(m_outlineMinSeparation, new GUIContent("最小分离像素数"));
            PropertyField(m_outlineWidth, new GUIContent("边缘检测阶段的描边宽度"));
            
            PropertyField(m_blurIntensity, new GUIContent("降噪阶段的 blur 强度"));
            
            PropertyField(m_enableAntiAliasing, new GUIContent("描边图抗锯齿"));
        }
        
    }
}