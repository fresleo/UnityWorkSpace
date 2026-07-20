// Created By: WangYu  Date: 2023-11-30

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Serialize
{
    /// <summary>
    /// 实例化对象组的配置
    /// </summary>
    public class IOGroupConfig : ScriptableObject
    {
        public enum EDataType
        {
            /// <summary>
            /// 平铺的
            /// </summary>
            Flat,
            /// <summary>
            /// 树形的
            /// </summary>
            Tree,
        }
        
        /// <summary>
        /// 数据类型
        /// </summary>
        public EDataType dataType;

        /// <summary>
        /// 2进制数据
        /// </summary>
        public string byteDataPath;
    }
}