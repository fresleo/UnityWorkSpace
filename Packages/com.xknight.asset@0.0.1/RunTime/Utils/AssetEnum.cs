using Sirenix.OdinInspector;
using UnityEngine;

namespace XKAsset
{
	/// <summary>
    /// 资源类型
    /// </summary>
    public enum AssetType
    {
        /// <summary>
        /// 游戏物体，预制等
        /// </summary>
        GameObject = 0,

        /// <summary>
        /// 动画片段
        /// </summary>
        AnimationClip = 1,

        /// <summary>
        /// 音效
        /// </summary>
        Audio = 2,

        /// <summary>
        /// 纹理
        /// </summary>
        Texture2d = 3,

        /// <summary>
        /// 材质
        /// </summary>
        Material = 4,

        /// <summary>
        /// 文本
        /// </summary>
        TextAsset = 5,

        /// <summary>
        /// 动画控制器
        /// </summary>
        AnimationController = 6,

        /// <summary>
        /// 场景
        /// </summary>
        Scene = 7,

        /// <summary>
        /// 立方体纹理
        /// </summary>
        TextureCube = 8,

        /// <summary>
        /// 其他
        /// </summary>
        Other = 9,

        /// <summary>
        /// 图集
        /// </summary>
        UGUIAtlas = 10,

        /// <summary>
        /// Playable
        /// </summary>
        Playable = 11,
        /// <summary>
        /// 字体
        /// </summary>
        Font = 12,
        /// <summary>
        /// 视频
        /// </summary>
        Video = 13,

        Sprite = 14,
        /// <summary>
        /// 错误
        /// </summary>
        Error = 99,                  
    }

    /// <summary>
    /// 资源打包类型
    /// </summary>
    public enum AssetPkgType : byte
    {
        /// <summary>
        /// bundle加载
        /// </summary>
        PT_BUNDLE,
        /// <summary>
        /// 流读取
        /// </summary>
        PT_STREAM,
    }

    public enum ProviderType : byte
    {
        /// <summary>
        /// AssetBundleProvider
        /// </summary>
        BUNDLE,
        /// <summary>
        /// BundleAssetProvider
        /// </summary>
        ASSET,
        /// <summary>
        /// AtlasProvider
        /// </summary>
        ATLAS,
        /// <summary>
        /// BytesProvider
        /// </summary>
        BYTES,
    }
}