// Created By: WangYu  Date: 2022-11-01

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces
{
    /// <summary>
    /// 虚拟纹理
    /// </summary>
    public interface IVT
    {
        /// <summary>
        /// 尺寸
        /// </summary>
        int Size { get; }
        
        /// <summary>
        /// 纹理
        /// </summary>
        Texture Tex { get; }
    }
}