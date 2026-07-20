// Created By: WangYu  Date: 2024-06-01

using com.xknight.mt.Lib.Runtime.MT.UnityComponent;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    public partial class DynamicOcclusionCuller
    {
        public void AddTestData(int rid, Bounds bounds, Matrix4x4 worldMatrix)
        {
            Vector3 boundingBoxScale = bounds.size * this.boxScale;
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(boundingBoxScale);
            Matrix4x4 scaledMatrix = worldMatrix * scaleMatrix;
            
            var testEntry = new TestEntry
            {
                rid = rid,
                bounds = bounds,
                worldMatrix = scaledMatrix,
            };

            // 不能重复添加
            foreach (var item in m_testEntryList)
            {
                if (item.rid == rid)
                {
                    return;
                }
            }
            
            m_testEntryList.Add(testEntry);
        }
        
        public void RemoveTestData(int rid)
        {
            int key = (int)SDTRendererId.EEventType.Show + rid;
            EventManager.GetInstance.TriggerEvent(key);

            int index = m_testEntryList.FindIndex(item => item.rid == rid);
            if (index >= 0)
            {
                m_testEntryList.RemoveAt(index);
            }
        }
    }
}