// Created By: WangYu  Date: 2023-11-22

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    /// <summary>
    /// 2进制数据加载器
    /// </summary>
    public interface IBytesLoader
    {
        /// <summary>
        /// 清理申请的资源
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 卸载资源
        /// </summary>
        void UnloadAsset(string path);
        
        /// <summary>
        /// 加载数据
        /// </summary>
        byte[] LoadAsset(string path);
    }
}