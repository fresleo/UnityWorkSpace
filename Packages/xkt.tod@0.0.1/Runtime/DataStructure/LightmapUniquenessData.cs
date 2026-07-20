// Created By: WangYu  Date: 2025-04-10

using System;
using UnityEngine;

namespace XKT.TOD.DataStructure
{
    /// <summary>
    /// Lightmap 的唯一性数据
    /// </summary>
    [Serializable]
    public class LightmapUniquenessData
    {
        /// <summary>
        /// 脚本 Id
        /// </summary>
        public string scriptId;
        
        public int lightmapIndex;
        public Vector4 lightmapScaleOffset;
        
    }
}