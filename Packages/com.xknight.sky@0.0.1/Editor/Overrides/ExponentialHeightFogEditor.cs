using UnityEngine.Rendering.Universal;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ExponentialHeightFog))]
    class ExponentialHeightFogEditor : VolumeComponentEditor
    {
        protected SerializedDataParameter m_FogDensity;
        protected SerializedDataParameter m_FogHeightFalloff;
        protected SerializedDataParameter m_FogHeight;
        protected SerializedDataParameter m_FogDensity2;
        protected SerializedDataParameter m_FogHeightFalloff2;
        protected SerializedDataParameter m_FogHeight2;

        protected SerializedDataParameter m_FogInscatteringColor;
        protected SerializedDataParameter m_FogMaxOpacity;
        protected SerializedDataParameter m_StartDistance;
        protected SerializedDataParameter m_FogCutoffDistance;
        
        protected SerializedDataParameter m_DirectionalInscatteringDir;
        protected SerializedDataParameter m_DirectionalInscatteringExponent;
        protected SerializedDataParameter m_DirectionalInscatteringStartDistance;
        protected SerializedDataParameter m_DirectionalInscatteringColor;

        bool m_SecondFogFoldout = true;


        private const string c_tip =
            "因为针对 Mobile 和 PC 平台做了算法优化，所以在质感会有区别。"
            + "\n如果想看到最佳效果，请切换到 PC 平台，或开启 “录制画质”。";
        
        static GUIContent s_FogDensity = new GUIContent("雾密度", "");
        static GUIContent s_FogHeightFalloff = new GUIContent("雾高度衰减", "控制高度增加时密度的增加方式。值越小，可见的过渡便越大");
        static GUIContent s_FogHeight = new GUIContent("雾高度偏差", "相对于原点位置Y");
        static GUIContent s_SecondFogFoldout = new GUIContent("第二雾数据", "");
        static GUIContent s_FogInscatteringColor = new GUIContent("雾内散射颜色", "");
        static GUIContent s_FogMaxOpacity = new GUIContent("雾最大不透明度", "值为1则意味着雾气在一定距离时可变为完全不透明，并完全替代场景颜色\n值为0则意味着雾气颜色将完全不会作为因素计入");
        static GUIContent s_StartDistance = new GUIContent("起始距离", "雾气开始出现的相机距离，按场景单位算");
        static GUIContent s_FogCutoffDistance = new GUIContent("雾切断距离", "超过此距离的场景元素将不会应用雾气，建议设为0");
        static GUIContent s_DirectionalInscatteringDir = new GUIContent("定向内散射光源方向（PC）", "控制定向内散射光源的方向，其用于模拟来自定向光源的内散射");
        static GUIContent s_DirectionalInscatteringExponent = new GUIContent("定向内散射指数（PC）", "控制定向内散射锥体的大小，其用于模拟来自定向光源的内散射");
        static GUIContent s_DirectionalInscatteringStartDistance = new GUIContent("定向内散射起始距离（PC）", "控制到定向内散射观察者的开始距离，其用于模拟来自定向光源的内散射");
        static GUIContent s_DirectionalInscatteringColor = new GUIContent("定向内散射颜色（PC）", "控制定向内散射的颜色，其用于模拟来自定向光源的内散射");

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ExponentialHeightFog>(serializedObject);

            m_FogDensity = Unpack(o.Find(x => x.fogDensity));
            m_FogHeightFalloff = Unpack(o.Find(x => x.fogHeightFalloff));
            m_FogHeight = Unpack(o.Find(x => x.fogHeight));
            m_FogDensity2 = Unpack(o.Find(x => x.fogDensity2));
            m_FogHeightFalloff2 = Unpack(o.Find(x => x.fogHeightFalloff2));
            m_FogHeight2 = Unpack(o.Find(x => x.fogHeight2));

            m_FogInscatteringColor = Unpack(o.Find(x => x.fogInscatteringColor));
            m_FogMaxOpacity = Unpack(o.Find(x => x.fogMaxOpacity));
            m_StartDistance = Unpack(o.Find(x => x.startDistance));
            m_FogCutoffDistance = Unpack(o.Find(x => x.fogCutoffDistance));

            m_DirectionalInscatteringDir = Unpack(o.Find(x => x.directionalInscatteringDir));
            m_DirectionalInscatteringExponent = Unpack(o.Find(x => x.directionalInscatteringExponent));
            m_DirectionalInscatteringStartDistance = Unpack(o.Find(x => x.directionalInscatteringStartDistance));
            m_DirectionalInscatteringColor = Unpack(o.Find(x => x.directionalInscatteringColor));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayoutExt.HelpBoxCustomFontSize(c_tip, MessageType.Warning, 16, 50);
            EditorGUILayout.Space();
            
            PropertyField(m_FogDensity, s_FogDensity);
            PropertyField(m_FogHeightFalloff, s_FogHeightFalloff);
            PropertyField(m_FogHeight, s_FogHeight);
            m_SecondFogFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SecondFogFoldout, s_SecondFogFoldout);
            if (m_SecondFogFoldout)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.Space(16, false);
                GUILayout.BeginVertical();

                PropertyField(m_FogDensity2, s_FogDensity);
                PropertyField(m_FogHeightFalloff2, s_FogHeightFalloff);
                PropertyField(m_FogHeight2, s_FogHeight);

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();          

            PropertyField(m_FogInscatteringColor, s_FogInscatteringColor);
            PropertyField(m_FogMaxOpacity, s_FogMaxOpacity);
            PropertyField(m_StartDistance, s_StartDistance);
            PropertyField(m_FogCutoffDistance, s_FogCutoffDistance);
            
            PropertyField(m_DirectionalInscatteringDir, s_DirectionalInscatteringDir);
            PropertyField(m_DirectionalInscatteringExponent, s_DirectionalInscatteringExponent);
            PropertyField(m_DirectionalInscatteringStartDistance, s_DirectionalInscatteringStartDistance);
            PropertyField(m_DirectionalInscatteringColor, s_DirectionalInscatteringColor);
        }
    }
}
