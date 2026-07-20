// Created By: WangYu  Date: 2025-04-28

using AirSticker.Runtime.Logic;
using UnityEngine;

namespace AirSticker.Runtime.Render
{
    [CreateAssetMenu(fileName = "New Knife Mark Decal Config", menuName = "空气贴纸/刀痕贴花配置")]
    public class KnifeMarkDecalConfig : AbsDecalConfig
    {
        /// <summary>
        /// 淡入时间
        /// </summary>
        public float fadeinTime = 0;
        /// <summary>
        /// 淡入速度曲线
        /// </summary>
        public AnimationCurve fadeinCurve = AnimationCurve.Linear(0, 0, 1, 1);

        /// <summary>
        /// 拉伸参数
        /// </summary>
        public Vector4 stretchLeft = new(1, 1, 0, 0);
        public Vector4 stretchRight = new(1, 1, 0, 0);
        
        // 升温过程
        public Gradient warmingLowTempGradient;
        public AnimationCurve warmingLowTempStrengthCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public Gradient warmingHighTempGradient;
        public AnimationCurve warmingHighTempStrengthCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve warmingHighTempSmoothingFactorCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        // 降温过程
        public Gradient coolingLowTempGradient;
        public AnimationCurve coolingLowTempStrengthCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public Gradient coolingHighTempGradient;
        public AnimationCurve coolingHighTempStrengthCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve coolingHighTempSmoothingFactorCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        /// <summary>
        /// 存续期间的透明度渐变控制
        /// </summary>
        public Gradient durationAlphaGradient;
        
        /// <summary>
        /// 淡出时间
        /// </summary>
        public float fadeoutTime;
        /// <summary>
        /// 淡出速度曲线
        /// </summary>
        public AnimationCurve fadeoutCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        public override int CalculateHash()
        {
            int hash1 = base.CalculateHash();
            
            int hash2 = fadeinTime.GetHashCode();
            int hash3 = UnityUtils.GetAnimationCurveHash(fadeinCurve);

            int hash4 = stretchLeft.GetHashCode();
            int hash5 = stretchRight.GetHashCode();
            
            int hash6 = UnityUtils.GetGradientHash(warmingLowTempGradient);
            int hash7 = UnityUtils.GetAnimationCurveHash(warmingLowTempStrengthCurve);
            
            int hash8 = UnityUtils.GetGradientHash(warmingHighTempGradient);
            int hash9 = UnityUtils.GetAnimationCurveHash(warmingHighTempStrengthCurve);
            int hash10 = UnityUtils.GetAnimationCurveHash(warmingHighTempSmoothingFactorCurve);

            int hash11 = UnityUtils.GetGradientHash(coolingLowTempGradient);
            int hash12 = UnityUtils.GetAnimationCurveHash(coolingLowTempStrengthCurve);
            
            int hash13 = UnityUtils.GetGradientHash(coolingHighTempGradient);
            int hash14 = UnityUtils.GetAnimationCurveHash(coolingHighTempStrengthCurve);
            int hash15 = UnityUtils.GetAnimationCurveHash(coolingHighTempSmoothingFactorCurve);
            
            int hash16 = UnityUtils.GetGradientHash(durationAlphaGradient);
            
            int hash17 = fadeoutTime.GetHashCode();
            int hash18 = UnityUtils.GetAnimationCurveHash(fadeoutCurve);

            int hash = UnityUtils.CombineHash(hash1, 
                hash2, hash3, 
                hash4, hash5, 
                hash6, hash7, hash8, hash9, hash10, 
                hash11, hash12, hash13, hash14, hash15, 
                hash16, 
                hash17, hash18);
            return hash;
        }
        
    }
}