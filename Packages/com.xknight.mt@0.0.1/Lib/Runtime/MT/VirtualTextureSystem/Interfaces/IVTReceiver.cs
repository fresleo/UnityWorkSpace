// Created By: WangYu  Date: 2022-11-01

namespace com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces
{
    /// <summary>
    /// 虚拟纹理接收器
    /// </summary>
    public interface IVTReceiver
    {
        /// <summary>
        /// 等待命令的id
        /// </summary>
        long WaitCmdId { get; }
        
        /// <summary>
        /// 当纹理准备好时调用
        /// </summary>
        void OnTextureReady(long cmdId, IVT[] textures);
    }
}