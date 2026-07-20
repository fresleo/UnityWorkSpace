// Created By: WangYu  Date: 2023-11-30

using System;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Serialize
{
    /// <summary>
    /// 对象的 Lightmap 数据
    /// </summary>
    [Serializable]
    public struct LightmapConfig
    {
        /// <summary>
        /// 烘焙标记
        /// </summary>
        public bool baked;
        
        /// <summary>
        /// 索引
        /// </summary>
        public int index;
        
        /// <summary>
        /// 缩放偏移
        /// </summary>
        public Vector4 scaleOffset;

        
        public void Serialize(Stream stream)
        {
            byte val = (byte)(baked ? 1 : 0);
            MTStreamUtils.WriteByte(stream, val);
            
            MTStreamUtils.WriteInt(stream, index);
            
            MTStreamUtils.WriteVector4(stream, scaleOffset);
        }

        public void Deserialize(Stream stream)
        {
            byte val = MTStreamUtils.ReadByte(stream);
            baked = val == 1;

            index = MTStreamUtils.ReadInt(stream);

            scaleOffset = MTStreamUtils.ReadVector4(stream);
        }
    }
}