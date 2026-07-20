// Created By: WangYu  Date: 2025-02-18

using UnityEngine;

namespace AirSticker.Runtime.Logic
{
    /// <summary>
    /// 贴花配置
    /// </summary>
    public abstract class AbsDecalConfig : ScriptableObject
    {
        /// <summary>
        /// box 的宽
        /// </summary>
        public float boxWidth = 1;
        /// <summary>
        /// box 的高
        /// </summary>
        public float boxHeight = 1;
        /// <summary>
        /// box 的深
        /// </summary>
        public float boxDepth = 1;
        
        /// <summary>
        /// 贴花的材质
        /// </summary>
        public Material material;

        /// <summary>
        /// 是否可以投射到背面
        /// </summary>
        public bool projectionBackside;
        
        /// <summary>
        /// 总持续时间
        /// </summary>
        public float duration = -1;

        public virtual int CalculateHash()
        {
            int hash1 = boxWidth.GetHashCode();
            int hash2 = boxHeight.GetHashCode();
            int hash3 = boxDepth.GetHashCode();
            
            int hash4 = material.GetInstanceID();
            
            int hash5 = projectionBackside.GetHashCode();
            int hash6 = duration.GetHashCode();

            int hash = UnityUtils.CombineHash(hash1, hash2, hash3, hash4, hash5, hash6);
            return hash;
        }
        
    }
}