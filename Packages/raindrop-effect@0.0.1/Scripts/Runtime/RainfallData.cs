// Created By: WangYu  Date: 2024-11-19

using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace RaindropEffect
{
    /// <summary>
    /// 雨量预配置
    /// </summary>
    //[CreateAssetMenu(fileName = "RainfallData", menuName = "屏幕雨滴特效/雨量预配置")]
    public class RainfallData : ScriptableObject
    {
#if UNITY_EDITOR
        [MenuItem("Assets/Create/屏幕雨滴特效/雨量预配置")]
        public static void CreateMyScriptableObject()
        {
            RaindropEffectEditorUtils.CreateScriptableObject<RaindropRendererData>(nameof(RainfallData));
        }
#endif // UNITY_EDITOR
        
        
        [Serializable]
        public class SimulateParameters
        {
            /// <summary>
            /// 生成间隔
            /// </summary>
            public Vector2 spawnInterval = new(0.05f, 0.2f);
            /// <summary>
            /// 雨滴的尺寸范围
            /// </summary>
            public Vector2 raindropSizeRange = new(50f, 100f);

            /// <summary>
            /// 重力
            /// </summary>
            public float gravity = 2200;
            /// <summary>
            /// 摩擦力系数
            /// </summary>
            public float frictionForceCoefficient = 4;
            
            /// <summary>
            /// x轴速度系数范围
            /// </summary>
            public Vector2 xVelocityCoefficientRange = new(0, 0.3f);

            /// <summary>
            /// 拖尾雨滴的尺寸扩散
            /// </summary>
            [Range(0, 0.1f)] public float trailRaindropSizeSpread = 0.006f;
        }
        
        [Serializable]
        public class RenderParameters
        {
            /// <summary>
            /// 液滴的生成速率 (每秒)
            /// </summary>
            [Range(0, 1000)] public float dropletsSpawnRate = 450;
        }

        public SimulateParameters simuParas;
        public RenderParameters rendParas;
        
    }
}