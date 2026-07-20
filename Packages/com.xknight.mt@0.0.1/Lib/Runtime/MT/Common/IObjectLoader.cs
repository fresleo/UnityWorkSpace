// Created By: WangYu  Date: 2024-01-09

namespace com.xknight.mt.Lib.Runtime.MT.Common
{
    /// <summary>
    /// Unity Object 加载器接口
    /// </summary>
    public interface IObjectLoader
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
        /// 加载资源
        /// </summary>
        TObject LoadAsset<TObject>(string path) where TObject : UnityEngine.Object;
    }
}