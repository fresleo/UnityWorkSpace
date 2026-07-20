using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces;
using com.xknight.mt.Lib.Runtime.MT.Log;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.UnityComponent
{
    /// <summary>
    /// 虚拟纹理创建器
    /// </summary>
    public class VTCreator : AbsVTCreator
    {
        /// <summary>
        /// 纹理质量
        /// </summary>
        public enum ETextureQuality
        {
            Full,
            Half,
            Quater,
        }

        [Header("纹理质量")]
        public ETextureQuality texQuality = ETextureQuality.Full;
        [Header("每帧最大烘焙数量")]
        public int maxBakeCountPerFrame = 8;
        
        //激活的虚拟纹理
        private List<IVT> m_activeVts = new();
        //虚拟纹理池
        private Dictionary<int, Queue<IVT[]>> m_vtPool = new();
        
        //创建命令队列
        private Queue<VTCreateCmd> m_cmdQ = new();
        //烘焙工作队列
        private Queue<VTRenderJob> m_bakedJobs = new();

        
        public override void AppendCmd(VTCreateCmd cmd)
        {
            m_cmdQ.Enqueue(cmd);
        }
        
        public override void DisposeTextures(IVT[] textures)
        {
            m_activeVts.Remove(textures[0]);
            m_activeVts.Remove(textures[1]);
            
            var size = textures[0].Size;
            if (m_vtPool.TryGetValue(size, out var vtsQueue))
            {
                vtsQueue.Enqueue(textures);
            }
            else
            {
                MTLogger.LogWarning($"VTCreator.DisposeTextures -> 池中没有这个尺寸的纹理: {size}");
            }
        }

        
        private void OnDestroy()
        {
            foreach (var item in m_activeVts)
            {
                if (item is RuntimeTextureBaker baker)
                {
                    baker.Clear();
                }
            }
            m_activeVts.Clear();
            
            foreach (var queue in m_vtPool.Values)
            {
                while (queue.Count > 0)
                {
                    var item = queue.Dequeue();
                    if (item is RuntimeTextureBaker[] bakers)
                    {
                        bakers[0].Clear();
                        bakers[1].Clear();
                    }
                }
            }
            m_vtPool.Clear();
            
            m_cmdQ.Clear();
            m_bakedJobs.Clear();
            
            VTCreateCmd.Clear();
            VTRenderJob.Clear();
        }
        
        private void Update()
        {
            //把激活的纹理保存下来，并把烘焙job回池
            while (m_bakedJobs.Count > 0)
            {
                var job = m_bakedJobs.Dequeue();
                job.SendTexturesReady();
                m_activeVts.Add(job.bakers[0]);
                m_activeVts.Add(job.bakers[1]);
                VTRenderJob.Push(job);
            }

            //响应命令队列，烘焙对应的纹理
            int bakeCount = 0;
            while (m_cmdQ.Count > 0 && bakeCount < maxBakeCountPerFrame)
            {
                var cmd = m_cmdQ.Dequeue();
                if (cmd.receiver.WaitCmdId == cmd.cmdId)
                {
                    var bakers = PopBakers(cmd.size);
                    bakers[0].Reset(cmd.uvMin, cmd.uvMax, cmd.bakeDiffuse);
                    bakers[1].Reset(cmd.uvMin, cmd.uvMax, cmd.bakeNormal);
                    
                    var job = VTRenderJob.Pop();
                    job.Reset(cmd.cmdId, bakers, cmd.receiver);
                    job.DoJob();
                    m_bakedJobs.Enqueue(job);
                    
                    VTCreateCmd.Push(cmd);
                    bakeCount++;
                }
                else
                {
                    VTCreateCmd.Push(cmd);
                }
            }

            //检查激活的纹理是否需要重新烘焙
            for (int count = m_activeVts.Count - 1; count >= 0; --count)
            {
                var baker = m_activeVts[count] as RuntimeTextureBaker;
                bool needRender = baker.Validate();
                if (needRender)
                {
                    baker.Bake();
                }
            }
        }
        
        //根据尺寸取出烘焙器
        private RuntimeTextureBaker[] PopBakers(int size)
        {
            //根据质量调整尺寸
            int texSize = size;
            if (texQuality == ETextureQuality.Half)
            {
                texSize = size >> 1;
            }
            else if (texQuality == ETextureQuality.Quater)
            {
                texSize = size >> 2;
            }
            
            //确保有对应尺寸的队列
            if (!m_vtPool.ContainsKey(texSize))
            {
                m_vtPool.Add(texSize, new Queue<IVT[]>());
            }

            //确保 diffuse 和 normal 的 Baker
            RuntimeTextureBaker[] ret;
            var queue = m_vtPool[texSize];
            if (queue.Count > 0)
            {
                ret = queue.Dequeue() as RuntimeTextureBaker[];
            }
            else
            {
                var diffuseBaker = new RuntimeTextureBaker(texSize);
                var normalBaker = new RuntimeTextureBaker(texSize);
                ret = new RuntimeTextureBaker[] { diffuseBaker, normalBaker };
            }
            return ret;
        }
        
    }
}