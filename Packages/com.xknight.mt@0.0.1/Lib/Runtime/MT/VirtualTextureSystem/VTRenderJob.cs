using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces;

namespace com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem
{
    /// <summary>
    /// 虚拟纹理渲染job
    /// </summary>
    public class VTRenderJob
    {
        #region 对象池
        static Queue<VTRenderJob> s_poolQ = new();

        public static void Clear()
        {
            s_poolQ.Clear();
        }
        
        public static VTRenderJob Pop()
        {
            if (s_poolQ.Count > 0)
            {
                return s_poolQ.Dequeue();
            }

            return new VTRenderJob();
        }

        public static void Push(VTRenderJob item)
        {
            item.bakers = null;
            item.m_receiver = null;
            
            s_poolQ.Enqueue(item);
        }
        #endregion 对象池

        
        /// <summary>
        /// 烘焙器
        /// </summary>
        public RuntimeTextureBaker[] bakers;
        
        private IVTReceiver m_receiver;
        private long m_cmdId = 0;

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset(long cmdId, RuntimeTextureBaker[] bakers, IVTReceiver receiver)
        {
            m_cmdId = cmdId;
            this.bakers = bakers;
            m_receiver = receiver;
        }

        /// <summary>
        /// 干活
        /// </summary>
        public void DoJob()
        {
            for (int i = 0; i < bakers.Length; i++)
            {
                var tex = bakers[i];
                tex.Bake();
            }
        }

        /// <summary>
        /// 通知接收器，纹理准备就绪
        /// </summary>
        public void SendTexturesReady()
        {
            m_receiver.OnTextureReady(m_cmdId, bakers);
            m_receiver = null;
        }
    }
}