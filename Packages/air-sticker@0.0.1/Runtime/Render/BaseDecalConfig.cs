// Created By: WangYu  Date: 2025-04-28

using AirSticker.Runtime.Logic;
using UnityEngine;

namespace AirSticker.Runtime.Render
{
    [CreateAssetMenu(fileName = "New Base Decal Config", menuName = "空气贴纸/基本贴花配置")]
    public class BaseDecalConfig : AbsDecalConfig
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
            int hash4 = fadeoutTime.GetHashCode();
            int hash5 = UnityUtils.GetAnimationCurveHash(fadeoutCurve);
            
            int hash = UnityUtils.CombineHash(hash1, hash2, hash3, hash4, hash5);
            return hash;
        }
        
    }
}