// Created By: WangYu  Date: 2025-04-28

using AirSticker.Runtime.Render;
using UnityEditor;
using UnityEngine;

namespace AirSticker
{
    public class KnifeMarkDecalConfigGUI : AbsDecalConfigGUI
    {
        // 淡入
        private SerializedProperty m_fadeinTime;
        private SerializedProperty m_fadeinCurve;

        // 拉伸
        private SerializedProperty m_stretchLeft, m_stretchRight;

        // 升温过程
        private SerializedProperty m_warmingLowTempGradient;
        private SerializedProperty m_warmingLowTempStrengthCurve;

        private SerializedProperty m_warmingHighTempGradient;
        private SerializedProperty m_warmingHighTempStrengthCurve;
        private SerializedProperty m_warmingHighTempSmoothingFactorCurve;

        // 降温过程
        private SerializedProperty m_coolingLowTempGradient;
        private SerializedProperty m_coolingLowTempStrengthCurve;

        private SerializedProperty m_coolingHighTempGradient;
        private SerializedProperty m_coolingHighTempStrengthCurve;
        private SerializedProperty m_coolingHighTempSmoothingFactorCurve;

        // 存续期间的透明度渐变控制
        private SerializedProperty m_durationAlphaGradient;
        
        // 淡出
        private SerializedProperty m_fadeoutTime;
        private SerializedProperty m_fadeoutCurve;

        public KnifeMarkDecalConfigGUI(KnifeMarkDecalConfig config) : base(config)
        {
            m_fadeinTime = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.fadeinTime));
            m_fadeinCurve = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.fadeinCurve));

            m_stretchLeft = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.stretchLeft));
            m_stretchRight = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.stretchRight));

            m_warmingLowTempGradient = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.warmingLowTempGradient));
            m_warmingLowTempStrengthCurve = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.warmingLowTempStrengthCurve));

            m_warmingHighTempGradient = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.warmingHighTempGradient));
            m_warmingHighTempStrengthCurve = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.warmingHighTempStrengthCurve));
            m_warmingHighTempSmoothingFactorCurve = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.warmingHighTempSmoothingFactorCurve));

            m_coolingLowTempGradient = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.coolingLowTempGradient));
            m_coolingLowTempStrengthCurve = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.coolingLowTempStrengthCurve));

            m_coolingHighTempGradient = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.coolingHighTempGradient));
            m_coolingHighTempStrengthCurve = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.coolingHighTempStrengthCurve));
            m_coolingHighTempSmoothingFactorCurve = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.coolingHighTempSmoothingFactorCurve));
            
            m_durationAlphaGradient = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.durationAlphaGradient));
            
            m_fadeoutTime = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.fadeoutTime));
            m_fadeoutCurve = m_configSo.FindProperty(nameof(KnifeMarkDecalConfig.fadeoutCurve));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            m_configSo.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("渲染器专用功能", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                EditorGUILayout.LabelField("淡入 - 拉伸，升温过程", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(m_fadeinTime, new GUIContent("时间"));
                    EditorGUILayout.PropertyField(m_fadeinCurve, new GUIContent("速度曲线"));
                    EditorGUILayout.HelpBox("注意：因为淡入是1个贴花拉伸的过程，所以它描述的不是透明度变化过程，而是拉伸的速度过程。", MessageType.Warning);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("拉伸参数");
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector4 stretchLeft = EditorGUILayout.Vector4Field("开始", m_stretchLeft.vector4Value);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_stretchLeft.vector4Value = stretchLeft;
                        }

                        EditorGUI.BeginChangeCheck();
                        Vector4 stretchRight = EditorGUILayout.Vector4Field("结束", m_stretchRight.vector4Value);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_stretchRight.vector4Value = stretchRight;
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("低温区");
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        EditorGUILayout.PropertyField(m_warmingLowTempGradient, new GUIContent("颜色变化"));
                        using (new EditorGUI.IndentLevelScope(1))
                        {
                            DrawHDRGradient(m_warmingLowTempGradient);
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(m_warmingLowTempStrengthCurve, new GUIContent("强度曲线"));
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("高温区");
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        EditorGUILayout.PropertyField(m_warmingHighTempGradient, new GUIContent("颜色变化"));
                        using (new EditorGUI.IndentLevelScope(1))
                        {
                            DrawHDRGradient(m_warmingHighTempGradient);
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(m_warmingHighTempStrengthCurve, new GUIContent("强度曲线"));
                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(m_warmingHighTempSmoothingFactorCurve, new GUIContent("平滑系数曲线"));
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("持续 - 降温过程", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.HelpBox("注意：持续时间因为是更通用的设置，所以它在更上面一点的位置。", MessageType.Warning);
                    
                    EditorGUILayout.LabelField("低温区");
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        EditorGUILayout.PropertyField(m_coolingLowTempGradient, new GUIContent("颜色变化"));
                        using (new EditorGUI.IndentLevelScope(1))
                        {
                            DrawHDRGradient(m_coolingLowTempGradient);
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(m_coolingLowTempStrengthCurve, new GUIContent("强度曲线"));
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("高温区");
                    using (new EditorGUI.IndentLevelScope(1))
                    {
                        EditorGUILayout.PropertyField(m_coolingHighTempGradient, new GUIContent("颜色变化"));
                        using (new EditorGUI.IndentLevelScope(1))
                        {
                            DrawHDRGradient(m_coolingHighTempGradient);
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(m_coolingHighTempStrengthCurve, new GUIContent("强度曲线"));
                        
                        EditorGUILayout.Space();
                        EditorGUILayout.PropertyField(m_coolingHighTempSmoothingFactorCurve, new GUIContent("平滑系数曲线"));
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_durationAlphaGradient, new GUIContent("透明度渐变控制"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SyncFadeoutCurve();
                    }
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("淡出", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.PropertyField(m_fadeoutTime, new GUIContent("时间"));
                    
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_fadeoutCurve, new GUIContent("速度曲线"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SyncFadeoutCurve();
                    }
                    EditorGUILayout.HelpBox("注意：淡出是1个透明度变化的过程，所以描述的是 Alpha 的改变速度。", MessageType.Warning);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_configSo.ApplyModifiedProperties();
            }
        }

        private void SyncFadeoutCurve()
        {
            var gKeys = m_durationAlphaGradient.gradientValue.alphaKeys;
            var acKeys = m_fadeoutCurve.animationCurveValue.keys;

            if (gKeys.Length == 0 || acKeys.Length == 0)
            {
                return;
            }
            
            // 获取最后的 alpha 值
            int lastAlphaKeyIndex = gKeys.Length - 1;
            float lastAlpha = gKeys[lastAlphaKeyIndex].alpha;
            
            // 修改旧曲线
            AnimationCurve curve = m_fadeoutCurve.animationCurveValue;
            
            int keyIndex = 0;  
            Keyframe oldKey = curve[keyIndex];
            Keyframe newKey = new Keyframe(oldKey.time, lastAlpha, oldKey.inTangent, oldKey.outTangent);
            curve.MoveKey(keyIndex, newKey);
            
            m_fadeoutCurve.animationCurveValue = curve;
        }
        
        private void DrawHDRGradient(SerializedProperty gradientProperty)
        {
            if (gradientProperty == null) return;
            
            Gradient gradient = gradientProperty.gradientValue;
            if (gradient == null) return;

            GradientColorKey[] colorKeys = gradient.colorKeys;
            
            string prefsKey = $"{nameof(KnifeMarkDecalConfigGUI)}_{gradientProperty.name}";
            bool foldout = EditorPrefs.GetBool(prefsKey, false);
            
            EditorGUI.BeginChangeCheck();
            
            bool newFoldout = EditorGUILayout.Foldout(foldout, "HDR 模式", true);
            if (newFoldout != foldout)
            {
                foldout = newFoldout;
                EditorPrefs.SetBool(prefsKey, newFoldout);
            }

            if (foldout)
            {
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"颜色 {i}", GUILayout.Width(120));
                        colorKeys[i].color = EditorGUILayout.ColorField(GUIContent.none, colorKeys[i].color, true, false, true);
                    }
                }
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                // 把数据写回去
                gradient.SetKeys(colorKeys, gradient.alphaKeys);
                gradientProperty.gradientValue = gradient;
                
                m_configSo.ApplyModifiedProperties();
            }
        }

    }
}