// Created By: WangYu  Date: 2024-06-01

using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.OcclusionCulling
{
    /// <summary>
    /// 测试条目
    /// </summary>
    public struct TestEntry
    {
        public int rid;
        public Bounds bounds;
        public Matrix4x4 worldMatrix;
        
        
        public override bool Equals(object obj)
        {
            if (obj is TestEntry testEntry)
            {
                return this == testEntry;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = rid.GetHashCode() + bounds.GetHashCode() + worldMatrix.GetHashCode();
            
            return hashCode;
        }
        
        public static bool operator ==(TestEntry left, TestEntry right)
        {
            return left.rid == right.rid 
                   && left.bounds == right.bounds 
                   && left.worldMatrix == right.worldMatrix;
        }
        
        public static bool operator !=(TestEntry left, TestEntry right)
        {
            return left.rid != right.rid 
                   || left.bounds != right.bounds 
                   || left.worldMatrix != right.worldMatrix;
        }
    }
}