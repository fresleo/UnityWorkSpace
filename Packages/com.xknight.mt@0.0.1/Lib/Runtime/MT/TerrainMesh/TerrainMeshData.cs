// Created By: WangYu  Date: 2022-10-15

using com.xknight.mt.Lib.Runtime.MT.Utils;
using UnityEngine;

namespace com.xknight.mt.Lib.Runtime.MT.TerrainMesh
{
    /// <summary>
    /// 地形网格数据
    /// </summary>
    public class TerrainMeshData
    {
        public Mesh mesh;
        public Vector2 uvMin;
        public Vector2 uvMax;

        public void Clear()
        {
            if (mesh != null)
            {
                MTRuntimeUtils.DestroyObject(mesh);
                mesh = null;
            }
        }
        
    }
}