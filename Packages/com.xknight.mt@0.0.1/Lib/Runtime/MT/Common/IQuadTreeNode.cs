// Created By: WangYu  Date: 2023-12-01

using System.IO;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    public interface IQuadTreeNode
    {
        /// <summary>
        /// 清理
        /// </summary>
        void Clear();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="cid">单元格id</param>
        void Initialize(int cid);
        
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="stream">流</param>
        void Serialize(Stream stream);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="stream">流</param>
        /// <param name="centerOffset">中心点偏移</param>
        void Deserialize(Stream stream, Vector3 centerOffset);
        
    }
}