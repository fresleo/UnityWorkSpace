// Created By: WangYu  Date: 2024-05-30

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    /// <summary>
    /// 对象的入口
    /// </summary>
    public class ObjectEntry
    {
        public GameObject objectGo;
        public MeshRenderer objectMr;
        public Matrix4x4 objectMatrix;
        
        public Matrix4x4 boundingBoxMatrix;
        
        public int listIndex;
        public int elementIndex;
    }
}