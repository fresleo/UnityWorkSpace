// Created By: WangYu  Date: 2023-12-07

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    public interface ICubeVolume
    {
        /// <summary>
        /// Cube体积的控制柄位置
        /// </summary>
        Vector3[] CubeHandlePositions { get; }
        
        /// <summary>
        /// Cube体积的中心位置
        /// </summary>
        Vector3 CubeCenter { get; set; }
        
        /// <summary>
        /// Cube体积的尺寸
        /// </summary>
        Vector3 CubeSize { get; set; }
    }
}