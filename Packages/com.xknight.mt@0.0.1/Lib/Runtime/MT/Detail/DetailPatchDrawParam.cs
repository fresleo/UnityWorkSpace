// Created By: WangYu  Date: 2022-10-10

using System.Collections.Generic;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.Detail
{
    /// <summary>
    /// 细节补丁绘制参数
    /// </summary>
    public class DetailPatchDrawParam
    {
        #region 对象池
        private static Queue<DetailPatchDrawParam> s_poolQ = new Queue<DetailPatchDrawParam>();

        public static void Clear()
        {
            s_poolQ.Clear();
        }
        
        public static DetailPatchDrawParam Pop()
        {
            if (s_poolQ.Count > 0)
            {
                return s_poolQ.Dequeue();
            }

            return new DetailPatchDrawParam();
        }

        public static void Push(DetailPatchDrawParam para)
        {
            para.used = 0;
            
            s_poolQ.Enqueue(para);
        }
        #endregion 对象池
        
        
        public Matrix4x4[] matrixs;
        public Vector4[] colors;
        
        public MaterialPropertyBlock matBlock;
        /// <summary>
        /// 可用标记
        /// </summary>
        public int used = 0;

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset(int size)
        {
            if (matrixs == null || size > matrixs.Length)
            {
                size = Mathf.Min(size, 1023);
                matrixs = new Matrix4x4[size];
                colors = new Vector4[size];
            }

            if (matBlock == null)
            {
                matBlock = new MaterialPropertyBlock();
            }
            matBlock.Clear();
            
            used = 0;
        }
        
    }
}