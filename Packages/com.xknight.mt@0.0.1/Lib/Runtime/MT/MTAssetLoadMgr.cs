// Created By: WangYu  Date: 2023-12-11

using com.xknight.mt.Lib.Runtime.MT.Common;

namespace com.xknight.mt.Lib.Runtime.MT
{
    /// <summary>
    /// 2进制数据加载器管理器
    /// </summary>
    public class MTAssetLoadMgr
    {
        #region 单例
        
        private static class Holder
        {
            public static MTAssetLoadMgr instance = new MTAssetLoadMgr();
        }

        public static MTAssetLoadMgr Instance => Holder.instance;
        
        #endregion 单例
        
        
        private MTAssetLoadMgr()
        {
        }

        /// <summary>
        /// 清理申请的各种资源
        /// </summary>
        public void Clear()
        {
            meshDataLoader?.Clear();
            objectLoader?.Clear();
        }
        
        
        /// <summary>
        /// 网格2进制数据加载器
        /// </summary>
        public IBytesLoader meshDataLoader;
        
        /// <summary>
        /// Unity Object 加载器
        /// </summary>
        public IObjectLoader objectLoader;


        /// <summary>
        /// 输出的根目录
        /// </summary>
        //public static string outRootDir = "Assets/OriginalRes/Scenes";
        public static string outRootDir = "Assets/OutputRes";

        /// <summary>
        /// 输出的场景目录
        /// </summary>
        public static string outSceneDir = outRootDir + "/scenedatas/{0}";
        
        /// <summary>
        /// 网格数据2进制化
        /// </summary>
        public static string terrainOutDir_Mesh = outSceneDir + "/mesh";

        /// <summary>
        /// 配置数据文件
        /// </summary>
        public static string terrainOutDir_Data = outSceneDir + "/data";

        /// <summary>
        /// 2进制数据文件
        /// </summary>
        public static string terrainOutDir_Binary = outSceneDir + "/dependenices";

        /// <summary>
        /// 依赖的材质球
        /// </summary>
        public static string terrainOutDir_Material = terrainOutDir_Binary + "/materials";

        /// <summary>
        /// 从 terrain 中导出的 control 图目录
        /// 这个目录和别的资源不一样，是放在 OriginalRes 里，而不是 OutputRes 里
        /// </summary>
        public static string terrainOutDir_Control = "Assets/OriginalRes/Scenes/controls/{0}";
        
    }
}