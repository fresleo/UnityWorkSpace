// Created By: WangYu  Date: 2024-11-18

using UnityEditor;
using UnityEngine;

namespace RaindropEffect
{
    public class SimulateParametersSP
    {
        private SerializedProperty m_root;
        private bool m_highlightSomeProperty;
        
        private SerializedProperty m_screenWidth, m_screenHeight;
        private SerializedProperty m_spawnInterval, m_raindropSizeRange, m_raindropMaxCount, m_initSizeSpread;
        private SerializedProperty m_gravity, m_frictionForceCoefficient;
        private SerializedProperty m_forceUpdateIntervalRange, m_xVelocityCoefficientRange, m_massEvaporateRate, m_yVelocitySpreadCoefficient, m_moveDirAngle, m_sizeShrinkRate;
        private SerializedProperty m_trailSizeScaleRange, m_trailRaindropDensity, m_trailRaindropSizeSpread, m_trailRaindropDistanceRange;
        
        public SimulateParametersSP(SerializedProperty root, bool highlightSomeProperty)
        {
            m_root = root;
            m_highlightSomeProperty = highlightSomeProperty;

            var tempObj = new SimulateParameters();
            
            m_screenWidth = m_root.FindPropertyRelative(nameof(tempObj.screenWidth));
            m_screenHeight = m_root.FindPropertyRelative(nameof(tempObj.screenHeight));
            
            m_spawnInterval = m_root.FindPropertyRelative(nameof(tempObj.spawnInterval));
            m_raindropSizeRange = m_root.FindPropertyRelative(nameof(tempObj.raindropSizeRange));
            m_raindropMaxCount = m_root.FindPropertyRelative(nameof(tempObj.raindropMaxCount));
            m_initSizeSpread = m_root.FindPropertyRelative(nameof(tempObj.initSizeSpread));
            
            m_gravity = m_root.FindPropertyRelative(nameof(tempObj.gravity));
            m_frictionForceCoefficient = m_root.FindPropertyRelative(nameof(tempObj.frictionForceCoefficient));
            
            m_forceUpdateIntervalRange = m_root.FindPropertyRelative(nameof(tempObj.forceUpdateIntervalRange));
            m_xVelocityCoefficientRange = m_root.FindPropertyRelative(nameof(tempObj.xVelocityCoefficientRange));
            m_massEvaporateRate = m_root.FindPropertyRelative(nameof(tempObj.massEvaporateRate));
            m_yVelocitySpreadCoefficient = m_root.FindPropertyRelative(nameof(tempObj.yVelocitySpreadCoefficient));
            m_moveDirAngle = m_root.FindPropertyRelative(nameof(tempObj.moveDirAngle));
            m_sizeShrinkRate = m_root.FindPropertyRelative(nameof(tempObj.sizeShrinkRate));
            
            m_trailSizeScaleRange = m_root.FindPropertyRelative(nameof(tempObj.trailSizeScaleRange));
            m_trailRaindropDensity = m_root.FindPropertyRelative(nameof(tempObj.trailRaindropDensity));
            m_trailRaindropSizeSpread = m_root.FindPropertyRelative(nameof(tempObj.trailRaindropSizeSpread));
            m_trailRaindropDistanceRange = m_root.FindPropertyRelative(nameof(tempObj.trailRaindropDistanceRange));
        }

        public void DrawGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("模拟参数", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                using (new EditorGUI.IndentLevelScope(1))
                {
                    if (m_screenWidth != null || m_screenHeight != null)
                    {
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUILayoutExt.PropertyField(m_screenWidth, new GUIContent("屏幕的宽"));
                            EditorGUILayoutExt.PropertyField(m_screenHeight, new GUIContent("屏幕的高"));
                        }
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUILayoutExt.PropertyField(m_spawnInterval, new GUIContent("生成间隔"), m_highlightSomeProperty);
                    EditorGUILayoutExt.PropertyField(m_raindropSizeRange, new GUIContent("雨滴的尺寸范围"), m_highlightSomeProperty);
                    EditorGUILayoutExt.PropertyField(m_raindropMaxCount, new GUIContent("雨滴的最大数量"));
                    EditorGUILayoutExt.PropertyField(m_initSizeSpread, new GUIContent("初始化尺寸散布"));
                    
                    EditorGUILayout.Space();
                    EditorGUILayoutExt.PropertyField(m_gravity, new GUIContent("重力"), m_highlightSomeProperty);
                    EditorGUILayoutExt.PropertyField(m_frictionForceCoefficient, new GUIContent("摩擦力系数"), m_highlightSomeProperty);
                    
                    EditorGUILayoutExt.PropertyField(m_forceUpdateIntervalRange, new GUIContent("强制更新间隔范围"));
                    EditorGUILayoutExt.PropertyField(m_xVelocityCoefficientRange, new GUIContent("x轴速度系数范围"), m_highlightSomeProperty);
                    EditorGUILayoutExt.PropertyField(m_massEvaporateRate, new GUIContent("质量蒸发速率 (每秒)"));
                    EditorGUILayoutExt.PropertyField(m_yVelocitySpreadCoefficient, new GUIContent("y轴速度扩散系数"));
                    EditorGUILayoutExt.PropertyField(m_moveDirAngle, new GUIContent("移动方向的角度"));
                    EditorGUILayoutExt.PropertyField(m_sizeShrinkRate, new GUIContent("雨滴收缩速率的大小（每秒）"));
                    
                    EditorGUILayout.Space();
                    EditorGUILayoutExt.PropertyField(m_trailSizeScaleRange, new GUIContent("拖尾尺寸范围"));
                    EditorGUILayoutExt.PropertyField(m_trailRaindropDensity, new GUIContent("拖尾雨滴的密度"));
                    EditorGUILayoutExt.PropertyField(m_trailRaindropSizeSpread, new GUIContent("拖尾雨滴的尺寸扩散"), m_highlightSomeProperty);
                    EditorGUILayoutExt.PropertyField(m_trailRaindropDistanceRange, new GUIContent("拖尾雨滴的距离范围"));
                }
            }
        }
        
    }
}