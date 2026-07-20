// Created By: WangYu  Date: 2022-10-03

using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainSampling
{
    /// <summary>
    /// 地形扫描器接口
    /// </summary>
    public interface ITerrainScanner
    {
        /// <summary>
        /// 执行扫描
        /// </summary>
        void Run(Vector3 center, out Vector3 hitPos, out Vector3 hitNormal);
    }
}