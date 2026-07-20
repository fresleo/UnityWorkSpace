using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem.Interfaces;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.VirtualTextureSystem
{
    /// <summary>
    /// 虚拟纹理创建命令
    /// </summary>
    public class VTCreateCmd
    {
        #region 对象池
        static readonly Queue<VTCreateCmd> s_poolQ = new();

        public static void Clear()
        {
            s_poolQ.Clear();
        }
        
        public static VTCreateCmd Pop()
        {
            if (s_poolQ.Count > 0)
            {
                return s_poolQ.Dequeue();
            }

            return new VTCreateCmd();
        }

        public static void Push(VTCreateCmd item)
        {
            item.bakeDiffuse = null;
            item.bakeNormal = null;
            item.receiver = null;
            
            s_poolQ.Enqueue(item);
        }
        #endregion 对象池
        
        
        private static long s_cmd_id_seed;

        /// <summary>
        /// 生成id
        /// </summary>
        public static long GenerateID()
        {
            s_cmd_id_seed++;
            return s_cmd_id_seed;
        }
        
        /// <summary>
        /// 命令id
        /// </summary>
        public long cmdId = 0;
        /// <summary>
        /// 尺寸
        /// </summary>
        public int size = 64;
        /// <summary>
        /// 烘焙漫反射的材质
        /// </summary>
        public Material[] bakeDiffuse;
        /// <summary>
        /// 烘焙法线的材质
        /// </summary>
        public Material[] bakeNormal;
        /// <summary>
        /// uv最小值
        /// </summary>
        public Vector2 uvMin;
        /// <summary>
        /// uv最大值
        /// </summary>
        public Vector2 uvMax;
        /// <summary>
        /// vt接收器
        /// </summary>
        public IVTReceiver receiver;
        
        protected VTCreateCmd() {}
        
    }
}