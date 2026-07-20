using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.UnityComponent
{
    /// <summary>
    /// 调试纹理
    /// </summary>
    public class DebugTexture : IVT
    {
        int m_size = 32;
        Texture2D m_tex;
        
        int IVT.Size => m_size;
        Texture IVT.Tex => m_tex;
        
        public DebugTexture(int size, Texture2D tex)
        {
            m_size = size;
            m_tex = tex;
        }
    }

    /// <summary>
    /// 调试创建器
    /// </summary>
    public class VTDebugCreator : AbsVTCreator
    {
        [Header("调试纹理")]
        public Texture2D tex64;
        public Texture2D tex128;
        public Texture2D tex256;
        public Texture2D tex512;
        public Texture2D tex1024;
        public Texture2D tex2048;
        
        //创建命令队列
        private Queue<VTCreateCmd> m_cmdQ = new Queue<VTCreateCmd>();
        
        public override void AppendCmd(VTCreateCmd cmd)
        {
            m_cmdQ.Enqueue(cmd);
        }
        public override void DisposeTextures(IVT[] textures) { }
        
        void Update()
        {
            //根据cmd里的size，返回对应的调试纹理
            while (m_cmdQ.Count > 0)
            {
                var cmd = m_cmdQ.Dequeue();
                switch (cmd.size)
                {
                    case 64:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new DebugTexture[] { new DebugTexture(64, tex64) });
                        break;
                    case 128:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new DebugTexture[] { new DebugTexture(128, tex128) });
                        break;
                    case 256:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new DebugTexture[] { new DebugTexture(256, tex256)});
                        break;
                    case 512:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new DebugTexture[] { new DebugTexture(512, tex512) });
                        break;
                    case 1024:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new DebugTexture[] { new DebugTexture(1024, tex1024) });
                        break;
                    case 2048:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new DebugTexture[] { new DebugTexture(2048, tex2048) });
                        break;

                    default:
                    {
                        if (cmd.size > 2048)
                        {
                            cmd.receiver.OnTextureReady(cmd.cmdId, new DebugTexture[] { new DebugTexture(2048, tex2048) });
                        }
                        else if (cmd.size < 64)
                        {
                            cmd.receiver.OnTextureReady(cmd.cmdId, new DebugTexture[] { new DebugTexture(64, tex64) });
                        }
                    }
                        break;
                }
                VTCreateCmd.Push(cmd);
            }
        }
        
    }
}
