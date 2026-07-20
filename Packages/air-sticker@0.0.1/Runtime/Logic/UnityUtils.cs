// Created By: WangYu  Date: 2025-03-25

using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AirSticker.Runtime.Logic
{
    internal static class UnityUtils
    {
        internal static void DestroyUnityObject(UnityObject obj)
        {
            if(obj == null) return;
            
#if UNITY_EDITOR
            if (Application.isPlaying && !UnityEditor.EditorApplication.isPaused)
                UnityObject.Destroy(obj);
            else
                UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
        }

        private const int c_hashStartPrime = 17;
        private const int c_hashMultipliedPrime = 31;
        
        /// <summary>
        /// 组合哈希值
        /// params 应该会产生 gc，但是架不住它好用呀
        /// </summary>
        public static int CombineHash(params int[] hashes)
        {
            unchecked
            {
                int hash = c_hashStartPrime;

                int hashCount = hashes.Length;
                hash = hash * c_hashMultipliedPrime + hashCount;
                
                for (int i = 0; i < hashCount; i++)
                {
                    int itemHash = hashes[i];
                    hash = hash * c_hashMultipliedPrime + itemHash;
                }
                
                return hash;
            }
        }
        
        /// <summary>
        /// 计算 AnimationCurve 的哈希值
        /// </summary>
        public static int GetAnimationCurveHash(AnimationCurve curve)
        {
            if (curve == null) return 0;
            
            unchecked
            {
                int hash = c_hashStartPrime;
                
                int keyLength = curve.keys.Length;
                hash = hash * c_hashMultipliedPrime + keyLength;
                
                for (int i = 0; i < keyLength; i++)
                {
                    Keyframe key = curve.keys[i];
                    
                    hash = hash * c_hashMultipliedPrime + key.time.GetHashCode();
                    hash = hash * c_hashMultipliedPrime + key.value.GetHashCode();
                    hash = hash * c_hashMultipliedPrime + key.inTangent.GetHashCode();
                    hash = hash * c_hashMultipliedPrime + key.outTangent.GetHashCode();
                }
                
                return hash;
            }
        }

        public static int GetGradientHash(Gradient gradient)
        {
            if (gradient == null) return 0;

            unchecked
            {
                int hash = c_hashStartPrime;

                // 处理颜色键
                GradientColorKey[] colorKeys = gradient.colorKeys;
                int colorKeyLength = colorKeys.Length;
                hash = hash * c_hashMultipliedPrime + colorKeyLength;
                
                for (int i = 0; i < colorKeyLength; i++)
                {
                    hash = hash * c_hashMultipliedPrime + colorKeys[i].color.GetHashCode();
                    hash = hash * c_hashMultipliedPrime + colorKeys[i].time.GetHashCode();
                }

                // 处理透明度键
                GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
                int alphaKeyLength = alphaKeys.Length;
                hash = hash * c_hashMultipliedPrime + alphaKeyLength;
                
                for (int i = 0; i < alphaKeyLength; i++)
                {
                    hash = hash * c_hashMultipliedPrime + alphaKeys[i].alpha.GetHashCode();
                    hash = hash * c_hashMultipliedPrime + alphaKeys[i].time.GetHashCode();
                }

                // 处理渐变模式
                hash = hash * c_hashMultipliedPrime + ((int)gradient.mode).GetHashCode();

                return hash;
            }
        }

    }
}