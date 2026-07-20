// Created By: WangYu  Date: 2024-06-01

using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public partial class DynamicOcclusionCuller
    {
        private static readonly int _InstanceOffset = Shader.PropertyToID("_InstanceOffset");
        private static readonly int _DebugBoxes = Shader.PropertyToID("_DebugBoxes");

        /// <summary>
        /// 可见性测试
        /// </summary>
        private void VisibilityTesting()
        {
            for (int i = 0; i < m_testEntryList.Count; i++)
            {
                if (i >= m_testWorldMatrices.Length)
                {
                    break;
                }
                
                var testEntry = m_testEntryList[i];
                m_testWorldMatrices[i] = testEntry.worldMatrix;
            }

            // 把对象的矩阵拆分到多个队列中
            SplitUtils.SplitArray(maxTestPerGroup, m_testWorldMatrices, m_drawMatricesGroups);
            
            // 提前绘制用来检查遮挡的对象
            int instanceOffset = 0;
            for (int i = 0; i < m_drawMatricesGroups.Count; i++)
            {
                MaterialPropertyBlock block = m_mpbPool.Get();
                block.SetInt(_InstanceOffset, instanceOffset);
                block.SetInt(_DebugBoxes, debugBoxes ? 1 : 0);

                Graphics.DrawMeshInstanced(m_testMesh, 0, m_testMaterial, m_drawMatricesGroups[i], m_drawMatricesGroups[i].Length, block, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
                m_mpbPool.Release(block);

                instanceOffset += m_drawMatricesGroups[i].Length;
            }
        }
    }
}