// Created by: WangYu   Date: 2025-11-03

using System;
using System.Collections.Generic;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    /// <summary>
    /// 角色的阴影设置
    /// </summary>
    [Serializable]
    public class CharacterShadowSettings
    {
        public bool override_shadow1Color;
        public Color shadow1Color;
        
        public bool override_shadow1Step;
        public float shadow1Step;
        
        public bool override_shadow1Feather;
        public float shadow1Feather;
        
        /// <summary>
        /// 和 MotionImpactDataEnum.cs 中的 EModelRenderPart 是同1个枚举，应保持一致
        /// </summary>
        [Flags]
        public enum EModelRenderPart
        {
            None = 0,
            Body = 1 << 1,
            Hair = 1 << 2,
            Eyes = 1 << 3,
            Face = 1 << 4,
            Wing = 1 << 5,
            LeftArm = 1 << 6,
            RightArm = 1 << 7,
            SMRWeaponHand1 = 1 << 8, //蒙皮武器使用1
            SMRWeaponBack1 = 1 << 9, //蒙皮武器收纳1
            LeftLeg = 1 << 10,
            RightLeg = 1 << 11,
            Silhouette = 1 << 12,   //剪影
            SMRWeaponHand2 = 1 << 13,   //蒙皮武器使用2
            // All = Body | Hair | Eyes | Face | Wing | LeftArm | RightArm | LeftLeg | RightLeg,
            AllNoSilhouette = Body | Hair | Eyes | Face | Wing | LeftArm | RightArm | SMRWeaponHand1 | SMRWeaponBack1 | LeftLeg | RightLeg | SMRWeaponHand2,
            All = ~(1 << 31),
        }
        
        public static readonly string[] s_modelRenderParts =
        {
            "无", 
            "躯干", "头发", "眼睛", "脸", 
            "翅膀", "左臂", "右臂", "蒙皮武器使用1", "蒙皮武器收纳1", "左腿", "右腿", "剪影", "蒙皮武器使用2", 
            "除剪影外的全部", 
            "全部"
        };

        public int modelRenderPart = (int)EModelRenderPart.None;
        
    }
}