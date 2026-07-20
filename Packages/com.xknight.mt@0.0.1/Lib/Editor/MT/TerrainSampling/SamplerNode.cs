// Created By: WangYu  Date: 2022-10-03

using System.Collections.Generic;
using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainSampling
{
    /// <summary>
    /// 采样器的节点
    /// </summary>
    public class SamplerNode : AbsSamplerBase
    {
        //子节点个数
        private const int c_childrenCount = 4;
        
        /// <summary>
        /// 子节点
        /// </summary>
        public AbsSamplerBase[] children = new AbsSamplerBase[c_childrenCount];

        /// <summary>
        /// 构建一颗完成的树
        /// </summary>
        public SamplerNode(int sub, Vector3 center, Vector2 size, Vector2 uv, Vector2 uvStep)
        {
            m_svd = new SampleVertexData();
            m_svd.position = center;
            m_svd.uv = uv;
            
            Vector2 subSize = 0.5f * size;
            Vector2 subUvStep = 0.5f * uvStep;

            Vector3 subCenter0 = new Vector3(MTRuntimeUtils.MinusHalf(center.x, subSize.x), center.y, MTRuntimeUtils.MinusHalf(center.z, subSize.y));
            Vector2 subUv0 = new Vector2(MTRuntimeUtils.MinusHalf(uv.x, subUvStep.x), MTRuntimeUtils.MinusHalf(uv.y, subUvStep.y));
            
            Vector3 subCenter1 = new Vector3(MTRuntimeUtils.AddHalf(center.x, subSize.x), center.y, MTRuntimeUtils.MinusHalf(center.z, subSize.y));
            Vector2 subUv1 = new Vector2(MTRuntimeUtils.AddHalf(uv.x, subUvStep.x), MTRuntimeUtils.MinusHalf(uv.y, subUvStep.y));
            
            Vector3 subCenter2 = new Vector3(MTRuntimeUtils.MinusHalf(center.x, subSize.x), center.y, MTRuntimeUtils.AddHalf(center.z, subSize.y));
            Vector2 subUv2 = new Vector2(MTRuntimeUtils.MinusHalf(uv.x, subUvStep.x), MTRuntimeUtils.AddHalf(uv.y, subUvStep.y));
            
            Vector3 subCenter3 = new Vector3(MTRuntimeUtils.AddHalf(center.x, subSize.x), center.y, MTRuntimeUtils.AddHalf(center.z, subSize.y));
            Vector2 subUv3 = new Vector2(MTRuntimeUtils.AddHalf(uv.x, subUvStep.x), MTRuntimeUtils.AddHalf(uv.y, subUvStep.y));
            
            //树干
            if (sub > 1)
            {
                int nextSub = sub - 1;
                
                children[0] = new SamplerNode(nextSub, subCenter0, subSize, subUv0, subUvStep);
                children[1] = new SamplerNode(nextSub, subCenter1, subSize, subUv1, subUvStep);
                children[2] = new SamplerNode(nextSub, subCenter2, subSize, subUv2, subUvStep);
                children[3] = new SamplerNode(nextSub, subCenter3, subSize, subUv3, subUvStep);
            }
            //树叶
            else
            {
                children[0] = new SamplerLeaf(subCenter0, subUv0);
                children[1] = new SamplerLeaf(subCenter1, subUv1);
                children[2] = new SamplerLeaf(subCenter2, subUv2);
                children[3] = new SamplerLeaf(subCenter3, subUv3);
            }
        }
        
        public override void GetData(List<SampleVertexData> svdLs, Dictionary<byte, List<SampleVertexData>> boundaries)
        {
            for (int i = 0; i < c_childrenCount; i++)
            {
                children[i].GetData(svdLs, boundaries);
            }
            
            foreach (var key in boundaryDict.Keys)
            {
                if (!boundaries.ContainsKey(key))
                {
                    boundaries.Add(key, new List<SampleVertexData>());
                }
                boundaries[key].Add(boundaryDict[key]);
            }
        }
        
        public override void AddBoundary(int subdivision, int segmentsX, int segmentsZ, byte boundaryKey, SampleVertexData svd)
        {
            int u = segmentsX >> subdivision; //= x / power(2, subdivision);
            int v = segmentsZ >> subdivision;
            int subx = segmentsX - u * (1 << subdivision);
            int subz = segmentsZ - v * (1 << subdivision);
            subdivision--;
            
            int idx = (subz >> subdivision) * 2 + (subx >> subdivision);
            
            children[idx].AddBoundary(subdivision, subx, subz, boundaryKey, svd);
        }
        
        public override void RunSample(ITerrainScanner scaner)
        {
            base.RunSample(scaner);
            for (int i = 0; i < c_childrenCount; i++)
            {
                children[i].RunSample(scaner);
            }
        }
        

        /// <summary>
        /// 是否全是叶子节点
        /// </summary>
        public bool IsFullLeaf
        {
            get
            {
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] == null || !(children[i] is SamplerLeaf))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        
        /// <summary>
        /// 合并节点
        /// </summary>
        public void CombineNode(float angleErr)
        {
            for (int i = 0; i < c_childrenCount; i++)
            {
                if (children[i] is SamplerNode subNode)
                {
                    subNode.CombineNode(angleErr);
                    if (subNode.IsFullLeaf)
                    {
                        SamplerLeaf replacedLeaf = subNode.CombineLeaf(angleErr);
                        if (replacedLeaf != null)
                        {
                            children[i] = replacedLeaf;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 合并树叶
        /// </summary>
        public SamplerLeaf CombineLeaf(float angleErr)
        {
            //不是叶子的会被丢掉
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null || !(children[i] is SamplerLeaf))
                {
                    return null;
                }
            }

            //误差过大的，会被丢掉
            for (int i = 0; i < children.Length; i++)
            {
                SamplerLeaf item = (SamplerLeaf)children[i];
                
                float dot = Vector3.Dot(item.Normal.normalized, m_svd.normal.normalized);
                float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
                if (angle >= angleErr)
                {
                    return null;
                }
            }

            //更新边
            SamplerLeaf nodeLeaf = new SamplerLeaf(m_svd);
            
            for (int i = 0; i < children.Length; i++)
            {
                SamplerLeaf childLeaf = (SamplerLeaf)children[i];
                
                foreach (var key in childLeaf.boundaryDict.Keys)
                {
                    if (boundaryDict.ContainsKey(key))
                    {
                        boundaryDict[key].Merge(childLeaf.boundaryDict[key]);
                    }
                    else
                    {
                        boundaryDict.Add(key, childLeaf.boundaryDict[key]);
                    }
                }
            }

            nodeLeaf.boundaryDict = boundaryDict;
            return nodeLeaf;
        }
        
    }
}