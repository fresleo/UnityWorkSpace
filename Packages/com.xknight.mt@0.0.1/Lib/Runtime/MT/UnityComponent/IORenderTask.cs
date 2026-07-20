// Created By: WangYu  Date: 2024-03-19

using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.UnityComponent
{
    public class IORenderTask
    {
        public int lodLv;
        public MTArray<Matrix4x4> worldMatrixs;
        
        public int lightmapIndex;
        public MTArray<Vector4> lightmapScaleOffsets;
        
        public IORenderTask(int maxCount)
        {
            worldMatrixs = new MTArray<Matrix4x4>(maxCount);
            lightmapScaleOffsets = new MTArray<Vector4>(maxCount);
        }
        
        public void Clear()
        {
            worldMatrixs.Clear();
            lightmapScaleOffsets.Clear();
        }
        
        public void Reset()
        {
            worldMatrixs.Reset();
            lightmapScaleOffsets.Reset();
        }
    }
}