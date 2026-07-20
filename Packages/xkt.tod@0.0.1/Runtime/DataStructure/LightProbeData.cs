// Created By: WangYu  Date: 2025-03-17

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace XKT.TOD.DataStructure
{
    [Serializable]
    public class SphericalHarmonics
    {
        /// <summary>
        /// 系数
        /// </summary>
        public float[] coefficients = new float[27];
    }
    
    [Serializable]
    public class LightProbeData
    {
        /// <summary>
        /// 初始光探针阵列位置
        /// </summary>
        public int initialLightProbesArrayPosition;
        
        /// <summary>
        /// 灯光探针球谐波数据
        /// </summary>
        public SphericalHarmonics[] lightProbes;
        
        /// <summary>
        /// 1维数据
        /// 如果后面搞 lerp 模式的话，这种数据结构会更方便
        /// </summary>
        public float[] lightProbes1D;

        public void Collect()
        {
            SphericalHarmonicsL2[] bakedProbes = LightmapSettings.lightProbes?.bakedProbes;
            if (bakedProbes == null || bakedProbes.Length == 0)
            {
                return;
            }
            
            int probeLength = bakedProbes.Length;
            this.initialLightProbesArrayPosition = probeLength;
            this.lightProbes = new SphericalHarmonics[probeLength];
            this.lightProbes1D = new float[bakedProbes.Length * 27];
            
            for (int i = 0; i < probeLength; i++)
            {
                var shData = new SphericalHarmonics();
                
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        shData.coefficients[j * 9 + k] = bakedProbes[i][j, k];
                    }
                }
                
                this.lightProbes[i] = shData;
            }
            
            int counter = 0;
            for (int i = 0; i < probeLength; i++)
            {
                for (int j = 0; j < this.lightProbes[i].coefficients.Length; j++)
                {
                    this.lightProbes1D[counter] = this.lightProbes[i].coefficients[j];
                    counter++;
                }
            }
        }
        
        public void Restore()
        {
            int probeLength = this.initialLightProbesArrayPosition;
            var bakedProbes = new SphericalHarmonicsL2[probeLength];
            
            for (int i = 0; i < probeLength; i++)
            {
                var shData = new SphericalHarmonicsL2();
                
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        shData[j, k] = this.lightProbes[i].coefficients[j * 9 + k];
                    }
                }

                bakedProbes[i] = shData;
            }
            
            LightmapSettings.lightProbes.bakedProbes = bakedProbes;
        }
        
    }
}