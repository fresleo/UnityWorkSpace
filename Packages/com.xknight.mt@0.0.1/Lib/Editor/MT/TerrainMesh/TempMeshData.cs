// Created By: WangYu  Date: 2022-10-05

using UnityEngine;

namespace com.xknight.mt.Lib.Editor.MT.TerrainMesh
{
    /// <summary>
    /// 临时网格数据
    /// </summary>
    internal class TempMeshData
    {
        public int Lod { get; private set; }
        public int MeshId { get; private set; }
        public Mesh Mtm { get; private set; }
        public Vector4 ScaleOffset { get; private set; }
        
        public TempMeshData(int lod, int mid, Mesh mtm, Vector2 uvMin, Vector2 uvMax)
        {
            Lod = lod;
            MeshId = mid;
            Mtm = mtm;
            
            var v4 = new Vector4(1, 1, 0, 0);
            v4.x = uvMax.x - uvMin.x;
            v4.y = uvMax.y - uvMin.y;
            v4.z = uvMin.x;
            v4.w = uvMin.y;
            ScaleOffset = v4;
        }

        public Vector2 ScaleOffsetXY => new Vector2(ScaleOffset.x, ScaleOffset.y);
        
    }
}