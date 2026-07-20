// Created By: WangYu  Date: 2022-11-01

namespace com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces
{
    /// <summary>
    /// 虚拟纹理创建器接口
    /// </summary>
    public interface IVTCreator
    {
        /// <summary>
        /// 附加创建命令
        /// </summary>
        void AppendCmd(VTCreateCmd cmd);
        
        /// <summary>
        /// 销毁纹理
        /// </summary>
        void DisposeTextures(IVT[] textures);
    }
}