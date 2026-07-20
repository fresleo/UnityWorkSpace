// Created By: WangYu  Date: 2023-11-20

using System;
using System.Collections.Generic;
using System.IO;
using com.xknight.mt.Lib.Runtime.MT.Common;
using com.xknight.mt.Lib.Runtime.MT.Serialize;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.StaticObject
{
    /// <summary>
    /// 静态对象的4叉树节点
    /// </summary>
    [Serializable]
    public class SOQuadTreeNode : AbsQuadTreeNode
    {
        //数据 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
        /// <summary>
        /// 持有的gid
        /// </summary>
        public List<int> holdGids = new();
        
        /// <summary>
        /// 持有的资源索引id
        /// </summary>
        public List<int> holdAssetIdxs = new();
        
        /// <summary>
        /// 持有的世界矩阵
        /// </summary>
        public List<Matrix4x4> holdWorldMatrixs = new();

        /// <summary>
        /// 持有的 Lightmap 配置数据
        /// </summary>
        public Dictionary<int, LightmapConfig[]> holdLightmapDatas = new();
        
        
        public override void Clear()
        {
            base.Clear();
            holdGids.Clear();
            holdAssetIdxs.Clear();
            holdWorldMatrixs.Clear();
            holdLightmapDatas.Clear();
        }
        
        public override void Serialize(Stream stream)
        {
            MTStreamUtils.WriteVector3(stream, bnd.center);
            MTStreamUtils.WriteVector3(stream, bnd.size);

            int len;

            len = holdGids.Count;
            MTStreamUtils.WriteInt(stream, len);
            foreach (var item in holdGids)
            {
                MTStreamUtils.WriteInt(stream, item);
            }

            len = holdAssetIdxs.Count;
            MTStreamUtils.WriteInt(stream, len);
            foreach (var item in holdAssetIdxs)
            {
                MTStreamUtils.WriteInt(stream, item);
            }

            len = holdWorldMatrixs.Count;
            MTStreamUtils.WriteInt(stream, len);
            foreach (var item in holdWorldMatrixs)
            {
                MTStreamUtils.WriteVector4(stream, item.GetColumn(0));
                MTStreamUtils.WriteVector4(stream, item.GetColumn(1));
                MTStreamUtils.WriteVector4(stream, item.GetColumn(2));
                MTStreamUtils.WriteVector4(stream, item.GetColumn(3));
            }

            len = holdLightmapDatas.Count;
            MTStreamUtils.WriteInt(stream, len);
            foreach (var iter in holdLightmapDatas)
            {
                MTStreamUtils.WriteInt(stream, iter.Key);
                
                var lds = iter.Value;
                MTStreamUtils.WriteInt(stream, lds.Length);
                foreach (var ld in lds)
                {
                    ld.Serialize(stream);
                }
            }
            
            len = children != null ? children.Length : 0;
            MTStreamUtils.WriteInt(stream, len);
            for (int i = 0; i < len; i++)
            {
                int cid = children[i];
                MTStreamUtils.WriteInt(stream, cid);
            }
        }

        public override void Deserialize(Stream stream, Vector3 centerOffset)
        {
            Vector3 center = MTStreamUtils.ReadVector3(stream);
            Vector3 size = MTStreamUtils.ReadVector3(stream);
            bnd = new Bounds(center + centerOffset, size);

            int len;
            
            len = MTStreamUtils.ReadInt(stream);
            holdGids.Clear();
            for (int i = 0; i < len; i++)
            {
                int gid = MTStreamUtils.ReadInt(stream);
                holdGids.Add(gid);
            }
            
            len = MTStreamUtils.ReadInt(stream);
            holdAssetIdxs.Clear();
            for (int i = 0; i < len; i++)
            {
                int assetIdx = MTStreamUtils.ReadInt(stream);
                holdAssetIdxs.Add(assetIdx);
            }
            
            len = MTStreamUtils.ReadInt(stream);
            holdWorldMatrixs.Clear();
            for (int i = 0; i < len; i++)
            {
                var column0 = MTStreamUtils.ReadVector4(stream);
                var column1 = MTStreamUtils.ReadVector4(stream);
                var column2 = MTStreamUtils.ReadVector4(stream);
                var column3 = MTStreamUtils.ReadVector4(stream);
                
                Matrix4x4 matr = new Matrix4x4(column0, column1, column2, column3);
                holdWorldMatrixs.Add(matr);
            }
            
            len = MTStreamUtils.ReadInt(stream);
            holdLightmapDatas.Clear();
            for (int i = 0; i < len; i++)
            {
                int gid = MTStreamUtils.ReadInt(stream);
                
                int ldsLen = MTStreamUtils.ReadInt(stream);
                var lds = new LightmapConfig[ldsLen];
                for (int j = 0; j < ldsLen; j++)
                {
                    lds[j] = new LightmapConfig();
                    lds[j].Deserialize(stream);
                }
                
                holdLightmapDatas.Add(gid, lds);
            }
            
            len = MTStreamUtils.ReadInt(stream);
            children = new int[len];
            for (int i = 0; i < len; i++)
            {
                children[i] = MTStreamUtils.ReadInt(stream);
            }
        }
    }
}