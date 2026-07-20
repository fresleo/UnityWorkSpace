// Created By: WangYu  Date: 2022-10-01

using System;

namespace com.xknight.mt.Lib.Runtime.MT.Serialize
{
    [Serializable]
    public class LODSetting
    {
        /// <summary>
        /// 细分级别
        /// </summary>
        public int subdivision = 4;
        /// <summary>
        /// 坡度角误差
        /// </summary>
        public float slopeAngleError = 1f;
    }
}